using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class ChannelMapManager : MonoBehaviour
{
    public static ChannelMapManager Instance { get; private set; }
    // TODO: All of these files that are being loaded as asset refs could be converted to assets in advance and then loaded as assets at runtime
    // scriptable objects?

    [SerializeField] private AssetReference[] _channelMapAssetRefs;
    [SerializeField] private List<ProbeProperties.ProbeType> _channelMapProbeTypes;

    [SerializeField] private TMP_Dropdown _selectionOptionDropdown;

    #region Unity

    private void Awake()
    {
        Instance = this;

        // Set up channel maps
        if (_channelMapAssetRefs.Length != _channelMapProbeTypes.Count)
            throw new System.Exception("(ChannelMapManager) Asset references must match number of names");
    }

    #endregion

    #region Public
    public async static Task<ChannelMap> GetChannelMap(ProbeProperties.ProbeType probeType)
    {
        var handle = Addressables.LoadAssetAsync<ChannelMapData>(Instance._channelMapAssetRefs[Instance._channelMapProbeTypes.FindIndex(x => x.Equals(probeType))]);
        await handle.Task;

        return new ChannelMap(handle.Result);
    }

    public async static void UpdateSelectionDropdown()
    {
        // Filter the list of selection layers by the active probe type
        if (ProbeManager.ActiveProbeManager != null)
        {
            var cmapTask = ProbeManager.ActiveProbeManager.GetChannelMap();
            await cmapTask;
            ChannelMap activeChannelMap = cmapTask.Result;
            List<string> selectionLayerNames = activeChannelMap.Data.SelectionLayerNames;

            int activeDropdownIdx = 0;
            var opts = new List<TMP_Dropdown.OptionData>();

            for (int i = 0; i < selectionLayerNames.Count; i++)
            {
                string selectionLayerName = selectionLayerNames[i];
                opts.Add(new TMP_Dropdown.OptionData(selectionLayerName));
                if (selectionLayerName.Equals(ProbeManager.ActiveProbeManager.SelectionLayerName))
                    activeDropdownIdx = i;
            }

            Instance._selectionOptionDropdown.options = opts;
            Instance._selectionOptionDropdown.SetValueWithoutNotify(activeDropdownIdx);
        }
        else
            Instance._selectionOptionDropdown.ClearOptions();
    }

    public void SelectionLayerDropdownChanged()
    {
        ProbeManager.ActiveProbeManager.UpdateSelectionLayer(_selectionOptionDropdown.options[_selectionOptionDropdown.value].text);
    }

    #endregion
}

public class ChannelMap
{

    public Vector3[] ChannelCoords { get { return Data.ChannelCoords; } }
    public Vector3 ChannelShape { get { return Data.ChannelShape; } }

    public float FullHeight { get { return Data.FullHeight; } }

    public string DefaultSelectionLayer { get { return Data.DefaultSelectionLayer; } }
    public List<string> SelectionLayerNames { get { return Data.SelectionLayerNames; } }

    public int ChannelCount { get { return ChannelCoords.Length; } }

    public float MinChannelHeight { get; private set; }
    public float MaxChannelHeight { get; private set; }

    public ChannelMapData Data { get; set; }
    public Texture2D Texture { get; set; }

    public ChannelMap(ChannelMapData data)
    {
        Data = data;

        Texture = new Texture2D(60, 10000, TextureFormat.Alpha8, false);
        Texture.wrapMode = TextureWrapMode.Clamp;
        Texture.filterMode = FilterMode.Point;
        SetSelectionLayer(Data.DefaultSelectionLayer);
    }

    public List<Vector3> GetLayerCoords(string selectionLayerName)
    {
        List<Vector3> coordinates = new();

        int layerIdx = Data.SelectionLayerNames.FindIndex(x => x.Equals(selectionLayerName));
        bool[] layerMask = Data.SelectionLayers[layerIdx].data;

        for (int i = 0; i < Data.ChannelCoords.Length; i++)
            if (layerMask[i])
                coordinates.Add(Data.ChannelCoords[i]);

        return coordinates;
    }

    public void SetSelectionLayer(string selectionLayerName)
    {
        MinChannelHeight = float.MaxValue;
        MaxChannelHeight = 0f;

        Color transparent = new Color(0f, 0f, 0f, 0f);
        Color opaque = new Color(0f, 0f, 0f, 1f);

        Color[] pixels = new Color[Texture.width * Texture.height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = transparent;
        Texture.SetPixels(pixels);

        int layerIdx = Data.SelectionLayerNames.FindIndex(x => x.Equals(selectionLayerName));
        bool[] layerMask = Data.SelectionLayers[layerIdx].data;


        for (int i = 0; i < Data.ChannelCoords.Length; i++)
            if (layerMask[i])
            {
                Vector3 coord = Data.ChannelCoords[i];

                if (coord.y < MinChannelHeight)
                    MinChannelHeight = coord.y;
                if (coord.y > MaxChannelHeight)
                    MaxChannelHeight = coord.y;

                for (int xi = Mathf.RoundToInt(coord.x); xi < coord.x + Data.ChannelShape.x; xi++)
                    for (int yi = Mathf.RoundToInt(coord.y); yi < coord.y + Data.ChannelShape.y; yi++)
                        Texture.SetPixel(xi, yi, opaque);
            }

        MinChannelHeight = MinChannelHeight / 1000f;
        MaxChannelHeight = MaxChannelHeight / 1000f;

        Texture.Apply();
    }
}
