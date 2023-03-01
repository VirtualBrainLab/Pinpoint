using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProbeProperties
{
    public static List<Color> ProbeColors = new List<Color> {
        ColorFromRGB(114, 87, 242), ColorFromRGB(240, 144, 96), ColorFromRGB(71, 147, 240),
        ColorFromRGB(240, 217, 48), ColorFromRGB(60, 240, 227), ColorFromRGB(180, 0, 0),
        ColorFromRGB(0, 180, 0), ColorFromRGB(0, 0, 180), ColorFromRGB(180, 180, 0),
        ColorFromRGB(0, 180, 180), ColorFromRGB(180, 0, 180), ColorFromRGB(240, 144, 96),
        ColorFromRGB(71, 147, 240), ColorFromRGB(240, 217, 48), ColorFromRGB(60, 240, 227),
        ColorFromRGB(255, 255, 255), ColorFromRGB(0, 125, 125), ColorFromRGB(125, 0, 125),
        ColorFromRGB(125, 125, 0)
    };

    private static int[] _colorOpts = Enumerable.Range(0, ProbeColors.Count).ToArray();
    private static List<int> _usedColors = new List<int>();

    public static readonly int FONT_SIZE_ACRONYM = 24;
    public static readonly int FONT_SIZE_AREA = 18;

    public enum ProbeType : int
    {
        Neuropixels1 = 0,
        Neuropixels21 = 21,
        Neuropixels24 = 24
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
