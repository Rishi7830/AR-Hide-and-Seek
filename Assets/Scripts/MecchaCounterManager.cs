using System;
using TMPro;
using UnityEngine;

public class MecchaCounterManager : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI counterText;

    [Header("Text Format Settings")]
    [SerializeField]
    private string labelPrefix = "Mecchas Spawned: ";

    public int CurrentCount { get; private set; }

    public event Action<int> CountChanged;

    private void Start()
    {
        CurrentCount = 0;
        UpdateCounterUI();
    }

    public void IncrementCount()
    {
        CurrentCount++;

        UpdateCounterUI();

        CountChanged?.Invoke(CurrentCount);
    }

    public void DecrementCount()
    {
        CurrentCount = Mathf.Max(
            0,
            CurrentCount - 1
        );

        UpdateCounterUI();

        CountChanged?.Invoke(CurrentCount);
    }

    private void UpdateCounterUI()
    {
        if (counterText != null)
        {
            counterText.text =
                $"{labelPrefix}{CurrentCount}";
        }
    }

    public int GetCount()
    {
        return CurrentCount;
    }
}