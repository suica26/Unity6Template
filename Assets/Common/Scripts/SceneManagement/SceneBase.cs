using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace Common.Scripts.SceneManagement;

/// <summary>
/// シーンの基底クラス
/// </summary>
public abstract class SceneBase<TContext> : MonoBehaviour where TContext : ISceneContext
{
    /// <summary>
    /// 初期化処理
    /// </summary>
    public abstract UniTask InitializeAsync(TContext context, CancellationToken cancellationToken);

    /// <summary>
    /// 初期化後の処理
    /// </summary>
    public virtual UniTask PostInitializeAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;

    /// <summary>
    /// このシーンを出る前の処理
    /// </summary>
    public virtual UniTask PreOutAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;

    /// <summary>
    /// このシーンを出るときの処理
    /// </summary>
    public virtual UniTask OnOutAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;
}

/// <summary>
/// シーンの基底クラス（コンテキスト省略版）
/// </summary>
public abstract class SceneBase : SceneBase<DefaultContext> { }