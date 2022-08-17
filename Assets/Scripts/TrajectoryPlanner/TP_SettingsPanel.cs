using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TP_SettingsPanel : MonoBehaviour
{
    [SerializeField] Transform settingsPanelT;

    List<Transform> childTs;
    RectTransform rt;
    bool active;

    // Start is called before the first frame update
    void Start()
    {
        childTs = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
            if (transform.GetChild(i) != settingsPanelT)
                childTs.Add(transform.GetChild(i));

        rt = GetComponent<RectTransform>();
        active = true;

        // start by setting to collapsed
        ChangePanelState();
    }

    public void ChangePanelState()
    {
        if (active)
        {
            rt.sizeDelta = new Vector2(rt.rect.width, 500);
            foreach (Transform t in childTs)
            {
                t.gameObject.SetActive(true);
            }
            active = !active;
        }
        else
        {
            rt.sizeDelta = new Vector2(rt.rect.width, 30);
            foreach (Transform t in childTs)
            {
                t.gameObject.SetActive(false);
            }
            active = !active;
        }
    }
}
