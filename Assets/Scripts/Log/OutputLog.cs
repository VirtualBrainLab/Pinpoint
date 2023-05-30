using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;

public class OutputLog : MonoBehaviour
{
    #region Webgl only
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void Copy2Clipboard(string str);
#endif
    #endregion

    public static OutputLog Instance;

    [SerializeField] private TMP_InputField _logField;

    private void Awake()
    {
        if (Instance != null)
            throw new Exception("There should only be one OutputLog in the scene");
        Instance = this;
    }

    /// <summary>
    /// Log data to the internal log
    /// </summary>
    /// <param name="data">Columns of data, the first value should be the data type</param>
    public void Log(IEnumerable<string> data)
    {
        Instance._logField.text += $"{string.Join(',',data)}\n";
    }

    public void CopyText2Clipboard()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Copy2Clipboard(_logField.text);
#else
        GUIUtility.systemCopyBuffer = _logField.text;
#endif
    }
}
