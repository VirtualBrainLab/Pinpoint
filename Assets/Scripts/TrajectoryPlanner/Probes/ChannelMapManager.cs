using System.Collections.Generic;
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

    [SerializeField] private List<AssetReference> _channelMapAssetRefs;
    [SerializeField] private List<string> _channelMapNames;
    [SerializeField] private List<AssetReference> _selectionLayerAssetRefs;
    [SerializeField] private List<string> _selectionLayerNames;
    [SerializeField] private List<int> _allowedProbeTypes;

    [SerializeField] private TMP_Dropdown _selectionOptionDropdown;

    private static Dictionary<string, ChannelMap> _channelMaps;
    private static Dictionary<string, bool[]> _selectionLayers;

    #region Unity

    private async void Awake()
    {
        Instance = this;

        _channelMaps = new();
        _selectionLayers = new();

        // Set up channel maps
        if (_channelMapAssetRefs.Count != _channelMapNames.Count)
            throw new System.Exception("(ChannelMapManager) Asset references must match number of names");

        for (int i = 0; i < _channelMapAssetRefs.Count; i++)
        {
            _channelMaps.Add(_channelMapNames[i], new ChannelMap(_channelMapAssetRefs[i]));
        }

        // Set up selection layer dictionary
        if (_selectionLayerAssetRefs.Count != _selectionLayerNames.Count)
            throw new System.Exception("(ChannelMapManager) Select layer references must match number of names");

        for (int i = 0; i < _selectionLayerAssetRefs.Count; i++)
        {
            var boolArrayTask = ConvertSelectionLayer2Bool(_selectionLayerAssetRefs[i]);
            await boolArrayTask;
            _selectionLayers.Add(_selectionLayerNames[i], boolArrayTask.Result);
        }
    }

    #endregion

    #region Public
    public static ChannelMap GetChannelMap(string channelMapName)
    {
        if (_channelMaps.ContainsKey(channelMapName))
            return _channelMaps[channelMapName];
        else
            throw new System.Exception($"Channel map {channelMapName} does not exist");
    }

    public static bool[] GetSelectionLayer(string selectionLayerName)
    {
        if (_selectionLayers.ContainsKey(selectionLayerName))
            return _selectionLayers[selectionLayerName];
        else
            throw new System.Exception($"Selection layer name {selectionLayerName} does not exist");
    }

    public static void UpdateSelectionDropdown()
    {
        // Filter the list of selection layers by the active probe type
        List<int> matchingIndexes = Instance._allowedProbeTypes.FindAll((x) => x.Equals(ProbeManager.ActiveProbeManager.ProbeType));

        var opts = new List<TMP_Dropdown.OptionData>();

        foreach (int idx in matchingIndexes)
        {
            opts.Add(new TMP_Dropdown.OptionData(Instance._selectionLayerNames[idx]));
        }

        Instance._selectionOptionDropdown.options = opts;
        Instance._selectionOptionDropdown.SetValueWithoutNotify(0);
    }

    public void SelectionLayerDropdownChanged()
    {
        ProbeManager.ActiveProbeManager.UpdateChannelMap(_selectionLayers[_selectionLayerNames[_selectionOptionDropdown.value]]);
    }

    #endregion

    #region Private

    private async Task<bool[]> ConvertSelectionLayer2Bool(AssetReference selectionLayerAssetRef)
    {
        // Load the asset CSV file
        AsyncOperationHandle<TextAsset> loadHandler = Addressables.LoadAssetAsync<TextAsset>(selectionLayerAssetRef);
        await loadHandler.Task;

        string[] separated = loadHandler.Result.text.Split(' ');

        bool[] selected = new bool[separated.Length];
        for (int i = 0; i < separated.Length; i++)
        {
            selected[i] = int.Parse(separated[i]) == 1;
        }

        return selected;
    }

    #endregion
}
