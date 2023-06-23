// this entire class does not exist on WebGL

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        APIManager.TriggerAPIPush();
    }

    private void OnDisable()
    {

    }

    public void SendData()
    {
        Debug.Log("(SpikeGLX) Starting process");


        Process sgl = new Process();

        sgl.StartInfo.FileName = Path.Join(_helloSpikeGLXPathInput.text, "HelloSGLX.exe");

        string[] serverPort = _serverPort.text.Split(':');

        // Get the probe data
        string probeDepthData = ProbeManager.ActiveProbeManager.GetProbeDepthIDs();

        string msg = $"-host={serverPort[0]} -port={serverPort[1]} -cmd=setAnatomy_Pinpoint -args={probeDepthData}";

        Debug.Log($"(SpikeGLX) Sending {msg}");

        sgl.StartInfo.Arguments = msg;

        sgl.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

        sgl.StartInfo.UseShellExecute = false;

        sgl.StartInfo.RedirectStandardOutput = true;

        sgl.StartInfo.RedirectStandardError = true;

        sgl.Start();

        string s = sgl.StandardOutput.ReadToEnd();

        Debug.Log(s);

        sgl.WaitForExit();
        sgl.Close();
    }
    #endregion

}