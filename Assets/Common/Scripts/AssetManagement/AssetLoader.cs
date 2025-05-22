using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AssetManagement
{
    public static class AssetLoader
    {
        /// <summary>
        /// アセットを非同期ロードする
        /// </summary>
        public static async UniTask<T?> LoadAsync<T>(string address, AssetLifeTimeBase assetLifeTime, CancellationToken cancellationToken)
            where T : UnityEngine.Object
        {
            var handle = Addressables.LoadAssetAsync<T>(address);

            try
            {
                await handle.WithCancellation(cancellationToken);

                // Addressablesの内部エラーがあったかを確認
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    var ex = handle.OperationException ?? new Exception("Unknown exception occurred while loading asset.");
                    handle.Release();
                    throw ex;
                }
            }
            catch (OperationCanceledException)
            {
                // キャンセルされたら解放してnullを返す
                Debug.Log("[AssetLoader] ロードがキャンセルされました");
                handle.Release();
                return null;
            }
            catch
            {
                // 例外が発生したら解放して投げ直す
                handle.Release();
                throw;
            }

            // AssetLifeTimeに追加
            assetLifeTime.AddAsyncOperationHandle(handle);

            // ロードしたアセットを返す
            return handle.Result;
        }

    }
}