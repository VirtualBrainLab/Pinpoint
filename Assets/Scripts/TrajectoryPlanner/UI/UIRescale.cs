using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRescale : MonoBehaviour
{
    private Canvas _canvas;
    [SerializeField] TMP_InputField inputField;

    private void Awake()
    {
        _canvas = GetComponent<Canvas>(); 
    }

    // Update is called once per frame
    void Update()
    {
        if (!inputField.isFocused)
            inputField.text = string.Format("{0:F2}", _canvas.scaleFactor);
    }

    public void UpdateScale(string newScale)
    {
        float newFactor;
        if (float.TryParse(newScale, out newFactor))
            _canvas.scaleFactor = newFactor;
    }
}
