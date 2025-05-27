using System.Threading;
using Cysharp.Threading.Tasks;

namespace Common.Scripts.SceneManagement;

/// <summary>
/// シーンを表す I / F
/// </summary>
public interface IScene<TContext> where TContext : ISceneContext
{
    /// <summary>
    /// 初期化
    /// </summary>
    UniTask InitializeAsync(TContext context, CancellationToken ct);

    /// <summary>
    /// 初期化後の処理
    /// </summary>
    virtual UniTask PostInitializeAsync(CancellationToken ct) => UniTask.CompletedTask;

    /// <summary>
    /// このシーンを出る前の処理
    /// </summary>
    virtual UniTask PreOutAsync(CancellationToken ct) => UniTask.CompletedTask;

    /// <summary>
    /// このシーンを出るときの処理
    /// </summary>
    virtual UniTask OnOutAsync(CancellationToken ct) => UniTask.CompletedTask;

    /// <summary>
    /// このシーンに戻るときの処理
    /// デフォルトシーンは必ずInitializeAsyncで初期化されるため、オーバーライドは意味なし
    /// </summary>
    public virtual UniTask OnBackedAsync(ISceneContext context, CancellationToken ct) => InitializeAsync((TContext)context, ct);
}
