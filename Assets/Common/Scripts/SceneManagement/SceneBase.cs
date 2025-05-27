using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace Common.Scripts.SceneManagement
{
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
        public virtual UniTask PostInitializeAsync(CancellationToken ct) => UniTask.CompletedTask;

        /// <summary>
        /// このシーンを出る前の処理
        /// </summary>
        public virtual UniTask PreOutAsync(CancellationToken ct) => UniTask.CompletedTask;

        /// <summary>
        /// このシーンを出るときの処理
        /// </summary>
        public virtual UniTask OnOutAsync(CancellationToken ct) => UniTask.CompletedTask;

        /// <summary>
        /// このシーンに戻るときの処理
        /// </summary>
        UniTask OnBackedAsync(ISceneContext context, CancellationToken ct) => InitializeAsync((TContext)context, ct);
    }

    /// <summary>
    /// シーンの基底クラス
    /// </summary>
    public abstract class SceneBase<TContext> : MonoBehaviour, IScene<TContext> where TContext : ISceneContext
    {
        public abstract UniTask InitializeAsync(TContext context, CancellationToken ct);
    }
}