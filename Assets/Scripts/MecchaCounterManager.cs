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

    /// <summary>
    /// Call when a new Meccha is spawned
    /// </summary>
    public void IncrementCount()
    {
        currentCount++;
        UpdateCounterUI();
    }

    /// <summary>
    /// Call when a Meccha is deleted
    /// </summary>
    public void DecrementCount()
    {
        currentCount = Mathf.Max(0, currentCount - 1);
        UpdateCounterUI();
    }

    /// <summary>
    /// Updates the text display
    /// </summary>
    private void UpdateCounterUI()
    {
        if (counterText != null)
        {
            counterText.text = $"{labelPrefix}{currentCount}";
        }
    }

    public int GetCount() => currentCount;
}