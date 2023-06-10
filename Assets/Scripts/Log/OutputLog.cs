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

    private List<string> _log;
    [SerializeField] private TMP_InputField _logField;

    private float _lastLogTime;
    private const float LOG_TIME_WARNING = 1f;
    private const int MAX_LOG_LINES = 60;

    private void Awake()
    {
        if (Instance != null)
            throw new Exception("There should only be one OutputLog in the scene");
        Instance = this;

        _log = new();
        _lastLogTime = float.MinValue;
    }

    /// <summary>
    /// Log data to the internal log
    /// </summary>
    /// <param name="data">Columns of data, the first value should be the data type</param>
    public static void Log(IEnumerable<string> data)
    {
        if ((Time.realtimeSinceStartup - Instance._lastLogTime) < LOG_TIME_WARNING)
            Debug.LogWarning("(OutputLog) You are logging data too quickly -- this could eventually cause memory problems");
        Instance._lastLogTime = Time.realtimeSinceStartup;

        Instance._log.Add(string.Join(',', data));
        Instance.UpdateLogText();
    }

    public void UpdateLogText()
    {
        Instance._logField.text = "";

        // strip only the last 50 lines
        for (int i = Mathf.Max(0, _log.Count - MAX_LOG_LINES); i < _log.Count; i++)
            Instance._logField.text += $"{_log[i]}\n";
    }

    public void CopyText2Clipboard()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Copy2Clipboard(string.Join("\n", _log));
#else
        GUIUtility.systemCopyBuffer = string.Join("\n", _log);
#endif
    }
}
