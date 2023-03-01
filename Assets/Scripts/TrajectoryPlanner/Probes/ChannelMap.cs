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
    public Texture2D ChannelMapTexture { get; private set; }

    private int[] xCoords;
    private int[] yCoords;
    private int[] zCoords;

    // For now, we assume all channels have identical sizes
    private int width;
    private int height;
    private int depth;

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
        xCoords = new int[n];
        yCoords = new int[n];
        zCoords = new int[n];

        // Also set up the blank texture, we'll draw a 1 at each location that has a probe
        // For now assume this is a Neuropixels probe
        ChannelMapTexture = new Texture2D(MAP_WIDTH, MAP_HEIGHT, TextureFormat.R8, false); // we'll represent the texture at um scale
        for (int x = 0; x < MAP_WIDTH; x++)
            for (int y = 0; y < MAP_HEIGHT; y++)
                ChannelMapTexture.SetPixel(x, y, Color.black);
        ChannelMapTexture.wrapMode = TextureWrapMode.Clamp;
        ChannelMapTexture.filterMode = FilterMode.Point;

        // don't need the header
        //var header = Regex.Split(lines[0], SPLIT_RE);

        for (var i = 1; i < lines.Length; i++)
        {
            var values = Regex.Split(lines[i], SPLIT_RE);
            if (values.Length == 0 || values[0] == "") continue;

            if (i == 1)
            {
                // Set the fixed w/h/d from the first channel
                width = int.Parse(values[4], System.Globalization.NumberStyles.Any);
                height = int.Parse(values[5], System.Globalization.NumberStyles.Any);
                depth = int.Parse(values[6], System.Globalization.NumberStyles.Any);
            }

            int idx = i - 1;
            // we're going to assume for now that electrodes are numbered in order
            // CAUTION: note that the x/y are inverted! In Unity we're using Y to represent "up", but in the channel map X represents up. bad convention
            xCoords[idx] = int.Parse(values[1], System.Globalization.NumberStyles.Any) + MAP_WIDTH / 2;
            yCoords[idx] = int.Parse(values[2], System.Globalization.NumberStyles.Any);
            zCoords[idx] = int.Parse(values[3], System.Globalization.NumberStyles.Any);

            // using this channel's x/y/w/h data, set the channel map texture pixels
            for (int x = xCoords[idx]; x < (xCoords[idx] + width); x++)
                for (int y = yCoords[idx]; y < (yCoords[idx] + height); y++)
                    ChannelMapTexture.SetPixel(x, y, Color.red);
        }

        ChannelMapTexture.Apply();
    }

    public int ChannelCount { get { return xCoords.Length; } }
    
    public List<Vector3> GetChannelPositions()
    {
        List<Vector3> data = new();

        for (int i = 0; i < xCoords.Length; i++)
                data.Add(new Vector3(xCoords[i], yCoords[i], zCoords[i]));

        return data;
    }

    public List<Vector3> GetChannelPositions(bool[] selected)
    {
        List<Vector3> data = new();

        for (int i = 0; i < selected.Length; i++)
            if (selected[i])
                data.Add(new Vector3(xCoords[i], yCoords[i], zCoords[i]));

        return data;
    }

    public (float width, float height, float depth) GetChannelScale()
    {
        return (width, height, depth);
    }
}
