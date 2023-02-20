using UnityEngine;
using UnityEngine.Networking;

public class APIManager : MonoBehaviour
{
    private UnityWebRequest _probeDataRequest;

#region POST probe data target
    public void UpdateProbeDataTarget(string targetURL)
    {
        _probeDataRequest = new UnityWebRequest(targetURL, "POST");
        SendProbeData();
    }

    public void SendProbeData()
    {
        if (Settings.ProbeDataPOST && _probeDataRequest != null)
        {
            //ProbeManager.ActiveProbeManager.GetProbeController().
            // add data
            //_probeDataRequest.SetRequestHeader

            //_probeDataRequest.SendWebRequest();
        }
    }
#endregion
}
