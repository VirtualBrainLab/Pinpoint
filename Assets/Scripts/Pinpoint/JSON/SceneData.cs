using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SceneData
{
    public string AtlasName;
    public string TransformName;

    public string[] Data;

    public string Settings;

    // public static SceneData ToSceneData(string atlasName, string transformName,
    //     RigData[] rigDatas,
    //     ProbeData[] probeDatas,
    //     CraniotomyData[] craniotomyDatas,
    //     string settings)
    // {
    //     SceneData sceneData = new SceneData();
    //     sceneData.AtlasName = atlasName;
    //     sceneData.TransformName = transformName;
    //     sceneData.Settings = settings;
    //
    //     sceneData.Data = new string[rigDatas.Length + probeDatas.Length];
    //     int di = 0;
    //
    //     foreach (RigData rigData in rigDatas)
    //     {
    //         SceneDataStorage temp = new SceneDataStorage();
    //         temp.Type = SceneDataType.Rig;
    //         temp.Data = JsonUtility.ToJson(rigData);
    //         sceneData.Data[di++] = JsonUtility.ToJson(temp);
    //     }
    //
    //     foreach (ProbeData probeData in probeDatas)
    //     {
    //         SceneDataStorage temp = new SceneDataStorage();
    //         temp.Type = SceneDataType.Probe;
    //         temp.Data = JsonUtility.ToJson(probeData);
    //         sceneData.Data[di++] = JsonUtility.ToJson(temp);
    //     }
    //
    //     foreach (CraniotomyData craniotomyData in craniotomyDatas)
    //     {
    //         SceneDataStorage temp = new SceneDataStorage();
    //         temp.Type = SceneDataType.Craniotomy;
    //         temp.Data = JsonUtility.ToJson(craniotomyData);
    //         sceneData.Data[di++] = JsonUtility.ToJson(temp);
    //     }
    //
    //     return sceneData;
    // }
    
    // public static (string atlasName, string transformName,
    //     RigData[] rigDatas,
    //     ProbeData[] probeDatas,
    //     CraniotomyData[] craniotomyDatas) FromSceneData(string sceneDataJSON)
    // {
    //     SceneData sceneData = JsonUtility.FromJson<SceneData>(sceneDataJSON);
    //     string atlasName = sceneData.AtlasName;
    //     string transformName = sceneData.TransformName;
    //
    //     List<RigData> rigDatas = new List<RigData>();
    //     List<ProbeData> probeDatas = new List<ProbeData>();
    //     List<CraniotomyData> craniotomyDatas = new();
    //
    //     foreach (string data in sceneData.Data)
    //     {
    //         SceneDataStorage temp = JsonUtility.FromJson<SceneDataStorage>(data);
    //         switch (temp.Type)
    //         {
    //             case SceneDataType.Probe:
    //                 probeDatas.Add(JsonUtility.FromJson<ProbeData>(temp.Data));
    //                 break;
    //             case SceneDataType.Rig:
    //                 rigDatas.Add(JsonUtility.FromJson<RigData>(temp.Data));
    //                 break;
    //             case SceneDataType.Craniotomy:
    //                 craniotomyDatas.Add(JsonUtility.FromJson<CraniotomyData>(temp.Data));
    //                 break;
    //         }
    //     }
    //
    //     return (atlasName, transformName,
    //         rigDatas.ToArray(),
    //         probeDatas.ToArray(),
    //         craniotomyDatas.ToArray());
    // }

    public override string ToString()
    {
        return JsonUtility.ToJson(this);
    }
}

[Serializable]
public struct SceneDataStorage
{
    public SceneDataType Type;
    public string Data;
}

public enum SceneDataType : int
{
    Rig = 0,
    Probe = 1,
    Craniotomy = 2,
}
