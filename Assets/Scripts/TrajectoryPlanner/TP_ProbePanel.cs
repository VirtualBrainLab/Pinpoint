using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using TrajectoryPlanner;
using UnityEngine.Serialization;

public class TP_ProbePanel : MonoBehaviour
{
    //[SerializeField] private GameObject pixelsGO;
    [FormerlySerializedAs("pixelsGPURenderer")] [SerializeField] private Renderer _pixelsGPURenderer;
    [FormerlySerializedAs("textPanelGO")] [SerializeField] private GameObject _textPanelGo;
    [FormerlySerializedAs("textPrefab")] [SerializeField] private GameObject _textPrefab;
    [FormerlySerializedAs("tickMarkGOs")] [SerializeField] private List<GameObject> _tickMarkGOs;
    [FormerlySerializedAs("probePanelPxHeight")] [SerializeField] private int _probePanelPxHeight = 500;

    TP_InPlaneSlice inPlaneSlice;

    private ProbeManager _probeController;
    private ProbeUIManager _probeUIManager;

    private List<GameObject> textGOs;

    private void Awake()
    {
        textGOs = new List<GameObject>();
    }

    private void Start()
    {
        // Because probe panels are never created early it is safe to wait to get the annotation dataset until this point
        inPlaneSlice = GameObject.Find("main").GetComponent<TrajectoryPlannerManager>().GetInPlaneSlice();
        AsyncStart();
    }

    private async void AsyncStart()
    {
        Task<Texture3D> textureTask = inPlaneSlice.GetAnnotationDatasetGPUTexture();
        await textureTask;

        _pixelsGPURenderer.material.SetTexture("_AnnotationTexture", textureTask.Result);
    }

    public void SetTipData(Vector3 tipPosition, Vector3 endPosition, float recordingHeight, bool recordingRegionOnly)
    {
        _pixelsGPURenderer.material.SetVector("_TipPosition", tipPosition);
        _pixelsGPURenderer.material.SetVector("_EndPosition", endPosition);
        _pixelsGPURenderer.material.SetFloat("_RecordingHeight", recordingHeight);
        _pixelsGPURenderer.material.SetFloat("_RecordingRegionOnly", recordingRegionOnly ? 1 : 0);
    }

    public float GetPanelHeight()
    {
        return _probePanelPxHeight;
    }

    public void RegisterProbeUIManager(ProbeUIManager probeUImanager)
    {
        _probeUIManager = probeUImanager;
        gameObject.name = _probeController.name + "_panel_" + probeUImanager.GetOrder();
    }

    public void RegisterProbeController(ProbeManager probeController)
    {
        _probeController = probeController;
    }

    public ProbeManager GetProbeController()
    {
        return _probeController;
    }

    public void UpdateText(List<int> heights, List<string> areaNames, int fontSize)
    {
        // [TODO] Replace this with a queue
        foreach (GameObject go in textGOs)
            Destroy(go);
        textGOs.Clear();

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
        textGOs.Add(newText);
        newText.transform.localPosition = new Vector3(15, pxHeight);
    }

    public void ResizeProbePanel(int newPxHeight)
    {
        _probePanelPxHeight = newPxHeight;
        _pixelsGPURenderer.gameObject.transform.localScale = new Vector3(25, newPxHeight);
    }
}
