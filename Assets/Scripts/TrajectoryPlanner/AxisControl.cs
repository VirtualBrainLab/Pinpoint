using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AxisControl : MonoBehaviour
{
    [SerializeField] GameObject apGO;
    [SerializeField] GameObject dvGO;
    [SerializeField] GameObject mlGO;

    public void SetAxisPosition(Vector3 pos)
    {
        apGO.transform.position = pos;
        dvGO.transform.position = pos;
        mlGO.transform.position = pos;
    }
    public void SetAPVisibility(bool state)
    {
        apGO.SetActive(state);
    }
    public void SetDVVisibility(bool state)
    {
        dvGO.SetActive(state);
    }
    public void SetMLVisibility(bool state)
    {
        mlGO.SetActive(state);
    }
}
