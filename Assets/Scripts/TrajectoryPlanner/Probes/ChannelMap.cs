using System.Collections.Generic;
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
    private float[] xCoords;
    private float[] yCoords;
    private float[] zCoords;

    // For now, we assume all channels have identical sizes
    private float width;
    private float height;
    private float depth;

    static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
    static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
    static char[] TRIM_CHARS = { '\"' };

    /// <summary>
    /// Create a channel map by loading a CSV file from an AddressableAsset address
    /// </summary>
    /// <param name="assetAddress"></param>
    public ChannelMap(AssetReference channelMapAsset)
    {
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
        xCoords = new float[n];
        yCoords = new float[n];
        zCoords = new float[n];

        // don't need the header
        //var header = Regex.Split(lines[0], SPLIT_RE);

        for (var i = 1; i < lines.Length; i++)
        {
            var values = Regex.Split(lines[i], SPLIT_RE);
            if (values.Length == 0 || values[0] == "") continue;

            if (i == 1)
            {
                // Set the fixed w/h/d from the first channel
                width = float.Parse(values[4]);
                height = float.Parse(values[5]);
                depth = float.Parse(values[6]);
            }

            // we're going to assume for now that electrodes are numbered in order
            xCoords[i] = float.Parse(values[1]);
            yCoords[i] = float.Parse(values[2]);
            zCoords[i] = float.Parse(values[3]);
        }


    }

    public int ChannelCount { get { return xCoords.Length; } }
    
    public List<(float, float, float)> GetChannelPositions(bool[] selected)
    {
        List<(float, float, float)> data = new();

        for (int i = 0; i < xCoords.Length; i++)
            if (selected[i])
                data.Add((xCoords[i], yCoords[i], zCoords[i]));

        return data;
    }

    public (float width, float height, float depth) GetChannelScale()
    {
        return (width, height, depth);
    }
}
