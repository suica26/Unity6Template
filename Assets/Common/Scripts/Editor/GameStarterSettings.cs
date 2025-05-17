using UnityEngine;
using UnityEditor;
using ZLinq;

/// <summary>
/// 開発中のゲーム開始設定
/// </summary>
public class GameStarterSettings : EditorWindow
{
    private string[] _sceneNames;
    private int _selectedIndex;

    private const string PREFS_KEY = "GameStarter_StartScene";

    [MenuItem("Tools/Game Starter Settings")]
    public static void ShowWindow()
    {
        GetWindow<GameStarterSettings>("Game Starter Settings");
    }

    private void OnEnable()
    {
        var scenes = EditorBuildSettings.scenes
            .AsValueEnumerable()
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        _sceneNames = scenes;

        string current = EditorPrefs.GetString(PREFS_KEY, scenes.AsValueEnumerable().FirstOrDefault() ?? "");
        _selectedIndex = System.Array.IndexOf(_sceneNames, current);
        if (_selectedIndex < 0)
            _selectedIndex = 0;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("起動シーン（Build Settingsから）", EditorStyles.boldLabel);

        if (_sceneNames.Length == 0)
        {
            EditorGUILayout.HelpBox("Build Settings に有効なシーンがありません", MessageType.Warning);
            return;
        }

        _selectedIndex = EditorGUILayout.Popup("Start Scene", _selectedIndex, _sceneNames.AsValueEnumerable().Select(PathToName).ToArray());

        if (GUILayout.Button("Save"))
        {
            EditorPrefs.SetString(PREFS_KEY, _sceneNames[_selectedIndex]);
            Debug.Log("起動シーンを保存しました: " + _sceneNames[_selectedIndex]);
        }
    }

    private string PathToName(string path)
    {
        return System.IO.Path.GetFileNameWithoutExtension(path);
    }
}
