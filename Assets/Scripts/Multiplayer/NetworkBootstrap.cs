using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkBootstrap : MonoBehaviour
{
    [Header("Photon Session")]
    [SerializeField]
    private string sessionName =
        "CG4002_AR_GAME";

    private NetworkRunner runner;

    private async void Start()
    {
        runner =
            GetComponent<NetworkRunner>();

        if (runner == null)
        {
            runner =
                gameObject.AddComponent<
                    NetworkRunner
                >();
        }

        NetworkSceneManagerDefault sceneManager =
            GetComponent<
                NetworkSceneManagerDefault
            >();

        if (sceneManager == null)
        {
            sceneManager =
                gameObject.AddComponent<
                    NetworkSceneManagerDefault
                >();
        }

        SceneRef sceneRef =
            SceneRef.FromIndex(
                SceneManager
                    .GetActiveScene()
                    .buildIndex
            );

        NetworkSceneInfo sceneInfo =
            new NetworkSceneInfo();

        sceneInfo.AddSceneRef(
            sceneRef,
            LoadSceneMode.Additive
        );

        StartGameResult result =
            await runner.StartGame(
                new StartGameArgs
                {
                    GameMode =
                        GameMode.Shared,

                    SessionName =
                        sessionName,

                    Scene =
                        sceneInfo,

                    SceneManager =
                        sceneManager
                }
            );

        if (result.Ok)
        {
            Debug.Log(
                "[Photon] Connected to room: " +
                sessionName
            );
        }
        else
        {
            Debug.LogError(
                "[Photon] Connection failed: " +
                result.ShutdownReason
            );
        }
    }
}