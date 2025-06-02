using UnityEngine;
using Common.Scripts.SceneManagement;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Common.Scripts.Test
{
    public class Test2Scene : SceneBase<Test2Scene.IContext>
    {
        public interface IContext : ISceneContext { }
        public record Context1 : IContext;
        public record Context2 : IContext;

        private static string SceneFileName => SceneHelper.GetSceneFileName<Test2Scene, IContext>();
        private static int instanceCount = 0;

        public override UniTask InitializeAsync(IContext context, CancellationToken cancellationToken)
        {
            Debug.Log($"{SceneFileName}: InitializeAsync called with context");

            instanceCount++;
            if (instanceCount > 1)
            {
                Debug.Log($"{SceneFileName}: Start Back To Default Scene");

                UniTask.Create(async () =>
                {
                    SceneManager.ClearStack();

                    await UniTask.Delay(1000, cancellationToken: cancellationToken);
                    Debug.Log($"{SceneFileName}: 3000ms delay completed before going back");

                    Debug.Log($"{SceneFileName}: Starting scene backing sequence...");
                    await SceneManager.BackAsync();
                }).Forget();

                return UniTask.CompletedTask;
            }

            UniTask.Create(async () =>
            {
                await UniTask.Delay(1000, cancellationToken: cancellationToken);
                Debug.Log($"{SceneFileName}: 3000ms delay completed before loading next scene");

                Debug.Log($"{SceneFileName}: Starting scene loading sequence...");
                await SceneManager.LoadAsync<Test3Scene>();
            }).Forget();

            return UniTask.CompletedTask;
        }
    }
}