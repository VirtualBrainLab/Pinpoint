using BrainAtlas;
using BrainAtlas.CoordinateSystems;
using CoordinateTransforms;
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

    [SerializeField] List<string> _transformName;
    [SerializeField] List<string> _transformAllowedAtlas;

    private Dictionary<string, string> _atlasNameMapping;

    public Dictionary<string, AtlasTransform> AtlasTransforms;

    private void Awake()
    {
        if (_atlasNames.Count != _atlasMappings.Count)
            throw new System.Exception("Atlas names and mapped names should be the same length");

        _atlasNameMapping = new();
        for (int i = 0; i < _atlasNames.Count; i++)
            _atlasNameMapping.Add(_atlasNames[i], _atlasMappings[i]);

        // Always add the null transform
        AtlasTransforms = new();
        AtlasTransforms.Add("None", new NullTransform());

        // Build the transform list
        switch (BrainAtlasManager.ActiveReferenceAtlas.Name)
        {
            case "allen_mouse_25um":
                AtlasTransform temp = new Qiu2018Transform();
                AtlasTransforms.Add(temp.Name, temp);

                temp = new Dorr2008Transform();
                AtlasTransforms.Add(temp.Name, temp);

                temp = new Dorr2008IBLTransform();
                AtlasTransforms.Add(temp.Name, temp);
                break;

            // we don't have transforms for waxholm rat
        }
    }

    private void Start()
    {
        // We can trust that 
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

    private void SetNewTransform(AtlasTransform newAtlasTransform)
    {
        BrainAtlasManager.ActiveAtlasTransform = newAtlasTransform;
        WarpBrain();

        // Check if active probe is a mis-match

        // Check all probes for mis-matches
        foreach (ProbeManager probeManager in ProbeManager.Instances)
            probeManager.Update2ActiveTransform();

        UpdateAllProbeUI();
    }



    public void InVivoTransformChanged(int invivoOption)
    {
        Debug.Log("(tpmanager) Attempting to set transform to: " + coordinateTransformOpts.Values.ElementAt(invivoOption).Name);
        if (Settings.BregmaLambdaDistance == 4.15f)
        {
            // if the BL distance is the default, just set the transform
            SetNewTransform(coordinateTransformOpts.Values.ElementAt(invivoOption));
        }
        else
        {
            // if isn't the default, then we have to adjust the transform now
            SetNewTransform(coordinateTransformOpts.Values.ElementAt(invivoOption));
            ChangeBLDistance(Settings.BregmaLambdaDistance);
        }
    }


    #region Warping

    // [TODO]
    public void WarpBrain()
    {
        //foreach (CCFTreeNode node in _modelControl.GetDefaultLoadedNodes())
        //    WarpNode(node);
        //_searchControl.ChangeWarp();
    }

    public void WarpNode()
    {
        //#if UNITY_EDITOR
        //            Debug.Log(string.Format("Transforming node {0}", node.Name));
        //#endif
        //            node.TransformVertices(CoordinateSpaceManager.WorldU2WorldT, true);
    }

    public void UnwarpBrain()
    {
        //foreach (CCFTreeNode node in _modelControl.GetDefaultLoadedNodes())
        //{
        //    node.ClearTransform(true);
        //}
    }



    #endregion
}
