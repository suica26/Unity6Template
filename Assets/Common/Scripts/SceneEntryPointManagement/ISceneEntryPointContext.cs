namespace SceneEntryPointManagement;

/// <summary>
/// シーンエントリーポイントコンテキストの I / F
/// </summary>
public interface ISceneEntryPointContext
{
    /// <summary>
    /// デフォルトのシーンエントリーポイントコンテキスト
    /// </summary>
    public record struct Default : ISceneEntryPointContext;
}