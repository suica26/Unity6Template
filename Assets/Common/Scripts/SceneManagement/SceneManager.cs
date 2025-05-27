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

    private static ISceneContext? DEFAULT_CONTEXT;
    private static Stack<ISceneContext> LOADED_SCENE_CONTEXTS = new();
    private static CurrentSceneInfo? CURRENT_SCENE_INFO;
    private static bool IS_LOADING = false;

    /// <summary>
    /// デフォルトのシーンコンテキストを設定する
    /// 1度のみ設定可能
    /// </summary>
    public static void SetDefaultContext(ISceneContext context)
    {
        if (DEFAULT_CONTEXT != null)
        {
            throw new InvalidOperationException("デフォルトコンテキストは一度だけ設定できます。");
        }

        DEFAULT_CONTEXT = context;
    }

    /// <summary>
    /// シーンを読み込む
    /// </summary>
    public static async UniTask LoadSceneAsync(ISceneContext context, CancellationToken ct)
    {
        await UniTask.WaitUntil(() => !IS_LOADING, cancellationToken: ct);
        ct.ThrowIfCancellationRequested();

        try
        {
            IS_LOADING = true;

            // 現在のシーンをアンロード
            await UnLoadCoreAsync(ct);
            ct.ThrowIfCancellationRequested();

            // 新しいシーンをロード
            var scene = await LoadCoreAsync(context, ct);
            ct.ThrowIfCancellationRequested();

            // 初期化
            await scene.InitializeAsync(context, ct);
            ct.ThrowIfCancellationRequested();

            // 初期化後の処理
            await scene.PostInitializeAsync(ct);
            ct.ThrowIfCancellationRequested();

            // 現在のシーン情報を更新
            CURRENT_SCENE_INFO = new CurrentSceneInfo(context, scene);
            LOADED_SCENE_CONTEXTS.Push(context);
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
    public static async UniTask BackAsync(CancellationToken ct)
    {
        if (LOADED_SCENE_CONTEXTS.Count <= 1)
        {
            if (DEFAULT_CONTEXT == null)
            {
                throw new InvalidOperationException("戻るシーンがありません。デフォルトのコンテキストを設定してください。");
            }
            else
            {
                // デフォルトシーンに戻る
                ClearStack();
                await LoadSceneAsync(DEFAULT_CONTEXT, ct);
                return;
            }
        }

        await UniTask.WaitUntil(() => !IS_LOADING, cancellationToken: ct);
        ct.ThrowIfCancellationRequested();

        try
        {
            IS_LOADING = true;

            // 現在のシーンをアンロード
            await UnLoadCoreAsync(ct);
            ct.ThrowIfCancellationRequested();

            // 前のシーンをロード
            LOADED_SCENE_CONTEXTS.Pop();
            var previousContext = LOADED_SCENE_CONTEXTS.Peek();

            var scene = await LoadCoreAsync(previousContext, ct);
            ct.ThrowIfCancellationRequested();

            // 現在のシーン情報を更新
            CURRENT_SCENE_INFO = new CurrentSceneInfo(previousContext, scene);
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
    public static void ClearStack() => LOADED_SCENE_CONTEXTS.Clear();

    private static async UniTask UnLoadCoreAsync(CancellationToken ct)
    {
        if (CURRENT_SCENE_INFO == null) return;

        // シーンを出る
        await CURRENT_SCENE_INFO.Scene.PreOutAsync(ct);
        ct.ThrowIfCancellationRequested();

        await CURRENT_SCENE_INFO.Scene.OnOutAsync(ct);
        ct.ThrowIfCancellationRequested();

        // シーンをアンロードする
        await UnitySceneManager
            .UnloadSceneAsync(CURRENT_SCENE_INFO.Context.SceneName)
            .ToUniTask(cancellationToken: ct);
        ct.ThrowIfCancellationRequested();

        GC.Collect();
    }

    private static async UniTask<IScene<ISceneContext>> LoadCoreAsync(ISceneContext context, CancellationToken ct)
    {
        // 新しいシーンをロードする
        await UnitySceneManager
            .LoadSceneAsync(context.SceneName)
            .ToUniTask(cancellationToken: ct);
        ct.ThrowIfCancellationRequested();

        // 新しいシーンを取得
        var scene = (IScene<ISceneContext>)UnityEngine.Object.FindFirstObjectByType<SceneBase<ISceneContext>>();
        if (scene == null)
        {
            throw new InvalidOperationException($"{context.SceneName} が見つかりませんでした。");
        }

        return scene;
    }
}