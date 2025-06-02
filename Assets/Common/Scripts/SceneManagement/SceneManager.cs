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
        public static CurrentSceneInfo Create<TSceneBase, TContext>(TSceneBase scene)
            where TSceneBase : SceneBase<TContext>
            where TContext : ISceneContext
        {
            return new CurrentSceneInfo(
                SceneHelper.GetSceneFileName<TSceneBase, TContext>(),
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

    private static Stack<Func<UniTask>> LOAD_SCENE_TASK_FACTORY_STACK = new();
    private static Func<UniTask>? DEFAULT_SCENE_LOAD_TASK_FACTORY = null;
    private static CurrentSceneInfo? CURRENT_SCENE_INFO;
    private static bool IS_LOADING = false;

    /// <summary>
    /// デフォルトのシーンを設定する(コンテキスト省略版)
    /// デフォルトのシーンは、シーンスタックが無い場合の戻る操作に使用される
    /// </summary>
    public static void SetDefaultScene<TSceneBase>() where TSceneBase : SceneBase
    {
        DEFAULT_SCENE_LOAD_TASK_FACTORY = (LoadAsync<TSceneBase>);
    }

    /// <summary>
    /// デフォルトのシーンを設定する
    /// デフォルトのシーンは、シーンスタックが無い場合の戻る操作に使用される
    /// </summary>
    public static void SetDefaultScene<TSceneBase, TContext>(TContext context)
        where TSceneBase : SceneBase<TContext>
        where TContext : ISceneContext
    {
        DEFAULT_SCENE_LOAD_TASK_FACTORY = () => LoadAsync<TSceneBase, TContext>(context);
    }

    /// <summary>
    /// シーンを読み込む(コンテキスト省略版)
    /// </summary>
    public static UniTask LoadAsync<TSceneBase>() where TSceneBase : SceneBase
        => LoadAsync<TSceneBase, DefaultContext>(new DefaultContext());

    /// <summary>
    /// シーンを読み込む
    /// </summary>
    public static async UniTask LoadAsync<TSceneBase, TContext>(TContext context)
        where TSceneBase : SceneBase<TContext>
        where TContext : ISceneContext
    {
        var cancellationToken = Application.exitCancellationToken;


        try
        {
            await WaitUntilLoadingCompleteAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IS_LOADING = true;

            // 現在のシーンの遷移時処理を実行
            await OnOutAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 新しいシーンをロード
            var scene = await LoadCoreAsync<TSceneBase, TContext>(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 初期化
            await scene.InitializeAsync(context, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 初期化後の処理
            await scene.PostInitializeAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 現在のシーン情報を更新
            CURRENT_SCENE_INFO = CurrentSceneInfo.Create<TSceneBase, TContext>(scene);
            LOAD_SCENE_TASK_FACTORY_STACK.Push(() => LoadAsync<TSceneBase, TContext>(context));
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
    public static async UniTask BackAsync()
    {
        var cancellationToken = Application.exitCancellationToken;

        if (LOAD_SCENE_TASK_FACTORY_STACK.Count < 2)
        {
            if (DEFAULT_SCENE_LOAD_TASK_FACTORY == null)
            {
                throw new InvalidOperationException("戻るシーンがありません。デフォルトのシーンを設定してください。");
            }
            else
            {
                ClearStack();
                await DEFAULT_SCENE_LOAD_TASK_FACTORY();
                return;
            }
        }

        try
        {
            await WaitUntilLoadingCompleteAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IS_LOADING = true;

            // 1つ前のシーンをロード
            LOAD_SCENE_TASK_FACTORY_STACK.Pop(); // 現在のシーンのタスクファクトリを削除
            var beforeSceneLoadTaskFactory = LOAD_SCENE_TASK_FACTORY_STACK.Pop()!;
            await beforeSceneLoadTaskFactory();
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

    private static UniTask OnOutAsync(CancellationToken cancellationToken)
    {
        if (CURRENT_SCENE_INFO == null) return UniTask.CompletedTask;

        // 現在のシーンの遷移時処理を実行
        return CURRENT_SCENE_INFO.Value.OnOutTaskFactory(cancellationToken);
    }

    private static async UniTask<TSceneBase> LoadCoreAsync<TSceneBase, TContext>(CancellationToken cancellationToken)
        where TSceneBase : SceneBase<TContext>
        where TContext : ISceneContext
    {
        var sceneName = SceneHelper.GetSceneFileName<TSceneBase, TContext>();

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

    private static UniTask WaitUntilLoadingCompleteAsync(CancellationToken cancellationToken)
    {
        if (!IS_LOADING) return UniTask.CompletedTask;

        Debug.LogWarning("シーンのロード中です。前のロードが完了するまで待機します。");
        return UniTask.WaitUntil(() => !IS_LOADING, cancellationToken: cancellationToken);
    }
}