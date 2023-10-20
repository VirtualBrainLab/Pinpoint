using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Editor_ChannelMaps
{
    private static AddressableAssetSettings _addressableSettings;
    private static AddressableAssetGroup _addressableAssetGroup;

    [MenuItem("Tools/Build Channel Maps")]
    public static void BuildChannelMaps()
    {
        _addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
        _addressableAssetGroup = _addressableSettings.groups.Find(x => x.Name == "ChannelMaps");

        DirectoryInfo directoryInfo = new DirectoryInfo("Assets/Data/Probes");
        FileInfo[] files = directoryInfo.GetFiles("*.csv");
        
        foreach (FileInfo file in files)
        {
            TextAsset csvFile = AssetDatabase.LoadAssetAtPath<TextAsset>(Path.Combine("Assets/Data/Probes",file.Name));
            Debug.Log("Loaded CSV File: " + csvFile.name);

            LoadChannelMapCSV(csvFile, file.Name);
        }
    }

    private const int MAP_WIDTH = 60;
    private const int MAP_HEIGHT = 10000;

    private static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
    private static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
    private static char[] TRIM_CHARS = { '\"' };

    private static void LoadChannelMapCSV(TextAsset asset, string name)
    {
        ChannelMapData channelMapData = ScriptableObject.CreateInstance<ChannelMapData>();

        channelMapData.DefaultSelectionLayer = "default";
        channelMapData.FullHeight = 10f;
        channelMapData.SelectionLayerNames = new();
        channelMapData.SelectionLayers = new();

        // Split the text by common splitters (should be ,)
        var lines = Regex.Split(asset.text, LINE_SPLIT_RE);

        // If we just have the header line, bail
        if (lines.Length <= 1) return;

        // Otherwise set up the x/y/z arrays
        int n = lines.Length - 1;
        channelMapData.ChannelCoords = new Vector3[n];

        // parse the header, pulling out the selection layers
        var header = Regex.Split(lines[0], SPLIT_RE);


        List<(string name, int index)> selectionLayerInfo = new();

        for (int i = 7; i < header.Length; i++)
        {
            Debug.Log($"Adding selection layer {header[i]}");
            string selectionLayerName = header[i];
            selectionLayerInfo.Add((selectionLayerName, i));

            channelMapData.SelectionLayerNames.Add(selectionLayerName);
            ChannelMapData.BoolArray array = new();
            array.data = new bool[n];
            channelMapData.SelectionLayers.Add(array);
        }

        for (var i = 1; i < lines.Length; i++)
        {
            var values = Regex.Split(lines[i], SPLIT_RE);
            if (values.Length == 0 || values[0] == "") continue;

            if (i == 1)
            {
                // Set the fixed w/h/d from the first channel
                float width = float.Parse(values[4], System.Globalization.NumberStyles.Any);
                float height = float.Parse(values[5], System.Globalization.NumberStyles.Any);
                float depth = float.Parse(values[6], System.Globalization.NumberStyles.Any);
                channelMapData.ChannelShape = new Vector3(width, height, depth);
            }

            int idx = i - 1;
            // we're going to assume for now that electrodes are numbered in order
            channelMapData.ChannelCoords[idx].x = float.Parse(values[1], System.Globalization.NumberStyles.Any) + MAP_WIDTH / 2;
            channelMapData.ChannelCoords[idx].y = float.Parse(values[2], System.Globalization.NumberStyles.Any);
            channelMapData.ChannelCoords[idx].z = float.Parse(values[3], System.Globalization.NumberStyles.Any);

            // For each selection layer, we need to set the channel bool[] and the pixel values
            foreach (var selectionInfo in selectionLayerInfo)
            {
                int layerIdx = channelMapData.SelectionLayerNames.FindIndex(x => x.Equals(selectionInfo.name));

                if (int.Parse(values[selectionInfo.index], System.Globalization.NumberStyles.Any) == 1)
                {
                    // This row is true, so set the bool[]
                    channelMapData.SelectionLayers[layerIdx].data[idx] = true;
                }
            }
        }

        name = name.Replace(".csv", "");
        string mapSOPath = Path.Join("Assets/AddressableAssets/ChannelMaps", $"{name}.asset");

        if (File.Exists(mapSOPath))
            AssetDatabase.DeleteAsset(mapSOPath);

        AssetDatabase.CreateAsset(channelMapData, mapSOPath);

        _addressableSettings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(mapSOPath), _addressableAssetGroup);

    }
}
