using System;
using UnityEngine;
using UnityEngine.UI;

/// The start screen: Play, Credits, Quit (bottom-right). Quit is hidden in web builds, where a page can't close itself.
public class MainMenu : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] Button playButton, creditsButton, quitButton;

    public event Action PlayPressed, CreditsPressed;
    public bool IsOpen => panel.activeSelf;

    void Awake()
    {
        playButton.onClick.AddListener(() => PlayPressed?.Invoke());
        creditsButton.onClick.AddListener(() => CreditsPressed?.Invoke());
        quitButton.onClick.AddListener(PauseMenu.QuitGame);
#if UNITY_WEBGL && !UNITY_EDITOR
        quitButton.gameObject.SetActive(false);
#endif
        panel.SetActive(false);
    }

    public void Show() => panel.SetActive(true);
    public void Hide() => panel.SetActive(false);
}
