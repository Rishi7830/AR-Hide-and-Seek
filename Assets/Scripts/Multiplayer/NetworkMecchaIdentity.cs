using UnityEngine;

public class NetworkMecchaIdentity :
    MonoBehaviour
{
    public int MecchaId
    {
        get;
        private set;
    }

    public int MecchaType
    {
        get;
        private set;
    }

    public bool IsInitialized
    {
        get;
        private set;
    }

    public void Initialize(
        int id,
        int type
    )
    {
        MecchaId =
            id;

        MecchaType =
            type;

        IsInitialized =
            true;
    }
}