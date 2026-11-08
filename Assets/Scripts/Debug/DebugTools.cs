using UnityEngine;

/// Test shortcuts, only active in the Editor and Development builds:
/// 1 = +5 hidden points   2 = spawn an anomaly   3 = show the flyer   4 = toggle the hidden-state readout
public class DebugTools : MonoBehaviour
{
    [SerializeField] AnomalyDirector director;
    [SerializeField] FlyerPopup flyer;

    bool showState;

    void Update()
    {
        if (!Debug.isDebugBuild) return;
        if (GameInput.DebugKey(1)) PlayerState.EarnPoint(5);
        if (GameInput.DebugKey(2)) director.SpawnOne(false);
        if (GameInput.DebugKey(3)) flyer.Show();
        if (GameInput.DebugKey(4)) showState = !showState;
    }

    void OnGUI()
    {
        if (!Debug.isDebugBuild || !showState) return;
        GUI.Label(new Rect(10, 10, 900, 24),
            $"Day {PlayerState.Day}  Points {PlayerState.Points}  Earned {PlayerState.PointsEarned}  " +
            $"Bought today {PlayerState.PurchasesToday}  Streak {PlayerState.NoPurchaseStreak}  Owned {PlayerState.OwnedCount}  Anomalies {director.ActiveCount}");
    }
}
