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
    private const float DATA_SEND_RATE = 1; // cap data sending at once per second maximum, it's fairly expensive to do
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
    public void UpdateProbeDataTarget()
    {
        _dirty = true;
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
