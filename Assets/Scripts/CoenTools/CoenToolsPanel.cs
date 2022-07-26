using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CoenToolsPanel : MonoBehaviour
{
    [SerializeField] TMP_Text distanceText;
    private List<GameObject> tipAndModelGOs;

    private float distance;

    public void ChangeDistance(float newDistance)
    {
        distance = newDistance;
    }

    public void OnDisable()
    {
        
    }

    public void OnEnable()
    {
        // Find the relevant GameObjects
    }
}
