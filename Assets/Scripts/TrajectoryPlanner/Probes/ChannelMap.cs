using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// A channel map is a data holder which tracks the position of electrodes along a probe shank (or multiple shanks)
/// 
/// The "selected" 
/// </summary>
public class ChannelMap
{
    private Dictionary<string, Texture2D> _channelMapTextures;

    private float[] _xCoords;
    private float[] _yCoords;
    private float[] _zCoords;
    private Dictionary<string, bool[]> _selectionLayers;
    private Dictionary<string, List<Vector3>> _selectionLayerCoords;

    // For now, we assume all channels have identical sizes
    private float width;
    private float height;
    private float depth;

    static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
    static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
    static char[] TRIM_CHARS = { '\"' };

    private const int MAP_WIDTH = 60;
    private const int MAP_HEIGHT = 10000;

    /// <summary>
    /// Create a channel map by loading a CSV file from an AddressableAsset address
    /// </summary>
    /// <param name="assetAddress"></param>
    public ChannelMap(AssetReference channelMapAsset)
    {
        _channelMapTextures = new();
        _selectionLayers = new();
        _selectionLayerCoords = new();

        LoadAsset(channelMapAsset);
    }

    private async void LoadAsset(AssetReference channelMapAsset)
    {
        // Load the asset CSV file
        AsyncOperationHandle<TextAsset> loadHandler = Addressables.LoadAssetAsync<TextAsset>(channelMapAsset);
        await loadHandler.Task;

        // Split the text by common splitters (should be ,)
        var lines = Regex.Split(loadHandler.Result.text, LINE_SPLIT_RE);

        // If we just have the header line, bail
        if (lines.Length <= 1) return;

        // Otherwise set up the x/y/z arrays
        int n = lines.Length - 1;
        _xCoords = new float[n];
        _yCoords = new float[n];
        _zCoords = new float[n];

        // parse the header, pulling out the selection layers
        var header = Regex.Split(lines[0], SPLIT_RE);

        List<(string name, int index)> selectionLayerInfo = new();

        for (int i = 7; i < header.Length; i++)
        {
            Debug.Log($"Adding selection layer {header[i]}");
            string selectionLayerName = header[i];
            selectionLayerInfo.Add((selectionLayerName, i));

            _selectionLayers.Add(selectionLayerName, new bool[n]);

            Texture2D mapTexture = new Texture2D(MAP_WIDTH, MAP_HEIGHT, TextureFormat.R8, false);
            for (int x = 0; x < MAP_WIDTH; x++)
                for (int y = 0; y < MAP_HEIGHT; y++)
                    mapTexture.SetPixel(x, y, Color.black);
            mapTexture.wrapMode = TextureWrapMode.Clamp;
            mapTexture.filterMode = FilterMode.Point;
            _channelMapTextures.Add(selectionLayerName, mapTexture);
        }

        for (var i = 1; i < lines.Length; i++)
        {
            var values = Regex.Split(lines[i], SPLIT_RE);
            if (values.Length == 0 || values[0] == "") continue;

            if (i == 1)
            {
                // Set the fixed w/h/d from the first channel
                width = float.Parse(values[4], System.Globalization.NumberStyles.Any);
                height = float.Parse(values[5], System.Globalization.NumberStyles.Any);
                depth = float.Parse(values[6], System.Globalization.NumberStyles.Any);
            }

            int idx = i - 1;
            // we're going to assume for now that electrodes are numbered in order
            _xCoords[idx] = float.Parse(values[1], System.Globalization.NumberStyles.Any) + MAP_WIDTH / 2;
            _yCoords[idx] = float.Parse(values[2], System.Globalization.NumberStyles.Any);
            _zCoords[idx] = float.Parse(values[3], System.Globalization.NumberStyles.Any);

            // For each selection layer, we need to set the channel bool[] and the pixel values
            foreach (var selectionInfo in selectionLayerInfo)
                if (int.Parse(values[selectionInfo.index], System.Globalization.NumberStyles.Any) == 1)
                {
                    // This row is true, so set the bool[]
                    _selectionLayers[selectionInfo.name][idx] = true;

                    // Then set the pixels for this electrode
                    for (int x = Mathf.RoundToInt(_xCoords[idx]); x < (_xCoords[idx] + width); x++)
                        for (int y = Mathf.RoundToInt(_yCoords[idx]); y < (_yCoords[idx] + height); y++)
                            _channelMapTextures[selectionInfo.name].SetPixel(x, y, Color.red);
                }
        }

        foreach (Texture2D tex in _channelMapTextures.Values)
            tex.Apply();

        // build the coordinate lists
        foreach (string selectionLayerName in _selectionLayers.Keys)
        {
            List<Vector3> data = new();
            bool[] selected = _selectionLayers[selectionLayerName];

            for (int i = 0; i < selected.Length; i++)
                if (selected[i])
                    data.Add(new Vector3(_xCoords[i], _yCoords[i], _zCoords[i]));

            _selectionLayerCoords.Add(selectionLayerName, data);
        }
    }

    public int ChannelCount { get { return _xCoords.Length; } }
    
    public List<Vector3> GetChannelPositions(string selectionLayer = "default")
    {
        return _selectionLayerCoords[selectionLayer];
    }

    public Vector3 GetChannelScale()
    {
        return new Vector3(width, height, depth);
    }

    public Texture2D GetChannelMapTexture(string selectionLayer = "default")
    {
        return _channelMapTextures[selectionLayer];
    }

    public bool[] GetSelected(string selectionLayer = "default")
    {
        return _selectionLayers[selectionLayer];
    }

    public List<string> GetSelectionLayerNames()
    {
        return _selectionLayers.Keys.ToList();
    }
}
