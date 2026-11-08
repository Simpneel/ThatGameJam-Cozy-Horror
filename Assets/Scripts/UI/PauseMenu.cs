using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Esc / P: Resume, Credits, Quit. Freezes game time while open.
public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] GameObject creditsPanel;
    [SerializeField] Button resumeButton, creditsButton, quitButton, creditsCloseButton;

    public bool IsPaused => panel.activeSelf || creditsPanel.activeSelf;

    void Awake()
    {
        resumeButton.onClick.AddListener(Resume);
        creditsButton.onClick.AddListener(() => { panel.SetActive(false); creditsPanel.SetActive(true); });
        creditsCloseButton.onClick.AddListener(() => { creditsPanel.SetActive(false); panel.SetActive(true); });
        quitButton.onClick.AddListener(QuitGame);
        panel.SetActive(false);
        creditsPanel.SetActive(false);
    }

    void Update()
    {
        if (!GameInput.PausePressed) return;
        if (creditsPanel.activeSelf) { creditsPanel.SetActive(false); panel.SetActive(true); }
        else if (panel.activeSelf) Resume();
        else if (GameManager.Instance == null || GameManager.Instance.CanPause) Pause();
    }

    public void Pause()
    {
        panel.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = false;
        StartCoroutine(UIAnim.PopIn(panel.transform, 0.95f, 0.15f));
    }

    public void Resume()
    {
        panel.SetActive(false);
        creditsPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    /// Quits the game (in the editor: stops Play mode; on the web: back to the start screen).
    public static void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // browsers can't quit: back to the start screen
#else
        Application.Quit();
#endif
    }
}
