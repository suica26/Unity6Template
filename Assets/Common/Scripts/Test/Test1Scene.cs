using UnityEngine;
using Common.Scripts.SceneManagement;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Common.Scripts.Test
{
    public class Test1Scene : SceneBase
    {
        public override UniTask InitializeAsync(DefaultContext _, CancellationToken destroyCancellationToken)
        {
            var SceneFileName = SceneHelper.GetSceneFileName(this);
            Debug.Log($"{SceneFileName}: InitializeAsync called");

            UniTask.Create(async () =>
            {
                await UniTask.Delay(1000, cancellationToken: destroyCancellationToken);
                Debug.Log($"{SceneFileName}: 3000ms delay completed");

                Debug.Log($"{SceneFileName}: Starting scene loading sequence...");
                await SceneManager.LoadAsync<Test2Scene, Test2Scene.IContext>(new Test2Scene.Context1());
            }).Forget();

            return UniTask.CompletedTask;
        }
    }
}