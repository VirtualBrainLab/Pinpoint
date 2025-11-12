using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using TrajectoryPlanner;
using UrchinUtilsUtils = Urchin.Utils.Utils;
using BrainAtlas;
using Unity.AppUI.MVVM;
using Unity.AppUI.Redux;

public class RelativeCoordinatePanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField _apField;
    [SerializeField] private TMP_InputField _mlField;
    [SerializeField] private TMP_InputField _dvField;

#if APP_UI
    private Services.StoreService _storeService;
#endif

    private void Awake()
    {
        _apField.onEndEdit.AddListener(delegate { UpdateRelativeCoordinate(); });
        _mlField.onEndEdit.AddListener(delegate { UpdateRelativeCoordinate(); });
        _dvField.onEndEdit.AddListener(delegate { UpdateRelativeCoordinate(); });

#if APP_UI
        _storeService = UI.PinpointApp.Services.GetService<Services.StoreService>();
#endif
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

#if APP_UI
            _storeService.Store.Dispatch(Models.Settings.AtlasSettingsActions.SET_REFERENCE_COORD, new Vector3(ap, ml, dv));
#else
            Settings.ReferenceCoord = new Vector3(ap, ml, dv);
#endif
        }
        catch
        {
            Debug.LogWarning("Bad formatting");
        }
    }

    public void Set2Bregma()
    {
        Vector3 bregmaCoord = Vector3.zero;
        if (UrchinUtilsUtils.BregmaDefaults.ContainsKey(BrainAtlasManager.ActiveReferenceAtlas.Name))
            bregmaCoord = UrchinUtilsUtils.BregmaDefaults[BrainAtlasManager.ActiveReferenceAtlas.Name];

#if APP_UI
        _storeService.Store.Dispatch(Models.Settings.AtlasSettingsActions.SET_REFERENCE_COORD, bregmaCoord);
#else
        Settings.ReferenceCoord = bregmaCoord;
#endif
    }

    public void Set2Lambda()
    {
        Vector3 lambdaCoord = Vector3.zero;
        if (UrchinUtilsUtils.LambdaDefaults.ContainsKey(BrainAtlasManager.ActiveReferenceAtlas.Name))
            lambdaCoord = UrchinUtilsUtils.LambdaDefaults[BrainAtlasManager.ActiveReferenceAtlas.Name];

#if APP_UI
        _storeService.Store.Dispatch(Models.Settings.AtlasSettingsActions.SET_REFERENCE_COORD, lambdaCoord);
#else
     Settings.ReferenceCoord = lambdaCoord;
#endif
    }
}
