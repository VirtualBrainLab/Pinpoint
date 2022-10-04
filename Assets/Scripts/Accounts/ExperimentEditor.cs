using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentEditor : MonoBehaviour
{
    [SerializeField] private GameObject experimentEditorGO;

    public void ShowEditor()
    {
        experimentEditorGO.SetActive(true);
    }

    public void HideEditor()
    {
        experimentEditorGO.SetActive(false);
    }

    public void AddExperiment()
    {

    }
}
