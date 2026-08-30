using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;

[System.Serializable]
public class MecchaPositionData
{
    public string objectName;
    public float x;
    public float y;
    public float z;

    public MecchaPositionData(string name, Vector3 pos)
    {
        objectName = name;
        x = pos.x;
        y = pos.y;
        z = pos.z;
    }
}

public class MecchaLockManager : MonoBehaviour
{
    [Header("UI References")]
    public Button lockMecchaButton;
    public TextMeshProUGUI coordinatesText;
    public GameObject mecchaLockedPanel;

    [Header("Settings")]
    public float popupDuration = 5f;
    public string fileName = "MecchaPosition.json";

    // Dynamic runtime reference
    private Transform mecchaTransform;

    private void Start()
    {
        if (lockMecchaButton != null)
        {
            lockMecchaButton.onClick.AddListener(LockAndSavePosition);
        }

        if (mecchaLockedPanel != null)
        {
            mecchaLockedPanel.SetActive(false);
        }
    }

    public void LockAndSavePosition()
    {
        // 1. Locate the dynamically spawned/fine-tuned Meccha in the scene
        LocateActiveMeccha();

        Vector3 currentPos = Vector3.zero;

        if (mecchaTransform != null)
        {
            currentPos = mecchaTransform.position;
            Debug.Log($"Found active Meccha at position: {currentPos}");
        }
        else
        {
            Debug.LogWarning("LockMecchaManager: Could not find active Meccha in scene! Saving (0,0,0).");
        }

        // 2. Save coordinates to JSON file
        SaveCoordinatesToJSON("Meccha 1", currentPos);

        // 3. Reset UI coordinate display back to NA
        if (coordinatesText != null)
        {
            coordinatesText.text = "NA";
        }

        // 4. Trigger 5-second notification popup
        if (mecchaLockedPanel != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowLockedPopupRoutine());
        }
    }

    private void LocateActiveMeccha()
    {
        // Strategy A: Get active Meccha from MecchaFineTuneController
        MecchaFineTuneController fineTuneScript = FindFirstObjectByType<MecchaFineTuneController>();
        if (fineTuneScript != null && fineTuneScript.currentMeccha != null)
        {
            mecchaTransform = fineTuneScript.currentMeccha.transform;
            return;
        }

        // Strategy B: Get active Meccha from MecchaCoordinateDisplay
        MecchaCoordinateDisplay coordDisplay = FindFirstObjectByType<MecchaCoordinateDisplay>();
        if (coordDisplay != null && coordDisplay.currentMeccha != null)
        {
            mecchaTransform = coordDisplay.currentMeccha.transform;
            return;
        }

        // Strategy C: Fallback search by GameObject tag
        GameObject mecchaObj = GameObject.FindWithTag("Meccha");
        if (mecchaObj != null)
        {
            mecchaTransform = mecchaObj.transform;
        }
    }

    private void SaveCoordinatesToJSON(string name, Vector3 position)
    {
        MecchaPositionData data = new MecchaPositionData(name, position);
        string json = JsonUtility.ToJson(data, true);

        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(filePath, json);

        Debug.Log($"Meccha position saved to JSON: {filePath}\n{json}");
    }

    private IEnumerator ShowLockedPopupRoutine()
    {
        mecchaLockedPanel.SetActive(true);

        yield return new WaitForSeconds(popupDuration);

        mecchaLockedPanel.SetActive(false);
    }
}