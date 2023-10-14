using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Urchin.Utils;

public class ActiveExperimentUI : MonoBehaviour
{
    [SerializeField] private UnisaveAccountsManager _accountsManager;

    #region Active experiment variables
    [SerializeField] private TMP_Dropdown _optionList;
    #endregion

    #region Insertion variables
    [SerializeField] private Transform _insertionPrefabParentT;
    [SerializeField] private GameObject _insertionPrefabGO;

    private string _currentExperiment;
    private Dictionary<string, ServerProbeInsertionUI> _activeInsertionUIs;
    #endregion

    #region Unity
    private void Awake()
    {
        _activeInsertionUIs = new Dictionary<string, ServerProbeInsertionUI>();
    }

    private void Start()
    {
        UpdateList();
        UpdateUIPanels();
    }
    #endregion

    #region Active experiment list
    public void UpdateList()
    {
        int prevIdx = _optionList.value;

        List<string> experimentList = _accountsManager.GetExperiments();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (string experiment in experimentList)
            options.Add(new TMP_Dropdown.OptionData(experiment));
        _optionList.ClearOptions();
        _optionList.AddOptions(options);
        SelectExperiment(prevIdx);
    }

    public void SelectExperiment(int optIdx)
    {
        if (_optionList.options.Count > optIdx)
            _accountsManager.ActiveExperimentChanged(_optionList.options[optIdx].text);
    }
    #endregion

    #region Insertions
    public void UpdateExperimentInsertionUIPanels()
    {
        // If the accounts manager is disconnected, clear the view
        if (!_accountsManager.Connected)
        {
            ResetUIPanels();
            return;
        }

        // don't bother updating if we are disabled
        if (!gameObject.activeSelf)
            return;

        // If the experiment was changed, reset the whole panel
        if (!_accountsManager.ActiveExperiment.Equals(_currentExperiment))
            ResetUIPanels();

        // Then, update the data in the panels
        UpdateUIPanels();
    }

    private void ResetUIPanels()
    {
        if (_activeInsertionUIs.Count > 0)
            foreach (ServerProbeInsertionUI probeInsertionUI in _activeInsertionUIs.Values)
                Destroy(probeInsertionUI.gameObject);
        _activeInsertionUIs = new Dictionary<string, ServerProbeInsertionUI>();
    }

    private void UpdateUIPanels()
    {
        Debug.Log("UI panels updated");
        var experimentData = _accountsManager.GetActiveExperimentInsertions();

        // Remove any panels that shouldn't exist
        foreach (var panelUI in _activeInsertionUIs)
            if (!experimentData.Keys.Contains(panelUI.Key))
                Destroy(panelUI.Value.gameObject);

        foreach (var kvp in experimentData)
        {
            string UUID = kvp.Key;

            if (!_activeInsertionUIs.ContainsKey(UUID))
            {
                // We don't have a panel yet for this insertion, add it now
                GameObject newPanel = Instantiate(_insertionPrefabGO, _insertionPrefabParentT);
                _activeInsertionUIs.Add(UUID, newPanel.GetComponent<ServerProbeInsertionUI>());
            }

            // Update this panel
            ServerProbeInsertion insertionData = kvp.Value;
            ServerProbeInsertionUI insertionUI = _activeInsertionUIs[UUID];

            // Get angles
            Vector3 angles = new Vector3(insertionData.phi, insertionData.theta, insertionData.spin);
            if (Settings.UseIBLAngles)
                angles = Utils.World2IBL(angles);

            // Set the insertion data and active state
            insertionUI.SetInsertionData(_accountsManager, insertionData.UUID, insertionData.name, insertionData.active);
            insertionUI.SetColor(insertionData.color);

            if (Settings.DisplayUM)
                insertionUI.UpdateDescription(string.Format("AP {0} ML {1} DV {2} Yaw {3} Pitch {4} Roll {5}",
                    Mathf.RoundToInt(insertionData.ap * 1000f), Mathf.RoundToInt(insertionData.ml * 1000f), Mathf.RoundToInt(insertionData.dv * 1000f),
                    angles.x, angles.y, angles.z));
            else
                insertionUI.UpdateDescription(string.Format("AP {0:0.00} ML {1:0.00} DV {2:0.00} Yaw {3} Pitch {4} Roll {5}",
                    insertionData.ap, insertionData.ml, insertionData.dv,
                    angles.x, angles.y, angles.z));
        }
    }
    #endregion
}
