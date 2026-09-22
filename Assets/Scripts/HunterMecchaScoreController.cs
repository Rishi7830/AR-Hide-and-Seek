using UnityEngine;
using TMPro;

public class HunterMecchaScoreController : MonoBehaviour
{
    // =============================================================
    // UI REFERENCES
    // =============================================================

    [Header("UI References")]

    [SerializeField]
    private TMP_Text remainingMecchaText;

    [SerializeField]
    private TMP_Text mecchaFoundText;

    // =============================================================
    // STATE
    // =============================================================

    private int totalMecchas;

    private int remainingMecchas;

    private int mecchasFound;

    private HunterMecchaTarget[] mecchaTargets;

    // =============================================================
    // START
    // =============================================================

    private void Start()
    {
        ResetScore();
    }

    // =============================================================
    // RESET SCORE
    // =============================================================

    public void ResetScore()
    {
        // ---------------------------------------------------------
        // FIND ALL MECCHA TARGETS
        // ---------------------------------------------------------

        mecchaTargets =
            FindObjectsByType<HunterMecchaTarget>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        // ---------------------------------------------------------
        // COUNT TOTAL
        // ---------------------------------------------------------

        totalMecchas =
            mecchaTargets != null
                ? mecchaTargets.Length
                : 0;

        // ---------------------------------------------------------
        // RESET COUNTERS
        // ---------------------------------------------------------

        mecchasFound = 0;

        remainingMecchas =
            totalMecchas;

        // ---------------------------------------------------------
        // UPDATE UI
        // ---------------------------------------------------------

        UpdateUI();

        Debug.Log(
            "[HunterScore] Score reset. " +
            "Total Mecchas = " +
            totalMecchas
        );
    }

    // =============================================================
    // MECCHA FOUND
    // =============================================================

    public void RegisterMecchaFound(
        HunterMecchaTarget target
    )
    {
        if (target == null)
        {
            return;
        }

        // ---------------------------------------------------------
        // PREVENT DOUBLE COUNTING
        // ---------------------------------------------------------

        if (!target.IsHit())
        {
            return;
        }

        // ---------------------------------------------------------
        // CHECK CURRENT COUNT
        // ---------------------------------------------------------

        if (
            mecchasFound >=
            totalMecchas
        )
        {
            return;
        }

        // ---------------------------------------------------------
        // INCREMENT FOUND
        // ---------------------------------------------------------

        mecchasFound++;

        // ---------------------------------------------------------
        // DECREASE REMAINING
        // ---------------------------------------------------------

        remainingMecchas =
            Mathf.Max(
                totalMecchas -
                mecchasFound,
                0
            );

        // ---------------------------------------------------------
        // UPDATE UI
        // ---------------------------------------------------------

        UpdateUI();

        Debug.Log(
            "[HunterScore] Meccha found! " +
            "Found = " +
            mecchasFound +
            "/" +
            totalMecchas +
            " | Remaining = " +
            remainingMecchas
        );
    }

    // =============================================================
    // UPDATE UI
    // =============================================================

    private void UpdateUI()
    {
        if (remainingMecchaText != null)
        {
            remainingMecchaText.text =
                "Meccha Remaining : " +
                remainingMecchas;
        }

        if (mecchaFoundText != null)
        {
            mecchaFoundText.text =
                "Meccha Found : " +
                mecchasFound;
        }
    }

    // =============================================================
    // GETTERS
    // =============================================================

    public int GetTotalMecchas()
    {
        return totalMecchas;
    }

    public int GetRemainingMecchas()
    {
        return remainingMecchas;
    }

    public int GetMecchasFound()
    {
        return mecchasFound;
    }

    // =============================================================
    // WIN CONDITION
    // =============================================================

    public bool AllMecchasFound()
    {
        return totalMecchas > 0 &&
               remainingMecchas <= 0;
    }
}