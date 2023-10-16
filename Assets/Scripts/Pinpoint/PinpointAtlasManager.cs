using BrainAtlas;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PinpointAtlasManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown _atlasDropdown;
    [SerializeField] List<string> _atlasNames;
    [SerializeField] List<string> _atlasMappings;

    private Dictionary<string, string> _atlasNameMapping;

    private void Awake()
    {
        if (_atlasNames.Count != _atlasMappings.Count)
            throw new System.Exception("Atlas names and mapped names should be the same length");

        _atlasNameMapping = new();
        for (int i = 0; i < _atlasNames.Count; i++)
            _atlasNameMapping.Add(_atlasNames[i], _atlasMappings[i]);
    }

    public void PopulateAtlasDropdown()
    {
        var atlasNames = BrainAtlasManager.AtlasNames;

        _atlasDropdown.options = atlasNames.ConvertAll(ConvertToFullName);
    }

    public void ResetAtlasDropdownIndex()
    {
        string activeAtlas = BrainAtlasManager.ActiveReferenceAtlas.Name;
        _atlasDropdown.SetValueWithoutNotify(_atlasDropdown.options.FindIndex(x => x.text.Equals(activeAtlas)));
    }

    public void SetAtlas(int option)
    {
        // force the scene to reset
        QuestionDialogue.Instance.YesCallback = delegate { ResetScene(option); };
        QuestionDialogue.Instance.NoCallback = delegate { ResetAtlasDropdownIndex(); };
        QuestionDialogue.Instance.NewQuestion("Changing the Atlas will reset the scene.\nAre you sure you want to proceed?");
    }

    private void ResetScene(int option)
    {
        Settings.AtlasName = BrainAtlasManager.AtlasNames[option];
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private TMP_Dropdown.OptionData ConvertToFullName(string atlasName)
    {
        if (_atlasNameMapping.ContainsKey(atlasName))
            return new TMP_Dropdown.OptionData(_atlasNameMapping[atlasName]);

        return new TMP_Dropdown.OptionData(atlasName);
    }
}
