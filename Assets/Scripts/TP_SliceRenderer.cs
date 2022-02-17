using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TP_SliceRenderer : MonoBehaviour
{
    [SerializeField] private GameObject sagittalSliceGO;
    [SerializeField] private GameObject coronalSliceGO;
    [SerializeField] private TrajectoryPlannerManager tpmanager;
    [SerializeField] private CCFModelControl ccfModelControl;
    [SerializeField] private TP_PlayerPrefs localPrefs;

    private AnnotationDataset annotationDataset;
    private int[] baseSize = { 528, 320, 456 };

    // color data

    private Texture2D sagittalTex;
    private int mlIdx;
    private Texture2D coronalTex;
    private int apIdx = 0;

    bool needToRender = false;

    private bool camXLeft;
    private bool camYBack;

    // Start is called before the first frame update
    void Start()
    {
        annotationDataset = tpmanager.GetAnnotationDataset();

        sagittalTex = new Texture2D(baseSize[0], baseSize[1]);
        sagittalTex.filterMode = FilterMode.Point;
        mlIdx = Mathf.RoundToInt(baseSize[2] / 2);

        sagittalSliceGO.GetComponent<Renderer>().material.mainTexture = sagittalTex;

        coronalTex = new Texture2D(baseSize[2], baseSize[1]);
        coronalTex.filterMode = FilterMode.Point;
        apIdx = Mathf.RoundToInt(baseSize[0] / 2);

        coronalSliceGO.GetComponent<Renderer>().material.mainTexture = coronalTex;

        RenderAnnotationLayer();
    }

    private void Update()
    {
        if (localPrefs.GetSlice3D())
        {
            // Check if the camera moved such that we have to flip the slice quads
            needToRender = UpdateCameraPosition();

            if (tpmanager.MovedThisFrame())
            {
                UpdateSlicePosition();

                needToRender = true;
            }

            if (needToRender) RenderAnnotationLayer();
        }
    }

    private void UpdateSlicePosition()
    {
        ProbeController activeProbeController = tpmanager.GetActiveProbeController();
        Transform activeProbeTipT = activeProbeController.GetTipTransform();
        Vector3 tipPosition = activeProbeTipT.position + activeProbeTipT.up * 0.2f; // add 200 um to get to the start of the recording region

        float mlPosition = tipPosition.x;
        sagittalSliceGO.transform.position = new Vector3(mlPosition, 0f, 0f);
        mlIdx = Mathf.RoundToInt(-(mlPosition - 5.7f) * 40);
        float apPosition = tipPosition.z;
        coronalSliceGO.transform.position = new Vector3(0f, 0f, apPosition);
        apIdx = Mathf.RoundToInt((apPosition + 6.6f) * 40);
    }

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
            // Something changed, rotate and re-render the 
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

    private void RenderAnnotationLayer()
    {
        // Render sagittal slice
        for (int x = 0; x < baseSize[0]; x++)
        {
            for (int y = 0; y < baseSize[1]; y++)
            {
                sagittalTex.SetPixel(camXLeft ? x : baseSize[0] - x, baseSize[1]-y, ccfModelControl.GetCCFAreaColor(annotationDataset.ValueAtIndex(x, y, mlIdx)));
            }
        }
        sagittalTex.Apply();
        // Render coronal slice
        for (int x = 0; x < baseSize[2]; x++)
        {
            for (int y = 0; y < baseSize[1]; y++)
            {
                coronalTex.SetPixel(camYBack ? x : baseSize[2] - x, baseSize[1]-y, ccfModelControl.GetCCFAreaColor(annotationDataset.ValueAtIndex(apIdx, y, x)));
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
        // Force everything to render
        UpdateCameraPosition();
        UpdateSlicePosition();
        RenderAnnotationLayer();
    }
}
