using UnityEngine;
using UnityEngine.Rendering;

public class Mask : MonoBehaviour
{
    public MaskTypeScript maskType;
    public SpriteRenderer spriteRenderer;

    // Colors (hidden by default)
    public Color speedColor = Color.green;
    public Color invisColor = Color.white;
    public Color sacrificeColor = new Color(1f, 0.5f, 0f); // saffron

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        HideMaskColor();
    }

    public void ShowMaskColor()
    {
        switch (maskType)
        {
            case maskType.Speed:
                spriteRenderer.color = speedColor;
                break;

            case maskType.Invisibility:
                spriteRenderer.color = invisColor;
                break;

            case maskType.Sacrifice:
                spriteRenderer.color = sacrificeColor;
                break;
        }
    }

    public void HideMaskColor()
    {
        spriteRenderer.color = Color.gray; // unknown mask
    }
}
