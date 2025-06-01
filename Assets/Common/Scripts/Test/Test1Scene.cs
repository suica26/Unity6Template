using UnityEngine;
using Common.Scripts.SceneManagement;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Common.Scripts.Test
{
    public class Test1Scene : SceneBase
    {
        public UniTask InitializeAsync(CancellationToken destroyCancellationToken)
        {
            Debug.Log($"{SceneFileName}: InitializeAsync called");

            UniTask.Create(async () =>
            {
                await UniTask.Delay(1000, cancellationToken: destroyCancellationToken);
                Debug.Log($"{SceneFileName}: 3000ms delay completed");

                Debug.Log($"{SceneFileName}: Starting scene loading sequence...");
                await SceneManager.LoadAsync<Test2Scene>((scene, ct) => scene.InitializeAsync(ct));
            }).Forget();

            return UniTask.CompletedTask;
        }
    }
}