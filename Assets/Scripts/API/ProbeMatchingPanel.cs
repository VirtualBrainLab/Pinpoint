using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Probe Matching Panel behavior
/// 
/// This panel handles tracking data about which probes are matched to each other between Pinpoint and SpikeGLX/OpenEphys
/// When the possible target probe list gets updated this function handles generating the list
/// 
/// APIManager connected -> UpdateUI
/// Probes added/deleted -> UpdateUI
/// 
/// APIManager connected -> UpdateMatchingOptions
/// </summary>
public class ProbeMatchingPanel : MonoBehaviour
{
    [SerializeField] private APIManager _apiManager;
    [SerializeField] private GameObject _matchingPanelPrefab;
    [SerializeField] private Transform _matchingPanelParentT;

    private Dictionary<ProbeManager, ProbeMatchDropdown> _dropdownMenus;
    private List<string> _probeOpts;

    private void Awake()
    {
        _dropdownMenus = new();
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    /// <summary>
    /// Ensure that each probe manager has a matching ProbeMatchDropdown prefab instantiated
    /// </summary>
    public void UpdateUI()
    {
        if (!isActiveAndEnabled)
            return;

        foreach (ProbeManager probeManager in ProbeManager.Instances)
        {
            if (_dropdownMenus.ContainsKey(probeManager))
            {
                // re-register to ensure we are linked properly with the dropdown options
                _dropdownMenus[probeManager].Register(probeManager);
            }
            else
            {
                // build a new prefab
                GameObject go = Instantiate(_matchingPanelPrefab, _matchingPanelParentT);
                ProbeMatchDropdown ui = go.GetComponent<ProbeMatchDropdown>();
                _dropdownMenus.Add(probeManager, ui);
                ui.Register(probeManager);
                ui.DropdownChangedEvent.AddListener(_apiManager.TriggerAPIPush);
            }
        }

        UpdateMatchingPanelOptions();
    }

    // Clear the probe matching panel UI entirely
    public void ClearUI()
    {
        _dropdownMenus?.Clear();

        for (int i = _matchingPanelParentT.childCount - 1; i >= 0; i--)
            Destroy(_matchingPanelParentT.GetChild(i).gameObject);
    }

    /// <summary>
    /// Update all the dropdown menus so that their option lists match the available options.
    /// 
    /// If a probe's previous option is gone, default to the first option.
    /// </summary>
    /// <param name="probeOpts"></param>
    public void UpdateMatchingPanelOptions(List<string> probeOpts)
    {
        _probeOpts = probeOpts;
        UpdateMatchingPanelOptions();
    }

    #region private

    private void UpdateMatchingPanelOptions()
    {
        if (_probeOpts == null)
            return;

        foreach (ProbeMatchDropdown ui in _dropdownMenus.Values)
        {
            ui.UpdateDropdown(_probeOpts);
        }
    }
    #endregion
}
