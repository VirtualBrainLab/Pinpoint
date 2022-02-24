using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TP_SliceRenderer : MonoBehaviour
{
    [SerializeField] private GameObject sagittalSliceGO;
    [SerializeField] private GameObject coronalSliceGO;
    [SerializeField] private TrajectoryPlannerManager tpmanager;
    [SerializeField] private CCFModelControl modelControl;
    [SerializeField] private TP_PlayerPrefs localPrefs;

    private AnnotationDataset annotationDataset;
    private int[] baseSize = { 528, 320, 456 };

    // color data

    private Texture2D sagittalTex;
    private int mlIdx;
    private float trueML;
    private Texture2D coronalTex;
    private int apIdx;
    private float trueAP;

    bool needToRender = false;
    bool loaded = false;

    private bool camXLeft;
    private bool camYBack;

    // Start is called before the first frame update
    void Start()
    {
        sagittalTex = new Texture2D(baseSize[0], baseSize[1]);
        sagittalTex.filterMode = FilterMode.Point;
        mlIdx = Mathf.RoundToInt(baseSize[2] / 2);
        sagittalSliceGO.GetComponent<Renderer>().material.mainTexture = sagittalTex;

        coronalTex = new Texture2D(baseSize[2], baseSize[1]);
        coronalTex.filterMode = FilterMode.Point;
        apIdx = Mathf.RoundToInt(baseSize[0] / 2);
        coronalSliceGO.GetComponent<Renderer>().material.mainTexture = coronalTex;

        needToRender = localPrefs.GetSlice3D();
    }

    public void AsyncStart()
    {
        annotationDataset = tpmanager.GetAnnotationDataset();
        loaded = true;
        ToggleSliceVisibility(localPrefs.GetSlice3D());
    }

    private void Update()
    {
        if (localPrefs.GetSlice3D() && loaded)
        {
            // Check if the camera moved such that we have to flip the slice quads
            needToRender = UpdateCameraPosition();

            if (tpmanager.MovedThisFrame())
            {
                UpdateSlicePosition();

                needToRender = true;
            }

            if (needToRender)
            {
                RenderAnnotationLayer();
                UpdateNodeModelSlicing();
            }
        }
    }
    
    /// <summary>
    /// Shift the position of the sagittal and coronal slices to match the tip of the active probe
    /// </summary>
    private void UpdateSlicePosition()
    {
        ProbeController activeProbeController = tpmanager.GetActiveProbeController();
        if (activeProbeController == null) return;
        Transform activeProbeTipT = activeProbeController.GetTipTransform();
        Vector3 tipPosition = activeProbeTipT.position + activeProbeTipT.up * 0.2f; // add 200 um to get to the start of the recording region

        float mlPosition = tipPosition.x;
        trueML = -(mlPosition - 5.7f);
        sagittalSliceGO.transform.position = new Vector3(mlPosition, 0f, 0f);
        mlIdx = Mathf.RoundToInt(trueML * 40);
        float apPosition = tipPosition.z;
        trueAP = apPosition + 6.6f;
        coronalSliceGO.transform.position = new Vector3(0f, 0f, apPosition);
        apIdx = Mathf.RoundToInt(trueAP * 40);
    }

    private void UpdateNodeModelSlicing()
    {
        // Update the renderers on the node objects
        foreach (CCFTreeNode node in modelControl.DefaultLoadedNodes())
        {
            if (camYBack)
                // clip from apPosition forward
                node.SetShaderProperty("_APClip", new Vector2(0f, trueAP));
            else
                node.SetShaderProperty("_APClip", new Vector2(trueAP, 13.2f));

            if (camXLeft)
                // clip from mlPosition forward
                node.SetShaderProperty("_MLClip", new Vector2(trueML, 11.4f));
            else
                node.SetShaderProperty("_MLClip", new Vector2(0f, trueML));
        }
    }

    private void ClearNodeModelSlicing()
    {
        // Update the renderers on the node objects
        foreach (CCFTreeNode node in modelControl.DefaultLoadedNodes())
        {
            node.SetShaderProperty("_APClip", new Vector2(0f, 13.2f));
            node.SetShaderProperty("_MLClip", new Vector2(0f, 11.4f));
        }
    }

    /// <summary>
    /// When the camera swtiches from looking from the "left" or "right" of a slice, flip the direction we are rendering
    /// [TODO: this would be better to solve with a two-sided shader, the current approach causes a lot of lag]
    /// </summary>
    /// <returns></returns>
    private bool UpdateCameraPosition()
    {
        Vector3 camPosition = Camera.main.transform.position;
        bool changed = false;
        if (camXLeft && camPosition.x < 0)
        {
            camXLeft = false;
            changed = true;
        }
        else if (!camXLeft && camPosition.x > 0)
        {
            camXLeft = true;
            changed = true;
        }
        else if (camYBack && camPosition.z < 0)
        {
            camYBack = false;
            changed = true;
        }
        else if (!camYBack && camPosition.z > 0)
        {
            camYBack = true;
            changed = true;
        }

        if (changed)
        {
            // Something changed, rotate and re-render the slices
            if (camXLeft)
                sagittalSliceGO.transform.localRotation = Quaternion.Euler(new Vector3(0f, -90f, 0f));
            else
                sagittalSliceGO.transform.localRotation = Quaternion.Euler(new Vector3(0f, -270f, 0f));

            if (camYBack)
                coronalSliceGO.transform.localRotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));
            else
                coronalSliceGO.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));

            return true;
        }
        return false;
    }

    /// <summary>
    /// Draw onto the sagittal and coronal slices the annotations at the current slice position in the CCF
    /// </summary>
    private void RenderAnnotationLayer()
    {
        // prevent use of annotation dataset until it is loaded properly
        if (!loaded) return;

        // Render sagittal slice
        for (int x = 0; x < baseSize[0]; x++)
        {
            for (int y = 0; y < baseSize[1]; y++)
            {
                sagittalTex.SetPixel(camXLeft ? x : baseSize[0] - x, baseSize[1]-y, modelControl.GetCCFAreaColor(annotationDataset.ValueAtIndex(x, y, mlIdx)));
            }
        }
        sagittalTex.Apply();
        // Render coronal slice
        for (int x = 0; x < baseSize[2]; x++)
        {
            for (int y = 0; y < baseSize[1]; y++)
            {
                coronalTex.SetPixel(camYBack ? x : baseSize[2] - x, baseSize[1]-y, modelControl.GetCCFAreaColor(annotationDataset.ValueAtIndex(apIdx, y, x)));
            }
        }
        coronalTex.Apply();

        needToRender = false;
    }

    public void ToggleSliceVisibility(bool visible)
    {
        localPrefs.SetSlice3D(visible);
        sagittalSliceGO.SetActive(visible);
        coronalSliceGO.SetActive(visible);
        if (visible)
        {
            // Force everything to render
            UpdateCameraPosition();
            UpdateSlicePosition();
            RenderAnnotationLayer();
            UpdateNodeModelSlicing();
        }
        else
        {
            ClearNodeModelSlicing();
        }
    }
}
