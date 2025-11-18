using BrainAtlas;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class TP_ProbePanel : MonoBehaviour
{
    [FormerlySerializedAs("pixelsGPURenderer")][SerializeField] private Renderer _channelRenderer;
    [SerializeField] private Renderer _sliceRenderer;
    [FormerlySerializedAs("textPanelGO")] [SerializeField] private GameObject _textPanelGo;
    [FormerlySerializedAs("textPrefab")] [SerializeField] private GameObject _textPrefab;
    [FormerlySerializedAs("tickMarkGOs")] [SerializeField] private List<GameObject> _tickMarkGOs;
    [FormerlySerializedAs("probePanelPxHeight")] [SerializeField] private int _probePanelPxHeight = 500;

    [SerializeField] private Material _channelMaterial;
    [SerializeField] private RenderTexture _channelTexture;
    [SerializeField] private Material _sliceMaterial;
    [SerializeField] private RenderTexture _sliceTexture;

    private List<GameObject> _textGOs;
    private ProbeManager _probeManager;

    public Action<List<string>, List<float>> OnTextDataChanged;

    private void Awake()
    {
        _textGOs = new List<GameObject>();
    }

    public void Start()
    {
        _channelMaterial.SetTexture("_AnnotationTexture", BrainAtlasManager.ActiveReferenceAtlas.AnnotationTexture);
        (int x, int y, int z) = BrainAtlasManager.ActiveReferenceAtlas.DimensionsIdx;
        _channelMaterial.SetVector("_AnnotationDimensions", new Vector3(x, y, z));

        _sliceMaterial.SetTexture("_AnnotationTexture", BrainAtlasManager.ActiveReferenceAtlas.AnnotationTexture);
        _sliceMaterial.SetVector("_AnnotationDimensions", new Vector3(x, y, z));
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
        _channelMaterial.SetTexture("_ChannelTexture", channelMapTexture);
    }

    public void SetTipData(Vector3 tipPosIdx, Vector3 endPositionIdx, float tipPerc, float endPerc, float recordingHeight)
    {
        _channelMaterial.SetVector("_TipPositionIdx", tipPosIdx);
        _channelMaterial.SetVector("_EndPositionIdx", endPositionIdx);
        _channelMaterial.SetFloat("_TipPerc", tipPerc);
        _channelMaterial.SetFloat("_EndPerc", endPerc);
        _channelMaterial.SetFloat("_RecordingHeight", recordingHeight);
        Graphics.Blit(null, _channelTexture, _channelMaterial);

        _sliceMaterial.SetVector("_TipPositionIdx", tipPosIdx);
        _sliceMaterial.SetVector("_EndPositionIdx", endPositionIdx);
        _sliceMaterial.SetFloat("_RecordingHeight", recordingHeight);
        Graphics.Blit(null, _sliceTexture, _sliceMaterial);
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
#if !APP_UI
        foreach (GameObject go in _textGOs)
            Destroy(go);
        _textGOs.Clear();

        for (int i = 0; i < heights.Count; i++)
            AddText(heights[i], areaNames[i], fontSize);
#else
        List<float> positionPercentages = new List<float>();
        for (int i = 0; i < heights.Count; i++)
        {
            float percentage = heights[i] / (float)_probePanelPxHeight;
            positionPercentages.Add(percentage);
        }

        OnTextDataChanged?.Invoke(areaNames, positionPercentages);
#endif
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
        newText.transform.localPosition = new Vector3(0f, pxHeight - _probePanelPxHeight);
    }

    public void ResizeProbePanel(int newPxHeight)
    {
        _probePanelPxHeight = newPxHeight;
        _channelRenderer.gameObject.transform.localScale = new Vector3(32, newPxHeight);
        _sliceRenderer.gameObject.transform.localScale = new Vector3(4, newPxHeight);
    }
}
