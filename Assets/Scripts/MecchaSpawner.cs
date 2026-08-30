using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class MecchaSpawner : MonoBehaviour
{
    [Header("UI References")]
    public Button spawnMecchaButton;
    public GameObject mecchaChoicePanel;
    public Button horizontalButton;
    public Button verticalButton;

    [Header("Prefabs")]
    public GameObject mecchaHorizontalPrefab;
    public GameObject mecchaVerticalPrefab;

    [Header("AR Managers")]
    public ARRaycastManager raycastManager;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Start()
    {
        // Hide choice panel initially
        mecchaChoicePanel.SetActive(false);

        // Button listeners
        spawnMecchaButton.onClick.AddListener(ShowChoicePanel);
        horizontalButton.onClick.AddListener(() => SpawnOnPlane(mecchaHorizontalPrefab));
        verticalButton.onClick.AddListener(() => SpawnOnPlane(mecchaVerticalPrefab));
    }

    public void ShowChoicePanel()
    {
        mecchaChoicePanel.SetActive(true);
    }

    void SpawnOnPlane(GameObject prefab)
    {
        // Raycast from center of screen
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);

        if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            Instantiate(prefab, hitPose.position, hitPose.rotation);
        }

        // Hide panel after spawning
        mecchaChoicePanel.SetActive(false);
    }
}
