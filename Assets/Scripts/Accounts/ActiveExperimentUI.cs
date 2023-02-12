using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ActiveExperimentUI : MonoBehaviour
{
    [SerializeField] private UnisaveAccountsManager _accountsManager;

    #region Active experiment variables
    [SerializeField] private TMP_Dropdown _optionList;
    #endregion

    #region Insertion variables
    [SerializeField] private Transform _insertionPrefabParentT;
    [SerializeField] private GameObject _insertionPrefabGO;
    #endregion

    #region Unity
    private void Awake()
    {
        _optionList = GetComponent<TMP_Dropdown>();
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
        // Destroy all children
        for (int i = _insertionPrefabParentT.childCount - 1; i >= 0; i--)
            Destroy(_insertionPrefabParentT.GetChild(i).gameObject);

        // Add new child prefabs that have the properties matched to the current experiment
        var experimentData = _accountsManager.GetActiveExperimentInsertions();

        foreach (ServerProbeInsertion insertion in experimentData.Values)
        {
            // Create a new prefab
            GameObject insertionPrefab = Instantiate(_insertionPrefabGO, _insertionPrefabParentT);
            ServerProbeInsertionUI insertionUI = insertionPrefab.GetComponent<ServerProbeInsertionUI>();

            // Insertions should be marked as active if they are in the scene already
            bool active = ProbeManager.instances.Any(x => experimentData.Keys.Contains(x.UUID));

            Vector3 angles = new Vector3(insertion.phi, insertion.theta, insertion.spin);
            if (Settings.UseIBLAngles)
                angles = TP_Utils.World2IBL(angles);

            insertionUI.SetInsertionData(_accountsManager, insertion.UUID, insertion.name, active);
            if (Settings.DisplayUM)
                insertionUI.UpdateDescription(string.Format("AP {0} ML {1} DV {2} Phi {3} Theta {4} Spin {5}",
                    Mathf.RoundToInt(insertion.ap * 1000f), Mathf.RoundToInt(insertion.ml * 1000f), Mathf.RoundToInt(insertion.dv * 1000f),
                    angles.x, angles.y, angles.z));
            else
                insertionUI.UpdateDescription(string.Format("AP {0:0.00} ML {1:0.00} DV {2:0.00} Phi {3} Theta {4} Spin {5}",
                    insertion.ap, insertion.ml, insertion.dv,
                    angles.x, angles.y, angles.z));
        }
    }

    public void SetInsertionActiveToggle(string UUID, bool state)
    {
        // Find the UI object and disable the active state
#if UNITY_EDITOR
        Debug.Log($"(Accounts) {UUID} was destroyed, disabling UI element, if it exists");
#endif
        foreach (ServerProbeInsertionUI serverProbeInsertionUI in _insertionPrefabParentT.GetComponentsInChildren<ServerProbeInsertionUI>())
            if (serverProbeInsertionUI.UUID.Equals(UUID))
            {
                serverProbeInsertionUI.SetToggle(false);
                return;
            }
    }
    #endregion
}
