using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace SceneEntryPointManagement;

/// <summary>
/// シーンエントリーポイントの基底クラス
/// </summary>
public abstract class SceneEntryPointBase<TArguments> : MonoBehaviour where TArguments : ISceneEntryPointArguments
{
    /// <summary>
    /// 初期化処理
    /// </summary>
    public abstract UniTask InitializeAsync(TArguments arguments, CancellationToken cancellationToken);

    /// <summary>
    /// 初期化後の処理
    /// </summary>
    public virtual UniTask PostInitializeAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;

    /// <summary>
    /// このシーンエントリーポイントを出る前の処理
    /// </summary>
    public virtual UniTask PreOutAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;

    /// <summary>
    /// このシーンエントリーポイントを出るときの処理
    /// </summary>
    public virtual UniTask OnOutAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;
}

/// <summary>
/// シーンエントリーポイントの基底クラス（引数省略版）
/// </summary>
public abstract class SceneEntryPointBase : SceneEntryPointBase<ISceneEntryPointArguments.Default> { }