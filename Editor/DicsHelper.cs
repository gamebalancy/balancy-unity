using System;
using System.Collections;
using UnityEditor;
using Balancy.Dictionaries;

namespace Balancy.Editor {

    public class DicsHelper : EditorWindow {

        private static Loader m_Loader;
        private static IEnumerator m_Coroutine;

        public static void LoadDocs(AppConfig settings, Action<ResponseData> onCompleted, Action<string, float> onProgress) {
            
            m_Loader = new Loader(settings, true);

            var helper = EditorCoroutineHelper.Create();
            
            m_Coroutine = m_Loader.Load(helper, responseData =>
            {
                AssetDatabase.Refresh();
                if (onCompleted != null)
                    onCompleted(responseData);
            }, onProgress);

            helper.LaunchCoroutine(m_Coroutine);
        }
    }
}