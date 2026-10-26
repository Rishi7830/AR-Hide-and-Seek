using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class PhotonConnectionStatusUI :
    MonoBehaviour
{
    [Header("Photon")]
    [SerializeField]
    private NetworkRunner runner;

    [SerializeField]
    private NetworkGameState networkState;

    [Header("Connection UI")]
    [SerializeField]
    private GameObject connectionPanel;

    [SerializeField]
    private TMP_Text connectionStatusText;

    [SerializeField]
    private float connectedDisplayDuration =
        3f;

    [Header("Role Buttons")]
    [SerializeField]
    private Button hiderButton;

    [SerializeField]
    private Button seekerButton;

    private Coroutine
        connectionRoutine;

    private void Start()
    {
        // Find NetworkRunner automatically if not assigned.
        if (runner == null)
        {
            runner =
                FindFirstObjectByType<
                    NetworkRunner
                >();
        }

        // Find NetworkGameState automatically if not assigned.
        if (networkState == null)
        {
            networkState =
                FindFirstObjectByType<
                    NetworkGameState
                >();
        }

        // Disable role selection immediately.
        SetRoleButtonsInteractable(
            false
        );

        // Show connection panel.
        if (connectionPanel != null)
        {
            connectionPanel.SetActive(
                true
            );
        }

        SetStatusText(
            "Connection Establishing..."
        );

        // Start waiting for Photon.
        connectionRoutine =
            StartCoroutine(
                WaitForPhotonConnection()
            );
    }

    // WAIT FOR PHOTON CONNECTION

    private IEnumerator
        WaitForPhotonConnection()
    {
        while (!IsPhotonReady())
        {
            yield return null;
        }

        Debug.Log(
            "[PhotonUI] " +
            "Photon connection established."
        );

        // Change message.
        SetStatusText(
            "Connection Established"
        );

        // Keep it visible for 3 seconds.
        yield return new WaitForSeconds(
            connectedDisplayDuration
        );

        // Hide panel.
        if (connectionPanel != null)
        {
            connectionPanel.SetActive(
                false
            );
        }

        // Enable role buttons.
        SetRoleButtonsInteractable(
            true
        );

        Debug.Log(
            "[PhotonUI] " +
            "Main Menu ready for role selection."
        );

        connectionRoutine =
            null;
    }

    // CHECK PHOTON READY

    private bool IsPhotonReady()
    {
        if (
            networkState != null &&
            networkState.IsNetworkReady
        )
        {
            return true;
        }

        // Fallback to the NetworkRunner.
        if (
            runner != null &&
            runner.IsRunning
        )
        {
            return true;
        }
        if (runner == null)
        {
            runner =
                FindFirstObjectByType<
                    NetworkRunner
                >();
        }

        if (
            networkState == null
        )
        {
            networkState =
                FindFirstObjectByType<
                    NetworkGameState
                >();
        }

        return
            (
                runner != null &&
                runner.IsRunning
            ) ||
            (
                networkState != null &&
                networkState.IsNetworkReady
            );
    }

    // ROLE BUTTONS

    private void SetRoleButtonsInteractable(
        bool interactable
    )
    {
        if (hiderButton != null)
        {
            hiderButton.interactable =
                interactable;
        }

        if (seekerButton != null)
        {
            seekerButton.interactable =
                interactable;
        }
    }

    // STATUS TEXT

    private void SetStatusText(
        string message
    )
    {
        if (
            connectionStatusText != null
        )
        {
            connectionStatusText.text =
                message;
        }
    }

    // CLEANUP

    private void OnDestroy()
    {
        if (
            connectionRoutine !=
            null
        )
        {
            StopCoroutine(
                connectionRoutine
            );

            connectionRoutine =
                null;
        }
    }
}