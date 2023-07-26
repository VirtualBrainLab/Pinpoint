// this entire class does not exist on WebGL
using KS.UnityToolbag;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Process = KS.Diagnostics.Process;

public class APISpikeGLX : MonoBehaviour
{
    [SerializeField] private TMP_InputField _helloSpikeGLXPathInput;

    #region Unity
    private void OnEnable()
    {
        GetSpikeGLXProbeInfo();
    }
    #endregion

    #region Public


    public void GetSpikeGLXProbeInfo()
    {
        string msg = $"{GetServerInfo()} -cmd=getProbeList";

        SendAPIMessage(msg, SetSpikeGLXProbeData);
    }

    private void SetSpikeGLXProbeData(string allProbeDataStr)
    {
        if (allProbeDataStr.ToLower().Contains("error"))
        {
            APIManager.UpdateStatusText(allProbeDataStr);
            enabled = false;
            return;
        }

        // parse the probe data
        // Returns string: (probeID, nShanks, partNumber)()...

        //-A parenthesized entry for each selected probe.

        //- probeID: zero - based integer.

        //- nShanks: integer { 1,4}.

        //-partNumber: string, e.g., NP1000.

        //- If no probes, return '()'.

        // Change the format to be probeID,nShanks,partNumber;... because the original format is bad

        string charSepString = allProbeDataStr.Replace(")(", ";");
        charSepString = charSepString.Remove(0, 1);
        charSepString = charSepString.Remove(charSepString.Length - 2, 1);


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

        if (probeOpts.Count > 0)
        {
            APIManager.UpdateStatusText("connected SpikeGLX");
            APIManager.ProbeMatchingPanelUpdate(probeOpts);

            // Send the current probe data immediately after setting everything up
            APIManager.ResetTimer();
            APIManager.TriggerAPIPush();
        }
        else
        {
            APIManager.UpdateStatusText("error, no probes");
            Settings.SpikeGLXToggle = false;
        }
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
        List<string> probeDepthData = probeManager.GetProbeDepthIDs();

        foreach (string shankData in probeDepthData)
        {
            string msg = $"{GetServerInfo()} -cmd=setAnatomy_Pinpoint -args={shankData}";

            SendAPIMessage(msg, Debug.Log);
        }
    }

    private string GetServerInfo()
    {
        // Get SpikeGLX target
        Debug.Log(Settings.SpikeGLXTarget);
        string[] serverPort = Settings.SpikeGLXTarget.Split(':');

        return $"-host={serverPort[0]} -port={serverPort[1]}";
    }

    private void SendAPIMessage(string msg, Action<string> callback = null)
    {
        Debug.Log(Application.streamingAssetsPath);

#if UNITY_EDITOR
        Debug.Log($"(SGLX) Sending: {msg}");
#endif

        string filePath = Path.Join(_helloSpikeGLXPathInput.text, "HelloSGLX.exe");

        if (!File.Exists(filePath))
        {
            APIManager.UpdateStatusText("error, HelloSGLX path incorrect");
            return;
        }

        Process proc = new Process()
        {
            StartInfo = new KS.Diagnostics.ProcessStartInfo()
            {
                FileName = Path.Join(_helloSpikeGLXPathInput.text, "HelloSGLX.exe"),
                Arguments = msg,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            },

            EnableRaisingEvents = true
        };

        proc.OutputDataReceived += (s, d) =>
        {
            Dispatcher.Invoke(() => callback(d.Data));
        };

        proc.Exited += (s, d) =>
        {
            proc.CancelOutputRead();
            proc.Dispose();
        };

        proc.Start();
        proc.BeginOutputReadLine();
    }


    private static void SortOutputHandler(object sendingProcess,
        DataReceivedEventArgs outLine)
    {
        // Collect the sort command output.
        if (!String.IsNullOrEmpty(outLine.Data))
        {
            // Add the text to the collected output.
            Debug.Log(outLine.Data);
        }
    }
#endregion
}