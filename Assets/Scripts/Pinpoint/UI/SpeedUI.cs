using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpeedUI : MonoBehaviour
{
    [SerializeField] List<Image> _speedImages;
    [SerializeField] Color _activeColor;
    [SerializeField] Color _inactiveColor;

    public void UpdateUI(int speed)
    {
        for (int i = 0; i < _speedImages.Count; i++)
        {
            if (speed == i)
            {
                _speedImages[i].color = _activeColor;
            }
            else
            {
                _speedImages[i].color = _inactiveColor;
            }
        }
    }
}
