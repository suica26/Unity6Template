using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace Common.Scripts.SceneManagement
{
    /// <summary>
    /// シーンの基底クラス
    /// </summary>
    public abstract class SceneBase<TContext> : MonoBehaviour, IScene<TContext> where TContext : ISceneContext
    {
        public abstract UniTask InitializeAsync(TContext context, CancellationToken ct);
    }
}