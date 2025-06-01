using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Common.Scripts.SceneManagement;

/// <summary>
/// シーンを管理するクラス
/// </summary>
public static class SceneManager
{
    private readonly struct CurrentSceneInfo
    {
        public readonly string SceneName;
        public readonly Func<CancellationToken, UniTask> OnOutTaskFactory;

        /// <summary>
        /// 現在のシーン情報を作成する
        /// </summary>
        public static CurrentSceneInfo Create<TSceneBase>(TSceneBase scene) where TSceneBase : SceneBase
        {
            return new CurrentSceneInfo(
                SceneHelper.GetSceneFileName<TSceneBase>(),
                async (CancellationToken ct) =>
                {
                    await scene.PreOutAsync(ct);
                    ct.ThrowIfCancellationRequested();

                    await scene.OnOutAsync(ct);
                    ct.ThrowIfCancellationRequested();
                }
            );
        }

        private CurrentSceneInfo(string sceneName, Func<CancellationToken, UniTask> onOutTaskFactory)
        {
            SceneName = sceneName;
            OnOutTaskFactory = onOutTaskFactory;
        }
    }

    private static Stack<Func<CancellationToken, UniTask>> LOAD_SCENE_TASK_FACTORY_STACK = new();
    private static Func<CancellationToken, UniTask>? DEFAULT_SCENE_LOAD_TASK_FACTORY = null;
    private static CurrentSceneInfo? CURRENT_SCENE_INFO;
    private static bool IS_LOADING = false;

    /// <summary>
    /// デフォルトのシーンを設定する
    /// </summary>
    public static void SetDefaultScene<TSceneBase>(Func<TSceneBase, CancellationToken, UniTask>? initializationTaskFactory = null)
        where TSceneBase : SceneBase
    {
        DEFAULT_SCENE_LOAD_TASK_FACTORY = ct => LoadAsync(initializationTaskFactory, cancellationToken: ct);
    }

    /// <summary>
    /// シーンを読み込む
    /// </summary>
    public static async UniTask LoadAsync<TSceneBase>(
        Func<TSceneBase, CancellationToken, UniTask>? initializationTaskFactory = null,
        CancellationToken cancellationToken = default
    )
        where TSceneBase : SceneBase
    {
        if (cancellationToken == default) cancellationToken = Application.exitCancellationToken;

        await WaitUntilFinishLoadingAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            IS_LOADING = true;

            // 現在のシーンの遷移時処理を実行
            await OnOutAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 新しいシーンをロード
            var scene = await LoadCoreAsync<TSceneBase>(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 初期化
            if (initializationTaskFactory != null) await initializationTaskFactory(scene, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 初期化後の処理
            await scene.PostInitializeAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 現在のシーン情報を更新
            CURRENT_SCENE_INFO = CurrentSceneInfo.Create(scene);
            LOAD_SCENE_TASK_FACTORY_STACK.Push(ct => LoadAsync(initializationTaskFactory, ct));
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            throw e;
        }
        finally
        {
            IS_LOADING = false;
        }
    }

    /// <summary>
    /// 1つ前のシーンに戻る
    /// </summary>
    public static async UniTask BackAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken == default) cancellationToken = Application.exitCancellationToken;

        if (LOAD_SCENE_TASK_FACTORY_STACK.Count < 2)
        {
            if (DEFAULT_SCENE_LOAD_TASK_FACTORY == null)
            {
                throw new InvalidOperationException("戻るシーンがありません。デフォルトのシーンを設定してください。");
            }
            else
            {
                ClearStack();
                await DEFAULT_SCENE_LOAD_TASK_FACTORY(cancellationToken);
                return;
            }
        }

        await WaitUntilFinishLoadingAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // 1つ前のシーンをロード
            LOAD_SCENE_TASK_FACTORY_STACK.Pop(); // 現在のシーンのタスクファクトリを削除
            var beforeSceneLoadTaskFactory = LOAD_SCENE_TASK_FACTORY_STACK.Pop()!;
            await beforeSceneLoadTaskFactory(cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            throw e;
        }
        finally
        {
            IS_LOADING = false;
        }
    }

    /// <summary>
    /// シーンスタックをクリアする
    /// </summary>
    public static void ClearStack() => LOAD_SCENE_TASK_FACTORY_STACK.Clear();

    private static async UniTask WaitUntilFinishLoadingAsync(CancellationToken cancellationToken)
    {
        if (!IS_LOADING) return;

        Debug.LogWarning("シーンのロード中です。前のロードが完了するまで待機します。");
        await UniTask.WaitUntil(() => !IS_LOADING, cancellationToken: cancellationToken);
    }

    private static UniTask OnOutAsync(CancellationToken cancellationToken)
    {
        if (CURRENT_SCENE_INFO == null) return UniTask.CompletedTask;

        // 現在のシーンの遷移時処理を実行
        return CURRENT_SCENE_INFO.Value.OnOutTaskFactory(cancellationToken);
    }

    private static async UniTask<TSceneBase> LoadCoreAsync<TSceneBase>(CancellationToken cancellationToken)
        where TSceneBase : SceneBase
    {
        var sceneName = SceneHelper.GetSceneFileName<TSceneBase>();

        // 新しいシーンをロードする
        await UnitySceneManager
            .LoadSceneAsync(sceneName)
            .ToUniTask(cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        // 新しいシーンを取得
        var scene = UnityEngine.Object.FindFirstObjectByType<TSceneBase>();
        if (scene == null)
        {
            throw new InvalidOperationException($"{sceneName}のシーンオブジェクトが見つかりません。シーンクラス名が正しく設定されているか確認してください。");
        }

        return scene;
    }
}