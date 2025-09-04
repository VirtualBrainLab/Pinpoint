using UnityEditor;
using UnityEngine;

namespace Utils
{
    public abstract class ResettingScriptableObject : ScriptableObject
    {
#if UNITY_EDITOR
        private string _initialJson = string.Empty;

        private void OnEnable()
        {
            if (Application.isPlaying)
                return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            _initialJson = EditorJsonUtility.ToJson(this);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode)
                return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorJsonUtility.FromJsonOverwrite(_initialJson, this);
        }
#endif
    }
}
