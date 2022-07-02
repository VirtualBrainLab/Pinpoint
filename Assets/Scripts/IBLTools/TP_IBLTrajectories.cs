using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class TP_IBLTrajectories : MonoBehaviour
{
    [SerializeField] TP_TrajectoryPlannerManager tpmanager;
    [SerializeField] AssetReference iblTrajectoryCSV;
    private bool loaded = false;

    private Vector3 iblBregma = new Vector3(5400, 5739f, 332f);
    private Vector3 invivoConversionAPMLDV = new Vector3(1.087f, 1f, 0.952f);

    public void Load()
    {
        if (!loaded)
        {
            LoadCSV();
            loaded = true;
        }
    }

    private async void LoadCSV()
    {
        AsyncOperationHandle<TextAsset> dataLoader = iblTrajectoryCSV.LoadAssetAsync<TextAsset>();
        await dataLoader.Task;
        TextAsset text = dataLoader.Result;

        List<Dictionary<string,object>> data = CSVReader.ParseText(text.text);

        foreach (Dictionary<string,object> row in data)
        {
            float ml = (int)row["ml"];
            float ap = (int)row["ap"] / invivoConversionAPMLDV.x;
            float dv = (int)row["dv"];

            Vector3 apdvlr25 = new Vector3(iblBregma.x - ap, iblBregma.z - dv, iblBregma.y + ml) / 25;
            // we need coordinates in bregma space
            Vector3 coords = new Vector3(ap, ml, -dv);
            float phi = (int)row["phi"];
            float theta = (int)row["theta"];
            float depth = (int)row["depth"];
            Vector3 angles = new Vector3(phi, theta, 0f);

            GameObject newProbe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            newProbe.transform.SetParent(transform);
            newProbe.transform.localScale = Vector3.one * 0.2f;
            newProbe.transform.position = Utils.apdvlr25_2World(apdvlr25) - tpmanager.GetCenterOffset();
            newProbe.GetComponent<Renderer>().material.color = Color.red;
            newProbe.layer = LayerMask.NameToLayer("Brain");
            TP_IBLPlannedTrajectory planned = newProbe.AddComponent<TP_IBLPlannedTrajectory>() as TP_IBLPlannedTrajectory;
            planned.coords = coords;
            planned.angles = angles;
        }
    }
}
