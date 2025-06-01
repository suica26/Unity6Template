using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace Common.Scripts.SceneManagement
{
    /// <summary>
    /// シーンの基底クラス
    /// </summary>
    public abstract class SceneBase : MonoBehaviour
    {
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

        /// <summary>
        /// シーンのファイル名
        /// </summary>
        public string SceneFileName => SceneHelper.GetSceneFileName(this);
    }
}