using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProbeProperties
{
    public static List<Color> ProbeColors = new List<Color> {
        new Color(0.12156862745098039f, 0.4666666666666667f, 0.7058823529411765f, 1.0f),
        new Color(0.6823529411764706f, 0.7803921568627451f, 0.9098039215686274f, 1.0f),
        new Color(1.0f, 0.4980392156862745f, 0.054901960784313725f, 1.0f),
        new Color(1.0f, 0.7333333333333333f, 0.47058823529411764f, 1.0f),
        new Color(0.17254901960784313f, 0.6274509803921569f, 0.17254901960784313f, 1.0f),
        new Color(0.596078431372549f, 0.8745098039215686f, 0.5411764705882353f, 1.0f),
        new Color(0.8392156862745098f, 0.15294117647058825f, 0.1568627450980392f, 1.0f),
        new Color(1.0f, 0.596078431372549f, 0.5882352941176471f, 1.0f),
        new Color(0.5803921568627451f, 0.403921568627451f, 0.7411764705882353f, 1.0f),
        new Color(0.7725490196078432f, 0.6901960784313725f, 0.8352941176470589f, 1.0f),
        new Color(0.5490196078431373f, 0.33725490196078434f, 0.29411764705882354f, 1.0f),
        new Color(0.7686274509803922f, 0.611764705882353f, 0.5803921568627451f, 1.0f),
        new Color(0.8901960784313725f, 0.4666666666666667f, 0.7607843137254902f, 1.0f),
        new Color(0.9686274509803922f, 0.7137254901960784f, 0.8235294117647058f, 1.0f),
        new Color(0.4980392156862745f, 0.4980392156862745f, 0.4980392156862745f, 1.0f),
        new Color(0.7803921568627451f, 0.7803921568627451f, 0.7803921568627451f, 1.0f),
        new Color(0.8588235294117647f, 0.8588235294117647f, 0.5529411764705883f, 1.0f),
        new Color(0.09019607843137255f, 0.7450980392156863f, 0.8117647058823529f, 1.0f),
        new Color(0.6196078431372549f, 0.8549019607843137f, 0.8980392156862745f, 1.0f)
    };

    private static int[] _colorOpts = Enumerable.Range(0, ProbeColors.Count).ToArray();
    private static List<int> _usedColors = new List<int>();

    public static readonly int FONT_SIZE_ACRONYM = 24;
    public static readonly int FONT_SIZE_AREA = 18;

    public enum ProbeType : int
    {
        Placeholder = -1,
        Neuropixels1 = 0,
        Neuropixels21 = 21,
        Neuropixels24 = 24,
        Neuropixels24x2 = 28,
        UCLA128K = 128,
        UCLA256F = 256
    }

    public static bool FourShank(ProbeType probeType)
    {
        switch (probeType)
        {
            case ProbeType.Placeholder:
                return false;
            case ProbeType.Neuropixels1:
                return false;
            case ProbeType.Neuropixels21:
                return false;
            case ProbeType.Neuropixels24:
                return true;
            case ProbeType.Neuropixels24x2:
                return true;
            default:
                return false;
        }
    }

    public static Color GetNextProbeColor()
    {
        // Generate list of indexes and remove those that have been used
        int[] indexes = _colorOpts.Except(_usedColors).ToArray();

        // Pick one randomly
        int randIdx = Mathf.FloorToInt(Random.value * indexes.Length);

        Color next = ProbeColors[randIdx];
        _usedColors.Add(randIdx);

        return next;
    }

    public static void UseColor(Color color)
    {
        int idx = ProbeColors.FindIndex(x => x.Equals(color));
        if (idx >= 0)
            _usedColors.Add(idx);
    }

    public static void ReturnProbeColor(Color returnColor)
    {
        int idx = ProbeColors.FindIndex(x => x.Equals(returnColor));
        if (idx >= 0)
            _usedColors.Remove(idx);
    }

    ///
    /// HELPER FUNCTIONS
    /// 
    public static Color ColorFromRGB(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }
}
