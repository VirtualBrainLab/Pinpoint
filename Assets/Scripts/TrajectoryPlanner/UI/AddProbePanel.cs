using UnityEngine;

public class AddProbePanel : MonoBehaviour
{
    public void ShowPanel()
    {
        gameObject.SetActive(true);
    }

    public void HidePanel()
    {
        gameObject.SetActive(false);
    }

    public void SetVisibility(bool state)
    {
        gameObject.SetActive(state);
    }
}
