using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;

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

                // Addressable内部の例外を確認
                var ex = handle.OperationException;
                if (ex != null) throw ex.GetBaseException();
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

            // Addressablesの内部エラーがあったかを確認
            if (handle.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                handle.Release();
                throw handle.OperationException ?? new Exception("Unknown Exception.");
            }

            // AssetLifeTimeに追加
            assetLifeTime.AddAsyncOperationHandle(handle);

            // ロードしたアセットを返す
            return handle.Result;
        }

    }
}