namespace Common.Scripts.SceneManagement;

/// <summary>
/// シーンのコンテキストを表す I / F
/// </summary>
public interface ISceneContext
{
    /// <summary>
    /// シーン名
    /// </summary>
    string SceneName { get; }
}