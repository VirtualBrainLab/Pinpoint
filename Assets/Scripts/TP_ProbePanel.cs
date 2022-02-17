using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TP_ProbePanel : MonoBehaviour
{
    [SerializeField] private GameObject pixelsGO;
    [SerializeField] private GameObject textPanelGO;
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private List<GameObject> tickMarkGOs;
    [SerializeField] private int probePanelPxHeight = 500;
    private ProbeController probeController;

    private Texture2D pixelTex;

    private List<GameObject> textGOs;

    private void Awake()
    {
        pixelTex = new Texture2D(25, probePanelPxHeight);
        pixelTex.filterMode = FilterMode.Point;
        pixelsGO.GetComponent<RawImage>().texture = pixelTex;

        textGOs = new List<GameObject>();
    }

    public float GetPanelHeight()
    {
        return probePanelPxHeight;
    }

    public void RegisterProbeController(ProbeController probeController)
    {
        this.probeController = probeController;
    }

    public ProbeController GetProbeController()
    {
        return probeController;
    }

    public void SetPixel(int x, int y, Color color)
    {
        pixelTex.SetPixel(x, y, color);
    }
    public void ApplyTex()
    {
        pixelTex.Apply();
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
        newText.transform.localPosition = new Vector3(10, pxHeight);
    }

    public void ResizeProbePanel(int newPxHeight)
    {
        probePanelPxHeight = newPxHeight;

        pixelTex = new Texture2D(25, probePanelPxHeight);
        pixelTex.filterMode = FilterMode.Point;
        pixelsGO.GetComponent<RawImage>().texture = pixelTex;
    }
}
