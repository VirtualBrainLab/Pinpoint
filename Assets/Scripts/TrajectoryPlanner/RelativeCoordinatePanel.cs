using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using TrajectoryPlanner;

public class RelativeCoordinatePanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField _apField;
    [SerializeField] private TMP_InputField _mlField;
    [SerializeField] private TMP_InputField _dvField;

    [SerializeField] private TrajectoryPlannerManager _tpmanager;

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
            float dv = float.Parse(_mlField.text);

            Settings.RelativeCoordinate = new Vector3(ap, ml, dv);
        }
        catch
        {
            Debug.LogWarning("Bad formatting");
        }
    }

    public void Set2Bregma()
    {
        Settings.RelativeCoordinate = Utils.IBL_BREGMA;
    }

    public void Set2Lambda()
    {
        Settings.RelativeCoordinate = Utils.IBL_LAMBDA;
    }
}
