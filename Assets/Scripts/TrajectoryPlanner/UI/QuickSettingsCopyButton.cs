using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class QuickSettingsCopyButton : MonoBehaviour, IPointerDownHandler
{
    public UnityEvent onPointerDown;

    public void OnPointerDown(PointerEventData eventData)
    {
        onPointerDown.Invoke();
    }

}
