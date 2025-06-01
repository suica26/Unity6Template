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
        public static string GetSceneFileName<TSceneBase>() where TSceneBase : SceneBase
        {
            return GetSceneFileName(typeof(TSceneBase).Name);
        }

        /// <summary>
        /// シーンファイル名を取得する
        /// クラス名から "Scene" を除いたものを返す
        /// </summary>
        public static string GetSceneFileName(SceneBase scene)
        {
            return GetSceneFileName(scene.GetType().Name);
        }

        private static string GetSceneFileName(string sceneClassName)
        {
            return sceneClassName.Replace("Scene", string.Empty);
        }
    }
}