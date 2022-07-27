using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using TrajectoryPlanner;

public class TP_ProbePanel : MonoBehaviour
{
    //[SerializeField] private GameObject pixelsGO;
    [SerializeField] private Renderer pixelsGPURenderer;
    [SerializeField] private GameObject textPanelGO;
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private List<GameObject> tickMarkGOs;
    [SerializeField] private int probePanelPxHeight = 500;

    TP_InPlaneSlice inPlaneSlice;

    private TP_ProbeController _probeController;
    private TP_ProbeUIManager _probeUIManager;

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

        pixelsGPURenderer.material.SetTexture("_AnnotationTexture", textureTask.Result);
    }

    public void SetTipData(Vector3 tipPosition, Vector3 endPosition, float recordingHeight, bool recordingRegionOnly)
    {
        pixelsGPURenderer.material.SetVector("_TipPosition", tipPosition);
        pixelsGPURenderer.material.SetVector("_EndPosition", endPosition);
        pixelsGPURenderer.material.SetFloat("_RecordingHeight", recordingHeight);
        pixelsGPURenderer.material.SetFloat("_RecordingRegionOnly", recordingRegionOnly ? 1 : 0);
    }

    public float GetPanelHeight()
    {
        return probePanelPxHeight;
    }

    public void RegisterProbeUIManager(TP_ProbeUIManager probeUImanager)
    {
        _probeUIManager = probeUImanager;
        gameObject.name = _probeController.name + "_panel_" + probeUImanager.GetOrder();
    }

    public void RegisterProbeController(TP_ProbeController probeController)
    {
        _probeController = probeController;
    }

    public TP_ProbeController GetProbeController()
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
        foreach (GameObject go in tickMarkGOs)
            go.SetActive(false);

        for (int i = 0; i < heights.Count; i++)
        {
            tickMarkGOs[tickIdxs[i]].SetActive(true);
            tickMarkGOs[tickIdxs[i]].transform.localPosition = new Vector3(4.5f, heights[i]);
        }
    }

    public void AddText(int pxHeight, string areaName, int fontSize)
    {
        GameObject newText = Instantiate(textPrefab, textPanelGO.transform);
        newText.GetComponent<TextMeshProUGUI>().text = areaName;
        newText.GetComponent<TextMeshProUGUI>().fontSize = fontSize;
        textGOs.Add(newText);
        newText.transform.localPosition = new Vector3(15, pxHeight);
    }

    public void ResizeProbePanel(int newPxHeight)
    {
        probePanelPxHeight = newPxHeight;
        pixelsGPURenderer.gameObject.transform.localScale = new Vector3(25, newPxHeight);
    }
}
