using BrainAtlas;
using BrainAtlas.CoordinateSystems;
using CoordinateTransforms;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Urchin.Managers;

public class PinpointAtlasManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown _atlasDropdown;
    [SerializeField] List<string> _atlasNames;
    [SerializeField] List<string> _atlasMappings;

    [SerializeField] private TMP_Dropdown _transformDropdown;

    private Dictionary<string, string> _atlasNameMapping;

    public HashSet<OntologyNode> DefaultNodes;

    private void Awake()
    {
        DefaultNodes = new();

        if (_atlasNames.Count != _atlasMappings.Count)
            throw new Exception("Atlas names and mapped names should be the same length");

        _atlasNameMapping = new();
        for (int i = 0; i < _atlasNames.Count; i++)
            _atlasNameMapping.Add(_atlasNames[i], _atlasMappings[i]);

        Settings.InvivoTransformChangedEvent += SetNewTransform;
    }

    public void Startup()
    {
        // Build the transform list
        switch (BrainAtlasManager.ActiveReferenceAtlas.Name)
        {
            case "allen_mouse_25um":
                BrainAtlasManager.AtlasTransforms.Add(new Qiu2018Transform());
                BrainAtlasManager.AtlasTransforms.Add(new Dorr2008Transform());
                BrainAtlasManager.AtlasTransforms.Add(new Dorr2008IBLTransform());
                break;

            // we don't have transforms (yet) for waxholm rat
            case "waxholm_rat_39um":
                break;
        }

        PopulateTransformDropdown();
    }

    #region Atlas

    public void PopulateAtlasDropdown()
    {
        var atlasNames = BrainAtlasManager.AtlasNames;

        _atlasDropdown.options = atlasNames.ConvertAll(ConvertAtlas2Userfriendly);
    }

    public void ResetAtlasDropdownIndex()
    {
        Debug.Log(BrainAtlasManager.ActiveReferenceAtlas.Name);
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
        PlayerPrefs.SetInt("scene-reset", 1);
        Settings.AtlasName = BrainAtlasManager.AtlasNames[option];
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private TMP_Dropdown.OptionData ConvertAtlas2Userfriendly(string atlasName)
    {
        if (_atlasNameMapping.ContainsKey(atlasName))
            return new TMP_Dropdown.OptionData(_atlasNameMapping[atlasName]);

        return new TMP_Dropdown.OptionData(atlasName);
    }

    #endregion

    #region Transforms

    public void PopulateTransformDropdown()
    {
        var transformNames = BrainAtlasManager.AtlasTransforms;

        _transformDropdown.options = transformNames.ConvertAll(x => new TMP_Dropdown.OptionData(x.Name));
    }

    public void ResetTransformDropdownIndex()
    {
        string activeTransformName = BrainAtlasManager.ActiveAtlasTransform.Name;
        _transformDropdown.SetValueWithoutNotify(_atlasDropdown.options.FindIndex(x => x.text.Equals(activeTransformName)));
    }

    public void SetTransform(int option)
    {
        int idx = BrainAtlasManager.AtlasTransforms.FindIndex(x => x.Name.Equals(_transformDropdown.options[option].text));
        AtlasTransform newTransform = BrainAtlasManager.AtlasTransforms[idx];

        Settings.InvivoTransformName = newTransform.Name;
    }

    public void SetNewTransform(string transformName)
    {
        BrainAtlasManager.ActiveAtlasTransform = BrainAtlasManager.AtlasTransforms.Find(x => x.Name.Equals(transformName));

        // Check all probes for mis-matches
        foreach (ProbeManager probeManager in ProbeManager.Instances)
            probeManager.Update2ActiveTransform();

        // custom transforms disabled for now...
        //if (Settings.BregmaLambdaDistance == 4.15f)
        //{
        //    // if the BL distance is the default, just set the transform
        //    SetNewTransform(coordinateTransformOpts.Values.ElementAt(invivoOption));
        //}
        //else
        //{
        //    // if isn't the default, then we have to adjust the transform now
        //    SetNewTransform(coordinateTransformOpts.Values.ElementAt(invivoOption));
        //    ChangeBLDistance(Settings.BregmaLambdaDistance);
        //}

        WarpBrain();
    }

    #endregion


    #region Warping

    public void WarpBrain()
    {
        foreach (OntologyNode node in DefaultNodes)
            WarpNode(node);
    }

    public void WarpNode(OntologyNode node)
    {
        node.ApplyAtlasTransform(WorldU2WorldT_Wrapper);
    }

    public void UnwarpBrain()
    {
        foreach (OntologyNode node in DefaultNodes)
        {
            node.ApplyAtlasTransform(WorldU2WorldT_Wrapper);
        }
    }

    private Vector3 WorldU2WorldT_Wrapper(Vector3 input)
    {
        return BrainAtlasManager.WorldU2WorldT(input);
    }


    #endregion
}
