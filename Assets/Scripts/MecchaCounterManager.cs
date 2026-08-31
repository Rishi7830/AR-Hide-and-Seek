using UnityEngine;
using TMPro;

public class MecchaCounterManager : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI counterText;

    [Header("Text Format Settings")]
    [SerializeField] private string labelPrefix = "Mecchas Spawned: ";

    private int currentCount = 0;

    private void Start()
    {
        UpdateCounterUI();
    }

    // Call when a new Meccha is spawned
    public void IncrementCount()
    {
        currentCount++;
        UpdateCounterUI();
    }

    // Call when a Meccha is deleted
    public void DecrementCount()
    {
        currentCount = Mathf.Max(0, currentCount - 1);
        UpdateCounterUI();
    }

    // Updates the text display
    private void UpdateCounterUI()
    {
        if (counterText != null)
        {
            counterText.text = $"{labelPrefix}{currentCount}";
        }
    }

    public int GetCount() => currentCount;
}