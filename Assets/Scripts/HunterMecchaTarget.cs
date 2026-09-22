using UnityEngine;

public class HunterMecchaTarget : MonoBehaviour
{
    // =============================================================
    // TARGET SETTINGS
    // =============================================================

    [Header("Target Settings")]

    [Tooltip(
        "Colour applied to the Meccha when it is shot."
    )]
    [SerializeField]
    private Color hitColor = Color.red;

    // =============================================================
    // REVEAL SETTINGS
    // =============================================================

    [Header("Reveal Phase")]

    [Tooltip(
        "Colour used during the reveal pulse."
    )]
    [SerializeField]
    private Color revealRedColor = Color.red;

    [Tooltip(
        "Second colour used during the reveal pulse."
    )]
    [SerializeField]
    private Color revealWhiteColor = Color.white;

    // =============================================================
    // INTERNAL STATE
    // =============================================================

    private Renderer[] mecchaRenderers;

    private Material[][] originalMaterials;

    private bool alreadyHit = false;

    private bool revealActive = false;

    // =============================================================
    // AWAKE
    // =============================================================

    private void Awake()
    {
        // ---------------------------------------------------------
        // FIND ALL RENDERERS
        //
        // Includes SkinnedMeshRenderer.
        // ---------------------------------------------------------

        mecchaRenderers =
            GetComponentsInChildren<Renderer>(
                true
            );

        // ---------------------------------------------------------
        // SAVE ORIGINAL MATERIALS
        // ---------------------------------------------------------

        originalMaterials =
            new Material[mecchaRenderers.Length][];

        for (
            int i = 0;
            i < mecchaRenderers.Length;
            i++
        )
        {
            Renderer renderer =
                mecchaRenderers[i];

            if (renderer == null)
            {
                continue;
            }

            originalMaterials[i] =
                renderer.materials;
        }
    }

    // =============================================================
    // SHOT
    // =============================================================

    public void OnShot()
    {
        // ---------------------------------------------------------
        // DON'T PROCESS SAME MECCHA TWICE
        // ---------------------------------------------------------

        if (alreadyHit)
        {
            return;
        }

        alreadyHit =
            true;

        // ---------------------------------------------------------
        // STOP REVEAL EFFECT
        // ---------------------------------------------------------

        revealActive =
            false;

        // ---------------------------------------------------------
        // TURN RED
        // ---------------------------------------------------------

        MakeRed();

        Debug.Log(
            "[HunterMeccha] Meccha has been shot: " +
            gameObject.name
        );
    }

    // =============================================================
    // MAKE RED
    // =============================================================

    private void MakeRed()
    {
        if (mecchaRenderers == null)
        {
            return;
        }

        foreach (
            Renderer renderer
            in mecchaRenderers
        )
        {
            if (renderer == null)
            {
                continue;
            }

            Material[] materials =
                renderer.materials;

            for (
                int i = 0;
                i < materials.Length;
                i++
            )
            {
                Material material =
                    materials[i];

                if (material == null)
                {
                    continue;
                }

                SetMaterialColor(
                    material,
                    hitColor
                );
            }

            renderer.materials =
                materials;
        }
    }

    // =============================================================
    // START REVEAL PULSE
    // =============================================================

    public void StartRevealPulse()
    {
        // ---------------------------------------------------------
        // ALREADY SHOT
        //
        // Shot Mecchas must NOT participate in the reveal.
        // ---------------------------------------------------------

        if (alreadyHit)
        {
            return;
        }

        revealActive =
            true;

        SetRevealColor(
            revealWhiteColor
        );
    }

    // =============================================================
    // UPDATE REVEAL PULSE
    // =============================================================

    public void UpdateRevealPulse(
        float pulseValue
    )
    {
        if (
            !revealActive ||
            alreadyHit
        )
        {
            return;
        }

        // ---------------------------------------------------------
        // pulseValue:
        //
        // 0 = white
        // 1 = red
        //
        // Smoothly interpolate between the two.
        // ---------------------------------------------------------

        Color pulseColor =
            Color.Lerp(
                revealWhiteColor,
                revealRedColor,
                pulseValue
            );

        SetRevealColor(
            pulseColor
        );
    }

    // =============================================================
    // SET REVEAL COLOUR
    // =============================================================

    private void SetRevealColor(
        Color color
    )
    {
        if (mecchaRenderers == null)
        {
            return;
        }

        foreach (
            Renderer renderer
            in mecchaRenderers
        )
        {
            if (renderer == null)
            {
                continue;
            }

            Material[] materials =
                renderer.materials;

            for (
                int i = 0;
                i < materials.Length;
                i++
            )
            {
                Material material =
                    materials[i];

                if (material == null)
                {
                    continue;
                }

                SetMaterialColor(
                    material,
                    color
                );
            }

            renderer.materials =
                materials;
        }
    }

    // =============================================================
    // SET MATERIAL COLOUR
    // =============================================================

    private void SetMaterialColor(
        Material material,
        Color color
    )
    {
        // ---------------------------------------------------------
        // URP
        // ---------------------------------------------------------

        if (
            material.HasProperty(
                "_BaseColor"
            )
        )
        {
            material.SetColor(
                "_BaseColor",
                color
            );
        }

        // ---------------------------------------------------------
        // STANDARD / OLDER SHADERS
        // ---------------------------------------------------------

        if (
            material.HasProperty(
                "_Color"
            )
        )
        {
            material.SetColor(
                "_Color",
                color
            );
        }
    }

    // =============================================================
    // END REVEAL
    // =============================================================

    public void EndRevealPulse()
    {
        if (alreadyHit)
        {
            return;
        }

        revealActive =
            false;

        RestoreOriginalMaterials();
    }

    // =============================================================
    // RESET TARGET
    // =============================================================

    public void ResetTarget()
    {
        RestoreOriginalMaterials();

        alreadyHit =
            false;

        revealActive =
            false;

        Debug.Log(
            "[HunterMeccha] Target reset."
        );
    }

    // =============================================================
    // RESTORE ORIGINAL MATERIALS
    // =============================================================

    private void RestoreOriginalMaterials()
    {
        if (
            mecchaRenderers == null ||
            originalMaterials == null
        )
        {
            return;
        }

        for (
            int i = 0;
            i < mecchaRenderers.Length;
            i++
        )
        {
            if (
                mecchaRenderers[i] != null &&
                originalMaterials[i] != null
            )
            {
                mecchaRenderers[i].materials =
                    originalMaterials[i];
            }
        }
    }

    // =============================================================
    // CHECK HIT
    // =============================================================

    public bool IsHit()
    {
        return alreadyHit;
    }
}