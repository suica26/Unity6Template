using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Common.Scripts.SceneManagement;

/// <summary>
/// シーンを管理するクラス
/// </summary>
public static class SceneManager
{
    private record CurrentSceneInfo(ISceneContext Context, IScene<ISceneContext> Scene);

    private static Stack<ISceneContext> LOADED_SCENE_CONTEXTS = new();
    private static CurrentSceneInfo? CURRENT_SCENE_INFO;
    private static bool IS_LOADING = false;

    /// <summary>
    /// シーンを読み込む
    /// </summary>
    public static async UniTask LoadSceneAsync<TScene, TContext>(
        TContext context,
        CancellationToken cancellationToken = default
    )
        where TScene : SceneBase<TContext>
        where TContext : ISceneContext
    {
        if (cancellationToken == default) cancellationToken = CancellationToken.None;

        await UniTask.WaitUntil(() => !IS_LOADING, cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            IS_LOADING = true;

            // 現在のシーンをアンロード
            await UnLoadCoreAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 新しいシーンをロード
            await LoadCoreAsync<TScene, TContext>(context, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
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

    private static async UniTask UnLoadCoreAsync(CancellationToken cancellationToken)
    {
        if (CURRENT_SCENE_INFO == null) return;

        // シーンを出る
        await CURRENT_SCENE_INFO.Scene.PreOutAsync(cancellationToken);
        await CURRENT_SCENE_INFO.Scene.OnOutAsync(cancellationToken);

        // シーンをアンロードする
        await UnitySceneManager
            .UnloadSceneAsync(CURRENT_SCENE_INFO.Context.SceneName)
            .ToUniTask(cancellationToken: cancellationToken);

        GC.Collect();
    }

    private static async UniTask LoadCoreAsync<TScene, TContext>(
        TContext context,
        CancellationToken cancellationToken
    )
        where TScene : IScene<TContext>
        where TContext : ISceneContext
    {
        // 新しいシーンをロードする
        await UnitySceneManager
            .LoadSceneAsync(context.SceneName)
            .ToUniTask(cancellationToken: cancellationToken);

        // 新しいシーンを取得
        var scene = (IScene<ISceneContext>)UnityEngine.Object.FindFirstObjectByType<SceneBase<TContext>>();
        if (scene == null)
        {
            throw new InvalidOperationException($"シーンエントリポイントが見つかりませんでした: {context.SceneName}");
        }

        // ロード時の処理を実行
        await scene.InitializeAsync(context);
        await scene.PostInitializeAsync(cancellationToken);

        // 現在のシーン情報を更新
        CURRENT_SCENE_INFO = new CurrentSceneInfo(context, scene);
        LOADED_SCENE_CONTEXTS.Push(context);
    }
}