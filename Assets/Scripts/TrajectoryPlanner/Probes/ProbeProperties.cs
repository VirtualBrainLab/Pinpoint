using System.Collections.Generic;
using UnityEngine;

public class ProbeProperties
{
    public static List<Color> ProbeColors = new List<Color> { ColorFromRGB(114, 87, 242), ColorFromRGB(240, 144, 96), ColorFromRGB(71, 147, 240), ColorFromRGB(240, 217, 48), ColorFromRGB(60, 240, 227),
                                    ColorFromRGB(180, 0, 0), ColorFromRGB(0, 180, 0), ColorFromRGB(0, 0, 180), ColorFromRGB(180, 180, 0), ColorFromRGB(0, 180, 180),
                                    ColorFromRGB(180, 0, 180), ColorFromRGB(240, 144, 96), ColorFromRGB(71, 147, 240), ColorFromRGB(240, 217, 48), ColorFromRGB(60, 240, 227),
                                    ColorFromRGB(114, 87, 242), ColorFromRGB(255, 255, 255), ColorFromRGB(0, 125, 125), ColorFromRGB(125, 0, 125), ColorFromRGB(125, 125, 0)};

    public static readonly int FONT_SIZE_ACRONYM = 18;
    public static readonly int FONT_SIZE_AREA = 14;

    public static Color GetNextProbeColor()
    {
        Color next = ProbeColors[0];
        ProbeColors.RemoveAt(0);
        return next;
    }

    public static void ReturnProbeColor(Color returnColor)
    {
        ProbeColors.Insert(0, returnColor);
    }

    ///
    /// HELPER FUNCTIONS
    /// 
    public static Color ColorFromRGB(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }
}
