using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ExperimentEditor : MonoBehaviour
{
    [SerializeField] private UnisaveAccountsManager _accountsManager;

    [FormerlySerializedAs("experimentEditorGO")] [SerializeField] private GameObject _experimentEditorGo;

    [FormerlySerializedAs("editorListGO")] [SerializeField] private GameObject _editorListGo;
    [FormerlySerializedAs("experimentListPrefab")] [SerializeField] private GameObject _experimentListPrefab;

    private List<string> experimentNames;

    public void ShowEditor()
    {
        if (_accountsManager.Connected)
            _experimentEditorGo.SetActive(true);
    }

    public void HideEditor()
    {
        _experimentEditorGo.SetActive(false);
    }

    public void AddExperiment()
    {
        Debug.Log("Add");
        _accountsManager.NewExperiment();
        UpdateList();
    }

    public bool IsFocused()
    {
        return _experimentEditorGo.activeSelf;
    }

    public void EditExperiment(string origExpName, string newExpName)
    {
        Debug.Log("Edit");
        _accountsManager.EditExperiment(origExpName, newExpName);
        UpdateList();
    }

    public void RemoveExperiment (string experiment)
    {
        Debug.Log("Remove");
        _accountsManager.DeleteExperiment(experiment);
        UpdateList();
    }

    public void UpdateList()
    {
        Debug.Log("List update");
        Transform[] children = new Transform[_editorListGo.transform.childCount];
        for (var i = 0; i < children.Length; i++)
            children[i] = _editorListGo.transform.GetChild(i);
        foreach (Transform t in children)
            Destroy(t.gameObject);

        experimentNames = _accountsManager.GetExperiments();

        foreach (string experiment in experimentNames)
        {
            GameObject newExpItem = Instantiate(_experimentListPrefab, _editorListGo.transform);

            ExperimentListPanelBehavior expListItem = newExpItem.GetComponent<ExperimentListPanelBehavior>();
            expListItem.ExperimentNameText.text = experiment;
            expListItem.ExperimentNameText.onEndEdit.AddListener(delegate
            {
                string expName = experimentNames[expListItem.transform.GetSiblingIndex()];
                EditExperiment(expName, expListItem.ExperimentNameText.text);
            });
            expListItem.DeleteButton.onClick.AddListener(delegate { RemoveExperiment(expListItem.ExperimentNameText.text); });
        }
    }
}
