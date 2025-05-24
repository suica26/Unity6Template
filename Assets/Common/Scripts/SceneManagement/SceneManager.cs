using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Common.Scripts.SceneManagement;

/// <summary>
/// シーンを管理するクラス
/// </summary>
public static class SceneManager
{
    private static (string SceneName, SceneEntryPointBase EntryPoint)? CURRENT_SCENE_INFO;
    private static bool IS_LOADING = false;

    /// <summary>
    /// シーンを読み込む
    /// </summary>
    public static async UniTask LoadSceneAsync<TSceneEntryPoint>(
        Func<TSceneEntryPoint, UniTask>? onLoadedTask = null,
        CancellationToken cancellationToken = default
    )
        where TSceneEntryPoint : SceneEntryPointBase
    {
        if (cancellationToken == default)
        {
            cancellationToken = CancellationToken.None;
        }

        while (IS_LOADING)
        {
            // ロード中は待機
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Yield(cancellationToken);
        }

        IS_LOADING = true;
        try
        {
            // 現在のシーンをアンロード
            cancellationToken.ThrowIfCancellationRequested();
            await UnLoadCoreAsync(cancellationToken);

            // 新しいシーンをロード
            cancellationToken.ThrowIfCancellationRequested();
            await LoadCoreAsync(cancellationToken, onLoadedTask);
        }
        catch (Exception e)
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
        if (CURRENT_SCENE_INFO == null)
        {
            return;
        }

        var (sceneName, entryPoint) = CURRENT_SCENE_INFO.Value;

        // シーンを出る
        await entryPoint.PreOutAsync(cancellationToken);
        await entryPoint.OnOutAsync(cancellationToken);

        // シーンをアンロードする
        await UnitySceneManager
            .UnloadSceneAsync(sceneName)
            .ToUniTask(cancellationToken: cancellationToken);

        GC.Collect();
    }

    private static async UniTask LoadCoreAsync<TSceneEntryPoint>(
        CancellationToken cancellationToken,
        Func<TSceneEntryPoint, UniTask>? onLoadedTask = null
    )
        where TSceneEntryPoint : SceneEntryPointBase
    {
        // 新しいシーンをロードする
        var sceneName = typeof(TSceneEntryPoint).Name.Replace("EntryPoint", string.Empty);
        await UnitySceneManager
            .LoadSceneAsync(sceneName)
            .ToUniTask(cancellationToken: cancellationToken);

        // 新しいシーンのエントリーポイントを取得
        var entryPoint = UnityEngine.Object.FindFirstObjectByType<TSceneEntryPoint>();
        if (entryPoint == null)
        {
            throw new InvalidOperationException($"シーンエントリポイントが見つかりませんでした: {sceneName}");
        }

        // ロード時の処理を実行
        if (onLoadedTask != null)
        {
            await onLoadedTask(entryPoint);
        }
        await entryPoint.PostLoadAsync(cancellationToken);

        // 現在のシーン情報を更新
        CURRENT_SCENE_INFO = (sceneName, entryPoint);
    }
}