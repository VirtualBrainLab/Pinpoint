// this entire class does not exist on WebGL

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Policy;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;


public class APISpikeGLX : MonoBehaviour
{
    [SerializeField] private TMP_InputField _serverPort;
    [SerializeField] private TMP_InputField _helloSpikeGLXPathInput;

    private bool connected;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(Application.dataPath);
    }

    #region Unity
    private void OnEnable()
    {
        GetProbeData();

        APIManager.TriggerAPIPush();
    }

    private void OnDisable()
    {

    }
    #endregion

    #region Public


    public async void GetProbeData()
    {
        string msg = $"{GetServerInfo()} -cmd=getProbeList";

        var responseTask = SendAPIMessage(msg);
        await responseTask;

        string allProbeDataStr = responseTask.Result;

        // parse the probe data
        // Returns string: (probeID, nShanks, partNumber)()...

        //-A parenthesized entry for each selected probe.

        //- probeID: zero - based integer.

        //- nShanks: integer { 1,4}.

        //-partNumber: string, e.g., NP1000.

        //- If no probes, return '()'.

        // Change the format to be probeID,nShanks,partNumber;... because the original format is bad

        string charSepString = allProbeDataStr.Replace(")(", ";");
        charSepString = charSepString.Remove(0,1);
        charSepString = charSepString.Remove(charSepString.Length - 2,1);


        // Now parse the improved formatting
        string[] data = charSepString.Split(';');

        List<string> probeOpts = new();
        probeOpts.Add("None");

        foreach (string dataStr in data)
        {
            string[] singleProbeData = dataStr.Split(',');
            // we just care about the probeID
            probeOpts.Add(singleProbeData[0]);
        }

        APIManager.ProbeMatchingPanelUpdate(probeOpts);
    }

    public void SendData()
    {
        Debug.Log("(SpikeGLX) Starting process");

        // Get the probe data
        foreach (ProbeManager probeManager in ProbeManager.Instances)
        {
            if (probeManager.APITarget == null || probeManager.APITarget.Equals("None"))
                continue;
            SendProbeData(probeManager);
        }
    }
    #endregion

    #region Private

    private void SendProbeData(ProbeManager probeManager)
    {
        string probeDepthData = probeManager.GetProbeDepthIDs();

        string msg = $"{GetServerInfo()} -cmd=setAnatomy_Pinpoint -args={probeDepthData}";

        var responseTask = SendAPIMessage(msg);
    }

    private string GetServerInfo()
    {
        // Get SpikeGLX target
        string[] serverPort = _serverPort.text.Split(':');

        return $"-host={serverPort[0]} -port={serverPort[1]}";
    }

    private async Task<string> SendAPIMessage(string msg)
    {
        Process sgl = new Process();

        sgl.StartInfo.FileName = Path.Join(_helloSpikeGLXPathInput.text, "HelloSGLX.exe");

        Debug.Log($"(SpikeGLX) Sending: {msg}");

        sgl.StartInfo.Arguments = msg;

        sgl.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

        sgl.StartInfo.UseShellExecute = false;

        sgl.StartInfo.RedirectStandardOutput = true;

        sgl.StartInfo.RedirectStandardError = true;

        sgl.Start();

        var responseTask = sgl.StandardOutput.ReadToEndAsync();

        await responseTask;

        string response = responseTask.Result;

        Debug.Log($"(SpikeGLX) Response: {response}");

        sgl.WaitForExit();
        sgl.Close();

        return response;
    }
    #endregion
}