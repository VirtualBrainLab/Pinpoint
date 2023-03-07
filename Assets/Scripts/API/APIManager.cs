using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class APIManager : MonoBehaviour
{
    #region Probe data variables
    [SerializeField] TMP_InputField _probeDataHTTPTarget;
    private float _lastDataSend;
    private const float DATA_SEND_RATE = 10f; // cap data sending at once per 10 s maximum, it's fairly expensive to do
    private bool _dirty = false;

    #endregion

    #region Unity
    private void Awake()
    {
        _lastDataSend = float.MinValue;
    }

    private void Update()
    {
        if (_dirty && Settings.ProbeDataPOST && (Time.realtimeSinceStartup > (_lastDataSend + DATA_SEND_RATE)))
        {
            _dirty = false;
            _lastDataSend = Time.realtimeSinceStartup;
            StartCoroutine(SendProbeData());
        }
    }
    #endregion

    #region POST probe data target

    /// <summary>
    /// The toggle enabling/disabling the API should trigger this
    /// </summary>
    public void UpdateProbeDataSettingChange(bool state)
    {
        if (state)
        {
            // If the setting just got turned on we should query the server for "NP INFO" to get the list of active probes

            StartCoroutine(GetProbeInfo());
        }
    }

    /// <summary>
    /// Trigger the API to send the Probe data to the current http server target
    /// </summary>
    public void UpdateProbeDataTarget()
    {
        _dirty = true;
    }

    private IEnumerator GetProbeInfo()
    {
        using (UnityWebRequest www = UnityWebRequest.Put(_probeDataHTTPTarget.text, "NP INFO"))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                string result = www.result.ToString();
                var parsed = LightJson.JsonValue.Parse(result).AsJsonObject;
                foreach (var probe in parsed["probes"].AsJsonArray)
                    Debug.Log(probe.AsJsonObject["name"]);
            }
        }
    }


    private IEnumerator SendProbeData()
    {
        Debug.Log("(API) Sending probe data");

        // For each probe, get the data string and send it to the request server

        foreach (ProbeManager probeManager in ProbeManager.instances)
        {

            // add data
            ProbeDataMessage msg = new ProbeDataMessage(probeManager.GetChannelAnnotationIDs());
            Debug.Log(msg);

            byte[] data = System.Text.Encoding.UTF8.GetBytes(msg.ToString());
            using (UnityWebRequest www = UnityWebRequest.Put(_probeDataHTTPTarget.text, data))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    Debug.Log("Upload complete!");
                }
            }
        }
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
