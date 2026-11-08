using UnityEditor;
using UnityEditor.SceneManagement;

/// Headless: COZY_PLAYTEST=1 Unity.exe -batchmode -projectPath . -executeMethod CozyPlaytest.Run   (no -quit)
public static class CozyPlaytest
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        EditorApplication.isPlaying = true; // the Playtest component takes over and exits the editor when done
    }
}
