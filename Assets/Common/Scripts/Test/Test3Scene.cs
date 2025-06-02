using UnityEngine;
using Common.Scripts.SceneManagement;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Common.Scripts.Test
{
    public class Test3Scene : SceneBase
    {
        private static string SceneFileName => SceneHelper.GetSceneFileName<Test3Scene>();

        public override UniTask InitializeAsync(DefaultContext _, CancellationToken cancellationToken)
        {
            Debug.Log($"{SceneFileName}: InitializeAsync called with context");

            UniTask.Create(async () =>
            {
                await UniTask.Delay(1000, cancellationToken: cancellationToken);
                Debug.Log($"{SceneFileName}: 3000ms delay completed");

                Debug.Log($"{SceneFileName}: Starting scene backing sequence...");
                await SceneManager.BackAsync();
            }).Forget();

            return UniTask.CompletedTask;
        }
    }
}