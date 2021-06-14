#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Balancy.Editor
{
    [ExecuteInEditMode]
    public class EditorCoroutineHelper : MonoBehaviour
    {
        public static Coroutine Execute(IEnumerator enumerator)
        {
            var helper = Create();
            return helper.LaunchCoroutine(enumerator);
        }

        public static EditorCoroutineHelper Create()
        {
            var obj = new GameObject("temp");
            return obj.AddComponent<EditorCoroutineHelper>();
        }
        
        public Coroutine LaunchCoroutine(IEnumerator enumerator)
        {
            return StartCoroutine(DoLogic(enumerator));
        }

        private void OnEnable()
        {
            EditorApplication.update += update;
        }

        private void OnDisable()
        {
            EditorApplication.update -= update;
        }

        void update()
        {
            EditorUtility.SetDirty(gameObject);
        }

        IEnumerator DoLogic(IEnumerator enumerator)
        {
            yield return enumerator;

            DestroyImmediate(gameObject);
        }
    }
}
#endif