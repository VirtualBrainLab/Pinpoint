using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BestHTTP.SocketIO3;
using System;
using Random = UnityEngine.Random;
using TrajectoryPlanner;

namespace IBLTools
{

    public class TP_EphysAtlas : MonoBehaviour
    {
        [SerializeField] TrajectoryPlannerManager tpmanager;
        [SerializeField] Material lineMaterial;

        private AnnotationDataset annotationDataset;

        SocketManager manager;

        private const int CHAN_COUNT = 40;
        List<Channel> allChannels;

        private float lastRequest;

        // Start is called before the first frame update
        void Start()
        {
            Debug.LogWarning("(Ephys Atlas) Development mode running");
            manager = new SocketManager(new Uri("http://128.95.53.25:5000"));
            manager.Socket.On("connect", () => Debug.Log(manager.Handshake.Sid));
            manager.Socket.On<List<float>>("data", ReceiveData);

            allChannels = new List<Channel>();

            // build 10 base channels for now
            for (int ci = 0; ci < CHAN_COUNT; ci++)
            {
                LineRenderer chanLine = transform.GetChild(ci).GetComponent<LineRenderer>();
                chanLine.startWidth = 0.02f;
                chanLine.endWidth = 0.02f;
                Channel chan = new Channel(370, chanLine);
                allChannels.Add(chan);
            }

            GetAnnotationDataset();
        }

        private async void GetAnnotationDataset()
        {
            await tpmanager.GetAnnotationDatasetLoadedTask();
            annotationDataset = tpmanager.GetAnnotationDataset();
        }

        private void OnDestroy()
        {
            manager.Close();
        }

        // Update is called once per frame
        void Update()
        {
            if (tpmanager.GetActiveProbeController() != null)
            {
                if (tpmanager.MovedThisFrame())
                    UpdateProbePosition();
                else
                    foreach (Channel chan in allChannels)
                        chan.UpdateChannel();
            }
        }

        private void UpdateProbePosition()
        {
            if ((Time.realtimeSinceStartup - lastRequest) < 1f)
            {
                //Debug.Log("(EphysAtlas) Too many requests -- ignoring");
                return;
            }
            lastRequest = Time.realtimeSinceStartup;

            (Vector3 tip_apdvlr, Vector3 top_apdvlr) = tpmanager.GetActiveProbeController().GetRecordingRegionCoordinatesAPDVLR();

            for (int ci = 0; ci <= (CHAN_COUNT - 1); ci++)
            {
                Vector3 coords = NearestInt(Vector3.Lerp(top_apdvlr, tip_apdvlr, ci / (float)(CHAN_COUNT - 1f)));
                if (annotationDataset != null)
                {
                    // check whether the coords are within the annotation dataset
                    if (annotationDataset.ValueAtIndex(coords) > 0)
                        RequestData(ci, coords);
                }
            }
        }

        private Vector3 NearestInt(Vector3 input)
        {
            return new Vector3(Mathf.RoundToInt(input.x), Mathf.RoundToInt(input.y), Mathf.RoundToInt(input.z));
        }

        private void RequestData(int chan, Vector3 coord)
        {
            allChannels[chan].RequestingData();
            Debug.Log("Requesting data for channel " + chan + ": " + coord.ToString());
            manager.Socket.Emit("data", new float[] { chan, coord.x, coord.y, coord.z });
        }

        // data ['row','index','area','clu_count','amp_min','amp_max','i0','i1','i2','i3','i4','i5','i6','i7','i8','i9']

        private void ReceiveData(List<float> data)
        {
            var (chan, chanData) = ParseData(data);
            Debug.Log("Received data for channel " + chan);
            allChannels[chan].AddData(chanData);
        }

        private (int, ChannelData) ParseData(List<float> data)
        {
            int channel = (int)data[0];

            float[] cumISIprob = new float[10];

            float sum = 0;
            for (int i = 7; i < 17; i++)
            {
                sum += data[i];
                cumISIprob[i - 7] = sum;
            }

            ChannelData chanData = new ChannelData(data[3], data[4], data[5], data[6], cumISIprob);

            return (channel, chanData);
        }
    }

    public class Channel
    {
        LineRenderer line;
        ChannelData data;

        // Line data
        private static float YSCALE = 0.5f;
        Vector3[] positionData;

        private bool enabled;

        public Channel(int lineWidth, LineRenderer line)
        {
            this.line = line;
            line.positionCount = lineWidth;

            ChannelSetup();
        }
        public Channel(int lineWidth, LineRenderer line, ChannelData data)
        {
            this.line = line;
            line.positionCount = lineWidth;
            this.data = data;

            ChannelSetup();

            enabled = true;
            GetNextSpike();
        }

        private void ChannelSetup()
        {
            // Setup line renderer
            positionData = new Vector3[line.positionCount];
            for (int i = 0; i < line.positionCount; i++)
                positionData[i] = new Vector3(i, 0f, 0f);
        }

        private bool prevSpike;
        private float nextSpike = 0f;
        private float nextAmp = 0f;

        public void UpdateChannel()
        {
            if (!enabled)
                return;

            if (prevSpike)
                positionData[positionData.Length - 1].y = 0f;

            if (Time.realtimeSinceStartup > nextSpike)
            {
                UpdateLine(nextAmp * 1000000);
                GetNextSpike();
            }
            else
                UpdateLine(0f);
        }

        public void RequestingData()
        {
            enabled = false;
        }

        public void AddData(ChannelData data)
        {
            this.data = data;
            enabled = true;
            GetNextSpike();
        }

        private void UpdateLine(float newVal)
        {
            for (int i = 1; i < positionData.Length; i++)
                positionData[i - 1].y = positionData[i].y;
            positionData[positionData.Length - 1].y = newVal * YSCALE;
            line.SetPositions(positionData);
        }

        private void GetNextSpike()
        {
            var (t, a) = data.NextSpike();
            nextSpike = Time.realtimeSinceStartup + t;
            nextAmp = a;
        }
    }

    public class ChannelData
    {
        float area;
        float cluCount;
        float ampMin;
        float ampMax;
        float ampDelta;
        float[] cumISIprob;

        float[] timeValues = new float[]{0f, 0.0027825594022071257f,
       0.007742636826811269f, 0.021544346900318832f, 0.05994842503189409f,
       0.1668100537200059f, 0.46415888336127775f, 1.2915496650148828f,
       3.593813663804626f, 10.0f , 20f};

        public ChannelData(float area, float cluCount, float ampMin, float ampMax, float[] cumISIprob)
        {
            this.area = area;
            this.cluCount = cluCount;
            this.ampMin = ampMin;
            this.ampMax = ampMax;
            ampDelta = ampMax - ampMin;
            this.cumISIprob = cumISIprob;
        }

        public (float, float) NextSpike()
        {
            float next = Random.value;
            float amp = NextAmplitude();

            for (int i = 0; i < cumISIprob.Length; i++)
            {
                if (next < cumISIprob[i])
                {
                    float spikeTime = timeValues[i] + Random.value * (timeValues[i + 1] - timeValues[i]);
                    return (spikeTime, amp);
                }
            }
            return (40f, amp);
        }

        public float NextAmplitude()
        {
            return Random.value * ampDelta + ampMin;
        }
    }
}