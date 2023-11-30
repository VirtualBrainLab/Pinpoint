// this entire class does not exist on WebGL
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;
using KS.UnityToolbag;
using Process = KS.Diagnostics.Process;
using SimpleFileBrowser;

public class APISpikeGLX : MonoBehaviour
{
    private const float FUZZY_DISTANCE = 0.01f;

    [SerializeField] private TMP_InputField _helloSpikeGLXPathInput;
    private HashSet<Process> _processList;
    private Dictionary<ProbeManager, Vector3> _lastPositions;

    #region Unity

    private void Awake()
    {
        _processList = new HashSet<Process>();
        _lastPositions = new();
    }

    private void OnEnable()
    {
        GetSpikeGLXProbeInfo();
    }

    private void OnDestroy()
    {
        foreach (var proc in _processList)
        {
            proc.Kill();
            proc.Dispose();
        }
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
        Debug.Log($"Received probe data from SpikeGLX: {allProbeDataStr}");

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

        if (allProbeDataStr.Length <= 4)
        {
            APIManager.UpdateStatusText("error, SpikeGLX returned 0 probes");
            Settings.SpikeGLXToggle = false;
            return;
        }

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
            return;
        }
    }

    public void SendData()
    {
        // Get the probe data
        foreach (ProbeManager probeManager in ProbeManager.Instances)
        {
            if (probeManager.APITarget == null || probeManager.APITarget.Equals("None"))
                continue;

            Vector3 pos = probeManager.ProbeController.Insertion.apmldv;

            if (_lastPositions.ContainsKey(probeManager))
            {
                if (FuzzyEquals(pos, _lastPositions[probeManager]))
                    continue;
                _lastPositions[probeManager] = pos;
            }
            else
                _lastPositions.Add(probeManager, pos);

            SendProbeData(probeManager);
        }
    }

    private bool FuzzyEquals(Vector3 a, Vector3 b)
    {
        Debug.Log(Vector3.Distance(a, b));
        return Vector3.Distance(a, b) < FUZZY_DISTANCE;
    }

    public void PickHelloSGLXPath()
    {
        Debug.Log("Opening file browser");
        FileBrowser.ShowLoadDialog(PickPathHelper, () => { }, FileBrowser.PickMode.Files);
    }

    private void PickPathHelper(string[] paths)
    {
        if (paths.Length > 0)
        {
            string filePath = paths[0];
            Settings.SpikeGLXHelloPath = filePath;
        }
    }
#endregion

#region Private

    private void SendProbeData(ProbeManager probeManager)
    {
        List<string> probeDepthData = probeManager.GetProbeDepthIDs();

        foreach (string shankData in probeDepthData)
        {
            string msg = $"{GetServerInfo()} -cmd=setAnatomy_Pinpoint -args=\"{shankData}\"";

            SendAPIMessage(msg);
        }
    }

    private string GetServerInfo()
    {
        // Get SpikeGLX target
        string[] serverPort = Settings.SpikeGLXTarget.Split(':');

        return $"-host={serverPort[0]} -port={serverPort[1]}";
    }

    private void SendAPIMessage(string msg, Action<string> callback = null)
    {
        string originalPath = _helloSpikeGLXPathInput.text;

        string filePath = originalPath.Contains("HelloSGLX.exe") ?
            originalPath :
            Path.Join(originalPath, "HelloSGLX.exe");


#if UNITY_EDITOR
        Debug.Log($"(SGLX) Sending: {msg} to target {filePath}");
#endif

        if (!File.Exists(filePath))
        {
            APIManager.UpdateStatusText("error, HelloSGLX path incorrect");
            return;
        }

        Process proc = new Process()
        {
            StartInfo = new KS.Diagnostics.ProcessStartInfo()
            {
                FileName = $"\"{filePath}\"",
                Arguments = msg,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            },

            EnableRaisingEvents = true

        };

        _processList.Add(proc);

        proc.OutputDataReceived += (s, d) =>
        {
            if (callback != null)
                Dispatcher.Invoke(() => callback(d.Data));
        };

        proc.Exited += (s, d) =>
        {
            _processList.Remove(proc);
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