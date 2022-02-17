using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TP_CameraMiniController : MonoBehaviour
{
    [SerializeField] Transform brainCameraT;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.localRotation = brainCameraT.localRotation;
    }
}
