using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Connects to pre-existing UI: updates MaskStats > Image with the player's picked mask sprite,
/// and cooldown > Cool-s with the remaining seconds. Does not modify alignment/anchors.
/// </summary>
public class MaskUi : MonoBehaviour
{
    [Header("Auto-Bind By Names")]
    [SerializeField] private string maskStatsRootName = "MaskStats"; // supports case-insensitive match
    [SerializeField] private string maskImageChildName = "Image";   // also matches "image"
    [SerializeField] private string cooldownRootName = "cooldown";   // case-insensitive
    [SerializeField] private string cooldownTextChildName = "Cool-s"; // matches "cool-s"

    [Header("Explicit References (optional)")]
    [SerializeField] private Image maskImage;
    [SerializeField] private Text cooldownText;
    [SerializeField] private TMP_Text cooldownTmpText;

    // TMP reflection support (if project uses TextMeshPro)
    private Component cooldownTmp;
    private PropertyInfo tmpTextProp;
    private PropertyInfo tmpColorProp;

    [Header("Visual Defaults")]
    [SerializeField] private Sprite defaultNoMaskSprite;
    [SerializeField] private Color defaultNoMaskTint = new Color(1f, 0.4f, 0.6f, 1f);
    [SerializeField] private Color cooldownDefaultColor = Color.white;
    [SerializeField] private Color cooldownSacrificeColor = Color.red;
    [SerializeField] private bool debugLogs = true;

    private PlayerMask playerMask;

    private void Awake()
    {
        // Cache PlayerMask (use non-obsolete API)
        playerMask = UnityEngine.Object.FindFirstObjectByType<PlayerMask>();

        // Auto-bind mask image by hierarchy names if not assigned
        if (maskImage == null)
        {
            Transform root = FindTransformNearbyOrGlobal(transform, maskStatsRootName);
            if (root != null)
            {
                Transform img = FindChildInsensitive(root, maskImageChildName);
                if (img != null) maskImage = img.GetComponent<Image>();
            }
            // Fallback: search globally for any Image named Image/image
            if (maskImage == null)
            {
                var anyImg = FindTransformAnywhere(maskImageChildName);
                if (anyImg != null) maskImage = anyImg.GetComponent<Image>();
                if (maskImage == null)
                {
                    var anyImgLower = FindTransformAnywhere("image");
                    if (anyImgLower != null) maskImage = anyImgLower.GetComponent<Image>();
                }
            }
        }

        // Auto-bind cooldown text by hierarchy names if not assigned
        if (cooldownText == null && cooldownTmpText == null && cooldownTmp == null)
        {
            Transform cdRoot = FindTransformNearbyOrGlobal(transform, cooldownRootName);
            if (cdRoot != null)
            {
                Transform txt = FindChildInsensitive(cdRoot, cooldownTextChildName);
                if (txt == null) txt = FindChildInsensitive(cdRoot, "cool-s");
                if (txt != null)
                {
                    cooldownText = txt.GetComponent<Text>();
                    if (cooldownText == null)
                    {
                        // Prefer direct TMP component if present
                        cooldownTmpText = txt.GetComponent<TMP_Text>();
                        if (cooldownTmpText == null)
                        {
                            // Fallback: TMP via reflection without hard dependency
                            cooldownTmp = txt.GetComponent(GetTmpTextType());
                            if (cooldownTmp != null)
                            {
                                tmpTextProp = cooldownTmp.GetType().GetProperty("text");
                                tmpColorProp = cooldownTmp.GetType().GetProperty("color");
                            }
                        }
                    }
                }
            }
            // Global fallback by name
            if (cooldownText == null && cooldownTmpText == null && cooldownTmp == null)
            {
                Transform txt = FindTransformAnywhere(cooldownTextChildName);
                if (txt == null) txt = FindTransformAnywhere("cool-s");
                if (txt != null)
                {
                    cooldownText = txt.GetComponent<Text>();
                    if (cooldownText == null)
                    {
                        cooldownTmpText = txt.GetComponent<TMP_Text>();
                        if (cooldownTmpText == null)
                        {
                            cooldownTmp = txt.GetComponent(GetTmpTextType());
                            if (cooldownTmp != null)
                            {
                                tmpTextProp = cooldownTmp.GetType().GetProperty("text");
                                tmpColorProp = cooldownTmp.GetType().GetProperty("color");
                            }
                        }
                    }
                }
            }
        }

        if (debugLogs)
        {
            Debug.Log($"[MaskUi] Bound: image={(maskImage!=null)} cooldownText={(cooldownText!=null)} tmpText={(cooldownTmpText!=null)} tmpReflect={(cooldownTmp!=null)}");
        }
    }

    private Type GetTmpTextType()
    {
        // Common assembly name for TextMeshPro in Unity
        var t = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
        if (t == null) t = Type.GetType("TMPro.TMP_Text");
        return t;
    }

    private void Update()
    {
        if (playerMask == null)
        {
            // Try re-acquire if PlayerMask wasn’t ready on Awake
            playerMask = UnityEngine.Object.FindFirstObjectByType<PlayerMask>();
            if (maskImage != null)
            {
                maskImage.sprite = defaultNoMaskSprite != null ? defaultNoMaskSprite : maskImage.sprite;
                maskImage.color = defaultNoMaskTint;
            }
            SetCooldownString("");
            return;
        }

        bool active = playerMask.HasActiveMask();
        if (maskImage != null)
        {
            if (active)
            {
                var sprite = playerMask.GetActiveMaskSprite();
                if (sprite != null)
                {
                    maskImage.sprite = sprite;
                    maskImage.color = Color.white;
                }
                else
                {
                    // Fallback: tint using mask color hint
                    maskImage.color = playerMask.GetActiveMaskUiColor();
                }
            }
            else
            {
                // No active mask: neutral default
                if (defaultNoMaskSprite != null) maskImage.sprite = defaultNoMaskSprite;
                maskImage.color = defaultNoMaskTint;
            }
        }

        // Seconds-only in Cool-s; red when sacrifice is active
        int secs = active ? playerMask.GetRemainingSeconds() : 0;
        SetCooldownString(active ? secs.ToString() : "");
        SetCooldownColor(PlayerMask.IsSacrificeEquipped ? cooldownSacrificeColor : cooldownDefaultColor);
    }

    // Global contains-name fallback
    private Transform FindTransformAnywhereContains(string nameFragment)
    {
        if (string.IsNullOrEmpty(nameFragment)) return null;
        var all = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var t in all)
        {
            if (t.name.IndexOf(nameFragment, StringComparison.OrdinalIgnoreCase) >= 0) return t;
        }
        return null;
    }

    private Transform FindChildInsensitive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName)) return null;
        // Try direct
        var direct = root.Find(childName);
        if (direct != null) return direct;
        // Case-insensitive scan
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(t.name, childName, StringComparison.OrdinalIgnoreCase)) return t;
        }
        // Contains fallback
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.IndexOf(childName, StringComparison.OrdinalIgnoreCase) >= 0) return t;
        }
        return null;
    }

    private Transform FindTransformAnywhere(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        var all = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var t in all)
        {
            if (string.Equals(t.name, name, StringComparison.OrdinalIgnoreCase)) return t;
        }
        // Contains fallback
        foreach (var t in all)
        {
            if (t.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return t;
        }
        return null;
    }

    private Transform FindTransformNearbyOrGlobal(Transform start, string name)
    {
        if (start != null)
        {
            var local = FindChildInsensitive(start, name);
            if (local != null) return local;
        }
        return FindTransformAnywhere(name);
    }

    private void SetCooldownString(string s)
    {
        if (cooldownText != null)
        {
            cooldownText.text = s;
        }
        else if (cooldownTmpText != null)
        {
            cooldownTmpText.text = s;
        }
        else if (cooldownTmp != null && tmpTextProp != null)
        {
            tmpTextProp.SetValue(cooldownTmp, s);
        }
    }

    private void SetCooldownColor(Color c)
    {
        if (cooldownText != null)
        {
            cooldownText.color = c;
        }
        else if (cooldownTmpText != null)
        {
            cooldownTmpText.color = c;
        }
        else if (cooldownTmp != null && tmpColorProp != null)
        {
            tmpColorProp.SetValue(cooldownTmp, c);
        }
    }
}
