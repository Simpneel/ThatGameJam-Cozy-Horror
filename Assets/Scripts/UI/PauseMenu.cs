using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Esc / P: Resume, What to do?, Credits, Quit. Freezes game time while open.
/// "What to do?" and Credits open a full-screen page; clicking it (or Esc) returns to the menu.
public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] GameObject creditsPanel;
    [SerializeField] GameObject instructionsPanel;
    [SerializeField] Button resumeButton, instructionsButton, creditsButton, quitButton, creditsCloseButton, instructionsCloseButton;

    public bool IsPaused => panel.activeSelf || SubPageOpen;
    bool SubPageOpen => creditsPanel.activeSelf || instructionsPanel.activeSelf;

    void Awake()
    {
        resumeButton.onClick.AddListener(Resume);
        instructionsButton.onClick.AddListener(() => OpenPage(instructionsPanel));
        creditsButton.onClick.AddListener(() => OpenPage(creditsPanel));
        instructionsCloseButton.onClick.AddListener(BackToMenu);
        creditsCloseButton.onClick.AddListener(BackToMenu);
        quitButton.onClick.AddListener(QuitGame);
        panel.SetActive(false);
        creditsPanel.SetActive(false);
        instructionsPanel.SetActive(false);
    }

    void OpenPage(GameObject page)
    {
        panel.SetActive(false);
        page.SetActive(true);
    }

    void BackToMenu()
    {
        creditsPanel.SetActive(false);
        instructionsPanel.SetActive(false);
        panel.SetActive(true);
    }

    void Update()
    {
        if (!GameInput.PausePressed) return;
        if (SubPageOpen) BackToMenu();
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
        instructionsPanel.SetActive(false);
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
