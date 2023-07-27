using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HelpUIVersion : MonoBehaviour
{
    private void Awake()
    {
        var text = GetComponent<TMP_Text>();

        text.text += $"\n\nYou are running " +
            $"Pinpoint v{Application.version} on Unity {Application.unityVersion}";
    }
}
