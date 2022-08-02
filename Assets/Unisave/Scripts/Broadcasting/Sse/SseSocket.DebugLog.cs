using System.Text;
using UnityEngine;

#if UNITY_EDITOR

namespace Unisave.Broadcasting.Sse
{
    public partial class SseSocket
    {
        /// <summary>
        /// Check this in the inspector to see the detailed communication log
        /// </summary>
        public bool displayDebugLog = false;

        private readonly StringBuilder debugLog = new StringBuilder();

        void AppendToDebugLog(string text)
        {
            const int maxLength = 1024 * 10;
            
            debugLog.Append(text);

            if (debugLog.Length > maxLength)
                debugLog.Remove(0, debugLog.Length - maxLength);
        }
        
        void OnGUI()
        {
            if (!displayDebugLog)
                return;

            GUI.Label(
                new Rect(20, 20, Screen.width - 40, Screen.height - 40),
                debugLog.ToString(),
                new GUIStyle(GUI.skin.textArea) {
                    alignment = TextAnchor.LowerLeft
                }
            );
        }
    }
}

#endif
