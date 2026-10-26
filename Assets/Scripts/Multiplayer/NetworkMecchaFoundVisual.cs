using UnityEngine;

public class NetworkMecchaFoundVisual :
    MonoBehaviour
{
    [Header("Network")]
    [SerializeField]
    private NetworkGameState networkState;

    [SerializeField]
    private NetworkMecchaIdentity identity;

    [Header("Found Visual")]
    [SerializeField]
    private Color foundColor =
        Color.red;

    [Header("Reveal Pulse")]
    [SerializeField]
    private Color revealRedColor =
        Color.red;

    [SerializeField]
    private Color revealWhiteColor =
        Color.white;

    [SerializeField]
    private float pulseSpeed =
        2f;

    private Renderer[] mecchaRenderers;

    private Color[][] originalColors;

    private bool originalColorsCaptured =
        false;

    private bool previousFoundState =
        false;

    private MultiplayerPhase
        previousPhase =
        (MultiplayerPhase)(-1);

    private void Awake()
    {
        mecchaRenderers =
            GetComponentsInChildren<
                Renderer
            >(
                true
            );
    }

    private void Start()
    {
        if (networkState == null)
        {
            networkState =
                FindFirstObjectByType<
                    NetworkGameState
                >();
        }

        if (identity == null)
        {
            identity =
                GetComponent<
                    NetworkMecchaIdentity
                >();
        }

        CaptureOriginalColors();
    }

    private void Update()
    {
        if (
            networkState == null ||
            !networkState.IsNetworkReady
        )
        {
            return;
        }

        if (identity == null)
        {
            identity =
                GetComponent<
                    NetworkMecchaIdentity
                >();
        }

        if (identity == null)
        {
            return;
        }

        NetworkMecchaData data;

        if (
            !TryGetNetworkMecchaData(
                identity.MecchaId,
                out data
            )
        )
        {
            return;
        }

        MultiplayerPhase phase =
            networkState.Phase;

        bool found =
            data.Found;

        // FOUND STATE

        if (found)
        {
            SetAllMaterialColors(
                foundColor
            );

            previousFoundState =
                true;

            previousPhase =
                phase;

            return;
        }

        // REVEAL PHASE

        if (
            phase ==
            MultiplayerPhase.Reveal
        )
        {
            float pulseValue =
                (
                    Mathf.Sin(
                        Time.time *
                        pulseSpeed *
                        Mathf.PI *
                        2f
                    ) +
                    1f
                ) *
                0.5f;

            Color pulseColor =
                Color.Lerp(
                    revealWhiteColor,
                    revealRedColor,
                    pulseValue
                );

            SetAllMaterialColors(
                pulseColor
            );

            previousFoundState =
                false;

            previousPhase =
                phase;

            return;
        }

        // NORMAL / SEARCHING / RESULT
        // Restore original material colour when a Meccha is not found and Reveal is over.
        if (
            previousFoundState ||
            previousPhase ==
                MultiplayerPhase.Reveal
        )
        {
            RestoreOriginalColors();
        }

        previousFoundState =
            false;

        previousPhase =
            phase;
    }

    private bool TryGetNetworkMecchaData(
        int mecchaId,
        out NetworkMecchaData data
    )
    {
        for (
            int i = 0;
            i <
            networkState.Mecchas.Length;
            i++
        )
        {
            data =
                networkState
                    .Mecchas
                    .Get(i);

            if (
                data.Active &&
                data.Id == mecchaId
            )
            {
                return true;
            }
        }

        data =
            default;

        return false;
    }

    private void CaptureOriginalColors()
    {
        if (
            mecchaRenderers == null
        )
        {
            return;
        }

        originalColors =
            new Color[
                mecchaRenderers.Length
            ][];

        for (
            int r = 0;
            r <
            mecchaRenderers.Length;
            r++
        )
        {
            Renderer renderer =
                mecchaRenderers[r];

            if (renderer == null)
            {
                continue;
            }

            Material[] materials =
                renderer.materials;

            originalColors[r] =
                new Color[
                    materials.Length
                ];

            for (
                int m = 0;
                m <
                materials.Length;
                m++
            )
            {
                if (
                    materials[m] == null
                )
                {
                    continue;
                }

                originalColors[r][m] =
                    GetMaterialColor(
                        materials[m]
                    );
            }
        }

        originalColorsCaptured =
            true;
    }

    private Color GetMaterialColor(
        Material material
    )
    {
        if (
            material.HasProperty(
                "_BaseColor"
            )
        )
        {
            return material.GetColor(
                "_BaseColor"
            );
        }

        if (
            material.HasProperty(
                "_Color"
            )
        )
        {
            return material.GetColor(
                "_Color"
            );
        }

        return Color.white;
    }

    private void SetMaterialColor(
        Material material,
        Color color
    )
    {
        if (
            material == null
        )
        {
            return;
        }

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

    private void SetAllMaterialColors(
        Color color
    )
    {
        if (
            mecchaRenderers == null
        )
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

            foreach (
                Material material
                in materials
            )
            {
                SetMaterialColor(
                    material,
                    color
                );
            }

            renderer.materials =
                materials;
        }
    }

    private void RestoreOriginalColors()
    {
        if (
            !originalColorsCaptured ||
            mecchaRenderers == null ||
            originalColors == null
        )
        {
            return;
        }

        for (
            int r = 0;
            r <
            mecchaRenderers.Length;
            r++
        )
        {
            Renderer renderer =
                mecchaRenderers[r];

            if (
                renderer == null ||
                originalColors[r] == null
            )
            {
                continue;
            }

            Material[] materials =
                renderer.materials;

            for (
                int m = 0;
                m <
                materials.Length;
                m++
            )
            {
                if (
                    materials[m] == null ||
                    m >=
                    originalColors[r].Length
                )
                {
                    continue;
                }

                SetMaterialColor(
                    materials[m],
                    originalColors[r][m]
                );
            }

            renderer.materials =
                materials;
        }
    }
}