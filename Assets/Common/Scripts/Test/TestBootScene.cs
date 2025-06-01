using UnityEngine;
using Common.Scripts.SceneManagement;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Common.Scripts.Test
{
    public class TestBootScene : SceneBase
    {
        private static int instanceCount = 0;

        private void Start()
        {
            if (!Application.isPlaying) return;

            instanceCount++;
            if (instanceCount > 1) return;
            Debug.Log($"{SceneFileName}: Start called");
            SceneManager.SetDefaultScene<TestBootScene>((scene, ct) => scene.InitializeAsync(ct));
            InitializeAsync(Application.exitCancellationToken).Forget();
        }

        public UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            Debug.Log($"{SceneFileName}: InitializeAsync called");
            SceneManager.ClearStack();

            UniTask.Create(async () =>
            {
                await UniTask.Delay(1000, cancellationToken: cancellationToken);
                Debug.Log($"{SceneFileName}: 3000ms delay completed");

                await SceneManager.LoadAsync<Test1Scene>((scene, ct) => scene.InitializeAsync(ct));
            }).Forget();

            return UniTask.CompletedTask;
        }
    }
}