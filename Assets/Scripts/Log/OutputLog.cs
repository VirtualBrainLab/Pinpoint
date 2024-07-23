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

#if UNITY_EDITOR
    private float _lastLogTime;
#endif
    private const float LOG_TIME_WARNING = 1f;
    private const int MAX_LOG_LINES = 60;
    private bool _warned;

#if !UNITY_WEBGL
    private string logPath =
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Pinpoint";
    private DateTime logStartTime = DateTime.Now;
#endif

    private void Awake()
    {
        if (Instance != null)
            throw new Exception("There should only be one OutputLog in the scene");
        Instance = this;

        _log = new();

#if UNITY_EDITOR
        _lastLogTime = float.MinValue;
#endif
#if !UNITY_WEBGL
        // Create log directory if it doesn't exist.
        System.IO.Directory.CreateDirectory(logPath);
#endif
    }

    /// <summary>
    /// Log data to the internal log
    /// </summary>
    /// <param name="data">Columns of data, the first value should be the data type</param>
    public static void Log(IEnumerable<string> data)
    {
#if UNITY_EDITOR
        if (!Instance._warned && (Time.realtimeSinceStartup - Instance._lastLogTime) < LOG_TIME_WARNING)
        {
            Debug.LogWarning("(OutputLog) You are logging data too quickly -- this could eventually cause memory problems");
            Instance._warned = true;
        }
        Instance._lastLogTime = Time.realtimeSinceStartup;
#endif
        var logLine = string.Join(',', data);

        Instance._log.Add(logLine);
        Instance.UpdateLogText();

#if !UNITY_WEBGL
        // Log to file.
        var fileName =
            Instance.logPath
            + "/log_"
            + Instance.logStartTime.ToString("yyyy-MM-dd_HH-mm-ss")
            + ".csv";
        System.IO.File.AppendAllText(fileName, logLine + "\n");
#endif
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
