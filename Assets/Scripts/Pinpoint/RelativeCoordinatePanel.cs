using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using TrajectoryPlanner;
using Urchin.Utils;
using BrainAtlas;

public class RelativeCoordinatePanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField _apField;
    [SerializeField] private TMP_InputField _mlField;
    [SerializeField] private TMP_InputField _dvField;

    private void Awake()
    {
        _apField.onEndEdit.AddListener(delegate { UpdateRelativeCoordinate(); });
        _mlField.onEndEdit.AddListener(delegate { UpdateRelativeCoordinate(); });
        _dvField.onEndEdit.AddListener(delegate { UpdateRelativeCoordinate(); });
    }

    public void SetRelativeCoordinateText(Vector3 coord)
    {
        _apField.text = coord.x.ToString();
        _mlField.text = coord.y.ToString();
        _dvField.text = coord.z.ToString();
    }

    public void UpdateRelativeCoordinate()
    {
        try
        {
            float ap = float.Parse(_apField.text);
            float ml = float.Parse(_mlField.text);
            float dv = float.Parse(_dvField.text);

            Settings.ReferenceCoord = new Vector3(ap, ml, dv);
        }
        catch
        {
            Debug.LogWarning("Bad formatting");
        }
    }

    public void Set2Bregma()
    {
        if (Utils.BregmaDefaults.ContainsKey(BrainAtlasManager.ActiveReferenceAtlas.Name))
            Settings.ReferenceCoord = Utils.BregmaDefaults[BrainAtlasManager.ActiveReferenceAtlas.Name];
        else
            Settings.ReferenceCoord = Vector3.zero;
    }

    public void Set2Lambda()
    {
        if (Utils.LambdaDefaults.ContainsKey(BrainAtlasManager.ActiveReferenceAtlas.Name))
            Settings.ReferenceCoord = Utils.LambdaDefaults[BrainAtlasManager.ActiveReferenceAtlas.Name];
        else
            Settings.ReferenceCoord = Vector3.zero;
    }
}
