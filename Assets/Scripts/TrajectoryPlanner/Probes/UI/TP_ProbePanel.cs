using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class TP_ProbePanel : MonoBehaviour
{
    //[SerializeField] private GameObject pixelsGO;
    [FormerlySerializedAs("pixelsGPURenderer")][SerializeField] private Renderer _channelRenderer;
    [SerializeField] private Renderer _sliceRenderer;
    [FormerlySerializedAs("textPanelGO")] [SerializeField] private GameObject _textPanelGo;
    [FormerlySerializedAs("textPrefab")] [SerializeField] private GameObject _textPrefab;
    [FormerlySerializedAs("tickMarkGOs")] [SerializeField] private List<GameObject> _tickMarkGOs;
    [FormerlySerializedAs("probePanelPxHeight")] [SerializeField] private int _probePanelPxHeight = 500;

    private List<GameObject> _textGOs;
    private ProbeManager _probeManager;

    private void Awake()
    {
        _textGOs = new List<GameObject>();
    }

    private void Start()
    {
        // Because probe panels are never created early it is safe to wait to get the annotation dataset until this point
        AsyncStart();
    }

    private async void AsyncStart()
    {
        await VolumeDatasetManager.Texture3DLoaded();

        _channelRenderer.material.SetTexture("_AnnotationTexture", VolumeDatasetManager.AnnotationDatasetTexture3D);
        _sliceRenderer.material.SetTexture("_AnnotationTexture", VolumeDatasetManager.AnnotationDatasetTexture3D);
    }

    public void RegisterProbeManager(ProbeManager probeManager)
    {
        _probeManager = probeManager;
    }

    public ProbeManager GetProbeManager()
    {
        return _probeManager;
    }

    public void SetChannelMap(Texture2D channelMapTexture)
    {

        _channelRenderer.material.SetTexture("_ChannelTexture", channelMapTexture);
    }

    public void SetTipData(Vector3 tipPosition, Vector3 endPosition, float tipPerc, float endPerc, float recordingHeight, bool recordingRegionOnly)
    {
        _channelRenderer.material.SetVector("_TipPosition", tipPosition);
        _channelRenderer.material.SetVector("_EndPosition", endPosition);
        _channelRenderer.material.SetFloat("_TipPerc", tipPerc);
        _channelRenderer.material.SetFloat("_EndPerc", endPerc);
        _channelRenderer.material.SetFloat("_RecordingHeight", recordingHeight);
        _channelRenderer.material.SetFloat("_RecordingRegionOnly", recordingRegionOnly ? 1 : 0);

        _sliceRenderer.material.SetVector("_TipPosition", tipPosition);
        _sliceRenderer.material.SetVector("_EndPosition", endPosition);
        _sliceRenderer.material.SetFloat("_RecordingHeight", recordingHeight);
        _sliceRenderer.material.SetFloat("_RecordingRegionOnly", recordingRegionOnly ? 1 : 0);
    }

    public float GetPanelHeight()
    {
        return _probePanelPxHeight;
    }

    public void RegisterProbeUIManager(ProbeUIManager probeUImanager)
    {
        gameObject.name = "panel_" + probeUImanager.GetOrder();
    }

    public void UpdateText(List<int> heights, List<string> areaNames, int fontSize)
    {
        // [TODO] Replace this with a queue
        foreach (GameObject go in _textGOs)
            Destroy(go);
        _textGOs.Clear();

        // add the area names
        for (int i = 0; i < heights.Count; i++)
            AddText(heights[i], areaNames[i], fontSize);
    }

    public void UpdateTicks(List<int> heights, List<int> tickIdxs)
    {
        foreach (GameObject go in _tickMarkGOs)
            go.SetActive(false);

        for (int i = 0; i < heights.Count; i++)
        {
            _tickMarkGOs[tickIdxs[i]].SetActive(true);
            _tickMarkGOs[tickIdxs[i]].transform.localPosition = new Vector3(4.5f, heights[i]);
        }
    }

    public void AddText(int pxHeight, string areaName, int fontSize)
    {
        GameObject newText = Instantiate(_textPrefab, _textPanelGo.transform);
        newText.GetComponent<TextMeshProUGUI>().text = areaName;
        newText.GetComponent<TextMeshProUGUI>().fontSize = fontSize;
        _textGOs.Add(newText);
        newText.transform.localPosition = new Vector3(-37.5f, pxHeight - _probePanelPxHeight);
    }

    public void ResizeProbePanel(int newPxHeight)
    {
        _probePanelPxHeight = newPxHeight;
        _channelRenderer.gameObject.transform.localScale = new Vector3(32, newPxHeight);
        _sliceRenderer.gameObject.transform.localScale = new Vector3(4, newPxHeight);
    }
}
