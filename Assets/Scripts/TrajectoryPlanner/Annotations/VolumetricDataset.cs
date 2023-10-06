using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumetricDataset
{
    protected (int x, int y, int z) size;
    protected int[,,] data;

    public VolumetricDataset((int x, int y, int z) size, byte[] volumeIndexes, uint[] map, ushort[] dataIndexes)
    {
        this.size = size;
        ConstructorHelper(volumeIndexes, map, dataIndexes);
    }
    public VolumetricDataset((int x, int y, int z) size, byte[] volumeIndexes, uint[] map, byte[] dataIndexes)
    {
        this.size = size;
        ushort[] dataIndexesCopy = new ushort[dataIndexes.Length];
        dataIndexes.CopyTo(dataIndexesCopy, 0);
        ConstructorHelper(volumeIndexes, map, dataIndexesCopy);
    }

    private void ConstructorHelper(byte[] volumeIndexes, uint[] map, ushort[] dataIndexes)
    {
        data = new int[size.x, size.y, size.z];

        int ccfi = 0;
        int i = 0;

        // Datasets are stored in column order, so go through in reverse
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    if (volumeIndexes[ccfi] == 1)
                    {
                        data[x, y, z] = (int)map[dataIndexes[i]];
                        i++;
                    }
                    ccfi++;
                }
            }
        }
    }

    public int ValueAtIndex(int x, int y, int z)
    {
        if (x >= 0 && x < size.x && y >= 0 && y < size.y && z >= 0 && z < size.z)
            return data[x, y, z];
        else
            return int.MinValue;
    }

    public int ValueAtIndex(Vector3 xyz)
    {
        return ValueAtIndex(Mathf.RoundToInt(xyz.x), Mathf.RoundToInt(xyz.y), Mathf.RoundToInt(xyz.z));
    }
}
