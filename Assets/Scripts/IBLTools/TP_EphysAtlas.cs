using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BestHTTP.SocketIO3;
using System;
using Random = UnityEngine.Random;

public class TP_EphysAtlas : MonoBehaviour
{
    [SerializeField] TP_TrajectoryPlannerManager tpmanager;
    [SerializeField] LineRenderer line;

    // Line data
    Vector3[] positionData;

    TP_ProbeController activeProbeController;
    SocketManager manager;

    private bool loaded = false;
    ChannelData curChan;

    private bool prevSpike;
    private float nextSpike = 0f;
    private float nextAmp = 0f;

    // Start is called before the first frame update
    void Start()
    {
        manager = new SocketManager(new Uri("http://128.95.53.25:5000"));
        manager.Socket.On("connect", () => Debug.Log(manager.Handshake.Sid));
        manager.Socket.On<List<float>>("data", ParseData);
        manager.Socket.Emit("data", new float[] { 100, 100, 150 });

        line.positionCount = 370;

        // Setup line renderer
        positionData = new Vector3[370];
        for (int i = 0; i < 370; i++)
            positionData[i] = Vector3.right * i;
    }

    private void OnDestroy()
    {
        manager.Close();
    }

    // Update is called once per frame
    void Update()
    {
        if (loaded)
        {
            if (prevSpike)
                positionData[positionData.Length - 1].y = 0f;

            if (Time.realtimeSinceStartup > nextSpike)
            {
                Debug.Log("Spike w/ amp: " + nextAmp);
                UpdateLine(nextAmp * 1000000);
                GetNextSpike();
            }
            else
                UpdateLine(0f);
        }
    }

    private void UpdateLine(float newVal)
    {
        for (int i = 1; i < positionData.Length; i++)
            positionData[i - 1].y = positionData[i].y;
        positionData[positionData.Length - 1].y = newVal;
        line.SetPositions(positionData);
    }

    private void GetNextSpike()
    {
        var (t, a) = curChan.NextSpike();
        nextSpike = Time.realtimeSinceStartup + t;
        nextAmp = a;
    }

    // data ['row','index','area','clu_count','amp_min','amp_max','i0','i1','i2','i3','i4','i5','i6','i7','i8','i9']

    private void ParseData(List<float> data)
    {
        float[] isiDist = new float[10];

        float sum = 0;
        for (int i = 6; i < 16; i++)
        {
            sum += data[i];
            isiDist[i - 6] = sum;
            Debug.Log(sum);
        }

        curChan = new ChannelData(data[2], data[3], data[4], data[5], isiDist);


        GetNextSpike();
        loaded = true;
    }
}

public class ChannelData
{
    float area;
    float cluCount;
    float ampMin;
    float ampMax;
    float ampDelta;
    float[] isiDist;

    float[] timeValues = new float[]{0f, 0.0027825594022071257f,
       0.007742636826811269f, 0.021544346900318832f, 0.05994842503189409f,
       0.1668100537200059f, 0.46415888336127775f, 1.2915496650148828f,
       3.593813663804626f, 10.0f };

    public ChannelData(float area, float cluCount, float ampMin, float ampMax, float[] isiDist)
    {
        this.area = area;
        this.cluCount = cluCount;
        this.ampMin = ampMin;
        this.ampMax = ampMax;
        ampDelta = ampMax - ampMin;
        this.isiDist = isiDist;
    }

    public (float, float) NextSpike()
    {
        float next = Random.value;
        for (int i = 0; i < isiDist.Length; i++)
        {
            if (next < isiDist[i])
            {
                float spikeTime = timeValues[i] + Random.value * (timeValues[i + 1] - timeValues[i]);
                return (spikeTime, NextAmplitude());
            }
        }
        return (0, 0);
    }

    public float NextAmplitude()
    {
        return Random.value * ampDelta + ampMin;
    }
}