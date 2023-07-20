using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public class APIManager : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void Copy2Clipboard(string str);
#endif

    #region static
    public static APIManager Instance;
#endregion

#region exposed fields
    [SerializeField] APISpikeGLX _spikeGLXAPI;
    [SerializeField] APIOpenEphys _openEphysAPI;
    [SerializeField] TMP_Text _statusText;
#endregion

#region Probe data variables
    private float _lastDataSend;
    private const float DATA_SEND_RATE = 10f; // cap data sending at once per 10 s maximum, it's fairly expensive to do
    private bool _dirty = false;

    public UnityEvent<List<string>> ProbeOptionsChangedEvent;
#endregion


#region Unity
    private void Awake()
    {
        if (Instance != null)
            throw new Exception("(APIManager) Singleton should only be created once");
        Instance = this;
        
        _lastDataSend = float.MinValue;
    }

    private void Update()
    {
        if (_dirty && (Time.realtimeSinceStartup > (_lastDataSend + DATA_SEND_RATE)))
        {
            _dirty = false;
            _lastDataSend = Time.realtimeSinceStartup;

            if (_spikeGLXAPI.isActiveAndEnabled)
                _spikeGLXAPI.SendData();

            if (_openEphysAPI.isActiveAndEnabled)
                _openEphysAPI.SendData();
        }
    }
    #endregion

    #region Static update functions

    /// <summary>
    /// Trigger the API to send the Probe data to the current http server target
    /// </summary>
    public static void TriggerAPIPush()
    {
        Instance._dirty = true;
    }

    /// <summary>
    /// Reset the timer, forces an immediate data send the next time you call TriggerAPIPush
    /// </summary>
    public static void ResetTimer()
    {
        Instance._dirty = false;
        Instance._lastDataSend = float.MinValue;
    }

    public static void UpdateStatusText(string status)
    {
        Instance._statusText.text = $"API Status: {status}";
    }

    public static void SetStatusDisconnected()
    {
        UpdateStatusText("disconnected");
    }

    /// <summary>
    /// Tell the probe matching panel to update all of the dropdown lists to the current set of options
    /// </summary>
    /// <param name="probeOpts"></param>
    public static void ProbeMatchingPanelUpdate(List<string> probeOpts)
    {
        Instance.ProbeOptionsChangedEvent.Invoke(probeOpts);
    }
    #endregion

    #region POST probe data target


    public void SetOpenEphysState(bool state)
    {
        _openEphysAPI.enabled = state;
        if (state)
        {
            UpdateStatusText("attempting connection...");
            _spikeGLXAPI.enabled = false;
        }
        else if (!_spikeGLXAPI.isActiveAndEnabled)
        {
            ProbeOptionsChangedEvent.Invoke(new());
            UpdateStatusText("not connected");
        }
    }

    public void SetSpikeGLXState(bool state)
    {
        _spikeGLXAPI.enabled = state;
        if (state)
        {
            UpdateStatusText("attempting connection...");
            _openEphysAPI.enabled = false;
        }
        else if (!_spikeGLXAPI.isActiveAndEnabled)
        {
            ProbeOptionsChangedEvent.Invoke(new());
            UpdateStatusText("not connected");
        }
    }

    public void CopyChannelData2Clipboard()
    {
        string all = $"[{string.Join(",", ProbeManager.GetAllChannelAnnotationData())}]";
#if UNITY_WEBGL && !UNITY_EDITOR
        Copy2Clipboard(all);
#else
        GUIUtility.systemCopyBuffer = all;
#endif
    }

#endregion
}

public class ProbeDataMessage
{
    public string text;

    public ProbeDataMessage(string text)
    {
        this.text = text;
    }

    public override string ToString()
    {
        return JsonUtility.ToJson(this);
    }
}
