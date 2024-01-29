using BrainAtlas;
using BrainAtlas.CoordinateSystems;
using CoordinateTransforms;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PinpointAtlasManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown _atlasDropdown;
    [SerializeField] List<string> _atlasNames;
    [SerializeField] List<string> _atlasMappings;
    [SerializeField] List<bool> _allowedOnWebGL;

    [SerializeField] private TMP_Dropdown _transformDropdown;

    private Dictionary<string, string> _atlasNameMapping;
    private Dictionary<string, bool> _allowedOnWebGLMapping;
    List<string> _allowedNames;

    public HashSet<OntologyNode> DefaultNodes;

    private void Awake()
    {
        DefaultNodes = new();

        if (_atlasNames.Count != _atlasMappings.Count)
            throw new Exception("Atlas names and mapped names should be the same length");

        _atlasNameMapping = new();
        _allowedOnWebGLMapping = new();
        for (int i = 0; i < _atlasNames.Count; i++)
        {
            _atlasNameMapping.Add(_atlasNames[i], _atlasMappings[i]);
            _allowedOnWebGLMapping.Add(_atlasNames[i], _allowedOnWebGL[i]);
        }


        Settings.AtlasTransformChangedEvent += SetNewTransform;
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
            // we don't have transforms (yet) for waxholm rat
            case "waxholm_rat_78um":
                break;
        }

        PopulateAtlasDropdown();
        PopulateTransformDropdown();
    }

    #region Atlas

    public void PopulateAtlasDropdown()
    {
        var atlasNames = BrainAtlasManager.AtlasNames;

#if UNITY_WEBGL
        _allowedNames = new();
        for (int i = 0; i < atlasNames.Count; i++)
            if (_allowedOnWebGLMapping[atlasNames[i]])
                _allowedNames.Add(atlasNames[i]);
#else
        var _allowedNames = atlasNames;
#endif

        _atlasDropdown.options = _allowedNames.ConvertAll(x => ConvertAtlas2Userfriendly(x));
    }

    public void ResetAtlasDropdownIndex()
    {
        string activeAtlas = BrainAtlasManager.ActiveReferenceAtlas.Name;
        _atlasDropdown.SetValueWithoutNotify(_atlasDropdown.options.FindIndex(x => x.text.Equals(_atlasNameMapping[activeAtlas])));
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
        PlayerPrefs.SetInt("scene-atlas-reset", 1);
        Settings.AtlasName = _allowedNames[option];
#if UNITY_EDITOR
        Debug.Log($"(PAM) Resetting atlas to {Settings.AtlasName}");
#endif
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
        _transformDropdown.options = BrainAtlasManager.AtlasTransforms.ConvertAll(x => new TMP_Dropdown.OptionData(ConverTransform2UserFriendly(x.Name)));
    }

    public void ResetTransformDropdownIndex()
    {
        string activeTransformName = BrainAtlasManager.ActiveAtlasTransform.Name;
        if (activeTransformName == "Custom")
            _transformDropdown.SetValueWithoutNotify(-1);
        else
            _transformDropdown.SetValueWithoutNotify(BrainAtlasManager.AtlasTransforms.FindIndex(x => x.Name.Equals(activeTransformName)));
    }

    public void SetTransform(int idx)
    {
        Settings.AtlasTransformName = BrainAtlasManager.AtlasTransforms[idx].Name;
    }

    public void SetNewTransform(string transformName)
    {
#if UNITY_EDITOR
        Debug.Log($"Atlas transform set to {transformName}");
#endif
        SetNewTransform(BrainAtlasManager.AtlasTransforms.Find(x => x.Name.Equals(transformName)));
    }

    public void SetNewTransform(AtlasTransform newTransform)
    {
        BrainAtlasManager.ActiveAtlasTransform = newTransform;
        ResetTransformDropdownIndex();

        // Check all probes for mis-matches
        foreach (ProbeManager probeManager in ProbeManager.Instances)
            probeManager.Update2ActiveTransform();
    }

    private string ConverTransform2UserFriendly(string transformName)
    {
        return $"Atlas transform: {transformName}";
    }

    #endregion


    #region Warping
    Vector3 _activeWarp;

    public void WarpBrain()
    {
#if UNITY_EDITOR
        Debug.Log("(PAM) Warp brain called");
#endif
        Vector3 newWarp = WorldU2WorldT_Wrapper(Vector3.one);

        // Check if the brain actually needs to be warped
        if (newWarp == _activeWarp)
        {
#if UNITY_EDITOR
            Debug.Log("(PAM) Active warp matches: saving time by skipping");
#endif
            return;
        }

        _activeWarp = newWarp;

        foreach (OntologyNode node in DefaultNodes)
            WarpNode(node, WorldU2WorldT_Wrapper);

        foreach (int areaID in TP_Search.VisibleSearchedAreas)
            WarpNode(BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Node(areaID), WorldU2WorldT_Wrapper);
    }

    public static void WarpNode(OntologyNode node, Func<Vector3, Vector3> warpFunction)
    {
        node.ApplyAtlasTransform(warpFunction);
    }

    public void UnwarpBrain()
    {
        foreach (OntologyNode node in DefaultNodes)
        {
            node.ResetAtlasTransform();
        }
    }

    public static Vector3 WorldU2WorldT_Wrapper(Vector3 input)
    {
        return BrainAtlasManager.WorldU2WorldT(input, true);
    }


#endregion
}
