using RainbowArt.CleanFlatUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
#if !UNITY_WEBGL
using System.Security.Policy;
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
    [SerializeField] ProbeMatchingPanel _probeMatchingPanel;
    [SerializeField] Toggle _openEphysToggle;
#endregion

#region Probe data variables
    [SerializeField] TMP_InputField _probeDataHTTPTarget;
    private float _lastDataSend;
    private const float DATA_SEND_RATE = 10f; // cap data sending at once per 10 s maximum, it's fairly expensive to do
    private bool _dirty = false;

    public UnityEvent<List<string>> ProbeOptionsChangedEvent;

    [SerializeField] TMP_InputField _pxiInput;
    private int _pxiID;

    private const string ENDPOINT_PROCESSORS = "/api/processors";
    private const string ENDPOINT_CONFIG = "/api/processors/<id>/config";
#endregion

#region Viewer variables
    [SerializeField] DropdownMultiCheck _viewerDropdown;
    List<string> _viewerTargets;
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
        if (_dirty && Settings.ProbeDataPOST && (Time.realtimeSinceStartup > (_lastDataSend + DATA_SEND_RATE)))
        {
            _dirty = false;
            _lastDataSend = Time.realtimeSinceStartup;
            SendAllProbeData();
        }
    }
#endregion

#region POST probe data target

    /// <summary>
    /// The toggle enabling/disabling the API should trigger this
    /// </summary>
    public void UpdateProbeDataSetting_OpenEphys(bool state)
    {
        if (state)
        {
            // If the setting just got turned on we should query the server for "NP INFO" to get the list of active probes
            _probeMatchingPanel.UpdateUI();
            StartCoroutine(GetProbeInfo_OpenEphys());
        }
        else
        {
            // Reset the timer
            _dirty = false;
            _lastDataSend = float.MinValue;
            _probeMatchingPanel.ClearUI();
            _viewerDropdown.ClearOptions();
            _viewerDropdown.interactable = false;
        }
    }

    ///// <summary>
    ///// The toggle enabling/disabling the API should trigger this
    ///// </summary>
    //public void UpdateProbeDataSetting_SpikeGLX(bool state)
    //{
    //    if (state)
    //    {
    //        // If the setting just got turned on we should query the server for "NP INFO" to get the list of active probes

    //        GetProbeInfo_SpikeGLX();
    //    }
    //}

    /// <summary>
    /// Trigger the API to send the Probe data to the current http server target
    /// </summary>
    public void TriggerAPIPush()
    {
        _dirty = true;
    }

    private IEnumerator GetProbeInfo_OpenEphys()
    {
        // First, get data about the processors
        string url = _probeDataHTTPTarget.text.ToLower().Trim();
        string uri = $"{url}{ENDPOINT_PROCESSORS}";

        // Clear variables
        _viewerTargets = new();

        Debug.Log($"(APIManager) Sending GET to {uri}");

        using (UnityWebRequest www = UnityWebRequest.Get(uri))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                var msg = LightJson.JsonValue.Parse(www.downloadHandler.text).AsJsonObject;
                var processors = msg["processors"].AsJsonArray;

                int probeViewerIdx = -1;

                if (processors != null)
                {
                    foreach (var processor in processors)
                    {
                        var processorObject = processor.AsJsonObject;
                        string name = (string)processorObject["name"];

                        //Debug.LogError($"Received processor with name: {name} and ID: {processorObject["id"]}");

                        if (name.Equals("Neuropix-PXI"))
                        {
                            _pxiID = processorObject["id"];
                            _pxiInput.text = _pxiID.ToString();
                            Debug.Log($"(APIManager-OpenEphys) Found Neuropix-PXI processor, setting active ID to {processorObject["id"]}");
                        }
                        else
                        {
                            _viewerTargets.Add($"{processorObject["id"]}: {name}");
                            if (name.Contains("Probe"))
                                probeViewerIdx = _viewerTargets.Count - 1;
                        }

                        if (name.Equals("Neuropix-PXI"))
                        {
                            _pxiID = processorObject["id"];
                            Debug.Log($"(APIManager-OpenEphys) Found Probe Viewer, targeting outbound message to {processorObject["id"]}");
                        }
                    }

#if UNITY_EDITOR
                    // debugging code for in the editor
                    if (_viewerTargets.Count == 0)
                        _viewerTargets.Add("100");
#endif

                    // Setup the probe viewer dropdown
                    int[] previousSelection = _viewerDropdown.SelectedOptions;

                    _viewerDropdown.ClearOptions();
                    _viewerDropdown.interactable = true;
                    _viewerDropdown.AddOptions(_viewerTargets);
                    if (previousSelection.Length > 0)
                        _viewerDropdown.SelectedOptions = previousSelection;
                    else if (_viewerDropdown.options.Count > 0)
                    {
                        _viewerDropdown.SelectedOptions = (probeViewerIdx >= 0) ? new int[] { probeViewerIdx } : new int[] { 0 };
                    }
                }
            }
        }

        if (_pxiID == 0)
        {
            Debug.LogError("(APIManager-OpenEphys) Warning: no Neuropix-PXI processor was found");
            _openEphysToggle.SetIsOnWithoutNotify(false);
            yield break;
        }

        // Now, send the actual message to request info about the available probes

        var infoMessage = new ProbeDataMessage("NP INFO");

        uri = $"{url}{ENDPOINT_CONFIG.Replace("<id>",_pxiID.ToString())}";

        Debug.Log($"Sending message {infoMessage} to {uri}");
        using (UnityWebRequest www = UnityWebRequest.Put(uri, infoMessage.ToString()))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
                _openEphysToggle.SetIsOnWithoutNotify(false);
                yield break;
            }
            else
            {
                var msg = LightJson.JsonValue.Parse(www.downloadHandler.text).AsJsonObject;
                string info = msg["info"];

                var infoJSON = LightJson.JsonValue.Parse(info).AsJsonObject;
                var probeArray = infoJSON["probes"].AsJsonArray;

                List<string> probeOpts = new();
                probeOpts.Add("None");

                foreach (var probe in probeArray)
                {
                    var probeParsed = probe.AsJsonObject;
                    probeOpts.Add(probeParsed["name"]);
                }

                ProbeOptionsChangedEvent.Invoke(probeOpts);
            }
        }

        // Send the current probe data immediately after setting everything up
        TriggerAPIPush();
        _lastDataSend = float.MinValue;
    }

    //private void GetProbeInfo_SpikeGLX()
    //{
    //    List<string> probeOpts = new();
    //    for (int i = 0; i < ProbeManager.Instances.Count; i++)
    //        probeOpts.Add(i.ToString());

    //    ProbeOptionsChangedEvent.Invoke(probeOpts);
    //}

    private void SendAllProbeData()
    {
        string url = _probeDataHTTPTarget.text.ToLower().Trim();

        Debug.Log($"(APIManager-OpenEphys) Sending probe data for {ProbeManager.Instances.Count} probes");
        foreach (ProbeManager probeManager in ProbeManager.Instances)
        {
            if (probeManager.APITarget == null || probeManager.APITarget.Equals("None"))
                continue;
            StartCoroutine(SendProbeData(probeManager, url));
        }
    }

    private IEnumerator SendProbeData(ProbeManager probeManager, string url)
    {            
        // add data
        string channelDataStr = probeManager.GetChannelAnnotationIDs();

        foreach (int optIdx in _viewerDropdown.SelectedOptions)
        {
            string processorID = _viewerTargets[optIdx].Substring(0,3);

            string uri = $"{url}{ENDPOINT_CONFIG.Replace("<id>", processorID)}";
            string fullMsg = $"{probeManager.APITarget};{channelDataStr}";

            ProbeDataMessage msg = new ProbeDataMessage(fullMsg);
            Debug.Log($"(APIManager-OpenEphys) Sending {msg} to {uri}");

            byte[] data = System.Text.Encoding.UTF8.GetBytes(msg.ToString());
            using (UnityWebRequest www = UnityWebRequest.Put(uri, data))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                }
            }
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
