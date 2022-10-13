using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentEditor : MonoBehaviour
{
    [SerializeField] private AccountsManager _accountsManager;

    [SerializeField] private GameObject experimentEditorGO;

    [SerializeField] private GameObject editorListGO;
    [SerializeField] private GameObject experimentListPrefab;

    private List<string> experimentNames;

    public void ShowEditor()
    {
        if (_accountsManager.Connected)
            experimentEditorGO.SetActive(true);
    }

    public void HideEditor()
    {
        experimentEditorGO.SetActive(false);
    }

    public void AddExperiment()
    {
        Debug.Log("Add");
        _accountsManager.AddExperiment();
        UpdateList();
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
        _accountsManager.RemoveExperiment(experiment);
        UpdateList();
    }

    public void UpdateList()
    {
        Debug.Log("List update");
        Transform[] children = new Transform[editorListGO.transform.childCount];
        for (var i = 0; i < children.Length; i++)
            children[i] = editorListGO.transform.GetChild(i);
        foreach (Transform t in children)
            Destroy(t.gameObject);

        experimentNames = _accountsManager.GetExperiments();

        foreach (string experiment in experimentNames)
        {
            GameObject newExpItem = Instantiate(experimentListPrefab, editorListGO.transform);

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
