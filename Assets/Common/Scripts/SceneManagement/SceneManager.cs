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
        public static CurrentSceneInfo Create<TContext>(SceneBase<TContext> scene, TContext context)
            where TContext : ISceneContext
        {
            return new CurrentSceneInfo(
                context.SceneName,
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

    private static Func<CancellationToken, UniTask>? DEFAULT_SCENE_LOAD_TASK_FACTORY = null;
    private static Stack<Func<CancellationToken, UniTask>> LOAD_SCENE_TASK_FACTORY_STACK = new();
    private static CurrentSceneInfo? CURRENT_SCENE_INFO;
    private static bool IS_LOADING = false;

    /// <summary>
    /// デフォルトのシーンコンテキストを設定する
    /// </summary>
    public static void SetDefaultSceneContext<TContext>(TContext context)
        where TContext : ISceneContext
    {
        if (DEFAULT_SCENE_LOAD_TASK_FACTORY != null)
        {
            throw new InvalidOperationException("デフォルトのシーンコンテキストは一度だけ設定できます。");
        }

        DEFAULT_SCENE_LOAD_TASK_FACTORY = ct => LoadAsync(context, ct);
    }

    /// <summary>
    /// シーンを読み込む
    /// </summary>
    public static async UniTask LoadAsync<TContext>(TContext context, CancellationToken cancellationToken = default)
        where TContext : ISceneContext
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
            var scene = await LoadCoreAsync(context, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 初期化
            await scene.InitializeAsync(context, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 初期化後の処理
            await scene.PostInitializeAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 現在のシーン情報を更新
            CURRENT_SCENE_INFO = CurrentSceneInfo.Create(scene, context);
            LOAD_SCENE_TASK_FACTORY_STACK.Push(ct => LoadAsync(context, ct));
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
                throw new InvalidOperationException("戻るシーンがありません。デフォルトのシーンコンテキストを設定してください。");
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

    private static async UniTask<SceneBase<TContext>> LoadCoreAsync<TContext>(TContext context, CancellationToken cancellationToken)
        where TContext : ISceneContext
    {
        // 新しいシーンをロードする
        await UnitySceneManager
            .LoadSceneAsync(context.SceneName)
            .ToUniTask(cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        // 新しいシーンを取得
        var scene = UnityEngine.Object.FindFirstObjectByType<SceneBase<TContext>>();
        if (scene == null)
        {
            throw new InvalidOperationException($"{context.SceneName}のシーンオブジェクトが見つかりません。シーンが正しく設定されているか確認してください。");
        }

        return scene;
    }
}