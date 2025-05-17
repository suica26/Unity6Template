using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// ゲームスターター
/// </summary>
public class GameStarter
{
    private const string SAVE_PATH = "backScene.txt";
    private const string START_SCENE = "Assets/Scenes/Boot.unity";
    private const string PREFS_KEY = "GameStarter_StartScene";

    private static string _startSceneName => EditorPrefs.GetString(PREFS_KEY, START_SCENE);


    [MenuItem("Tools/PlayGame %;")]
    public static void PlayFromPrelaunchScene()
    {
        // プレイ中ならば停止する
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        var currentScene = EditorSceneManager.GetActiveScene();
        File.WriteAllText(SAVE_PATH, currentScene.path);

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            // 起動シーンを開いて再生
            EditorSceneManager.OpenScene(_startSceneName);
            EditorApplication.isPlaying = true;
        }
    }

    [InitializeOnLoadMethod]
    static void WatchGameEnd()
    {
        // 戻り先シーンが保存されていたら終了時にそのシーンへ戻る
        if (File.Exists(SAVE_PATH))
        {
            EditorApplication.playModeStateChanged += t =>
            {
                if (!EditorApplication.isPaused &&
                    !EditorApplication.isPlaying &&
                    !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    var backScene = File.ReadAllText(SAVE_PATH);
                    File.Delete(SAVE_PATH);
                    EditorSceneManager.OpenScene(backScene);
                }
            };
        }
    }
}
