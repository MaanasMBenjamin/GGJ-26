using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PlayerMaskSystem : MonoBehaviour
{
    public float abilityCooldown = 30f;
    private bool canUseAbility = true;

    public void EquipMask(Mask mask)
    {
        if (!canUseAbility) return;

        StartCoroutine(UseMaskAbility(mask));
    }

    IEnumerator UseMaskAbility(Mask mask)
    {
        canUseAbility = false;

        switch (mask.maskType)
        {
            case MaskType.Speed:
                yield return StartCoroutine(SpeedBoost());
                break;

            case MaskType.Invisibility:
                yield return StartCoroutine(Invisibility());
                break;

            case MaskType.Sacrifice:
                yield return StartCoroutine(RevealAllMasks());
                break;
        }

        yield return new WaitForSeconds(abilityCooldown);
        canUseAbility = true;
    }

    IEnumerator SpeedBoost()
    {
        Debug.Log("Speed mask activated!");
        // add speed logic here
        yield return new WaitForSeconds(5f);
    }

    IEnumerator Invisibility()
    {
        Debug.Log("Invisible mask activated!");
        // invisibility logic here
        yield return new WaitForSeconds(5f);
    }

    IEnumerator RevealAllMasks()
    {
        Debug.Log("Sacrifice mask activated!");

        Mask[] allMasks = FindObjectsOfType<Mask>();

        foreach (Mask m in allMasks)
            m.ShowMaskColor();

        yield return new WaitForSeconds(15f);

        foreach (Mask m in allMasks)
            m.HideMaskColor();
    }
}
