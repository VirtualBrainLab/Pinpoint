using RainbowArt.CleanFlatUI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class APIOpenEphys : MonoBehaviour
{
    [SerializeField] ProbeMatchingPanel _probeMatchingPanel;
    [SerializeField] Toggle _openEphysToggle;
    [SerializeField] TMP_InputField _probeDataHTTPTarget;
    [SerializeField] TMP_InputField _pxiInput;
    private int _pxiID;

    private const string ENDPOINT_PROCESSORS = "/api/processors";
    private const string ENDPOINT_CONFIG = "/api/processors/<id>/config";

    #region Viewer variables
    [SerializeField] DropdownMultiCheck _viewerDropdown;
    List<string> _viewerTargets;
    #endregion

    #region Unity
    private void OnEnable()
    {
        // If the setting just got turned on we should query the server for "NP INFO" to get the list of active probes
        _probeMatchingPanel.UpdateUI();
        StartCoroutine(GetProbeInfo_OpenEphys());
    }

    private void OnDisable()
    {
        _probeMatchingPanel.ClearUI();
        _viewerDropdown.ClearOptions();
        _viewerDropdown.interactable = false;
    }

    public void SendData()
    {
        SendAllProbeData();
    }
    #endregion

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

        uri = $"{url}{ENDPOINT_CONFIG.Replace("<id>", _pxiID.ToString())}";

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

                APIManager.ProbeMatchingPanelUpdate(probeOpts);
            }
        }

        // Send the current probe data immediately after setting everything up
        APIManager.ResetTimer();
        APIManager.TriggerAPIPush();
    }

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
            string processorID = _viewerTargets[optIdx].Substring(0, 3);

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
}
