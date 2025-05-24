using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace Common.Scripts.SceneManagement
{
    /// <summary>
    /// シーンのエントリポイント基底
    /// "EntryPoint"を省いたクラス名をシーン名とする
    /// 初期化処理は具象クラスで実装することで、引数を持たせることができる
    /// </summary>
    public abstract class SceneEntryPointBase : MonoBehaviour
    {
        /// <summary>
        /// シーン読み込み後の処理
        /// </summary>
        public virtual UniTask PostLoadAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;

        /// <summary>
        /// シーンを出る前の処理
        /// </summary>
        public virtual UniTask PreOutAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;

        /// <summary>
        /// シーンを出るときの処理
        /// </summary>
        public virtual UniTask OnOutAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;
    }
}