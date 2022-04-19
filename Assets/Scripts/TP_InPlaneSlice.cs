using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class TP_InPlaneSlice : MonoBehaviour
{
    // In plane slice handling
    [SerializeField] private TP_TrajectoryPlannerManager tpmanager;
    [SerializeField] private GameObject inPlaneSliceUIGO;
    [SerializeField] private GameObject inPlaneSliceGO;
    [SerializeField] private CCFModelControl modelControl;
    [SerializeField] private Utils util;
    [SerializeField] private TP_PlayerPrefs localPrefs;

    [SerializeField] private TextMeshProUGUI areaText;

    [SerializeField] private GameObject gpuInPlaneSliceGO;
    [SerializeField] private Texture3D preLoadedGPUTexture;
    private Renderer gpuSliceRenderer;

    private AnnotationDataset annotationDataset;
    private int[,] annotationValues = new int[401, 401];

    private Texture2D inPlaneSliceTex;
    private float probeWidth = 70; // probes are 70um wide

    private RectTransform rect;

    private Texture3D annotationDatasetGPUTexture;
    private TaskCompletionSource<bool> gpuTextureLoadedSource;
    private Task gpuTextureLoadedTask;

    private bool useCPUFallback = true;

    private void Awake()
    {
        gpuTextureLoadedSource = new TaskCompletionSource<bool>();
        gpuTextureLoadedTask = gpuTextureLoadedSource.Task;

        if (gpuInPlaneSliceGO)
            gpuSliceRenderer = gpuInPlaneSliceGO.GetComponent<Renderer>();

        if (useCPUFallback)
        {
            inPlaneSliceGO.SetActive(true);
            gpuInPlaneSliceGO.gameObject.SetActive(false);
        }
        else
        {
            inPlaneSliceGO.SetActive(false);
            gpuInPlaneSliceGO.gameObject.SetActive(true);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        rect = GetComponent<RectTransform>();

        inPlaneSliceTex = new Texture2D(401, 401);
        inPlaneSliceTex.filterMode = FilterMode.Bilinear;

        inPlaneSliceGO.GetComponent<RawImage>().texture = inPlaneSliceTex;
    }

    public Texture3D GetAnnotationDatasetGPUTexture()
    {
        return annotationDatasetGPUTexture;
    }

    public void StartAnnotationDataset()
    {
        annotationDataset = tpmanager.GetAnnotationDataset();
        annotationDataset.ComputeBorders();

        DelayedStartAnnotationDataset();
    }

    public Task GetGPUTextureTask()
    {
        return gpuTextureLoadedTask;
    }

    public async void DelayedStartAnnotationDataset()
    {
        await modelControl.GetDefaultLoaded();

        if (preLoadedGPUTexture != null)
        {
            annotationDatasetGPUTexture = preLoadedGPUTexture;
            gpuTextureLoadedSource.SetResult(true);
        }
        else
        {
            StartCoroutine(DelayedStartBuildGPUTexture());
        }
    }

    private IEnumerator DelayedStartBuildGPUTexture()
    {
        // Build the 3D annotation dataset texture
        annotationDatasetGPUTexture = new Texture3D(528, 320, 456, TextureFormat.RGB24, false);
        annotationDatasetGPUTexture.filterMode = FilterMode.Point;
        annotationDatasetGPUTexture.wrapMode = TextureWrapMode.Clamp;

        Debug.Log("Converting annotation dataset to texture format");
        for (int ap = 0; ap < 528; ap++)
        {
            for (int dv = 0; dv < 320; dv++)
                for (int ml = 0; ml < 456; ml++)
                    annotationDatasetGPUTexture.SetPixel(ap, dv, ml, modelControl.GetCCFAreaColor(annotationDataset.ValueAtIndex(ap, dv, ml)));
            // Running this as a coroutine just allows the user to interact with things while waiting for this to load fully
            // It takes at minimum 100 frames to run this code because of how we structured it here
            if (ap % 5 == 0)
                yield return null;
        }
        annotationDatasetGPUTexture.Apply();

        // Assign 3D texture to the material
        Debug.Log("Assigning texture to material");
        gpuSliceRenderer.material.SetTexture("_Volume", annotationDatasetGPUTexture);
        gpuSliceRenderer.material.SetVector("_VolumeSize", new Vector4(528, 320, 456, 0));

        if (Application.isEditor)
            AssetDatabase.CreateAsset(annotationDatasetGPUTexture, "Assets/AddressableAssets/Textures/AnnotationDatasetTexture3D.asset");

        gpuTextureLoadedSource.SetResult(true);
    }

    // *** INPLANE SLICE CODE *** //
    public void UpdateInPlane()
    {
        inPlaneSliceUIGO.SetActive(localPrefs.GetInplane());
    }

    public void UpdateInPlaneSlice()
    {
        if (!localPrefs.GetInplane()) return;

        TP_ProbeController activeProbeController = tpmanager.GetActiveProbeController();

        // Calculate the size
        float[] heightPerc = activeProbeController.GetRecordingRegionHeight();
        float mmStartPos = heightPerc[0] * (10 - heightPerc[1]);
        float mmRecordingSize = heightPerc[1];

        float inPlaneYmm = mmRecordingSize * 1.5f;
        float inPlaneXmm = inPlaneYmm;

        GameObject.Find("SliceTextX").GetComponent<TextMeshProUGUI>().text = "<- " + inPlaneXmm + "mm ->";
        GameObject.Find("SliceTextY").GetComponent<TextMeshProUGUI>().text = "<- " + inPlaneYmm + "mm ->";

        // Take the active probe, find the position and rotation, and interpolate across the annotation dataset to render a 400x400 image of the brain at that slice
        Transform tipTransform = activeProbeController.GetTipTransform();


        Vector3 tipPosition = tipTransform.position + tipTransform.up * (0.2f + mmStartPos);
        Vector3 tipPositionAPDVLR = util.WorldSpace2apdvlr(tipPosition + tpmanager.GetCenterOffset());

        if (!useCPUFallback)
        {
            gpuSliceRenderer.material.SetVector("_TipPosition", tipPositionAPDVLR);
            gpuSliceRenderer.material.SetVector("_ForwardDirection", tipTransform.forward);
            gpuSliceRenderer.material.SetVector("_UpDirection", tipTransform.up);
            gpuSliceRenderer.material.SetFloat("_RecordingRegionSize", mmRecordingSize);
            gpuSliceRenderer.material.SetFloat("_Scale", mmRecordingSize * 1.5f * 1000f / 25f);
            return;
        }

        // Setup variables for view
        bool fourShank = activeProbeController.GetProbeType() == 4;
        float pixelWidth = inPlaneXmm * 1000 / 401f; // how wide each pixel is in um

        // If this is the fourShank probe, let's shift the center position to be offset so that it's at the center of the four probes
        if (fourShank)
            tipPosition += tipTransform.forward * 0.375f;

        // calculate the center region
        List<int> centerValues = new List<int>();
        List<int> probeStartPos = new List<int>();
        if (fourShank)
        {
            // Note that we offset by 375 so that the center will be centered properly, then we subtract 0/250/500/750 for the corresponding shank
            probeStartPos.Add(200 + Mathf.RoundToInt(375 / pixelWidth));
            probeStartPos.Add(200 + Mathf.RoundToInt((375 - 250) / pixelWidth));
            probeStartPos.Add(200 + Mathf.RoundToInt((375 - 500) / pixelWidth));
            probeStartPos.Add(200 + Mathf.RoundToInt((375 - 750) / pixelWidth));
        }
        else
            probeStartPos.Add(200);

        foreach (int pos in probeStartPos)
            centerValues.Add(pos);

        // probe width
        for (int i = 1; (i * pixelWidth) < (probeWidth / 2f) ; i++)
        {
            foreach (int pos in probeStartPos)
            {
                centerValues.Add(pos + i);
                centerValues.Add(pos - i);
            }
        }

        // Figure out what chunk of the y axis will be the recording region
        int maxYRecordRegion = Mathf.RoundToInt(mmRecordingSize * 401f / inPlaneYmm);

        // using the tip UP and FORWARD axis, go out 2 mm in each direction side to side and up 4mm, and interpolate that into the slice view
        for (int x = 0; x <= 400; x++)
        {
            bool grey = centerValues.Contains(x);
            for (int y = 0; y <= 400; y++)
            {
                // those are the actual x/y positions, convert to mm
                float xmm = (x - 200) * inPlaneXmm / 401f;
                float ymm = (y - 67) * inPlaneYmm / 401f;
                Vector3 pos = util.WorldSpace2apdvlr(tipPosition + tipTransform.up * ymm + tipTransform.forward * -xmm + tpmanager.GetCenterOffset());
                annotationValues[x, y] = annotationDataset.ValueAtIndex(pos);

                // Set the color of this pixel
                if (grey && y > 67)
                    if ((y > 67) && (y < (maxYRecordRegion+67)))
                        inPlaneSliceTex.SetPixel(x, y, Color.red);
                    else
                        inPlaneSliceTex.SetPixel(x, y, Color.gray);
                else
                {
                    // get the apdvlr position
                    // set the texture to the interpolated value at this position
                    if (annotationDataset.BorderAtIndex(pos))
                        inPlaneSliceTex.SetPixel(x, y, Color.black);
                    else
                        inPlaneSliceTex.SetPixel(x, y, modelControl.GetCCFAreaColor(annotationValues[x, y]));
                }
            }
        }

        inPlaneSliceTex.Apply();
    }

    public void InPlaneSliceHover(Vector2 pointerData)
    {
        if (!useCPUFallback) return;

        Vector2 inPlanePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, pointerData, null, out inPlanePos);
        inPlanePos += new Vector2(rect.rect.width, rect.rect.height / 2);
        if (inPlanePos.x > 0 && inPlanePos.y > 0 && inPlanePos.x < 401 && inPlanePos.y < 401)
        {
            int annotation = annotationValues[Mathf.RoundToInt(inPlanePos.x), Mathf.RoundToInt(inPlanePos.y)];
            if (tpmanager.UseAcronyms())
                areaText.text = modelControl.GetCCFAreaAcronym(annotation);
            else
                areaText.text = modelControl.GetCCFAreaName(annotation);
        }
    }

    public void TargetBrainArea(Vector2 pointerData)
    {
        if (!useCPUFallback) return;

        Vector2 inPlanePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, pointerData, null, out inPlanePos);
        inPlanePos += new Vector2(rect.rect.width, rect.rect.height / 2);
        if (inPlanePos.x > 0 && inPlanePos.y > 0 && inPlanePos.x < 401 && inPlanePos.y < 401)
        {
            int annotation = annotationValues[Mathf.RoundToInt(inPlanePos.x), Mathf.RoundToInt(inPlanePos.y)];
            if (annotation > 0)
                tpmanager.SelectBrainArea(annotation);
        }
    }

}
