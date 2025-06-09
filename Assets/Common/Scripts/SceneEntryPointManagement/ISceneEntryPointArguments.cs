namespace SceneEntryPointManagement;

/// <summary>
/// シーンエントリーポイント引数の I / F
/// </summary>
public interface ISceneEntryPointArguments
{
    /// <summary>
    /// デフォルトのシーンエントリーポイント引数
    /// </summary>
    public record struct Default : ISceneEntryPointArguments;
}