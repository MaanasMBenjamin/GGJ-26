using UnityEngine;

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

	[Header("Target Settings")]
	[SerializeField] private Transform player;          // assign your player here (optional)
	[SerializeField] private bool findByTag = true;     // if true, auto-find player by tag
	[SerializeField] private string playerTag = "Player";

	private bool isChasing;
	private Vector3 homePosition;                       // per-instance home (spawn) position

	private void Awake()
	{
		if (player == null && findByTag)
		{
			GameObject p = GameObject.FindWithTag(playerTag);
			if (p != null) player = p.transform;
		}
	}

	private void Start()
	{
		// Capture this instance's starting position as its home.
		// Safe for many prefab instances placed randomly.
		homePosition = transform.position;
	}

	private void Update()
	{
		if (player == null) return;

		// Distance to player (use squared distances to avoid sqrt cost)
		Vector2 toPlayer = player.position - transform.position;
		float sqrDist = toPlayer.sqrMagnitude;

		float sqrAgro = agroRadius * agroRadius;
		float sqrLose = loseRadius * loseRadius;
		float sqrStop = stoppingDistance * stoppingDistance;

		// Hysteresis: start chase inside agro; stop chase outside lose
		if (!isChasing && sqrDist <= sqrAgro)
		{
			isChasing = true;
		}
		else if (isChasing && sqrDist > sqrLose)
		{
			isChasing = false;
		}

		// Move towards player while chasing and not too close
		if (isChasing && sqrDist > sqrStop)
		{
			Vector3 step = (Vector3)(toPlayer.normalized * moveSpeed * Time.deltaTime);
			transform.position += step;
		}
		// When not chasing, return to home position
		else if (!isChasing)
		{
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
	}
}
