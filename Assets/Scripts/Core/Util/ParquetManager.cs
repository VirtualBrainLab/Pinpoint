//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using System.IO;
//using System.Linq;
//using Parquet.Data;
//using Parquet;
//using System;
//using UnityEngine.Networking;
//using System.Text;

//public class ParquetManager : MonoBehaviour
//{
//    float[] lc_l_paw_x;
//    float[] lc_l_paw_y;
//    float[] rc_r_paw_x;
//    float[] rc_r_paw_y;

//    bool gotLeft;
//    bool gotRight;
//    int t;

//    string leftCam_uri = "https://ibl.flatironinstitute.org/wittenlab/Subjects/ibl_witten_27/2021-01-21/001/alf/_ibl_leftCamera.dlc.db964f3b-cd47-4372-8ffc-18fb592a24b5.pqt";
//    string rightCam_uri = "https://ibl.flatironinstitute.org/wittenlab/Subjects/ibl_witten_27/2021-01-21/001/alf/_ibl_rightCamera.dlc.d2439829-bc32-460a-848b-60169da8f07d.pqt";

//    [SerializeField] private Transform leftCamera;
//    [SerializeField] private Transform rightCamera;

//    // Start is called before the first frame update
//    void Start()
//    {
//        StartCoroutine(FlationRequestParquet(leftCam_uri, true));
//        StartCoroutine(FlationRequestParquet(rightCam_uri, false));
//        t = 0;
//    }

//    private void Update()
//    {
//        if (true) //gotLeft && gotRight)
//        {
//            Vector3 leftCameraPaw = new Vector3(lc_l_paw_x[t], lc_l_paw_y[t]);
//            Vector3 rightCameraPaw = new Vector3(rc_r_paw_x[t], rc_r_paw_y[t]);

//            Vector3 leftCameraPawAngles = leftCameraPaw / 400;

//            //Vector3 leftCameraPawAngles = new Vector3(UnityEngine.Random.value*10-5, UnityEngine.Random.value*10-5, 0f);
//            //leftCameraPawAngles *= Mathf.PI / 180;

//            Debug.DrawRay(leftCamera.position, 20 * (leftCamera.up + leftCameraPawAngles)); // leftCamera.up + Quaternion.Euler(leftCameraPaw.x, leftCameraPaw.y, 0).eulerAngles);

//            t++;
//        }
//    }

//    public IEnumerator FlationRequestParquet(string uri, bool left)
//    {
//        Debug.Log("A Coroutine requested was started for: " + uri);

//        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
//        {
//            // Request and wait for the desired page.
//            var username = "iblmember";
//            var password = "GrayMatter19";
//            string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
//                                           .GetBytes(username + ":" + password));
//            webRequest.SetRequestHeader("Authorization", "Basic " + encoded);
//            webRequest.SetRequestHeader("Encoding", "gzip");
//            yield return webRequest.SendWebRequest();

//            string[] pages = uri.Split('/');
//            int page = pages.Length - 1;

//            switch (webRequest.result)
//            {
//                case UnityWebRequest.Result.ConnectionError:
//                case UnityWebRequest.Result.DataProcessingError:
//                    Debug.LogError(pages[page] + ": Error: " + webRequest.error);
//                    break;
//                case UnityWebRequest.Result.ProtocolError:
//                    Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
//                    break;
//                case UnityWebRequest.Result.Success:
//                    Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
//                    Debug.Log(webRequest.downloadedBytes);
//                    byte[] data = webRequest.downloadHandler.data;
//                    MemoryStream stream = new MemoryStream(data);
//                    ParseParquet(stream, left);
//                    break;
//            }
//        }

//        Debug.Log("GetRequest complete on URL" + uri);
//    }

//    private void ParseParquet(Stream stream, bool left)
//    {

//        // open parquet file reader
//        using (var parquetReader = new ParquetReader(stream))
//        {
//            // get file schema (available straight after opening parquet reader)
//            // however, get only data fields as only they contain data values
//            DataField[] dataFields = parquetReader.Schema.GetDataFields();

//            string[] relevantFields = { "paw_l_x", "paw_l_y", "paw_r_x", "paw_r_y" };
//            int[] relevantFieldIdxs = new int[4];

//            for (int i = 0; i < dataFields.Length; i++)
//            {
//                DataField cfield = dataFields[i];

//                for (int ri = 0; ri < relevantFields.Length; ri++)
//                {
//                    if (cfield.ToString().Contains(relevantFields[ri]))
//                    {
//                        relevantFieldIdxs[ri] = i;
//                    }
//                }
//            }
//            Debug.Log(relevantFieldIdxs);

//            ParquetRowGroupReader groupReader = parquetReader.OpenRowGroupReader(0);

//            // read all columns inside each row group (you have an option to read only
//            // required columns if you need to.
//            DataColumn[] columns = dataFields.Select(groupReader.ReadColumn).ToArray();

//            // get first column, for instance
//            if (left)
//            {
//                // this is the left camera, so grab the left paw data
//                lc_l_paw_x = DataCol2DoubleArray(columns[relevantFieldIdxs[0]]);
//                lc_l_paw_y = DataCol2DoubleArray(columns[relevantFieldIdxs[1]]);
//                gotLeft = true;
//            }
//            else
//            {
//                // this is the right camera, so grab the right paw data
//                rc_r_paw_x = DataCol2DoubleArray(columns[relevantFieldIdxs[2]]);
//                rc_r_paw_y = DataCol2DoubleArray(columns[relevantFieldIdxs[3]]);
//                gotRight = true;
//            }

//        }
//    }

//    private float[] DataCol2DoubleArray(DataColumn input)
//    {
//        Array data = input.Data;
//        float[] output = new float[data.Length];
//        for (int i = 0; i < data.Length; i++)
//        {
//            output[i] = (float)Convert.ToDouble(data.GetValue(i));
//        }
//        return output;
//    }
//}
