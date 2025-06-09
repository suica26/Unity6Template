using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace SceneEntryPointManagement;

/// <summary>
/// シーンエントリーポイントを管理するクラス
/// </summary>
public static class SceneEntryPointManager
{
    private readonly struct CurrentSceneEntryPointInfo
    {
        public readonly string SceneFileName;
        public readonly Func<CancellationToken, UniTask> OnOutTaskFactory;

        /// <summary>
        /// 現在のシーンエントリーポイント情報を作成する
        /// </summary>
        public static CurrentSceneEntryPointInfo Create<TSceneEntryPoint, TContext>(TSceneEntryPoint sceneEntryPoint)
            where TSceneEntryPoint : SceneEntryPointBase<TContext>
            where TContext : ISceneEntryPointContext
        {
            return new CurrentSceneEntryPointInfo(
                SceneEntryPointHelper.GetSceneFileName<TSceneEntryPoint, TContext>(),
                async (CancellationToken ct) =>
                {
                    await sceneEntryPoint.PreOutAsync(ct);
                    ct.ThrowIfCancellationRequested();

                    await sceneEntryPoint.OnOutAsync(ct);
                    ct.ThrowIfCancellationRequested();
                }
            );
        }

        private CurrentSceneEntryPointInfo(string sceneFileName, Func<CancellationToken, UniTask> onOutTaskFactory)
        {
            SceneFileName = sceneFileName;
            OnOutTaskFactory = onOutTaskFactory;
        }
    }

    private static Stack<Func<UniTask>> LOAD_SCENE_ENTRY_POINT_TASK_FACTORY_STACK = new();
    private static Func<UniTask>? DEFAULT_SCENE_ENTRY_POINT_LOAD_TASK_FACTORY = null;
    private static CurrentSceneEntryPointInfo? CURRENT_SCENE_ENTRY_POINT_INFO;
    private static bool IS_LOADING = false;

    /// <summary>
    /// デフォルトのシーンエントリーポイントを設定する(コンテキスト省略版)
    /// デフォルトのシーンエントリーポイントは、シーンエントリーポイントスタックが無い場合の戻る操作に使用される
    /// </summary>
    public static void SetDefault<TSceneEntryPoint>() where TSceneEntryPoint : SceneEntryPointBase
    {
        DEFAULT_SCENE_ENTRY_POINT_LOAD_TASK_FACTORY = (LoadAsync<TSceneEntryPoint>);
    }

    /// <summary>
    /// デフォルトのシーンエントリーポイントを設定する
    /// デフォルトのシーンエントリーポイントは、シーンエントリーポイントスタックが無い場合の戻る操作に使用される
    /// </summary>
    public static void SetDefault<TSceneEntryPoint, TContext>(TContext context)
        where TSceneEntryPoint : SceneEntryPointBase<TContext>
        where TContext : ISceneEntryPointContext
    {
        DEFAULT_SCENE_ENTRY_POINT_LOAD_TASK_FACTORY = () => LoadAsync<TSceneEntryPoint, TContext>(context);
    }

    /// <summary>
    /// シーンエントリーポイントを読み込む(コンテキスト省略版)
    /// </summary>
    public static UniTask LoadAsync<TSceneEntryPoint>() where TSceneEntryPoint : SceneEntryPointBase
        => LoadAsync<TSceneEntryPoint, DefaultContext>(new DefaultContext());

    /// <summary>
    /// シーンエントリーポイントを読み込む
    /// </summary>
    public static async UniTask LoadAsync<TSceneEntryPoint, TContext>(TContext context)
        where TSceneEntryPoint : SceneEntryPointBase<TContext>
        where TContext : ISceneEntryPointContext
    {
        var cancellationToken = Application.exitCancellationToken;

        try
        {
            await WaitUntilLoadingCompleteAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IS_LOADING = true;

            // 現在のシーンエントリーポイントの遷移時処理を実行
            await OnOutAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 新しいシーンエントリーポイントをロード
            var scene = await LoadCoreAsync<TSceneEntryPoint, TContext>(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 初期化
            await scene.InitializeAsync(context, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 初期化後の処理
            await scene.PostInitializeAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 現在のシーンエントリーポイント情報を更新
            CURRENT_SCENE_ENTRY_POINT_INFO = CurrentSceneEntryPointInfo.Create<TSceneEntryPoint, TContext>(scene);
            LOAD_SCENE_ENTRY_POINT_TASK_FACTORY_STACK.Push(() => LoadAsync<TSceneEntryPoint, TContext>(context));
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
    /// 1つ前のシーンエントリーポイントに戻る
    /// </summary>
    public static async UniTask BackAsync()
    {
        var cancellationToken = Application.exitCancellationToken;

        if (LOAD_SCENE_ENTRY_POINT_TASK_FACTORY_STACK.Count < 2)
        {
            if (DEFAULT_SCENE_ENTRY_POINT_LOAD_TASK_FACTORY == null)
            {
                throw new InvalidOperationException("戻るシーンエントリーポイントがありません。デフォルトのシーンエントリーポイントを設定してください。");
            }
            else
            {
                ClearStack();
                await DEFAULT_SCENE_ENTRY_POINT_LOAD_TASK_FACTORY();
                return;
            }
        }

        try
        {
            await WaitUntilLoadingCompleteAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 1つ前のシーンエントリーポイントをロード
            LOAD_SCENE_ENTRY_POINT_TASK_FACTORY_STACK.Pop(); // 現在のシーンエントリーポイントのタスクファクトリを削除
            var beforeSceneLoadTaskFactory = LOAD_SCENE_ENTRY_POINT_TASK_FACTORY_STACK.Pop()!;
            await beforeSceneLoadTaskFactory();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            throw e;
        }
    }

    /// <summary>
    /// シーンエントリーポイントスタックをクリアする
    /// </summary>
    public static void ClearStack() => LOAD_SCENE_ENTRY_POINT_TASK_FACTORY_STACK.Clear();

    private static UniTask OnOutAsync(CancellationToken cancellationToken)
    {
        if (CURRENT_SCENE_ENTRY_POINT_INFO == null) return UniTask.CompletedTask;

        // 現在のシーンエントリーポイントの遷移時処理を実行
        return CURRENT_SCENE_ENTRY_POINT_INFO.Value.OnOutTaskFactory(cancellationToken);
    }

    private static async UniTask<TSceneEntryPoint> LoadCoreAsync<TSceneEntryPoint, TContext>(CancellationToken cancellationToken)
        where TSceneEntryPoint : SceneEntryPointBase<TContext>
        where TContext : ISceneEntryPointContext
    {
        var sceneName = SceneEntryPointHelper.GetSceneFileName<TSceneEntryPoint, TContext>();

        // 新しいシーンエントリーポイントをロードする
        await UnityEngine.SceneManagement.SceneManager
            .LoadSceneAsync(sceneName)
            .ToUniTask(cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        // 新しいシーンエントリーポイントを取得
        var scene = UnityEngine.Object.FindFirstObjectByType<TSceneEntryPoint>();
        if (scene == null)
        {
            throw new InvalidOperationException($"{sceneName}のシーンエントリーポイントオブジェクトが見つかりません。シーンエントリーポイントクラス名が正しく設定されているか確認してください。");
        }

        return scene;
    }

    private static UniTask WaitUntilLoadingCompleteAsync(CancellationToken cancellationToken)
    {
        if (!IS_LOADING) return UniTask.CompletedTask;

        Debug.LogWarning("シーンエントリーポイントのロード中です。前のロードが完了するまで待機します。");
        return UniTask.WaitUntil(() => !IS_LOADING, cancellationToken: cancellationToken);
    }
}