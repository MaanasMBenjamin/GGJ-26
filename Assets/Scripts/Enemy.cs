using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Enemy : MonoBehaviour
{
	[Header("Chase Settings")]
	[SerializeField] private float moveSpeed = 3f;
	[SerializeField] private float agroRadius = 5f;     // start chasing when within this distance
	[SerializeField] private float loseRadius = 7f;     // stop chasing when beyond this distance
	[SerializeField] private float stoppingDistance = 0.15f; // keep a small gap from player

	[Header("Home Return")]
	[SerializeField] private float returnSpeed = 3f;    // speed to return home
	[SerializeField] private float homeStopDistance = 0.05f; // how close to home to stop

	[Header("Guarding / Patrol")]
	[SerializeField] private bool guardingEnabled = false; // when not chasing, patrol along X
	[SerializeField] private float patrolDistanceX = 3f;    // distance along X from home
	[SerializeField] private float patrolPauseSeconds = 8f; // wait at each patrol point
	[SerializeField] private float patrolSpeed = 3f;        // speed while patrolling
	[SerializeField] private float postChasePauseSeconds = 4.5f; // pause after chase ends

	[Header("Per-Instance Randomization")]
	[Tooltip("If enabled, each enemy randomizes its patrol distance/speed/pause to avoid synchronized movement.")]
	[SerializeField] private bool randomizePatrol = true;
	[Tooltip("Range for patrol distance along X (min, max).")]
	[SerializeField] private Vector2 patrolDistanceXRange = new Vector2(2f, 6f);
	[Tooltip("Range for pause time at patrol points (min, max) seconds.")]
	[SerializeField] private Vector2 patrolPauseSecondsRange = new Vector2(2f, 8f);
	[Tooltip("Range for patrol movement speed (min, max).")]
	[SerializeField] private Vector2 patrolSpeedRange = new Vector2(2f, 5f);
	[Tooltip("Randomize whether the enemy starts targeting A or B at the beginning.")]
	[SerializeField] private bool randomizeStartSide = true;

	[Header("Enemy Light (URP 2D)")]
	[SerializeField] private Light2D enemyLight; // optional light to sync with agro
	[SerializeField] private bool autoFindChildLight = true;
	[SerializeField] private bool autoCreateLightIfMissing = true;
	[SerializeField] private bool syncLightWithAgro = true;
	[SerializeField] private float lightInnerRadiusRatio = 0.4f; // inner radius relative to outer
	[SerializeField] private float lightIntensity = 1f; // 0..1
	[SerializeField] private bool respectGlobalLightingState = true; // disable during scene flicker
	[SerializeField] private bool debugLogs = false; // enable for chase/invisibility logs

	[Header("Collision Handling")]
	[Tooltip("When enabled, enemy colliders become triggers during chase so they pass through obstacles and the player.")]
	[SerializeField] private bool passThroughCollisionsWhenChasing = true;
	private Collider2D[] selfColliders;
	private bool[] defaultIsTrigger;

	[Header("Light Blink")]
	[SerializeField] private float defaultBlinkFrequency = 6f;
	[SerializeField, Range(0f,1f)] private float defaultBlinkMinFactor = 0f;
	[SerializeField, Range(0f,1f)] private float defaultBlinkMaxFactor = 1f;

	private bool blinkActive;
	private float blinkEndTime;
	private float blinkFrequency;
	private float blinkMinFactor;
	private float blinkMaxFactor;
	private bool overrideGate; // allows local light even when global gate is off

	[Header("Target Settings")]
	[SerializeField] private Transform player;          // assign your player here (optional)
	[SerializeField] private bool findByTag = true;     // if true, auto-find player by tag
	[SerializeField] private string playerTag = "Player";

	private bool isChasing;
	private Vector3 homePosition;                       // per-instance home (spawn) position
	private Vector3 patrolPointA;                       // home
	private Vector3 patrolPointB;                       // home + X distance
	private int patrolIndex;                            // 0 -> A, 1 -> B
	private float patrolWaitTimer;                      // time waited at current point
	private bool postChaseWaiting;                      // true while pausing after chase
	private float postChaseWaitTimer;                   // timer for post-chase pause

	private void Awake()
	{
		// Cache colliders and original trigger states for runtime toggling
		selfColliders = GetComponentsInChildren<Collider2D>(includeInactive: false);
		if (selfColliders != null && selfColliders.Length > 0)
		{
			defaultIsTrigger = new bool[selfColliders.Length];
			for (int i = 0; i < selfColliders.Length; i++)
			{
				defaultIsTrigger[i] = selfColliders[i].isTrigger;
			}
		}

		if (enemyLight == null && autoFindChildLight)
		{
			enemyLight = GetComponentInChildren<Light2D>();
		}

		if (enemyLight == null && autoCreateLightIfMissing)
		{
			var go = new GameObject("Enemy Light 2D");
			go.transform.SetParent(transform);
			go.transform.localPosition = Vector3.zero;
			enemyLight = go.AddComponent<Light2D>();
			enemyLight.lightType = Light2D.LightType.Point;
			enemyLight.intensity = Mathf.Clamp01(lightIntensity);
			enemyLight.pointLightOuterRadius = Mathf.Max(0.01f, agroRadius);
			enemyLight.pointLightInnerRadius = Mathf.Clamp01(lightInnerRadiusRatio) * enemyLight.pointLightOuterRadius;
		}

		if (player == null && findByTag)
		{
			GameObject p = GameObject.FindWithTag(playerTag);
			if (p != null) player = p.transform;
		}

		// Prevent first-frame flash when global lighting disables local lights
		if (respectGlobalLightingState && enemyLight != null && !LightingState.LocalLightsEnabled)
		{
			enemyLight.intensity = 0f;
		}
	}

	private void Start()
	{
		// Capture this instance's starting position as its home.
		// Safe for many prefab instances placed randomly.
		homePosition = transform.position;

		// Per-instance randomization to make each police patrol unique
		if (randomizePatrol)
		{
			float dMin = Mathf.Min(patrolDistanceXRange.x, patrolDistanceXRange.y);
			float dMax = Mathf.Max(patrolDistanceXRange.x, patrolDistanceXRange.y);
			patrolDistanceX = Random.Range(dMin, dMax);

			float pMin = Mathf.Min(patrolPauseSecondsRange.x, patrolPauseSecondsRange.y);
			float pMax = Mathf.Max(patrolPauseSecondsRange.x, patrolPauseSecondsRange.y);
			patrolPauseSeconds = Random.Range(pMin, pMax);

			float sMin = Mathf.Min(patrolSpeedRange.x, patrolSpeedRange.y);
			float sMax = Mathf.Max(patrolSpeedRange.x, patrolSpeedRange.y);
			patrolSpeed = Random.Range(sMin, sMax);

			if (randomizeStartSide)
			{
				patrolIndex = Random.Range(0, 2); // 0 or 1
			}
		}

		// Initialize patrol points based on home
		patrolPointA = homePosition;
		patrolPointB = homePosition + Vector3.right * patrolDistanceX;
		if (!randomizePatrol || !randomizeStartSide)
		{
			patrolIndex = 1; // default: start by moving away from home to B
		}
		// Add a small random initial wait to de-sync pauses
		patrolWaitTimer = randomizePatrol ? Random.Range(0f, Mathf.Min(2f, patrolPauseSeconds * 0.5f)) : 0f;
	}

	private void Update()
	{
		if (player == null) return;

		// If player is invisible (via mask), do not chase
		if (PlayerMask.IsInvisible)
		{
			// When invisible, treat as idle state
			if (debugLogs) Debug.Log("[Enemy] Player is invisible: stopping chase");
			isChasing = false;
			// Optionally perform patrol/return logic
			if (guardingEnabled)
			{
				// Simple patrol wait
				patrolWaitTimer += Time.deltaTime;
			}
			else
			{
				// Return to home
				Vector2 toHome = (Vector2)(homePosition - transform.position);
				float sqrHomeDist = toHome.sqrMagnitude;
				float sqrHomeStop = homeStopDistance * homeStopDistance;
				if (sqrHomeDist > sqrHomeStop)
				{
					Vector3 homeStep = (Vector3)(toHome.normalized * returnSpeed * Time.deltaTime);
					transform.position += homeStep;
				}
			}
			ApplyLightSync();
			return;
		}

		ApplyLightSync();

		// Distance to player (use squared distances to avoid sqrt cost)
		Vector2 toPlayer = player.position - transform.position;
		float sqrDist = toPlayer.sqrMagnitude;

		float sqrAgro = agroRadius * agroRadius;
		float sqrLose = loseRadius * loseRadius;
		float sqrStop = stoppingDistance * stoppingDistance;

		bool wasChasing = isChasing;
		// Hysteresis: start chase inside agro; stop chase outside lose
		if (!isChasing && sqrDist <= sqrAgro)
		{
			isChasing = true;
		}
		else if (isChasing && sqrDist > sqrLose)
		{
			isChasing = false;
		}

		// Toggle collision pass-through mode on chase state changes
		if (wasChasing != isChasing)
		{
			ApplyChaseCollisionMode(isChasing);
		}

		// If we just stopped chasing, start a short pause before resuming patrol
		if (wasChasing && !isChasing && guardingEnabled)
		{
			postChaseWaiting = true;
			postChaseWaitTimer = 0f;
		}

		// Move towards player while chasing and not too close
		if (isChasing && sqrDist > sqrStop)
		{
			Vector3 step = (Vector3)(toPlayer.normalized * moveSpeed * Time.deltaTime);
			transform.position += step;
		}
		// When not chasing
		else if (!isChasing)
		{
			if (guardingEnabled)
			{
				// Pause briefly after chase ends, then resume patrol to last target
				if (postChaseWaiting)
				{
					postChaseWaitTimer += Time.deltaTime;
					if (postChaseWaitTimer >= postChasePauseSeconds)
					{
						postChaseWaiting = false;
					}
				}
				else
				{
					// Patrol between A and B along X, pausing at each point
					Vector3 target = (patrolIndex == 0) ? patrolPointA : patrolPointB;
					Vector2 toTarget = (Vector2)(target - transform.position);
					float sqrTargetDist = toTarget.sqrMagnitude;
					float sqrPatrolStop = homeStopDistance * homeStopDistance;

					if (sqrTargetDist <= sqrPatrolStop)
					{
						patrolWaitTimer += Time.deltaTime;
						if (patrolWaitTimer >= patrolPauseSeconds)
						{
							patrolIndex = 1 - patrolIndex; // toggle between 0 and 1
							patrolWaitTimer = 0f;
						}
					}
					else
					{
						Vector3 step = (Vector3)(toTarget.normalized * patrolSpeed * Time.deltaTime);
						transform.position += step;
					}
				}
			}
			else
			{
				// Return to home position when idle
				Vector2 toHome = (Vector2)(homePosition - transform.position);
				float sqrHomeDist = toHome.sqrMagnitude;
				float sqrHomeStop = homeStopDistance * homeStopDistance;

				if (sqrHomeDist > sqrHomeStop)
				{
					Vector3 homeStep = (Vector3)(toHome.normalized * returnSpeed * Time.deltaTime);
					transform.position += homeStep;
				}
			}
		}
	}

	private void ApplyChaseCollisionMode(bool chasing)
	{
		if (!passThroughCollisionsWhenChasing) return;
		if (selfColliders == null || selfColliders.Length == 0) return;

		if (chasing)
		{
			for (int i = 0; i < selfColliders.Length; i++)
			{
				selfColliders[i].isTrigger = true;
			}
		}
		else
		{
			// Restore original trigger states when not chasing
			if (defaultIsTrigger != null && defaultIsTrigger.Length == selfColliders.Length)
			{
				for (int i = 0; i < selfColliders.Length; i++)
				{
					selfColliders[i].isTrigger = defaultIsTrigger[i];
				}
			}
		}
	}

	private void ApplyLightSync()
	{
		if (enemyLight == null) return;
		if (enemyLight.lightType != Light2D.LightType.Point)
		{
			enemyLight.lightType = Light2D.LightType.Point;
		}

		// expire blink
		if (blinkActive && Time.time >= blinkEndTime)
		{
			blinkActive = false;
			// do not clear overrideGate; scene may keep enemy on until player turns on
		}
		// Optionally sync light radius to agro; otherwise preserve current light radius
		float outer = syncLightWithAgro ? Mathf.Max(0.01f, agroRadius) : Mathf.Max(0.01f, enemyLight.pointLightOuterRadius);
		float inner = Mathf.Clamp01(lightInnerRadiusRatio) * outer;
		enemyLight.pointLightOuterRadius = outer;
		enemyLight.pointLightInnerRadius = inner;
		float baseIntensity = Mathf.Clamp01(lightIntensity);
		bool gateAllows = !respectGlobalLightingState || LightingState.LocalLightsEnabled || overrideGate;
		float targetIntensity = gateAllows ? baseIntensity : 0f;
		if (gateAllows && blinkActive)
		{
			float phase = Mathf.PingPong(Time.time * blinkFrequency, 1f);
			float factor = Mathf.Lerp(blinkMinFactor, blinkMaxFactor, phase);
			targetIntensity = baseIntensity * Mathf.Clamp01(factor);
		}
		enemyLight.intensity = targetIntensity;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;   // agro radius
		Gizmos.DrawWireSphere(transform.position, agroRadius);

		Gizmos.color = Color.yellow; // lose radius
		Gizmos.DrawWireSphere(transform.position, loseRadius);

		Gizmos.color = Color.green; // stop distance
		Gizmos.DrawWireSphere(transform.position, stoppingDistance);

		// Home position marker (uses current transform when not playing)
		Gizmos.color = new Color(0f, 1f, 0.5f, 0.9f);
		Vector3 home = Application.isPlaying ? homePosition : transform.position;
		Gizmos.DrawSphere(home, 0.06f);

		// Patrol gizmos: endpoints and path
		Vector3 pointB = home + Vector3.right * patrolDistanceX;
		Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
		Gizmos.DrawSphere(pointB, 0.05f);
		Gizmos.DrawLine(home, pointB);
	}

	/// <summary>
	/// Start a local blink for this enemy's light.
	/// </summary>
	public void StartLightBlink(float duration, float frequency = -1f, float minFactor = -1f, float maxFactor = -1f, bool overrideGate = true)
	{
		blinkActive = true;
		blinkEndTime = Time.time + Mathf.Max(0f, duration);
		blinkFrequency = (frequency > 0f) ? frequency : defaultBlinkFrequency;
		blinkMinFactor = (minFactor >= 0f) ? Mathf.Clamp01(minFactor) : defaultBlinkMinFactor;
		blinkMaxFactor = (maxFactor >= 0f) ? Mathf.Clamp01(maxFactor) : defaultBlinkMaxFactor;
		this.overrideGate = overrideGate;

		if (enemyLight != null && enemyLight.lightType != Light2D.LightType.Point)
		{
			enemyLight.lightType = Light2D.LightType.Point;
		}
	}

	/// <summary>
	/// Explicitly allow or disallow this local light while the global gate is off.
	/// </summary>
	public void SetGateOverride(bool enabled)
	{
		overrideGate = enabled;
	}
}
