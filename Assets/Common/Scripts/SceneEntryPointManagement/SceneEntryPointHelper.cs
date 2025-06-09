namespace SceneEntryPointManagement
{
    /// <summary>
    /// シーンエントリーポイントヘルパークラス
    /// </summary>
    public static class SceneEntryPointHelper
    {
        /// <summary>
        /// シーンファイル名を取得する
        /// クラス名から "EntryPoint" を除いたものを返す
        /// </summary>
        public static string GetSceneFileName<TSceneEntryPoint, TContext>()
            where TSceneEntryPoint : SceneEntryPointBase<TContext>
            where TContext : ISceneEntryPointContext
        {
            return GetSceneFileName(typeof(TSceneEntryPoint).Name);
        }

        /// <summary>
        /// シーンファイル名を取得する(コンテキスト省略版)
        /// クラス名から "EntryPoint" を除いたものを返す
        /// </summary>
        public static string GetSceneFileName<TSceneEntryPoint>() where TSceneEntryPoint : SceneEntryPointBase
        {
            return GetSceneFileName(typeof(TSceneEntryPoint).Name);
        }

        /// <summary>
        /// シーンファイル名を取得する
        /// クラス名から "EntryPoint" を除いたものを返す
        /// </summary>
        public static string GetSceneFileName<TSceneEntryPoint, TContext>(TSceneEntryPoint scene)
            where TSceneEntryPoint : SceneEntryPointBase<TContext>
            where TContext : ISceneEntryPointContext
        {
            return GetSceneFileName(scene.GetType().Name);
        }

        /// <summary>
        /// シーンファイル名を取得する(コンテキスト省略版)  
        /// クラス名から "EntryPoint" を除いたものを返す
        /// </summary>
        public static string GetSceneFileName<TSceneEntryPoint>(TSceneEntryPoint scene) where TSceneEntryPoint : SceneEntryPointBase
        {
            return GetSceneFileName(scene.GetType().Name);
        }

        private static string GetSceneFileName(string sceneClassName)
        {
            return sceneClassName.Replace("EntryPoint", string.Empty);
        }
    }
}