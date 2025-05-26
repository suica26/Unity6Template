using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace AssetManagement
{
    /// <summary>
    /// アセットのライフタイムをまとめて管理・解放するもの
    /// </summary>
    public abstract class AssetLifeTimeBase : IDisposable
    {
        private AsyncOperationHandle[]? _handles;
        private int _count;
        private bool _isDisposed;

        /// <summary>
        /// AsyncOperationHandleを追加
        /// </summary>
        internal void AddAsyncOperationHandle(AsyncOperationHandle handle)
        {
            if (_handles == null)
            {
                _handles = new AsyncOperationHandle[4];
            }
            else if (_handles.Length == _count)
            {
                Array.Resize(ref _handles, _handles.Length * 2);
            }
            _handles[_count++] = handle;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            for (int i = 0; i < _count; i++)
            {
                _handles?[i].Release();
            }
        }
    }

    /// <summary>
    /// アセットのライフタイムを手動でDisposeするまでにする
    /// </summary>
    public class AssetLifeTime : AssetLifeTimeBase { }

    /// <summary>
    /// アセットのライフタイムをゲームオブジェクトに紐づける
    /// </summary>
    public class GameObjectLinkedAssetLifeTime : AssetLifeTimeBase
    {
        public GameObjectLinkedAssetLifeTime(GameObject gameObject)
        {
            gameObject.GetCancellationTokenOnDestroy()
            .RegisterWithoutCaptureExecutionContext(Dispose);
        }
    }

    /// <summary>
    /// アセットのライフタイムをシーンに紐づける
    /// </summary>
    public class SceneLinkedAssetLifeTime : AssetLifeTimeBase
    {
        private string _sceneName;

        public SceneLinkedAssetLifeTime(string sceneName)
        {
            _sceneName = sceneName;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (scene.name == _sceneName)
            {
                Dispose();
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
            }
        }
    }
}