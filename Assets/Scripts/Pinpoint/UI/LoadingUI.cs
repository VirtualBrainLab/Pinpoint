using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] TMP_Text _loadingText;
    [SerializeField] GameObject _loadingPanel;

    string baseString = "Pinpoint is loading...\n";

    private void Awake()
    {
        _loadingPanel.SetActive(true);
        _loadingText.text = $"{baseString}getting metadata from server";
    }

    public void MetaLoaded()
    {
        _loadingText.text = $"{baseString}acquiring and transforming 3D mesh files";
    }

    public void AtlasLoaded()
    {
        _loadingText.text = $"{baseString}building annotation textures";
    }

    public void AnnotationsLoaded()
    {
        _loadingText.text = $"{baseString}finalizing 3D scene";
    }

    public void SceneLoaded()
    {
        _loadingText.text = $"{baseString}loading settings and launching app!";
    }

    public void Complete()
    {
        gameObject.SetActive(false);
    }
}
