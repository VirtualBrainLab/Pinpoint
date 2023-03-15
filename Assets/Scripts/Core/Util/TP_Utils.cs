using System;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class TP_Utils : MonoBehaviour
{
    public static Vector3 IBL_BREGMA = new Vector3(5.4f, 5.7f, 0.332f);
    public static Vector3 IBL_LAMBDA = new Vector3(9.9f, 5.7f, 0.332f);

    /// <summary>
    /// Rotate phi and theta to match IBL coordinates
    /// </summary>
    /// <param name="phiTheta"></param>
    /// <returns></returns>
    public static Vector3 World2IBL(Vector3 phiThetaSpin)
    {
        float iblPhi = -phiThetaSpin.x - 90f;
        float iblTheta = -phiThetaSpin.y;
        return new Vector3(iblPhi, iblTheta, phiThetaSpin.z);
    }

    /// <summary>
    /// Rotate IBL coordinates to return to pinpoint space
    /// </summary>
    /// <param name="iblPhiTheta"></param>
    /// <returns></returns>
    public static Vector3 IBL2World(Vector3 iblAngles)
    {
        float worldPhi = -iblAngles.x - 90f;
        float worldTheta = -iblAngles.y;
        return new Vector3(worldPhi, worldTheta, iblAngles.z);
    }


    public static float CircDeg(float deg, float minDeg, float maxDeg)
    {
        float diff = Mathf.Abs(maxDeg - minDeg);

        if (deg < minDeg) deg += diff;
        if (deg > maxDeg) deg -= diff;

        return deg;
    }

    public float Hypot(Vector2 values)
    {
        return Mathf.Sqrt(values.x * values.x + values.y * values.y);
    }

    public static Color ParseHexColor(string hexString)
    {
        Color color = new Color();
        ColorUtility.TryParseHtmlString(hexString, out color);
        return color;
    }

    // From Math3d: http://wiki.unity3d.com/index.php/3d_Math_functions
    //Two non-parallel lines which may or may not touch each other have a point on each line which are closest
    //to each other. This function finds those two points. If the lines are not parallel, the function 
    //outputs true, otherwise false.
    public static bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {

        closestPointLine1 = Vector3.zero;
        closestPointLine2 = Vector3.zero;

        float a = Vector3.Dot(lineVec1, lineVec1);
        float b = Vector3.Dot(lineVec1, lineVec2);
        float e = Vector3.Dot(lineVec2, lineVec2);

        float d = a * e - b * b;

        //lines are not parallel
        if (d != 0.0f)
        {

            Vector3 r = linePoint1 - linePoint2;
            float c = Vector3.Dot(lineVec1, r);
            float f = Vector3.Dot(lineVec2, r);

            float s = (b * f - c * e) / d;
            float t = (a * f - c * b) / d;

            closestPointLine1 = linePoint1 + lineVec1 * s;
            closestPointLine2 = linePoint2 + lineVec2 * t;

            return true;
        }

        else
        {
            return false;
        }
    }

    public float4 Color2float4(Color color)
    {
        return new float4(color.r, color.g, color.b, color.a);
    }

    // Note that this might be possible to replace by:
    // Buffer.BlockCopy()
    // see https://stackoverflow.com/questions/6952923/conversion-double-array-to-byte-array
    public float[] LoadBinaryFloatHelper(string datasetName)
    {
        TextAsset textAsset = Resources.Load("Datasets/" + datasetName) as TextAsset;
        byte[] tempData = textAsset.bytes;
        Debug.Log("Loading " + datasetName + " with " + tempData.Length + " bytes");
        float[] data = new float[tempData.Length / 4];

        Buffer.BlockCopy(tempData, 0, data, 0, tempData.Length);
        Debug.LogFormat("Found {0} floats", data.Length);

        return data;
    }
    public uint[] LoadBinaryUInt32Helper(string datasetName)
    {
        TextAsset textAsset = Resources.Load("Datasets/" + datasetName) as TextAsset;
        byte[] tempData = textAsset.bytes;
        Debug.Log("Loading " + datasetName + " with " + tempData.Length + " bytes");
        uint[] data = new uint[tempData.Length / 4];

        Buffer.BlockCopy(tempData, 0, data, 0, tempData.Length);
        Debug.LogFormat("Found {0} UInt32", data.Length);

        return data;
    }
    public ushort[] LoadBinaryUShortHelper(string datasetName)
    {
        TextAsset textAsset = Resources.Load("Datasets/" + datasetName) as TextAsset;
        byte[] tempData = textAsset.bytes;
        Debug.Log("Loading " + datasetName + " with " + tempData.Length + " bytes");
        ushort[] data = new ushort [tempData.Length / 2];

        Buffer.BlockCopy(tempData, 0, data, 0, tempData.Length);
        Debug.LogFormat("Found {0} UShort", data.Length);

        return data;
    }
    public byte[] LoadBinaryByteHelper(string datasetName)
    {
        TextAsset textAsset = Resources.Load("Datasets/" + datasetName) as TextAsset;
        byte[] tempData = textAsset.bytes;
        Debug.Log("Loading " + datasetName + " with " + tempData.Length + " bytes");
        Debug.LogFormat("Found {0} bytes", tempData.Length);

        return tempData;
    }
    public double[] LoadBinaryDoubleHelper(string datasetName)
    {
        TextAsset textAsset = Resources.Load("Datasets/" + datasetName) as TextAsset;
        byte[] tempData = textAsset.bytes;
        Debug.Log("Loading " + datasetName + " with " + tempData.Length + " bytes");
        double[] data = new double[tempData.Length / 8];

        Buffer.BlockCopy(tempData, 0, data, 0, tempData.Length);
        Debug.LogFormat("Found {0} Doubles", data.Length);

        return data;
    }

    private static Array LoadNPY(Stream stream)
    {
        Debug.Log("Parsing NPY stream received from URL");
        using (var reader = new BinaryReader(stream, System.Text.Encoding.ASCII
            #if !NET35 && !NET40
                , leaveOpen: true
            #endif
            ))
        {
            int bytes;
            Type type;
            int[] shape;
            if (!parseReader(reader, out bytes, out type, out shape))
                throw new FormatException();

            Debug.Log(type);
            Debug.Log("Shape length: " + shape.Length);

            if (shape.Length==1)
            {
                Array array = Array.CreateInstance(type, shape[0]);
                array = readValueMatrix(reader, array, bytes, type, shape);

                // TODO: Double arrays are useless in Unity we should just convert them to float and save memory
                return array;
            }
            else
            {
                Debug.LogError("Warning: cannot handle this data shape");
            }
        }

        return null;
    }

    private static Array readValueMatrix(BinaryReader reader, Array matrix, int bytes, Type type, int[] shape)
    {
        int total = 1;
        for (int i = 0; i < shape.Length; i++)
            total *= shape[i];
        var buffer = new byte[bytes * total];

        reader.Read(buffer, 0, buffer.Length);
        Buffer.BlockCopy(buffer, 0, matrix, 0, buffer.Length);

        return matrix;
    }

    private static bool parseReader(BinaryReader reader, out int bytes, out Type t, out int[] shape)
    {
        bytes = 0;
        t = null;
        shape = null;

        // The first 6 bytes are a magic string: exactly "x93NUMPY"
        if (reader.ReadChar() != 63) return false;
        if (reader.ReadChar() != 'N') return false;
        if (reader.ReadChar() != 'U') return false;
        if (reader.ReadChar() != 'M') return false;
        if (reader.ReadChar() != 'P') return false;
        if (reader.ReadChar() != 'Y') return false;

        byte major = reader.ReadByte(); // 1
        byte minor = reader.ReadByte(); // 0

        if (major != 1 || minor != 0)
            throw new NotSupportedException();

        ushort len = reader.ReadUInt16();

        string header = new String(reader.ReadChars(len));
        string mark = "'descr': '";
        int s = header.IndexOf(mark) + mark.Length;
        int e = header.IndexOf("'", s + 1);
        string type = header.Substring(s, e - s);
        bool? isLittleEndian;
        t = GetType(type, out bytes, out isLittleEndian);

        if (isLittleEndian.HasValue && isLittleEndian.Value == false)
            throw new Exception();

        mark = "'fortran_order': ";
        s = header.IndexOf(mark) + mark.Length;
        e = header.IndexOf(",", s + 1);
        bool fortran = bool.Parse(header.Substring(s, e - s));

        if (fortran)
            throw new Exception();

        mark = "'shape': (";
        s = header.IndexOf(mark) + mark.Length;
        e = header.IndexOf(")", s + 1);
        shape = header.Substring(s, e - s).Split(',').Where(v => !String.IsNullOrEmpty(v)).Select(Int32.Parse).ToArray();

        return true;
    }


    private static Type GetType(string dtype, out int bytes, out bool? isLittleEndian)
    {
        isLittleEndian = IsLittleEndian(dtype);
        bytes = Int32.Parse(dtype.Substring(2));

        string typeCode = dtype.Substring(1);

        if (typeCode == "b1")
            return typeof(bool);
        if (typeCode == "i1")
            return typeof(Byte);
        if (typeCode == "i2")
            return typeof(Int16);
        if (typeCode == "i4")
            return typeof(Int32);
        if (typeCode == "i8")
            return typeof(Int64);
        if (typeCode == "u1")
            return typeof(Byte);
        if (typeCode == "u2")
            return typeof(UInt16);
        if (typeCode == "u4")
            return typeof(UInt32);
        if (typeCode == "u8")
            return typeof(UInt64);
        if (typeCode == "f4")
            return typeof(Single);
        if (typeCode == "f8")
            return typeof(Double);
        if (typeCode.StartsWith("S"))
            return typeof(String);

        throw new NotSupportedException();
    }

    private static bool? IsLittleEndian(string type)
    {
        bool? littleEndian = null;

        switch (type[0])
        {
            case '<':
                littleEndian = true;
                break;
            case '>':
                littleEndian = false;
                break;
            case '|':
                littleEndian = null;
                break;
            default:
                throw new Exception();
        }

        return littleEndian;
    }
}
