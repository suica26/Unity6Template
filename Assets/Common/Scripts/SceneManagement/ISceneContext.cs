namespace Common.Scripts.SceneManagement;

/// <summary>
/// シーンコンテキストの I / F
/// </summary>
public interface ISceneContext { }

/// <summary>
/// デフォルトのシーンコンテキスト
/// </summary>
public record DefaultContext : ISceneContext;