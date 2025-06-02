namespace Common.Scripts.SceneManagement
{
    /// <summary>
    /// シーンヘルパークラス
    /// </summary>
    public static class SceneHelper
    {
        /// <summary>
        /// シーンファイル名を取得する
        /// クラス名から "Scene" を除いたものを返す
        /// </summary>
        public static string GetSceneFileName<TSceneBase, TContext>()
            where TSceneBase : SceneBase<TContext>
            where TContext : ISceneContext
        {
            return GetSceneFileName(typeof(TSceneBase).Name);
        }

        /// <summary>
        /// シーンファイル名を取得する(コンテキスト省略版)
        /// クラス名から "Scene" を除いたものを返す
        /// </summary>
        public static string GetSceneFileName<TSceneBase>() where TSceneBase : SceneBase
        {
            return GetSceneFileName(typeof(TSceneBase).Name);
        }

        /// <summary>
        /// シーンファイル名を取得する
        /// クラス名から "Scene" を除いたものを返す
        /// </summary>
        public static string GetSceneFileName<TSceneBase, TContext>(TSceneBase scene)
            where TSceneBase : SceneBase<TContext>
            where TContext : ISceneContext
        {
            return GetSceneFileName(scene.GetType().Name);
        }

        /// <summary>
        /// シーンファイル名を取得する(コンテキスト省略版)  
        /// クラス名から "Scene" を除いたものを返す
        /// </summary>
        public static string GetSceneFileName<TSceneBase>(TSceneBase scene) where TSceneBase : SceneBase
        {
            return GetSceneFileName(scene.GetType().Name);
        }

        private static string GetSceneFileName(string sceneClassName)
        {
            return sceneClassName.Replace("Scene", string.Empty);
        }
    }
}