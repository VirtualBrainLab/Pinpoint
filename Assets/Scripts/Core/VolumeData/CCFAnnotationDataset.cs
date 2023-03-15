using UnityEngine;
using CoordinateSpaces;
using System;

public class CCFAnnotationDataset : VolumetricDataset
{
    private CoordinateSpace _coordinateSpace;
    public CoordinateSpace CoordinateSpace { get { return _coordinateSpace; } }
    private bool[,,] areaBorders;
    
    /// <summary>
    /// Create a new annotation dataset
    /// </summary>
    /// <param name="size"></param>
    /// <param name="data"></param>
    /// <param name="map"></param>
    /// <param name="ccfIndexMap"></param>
    public CCFAnnotationDataset((int ap, int dv, int lr) size, ushort[] data, uint[] map, byte[] ccfIndexMap) : base(size, ccfIndexMap, map, data)
    {
        _coordinateSpace = new CCFSpace25();
    }

    public void ComputeBorders()
    {
        if (areaBorders != null)
        {
            Debug.LogWarning("(AnnotationDataset) Borders were going to be re-computed unnecessarily. Skipping");
            return;
        }
        areaBorders = new bool[size.x, size.y, size.z];

        for (int ap = 0; ap < size.x; ap++)
        {
            // We go through coronal slices, going down each DV depth, anytime the *next* annotation point changes, we mark this as a border
            for (int lr = 0; lr < (size.z-1); lr++)
            {
                for (int dv = 0; dv < (size.y-1); dv ++)
                {
                    if ((data[ap, dv, lr] != data[ap, dv + 1, lr]) || data[ap,dv,lr] != data[ap, dv, lr+1])
                        areaBorders[ap, dv, lr] = true;
                }
            }
        }

    }

    public bool BorderAtIndex(int ap, int dv, int lr)
    {
        if ((ap >= 0 && ap < size.x) && (dv >= 0 && dv < size.y) && (lr >= 0 && lr < size.z))
            return areaBorders[ap, dv, lr];
        else
            return false;
    }

    public bool BorderAtIndex(Vector3 apdvlr)
    {
        return BorderAtIndex(Mathf.RoundToInt(apdvlr.x), Mathf.RoundToInt(apdvlr.y), Mathf.RoundToInt(apdvlr.z));
    }

    /// <summary>
    /// Use the annotation dataset to discover whether there is a surface coordinate by going *down* from a point searchDistance
    /// *above* the startPos
    /// returns the coordinate in the annotation dataset that corresponds to the surface.
    ///  
    /// Function guarantees that you enter the brain *once* before exiting, so if you start below the brain you need
    /// to enter first to discover the surface coordinate.
    /// </summary>
    /// <param name="bottomPos">coordinate to go down to</param>
    /// <param name="up"></param>
    /// <returns></returns>
    public Vector3 FindSurfaceCoordinate(Vector3 bottomPos, Vector3 up, float searchDistance = 408f)
    {
        // We'll start at a point that is pretty far above the brain surface
        // note that search distance is in 25um units, so this is actually 10000 um up (i.e. the top of the probe)
        Vector3 topPos = bottomPos + up * searchDistance;

        // If by chance we are inside the brain, go farther
        if (ValueAtIndex(topPos) > 0)
            topPos = bottomPos + up * searchDistance * 2f;

        for (float perc = 0; perc <= 1f; perc += 0.0005f)
        {
            Vector3 point = Vector3.Lerp(topPos, bottomPos, perc);
            if (ValueAtIndex(point) > 0)
                return point;
        }

        // If you got here it means you *never* entered and then exited the brain
        return new Vector3(float.NaN, float.NaN, float.NaN);
    }
}
