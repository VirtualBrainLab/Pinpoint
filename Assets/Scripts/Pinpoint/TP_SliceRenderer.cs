using BrainAtlas;
using System;
using TMPro;
using TrajectoryPlanner;
using UnityEngine;
using UnityEngine.Serialization;

public class TP_SliceRenderer : MonoBehaviour
{
    [FormerlySerializedAs("sagittalSliceGO")] [SerializeField] private GameObject _sagittalSliceGo;
    [FormerlySerializedAs("coronalSliceGO")] [SerializeField] private GameObject _coronalSliceGo;
    [FormerlySerializedAs("tpmanager")] [SerializeField] private TrajectoryPlannerManager _tpmanager;
    [FormerlySerializedAs("inPlaneSlice")] [SerializeField] private TP_InPlaneSlice _inPlaneSlice;
    [FormerlySerializedAs("dropdownMenu")] [SerializeField] private TMP_Dropdown _dropdownMenu;
    [SerializeField] PinpointAtlasManager _pinpointAtlasManager;

    private bool camXLeft;
    private bool camYBack;

    private bool _started;

    private Material saggitalSliceMaterial;
    private Material coronalSliceMaterial;

    private Vector3[] _coronalOrigWorldU;
    private Vector3[] _sagittalOrigWorldU;

    private void Awake()
    {
        saggitalSliceMaterial = _sagittalSliceGo.GetComponent<Renderer>().material;
        coronalSliceMaterial = _coronalSliceGo.GetComponent<Renderer>().material;
        _started = false;
    }

    public void Startup(Texture3D annotationTexture)
    {
        saggitalSliceMaterial.SetTexture("_Volume", annotationTexture);
        coronalSliceMaterial.SetTexture("_Volume", annotationTexture);

        Vector3 dims = BrainAtlasManager.ActiveReferenceAtlas.Dimensions / 2f;

        // x = ml
        // y = dv
        // z = ap

        // (dims.y, dims.z, dims.x)

        // vertex order -x-y, +x-y, -x+y, +x+y

        // -x+y, +x+y, +x-y, -x-y
        _coronalOrigWorldU = new Vector3[] {
                new Vector3(-dims.y, -dims.z, 0f),
                new Vector3(dims.y, -dims.z, 0f),
                new Vector3(-dims.y, dims.z, 0f),
                new Vector3(dims.y, dims.z, 0f)
            };

        // -z+y, +z+y, +z-y, -z-y
        _sagittalOrigWorldU = new Vector3[] {
                new Vector3(0f, -dims.z, -dims.x),
                new Vector3(0f, -dims.z, dims.x),
                new Vector3(0f, dims.z, -dims.x),
                new Vector3(0f, dims.z, dims.x)
            };

        _started = true;
    }

    private float apWorldmm;
    private float mlWorldmm;
    
    /// <summary>
    /// Shift the position of the sagittal and coronal slices to match the tip of the active probe
    /// </summary>
    public void UpdateSlicePosition()
    {
        if (Settings.Slice3DDropdownOption > 0 && _started)
        {
            // Use the un-transformed CCF coordinates to obtain the position in the CCF volume
            Vector3 tipCoordWorld = Vector3.zero;
            if (ProbeManager.ActiveProbeManager != null)
                (tipCoordWorld, _, _, _) = ProbeManager.ActiveProbeManager.ProbeController.GetTipWorldU();

            // vertex order -x-y, +x-y, -x+y, +x+y

            // compute the world vertex positions from the raw coordinates
            // then get the four corners, and warp these according to the active warp
            Vector3[] newCoronalVerts = new Vector3[4];
            Vector3[] newSagittalVerts = new Vector3[4];
            for (int i = 0; i < _coronalOrigWorldU.Length; i++)
            {
                newCoronalVerts[i] = BrainAtlasManager.WorldU2WorldT(new Vector3(_coronalOrigWorldU[i].x, _coronalOrigWorldU[i].y, tipCoordWorld.z), false);
                newSagittalVerts[i] = BrainAtlasManager.WorldU2WorldT(new Vector3(tipCoordWorld.x, _sagittalOrigWorldU[i].y, _sagittalOrigWorldU[i].z), false);
            }

            _coronalSliceGo.GetComponent<MeshFilter>().mesh.vertices = newCoronalVerts;
            _sagittalSliceGo.GetComponent<MeshFilter>().mesh.vertices = newSagittalVerts;

            // Use that coordinate to render the actual slice position
            Vector3 dims = BrainAtlasManager.ActiveReferenceAtlas.Dimensions;

            apWorldmm = dims.x / 2f - tipCoordWorld.z;
            coronalSliceMaterial.SetFloat("_SlicePosition", apWorldmm / dims.x);

            mlWorldmm = dims.y / 2f + tipCoordWorld.x;
            saggitalSliceMaterial.SetFloat("_SlicePosition", mlWorldmm / dims.y);

            UpdateNodeModelSlicing();
        }
    }

    public void UpdateCameraPosition()
    {
        if (Settings.Slice3DDropdownOption == 0 || !_started)
            return;

        Vector3 camPosition = Camera.main.transform.position;
        bool changed = false;
        if (!camXLeft && camPosition.x < 0)
        {
            camXLeft = true;
            changed = true;
        }
        else if (camXLeft && camPosition.x > 0)
        {
            camXLeft = false;
            changed = true;
        }
        else if (!camYBack && camPosition.z < 0)
        {
            camYBack = true;
            changed = true;
        }
        else if (camYBack && camPosition.z > 0)
        {
            camYBack = false;
            changed = true;
        }
        if (changed)
            UpdateNodeModelSlicing();
    }

    private void UpdateNodeModelSlicing()
    {   
        Vector3 dims = BrainAtlasManager.ActiveReferenceAtlas.Dimensions;

        Vector3 tipCoordWorld = Vector3.zero;
        if (ProbeManager.ActiveProbeManager != null)
            (tipCoordWorld, _, _, _) = ProbeManager.ActiveProbeManager.ProbeController.GetTipWorldU();

        foreach (OntologyNode node in _pinpointAtlasManager.DefaultNodes)
        {
            // camYBack means the camera is looking from the back
            if (camYBack)
                // if we're looking from the back, we want to show the brain in the front
                node.SetShaderProperty("_APClip", new Vector2(-dims.x/2f, tipCoordWorld.z));
            else
                node.SetShaderProperty("_APClip", new Vector2(tipCoordWorld.z, dims.x/2f));

            if (!camXLeft)
                // clip from mlPosition forward
                node.SetShaderProperty("_MLClip", new Vector2(tipCoordWorld.x, dims.y/2f));
            else
                node.SetShaderProperty("_MLClip", new Vector2(-dims.y/2f, tipCoordWorld.x));
        }
    }

    private void ClearNodeModelSlicing()
    {
        // Update the renderers on the node objects
        foreach (OntologyNode node in _pinpointAtlasManager.DefaultNodes)
        {
            node.SetShaderProperty("_APClip", Vector2.zero);
            node.SetShaderProperty("_MLClip", Vector2.zero);
        }
    }

    public void ToggleSliceVisibility(int sliceType)
    {
        Debug.Log("here");
        _dropdownMenu.SetValueWithoutNotify(sliceType);

        if (sliceType==0)
        {
            // make slices invisible
            _sagittalSliceGo.SetActive(false);
            _coronalSliceGo.SetActive(false);
            ClearNodeModelSlicing();
            Debug.Log("here2");
        }
        else
        {
            // Standard sagittal/coronal slices
            _sagittalSliceGo.SetActive(true);
            _coronalSliceGo.SetActive(true);

            UpdateCameraPosition();
            Debug.Log("here3");
        }
    }
}
