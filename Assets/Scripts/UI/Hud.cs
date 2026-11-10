using UnityEngine;
using UnityEngine.UI;

/// TEMPORARY top bar: current day and spendable points. Placeholder until drawn UI exists.
/// Only visible during play (hidden on the title, welcome card, bedtime video and end screens).
public class Hud : MonoBehaviour
{
    public Text dayLabel;
    public Text pointsLabel;
    public CanvasGroup group;

    int shownDay = -1, shownPoints = -1;

    void Update()
    {
        var gm = GameManager.Instance;
        group.alpha = gm != null && gm.CanPause && Time.timeScale > 0f ? 1f : 0f; // hidden while paused

        if (PlayerState.Day != shownDay)
        {
            shownDay = PlayerState.Day;
            dayLabel.text = "DAY " + shownDay;
        }
        if (PlayerState.Points != shownPoints)
        {
            if (shownPoints >= 0 && isActiveAndEnabled) StartCoroutine(UIAnim.PopIn(pointsLabel.transform.parent, 1.25f, 0.25f));
            shownPoints = PlayerState.Points;
            pointsLabel.text = "POINTS " + shownPoints;
        }
    }
}
