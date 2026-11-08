using System;
using UnityEngine;
using UnityEngine.UI;

/// "Go to bed?" Yes / No.
public class ConfirmDialog : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] Button yesButton, noButton;

    Action onYes;
    public bool IsOpen => panel.activeSelf;

    void Awake()
    {
        yesButton.onClick.AddListener(() => { panel.SetActive(false); var a = onYes; onYes = null; a?.Invoke(); });
        noButton.onClick.AddListener(() => { panel.SetActive(false); onYes = null; });
        panel.SetActive(false);
    }

    public void Show(Action yes)
    {
        onYes = yes;
        panel.SetActive(true);
        StartCoroutine(UIAnim.PopIn(panel.transform, 0.9f, 0.2f));
    }
}
