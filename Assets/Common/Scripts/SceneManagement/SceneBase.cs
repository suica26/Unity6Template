using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace Common.Scripts.SceneManagement
{
    /// <summary>
    /// シーンの基底クラス
    /// </summary>
    public abstract class SceneBase<TContext> : MonoBehaviour where TContext : ISceneContext
    {
        /// <summary>
        /// 初期化
        /// </summary>
        public abstract UniTask InitializeAsync(TContext context, CancellationToken ct);

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
    }
}