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

    private MaterialPropertyBlock[]
        propertyBlocks;

    private MultiplayerPhase
        previousPhase =
        (MultiplayerPhase)(-1);

    private bool previousFoundState =
        false;

    private bool stateInitialized =
        false;

    private void Awake()
    {
        // Find all renderers on this Meccha.
        mecchaRenderers =
            GetComponentsInChildren<
                Renderer
            >(
                true
            );

        propertyBlocks =
            new MaterialPropertyBlock[
                mecchaRenderers.Length
            ];

        for (
            int i = 0;
            i <
            propertyBlocks.Length;
            i++
        )
        {
            propertyBlocks[i] =
                new MaterialPropertyBlock();
        }
    }

    private void Start()
    {
        // Scene-level NetworkGameState.
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
    }

    private void Update()
    {
        if (
            networkState == null ||
            identity == null ||
            !networkState.IsNetworkReady
        )
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

        // MECCHA FOUND

        if (data.Found)
        {
            ApplyColor(
                foundColor
            );

            previousFoundState =
                true;

            previousPhase =
                phase;

            stateInitialized =
                true;

            return;
        }

        // REVEAL

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

            ApplyColor(
                pulseColor
            );

            previousFoundState =
                false;

            previousPhase =
                phase;

            stateInitialized =
                true;

            return;
        }

        // NORMAL GAMEPLAY

        if (
            previousFoundState ||
            (
                stateInitialized &&
                previousPhase ==
                    MultiplayerPhase.Reveal
            )
        )
        {
            ClearColorOverride();
        }

        previousFoundState =
            false;

        previousPhase =
            phase;

        stateInitialized =
            true;
    }

    // GET NETWORK DATA

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

    // APPLY COLOR USING MATERIAL PROPERTY BLOCK

    private void ApplyColor(
        Color color
    )
    {
        if (
            mecchaRenderers == null
        )
        {
            return;
        }

        for (
            int i = 0;
            i <
            mecchaRenderers.Length;
            i++
        )
        {
            Renderer renderer =
                mecchaRenderers[i];

            if (renderer == null)
            {
                continue;
            }

            MaterialPropertyBlock block =
                propertyBlocks[i];

            if (block == null)
            {
                block =
                    new MaterialPropertyBlock();

                propertyBlocks[i] =
                    block;
            }
            renderer.GetPropertyBlock(
                block
            );

            // URP / Standard common colour.
            if (
                HasBaseColorProperty(
                    renderer
                )
            )
            {
                block.SetColor(
                    "_BaseColor",
                    color
                );
            }

            if (
                HasColorProperty(
                    renderer
                )
            )
            {
                block.SetColor(
                    "_Color",
                    color
                );
            }

            renderer.SetPropertyBlock(
                block
            );
        }
    }

    // CLEAR OUR VISUAL OVERRIDE

    private void ClearColorOverride()
    {
        if (
            mecchaRenderers == null
        )
        {
            return;
        }

        for (
            int i = 0;
            i <
            mecchaRenderers.Length;
            i++
        )
        {
            Renderer renderer =
                mecchaRenderers[i];

            if (renderer == null)
            {
                continue;
            }
            renderer.SetPropertyBlock(
                null
            );

            if (
                propertyBlocks[i] != null
            )
            {
                propertyBlocks[i].Clear();
            }
        }
    }

    // CHECK MATERIAL PROPERTIES

    private bool HasBaseColorProperty(
        Renderer renderer
    )
    {
        if (
            renderer == null ||
            renderer.sharedMaterials == null
        )
        {
            return false;
        }

        foreach (
            Material material
            in renderer.sharedMaterials
        )
        {
            if (
                material != null &&
                material.HasProperty(
                    "_BaseColor"
                )
            )
            {
                return true;
            }
        }

        return false;
    }

    private bool HasColorProperty(
        Renderer renderer
    )
    {
        if (
            renderer == null ||
            renderer.sharedMaterials == null
        )
        {
            return false;
        }

        foreach (
            Material material
            in renderer.sharedMaterials
        )
        {
            if (
                material != null &&
                material.HasProperty(
                    "_Color"
                )
            )
            {
                return true;
            }
        }

        return false;
    }

    // CLEANUP

    private void OnDisable()
    {
        ClearColorOverride();
    }

    private void OnDestroy()
    {
        ClearColorOverride();
    }
}