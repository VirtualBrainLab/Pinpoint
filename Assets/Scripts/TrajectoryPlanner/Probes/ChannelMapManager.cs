using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private ProbeProperties.ProbeType[] _channelMapProbeTypes;

    [SerializeField] private TMP_Dropdown _selectionOptionDropdown;

    private static Dictionary<ProbeProperties.ProbeType, ChannelMap> _channelMaps;

    #region Unity

    private void Awake()
    {
        Instance = this;

        _channelMaps = new();

        // Set up channel maps
        if (_channelMapAssetRefs.Length != _channelMapProbeTypes.Length)
            throw new System.Exception("(ChannelMapManager) Asset references must match number of names");

        for (int i = 0; i < _channelMapAssetRefs.Length; i++)
        {
            _channelMaps.Add(_channelMapProbeTypes[i], new ChannelMap(_channelMapAssetRefs[i]));
        }
    }

    #endregion

    #region Public
    public static ChannelMap GetChannelMap(ProbeProperties.ProbeType probeType)
    {
        if (_channelMaps.ContainsKey(probeType))
            return _channelMaps[probeType];
        else
            throw new System.Exception($"Channel map {(int)probeType} does not exist");
    }

    public static void UpdateSelectionDropdown()
    {
        // Filter the list of selection layers by the active probe type
        if (ProbeManager.ActiveProbeManager != null)
        {
            ChannelMap activeChannelMap = ProbeManager.ActiveProbeManager.ChannelMap;
            List<string> selectionLayerNames = activeChannelMap.GetSelectionLayerNames();

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
