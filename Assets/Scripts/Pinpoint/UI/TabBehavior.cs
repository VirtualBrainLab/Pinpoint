using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Generic behavior for all Selectable objects, allowing tab to move between left and right linked elements
/// </summary>
public class TabBehavior : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            GameObject c = EventSystem.current.currentSelectedGameObject;
            if (c == null) return;
            Selectable s = c.GetComponent<Selectable>();
            if (s == null) return;
            Selectable jump = Input.GetKey(KeyCode.LeftShift)
                ? s.FindSelectableOnLeft() : s.FindSelectableOnRight();
            if (jump != null)
                jump.Select();
            else
            {
                // if jump is null, check the up down
                jump = Input.GetKey(KeyCode.LeftShift)
                ? s.FindSelectableOnUp() : s.FindSelectableOnDown();
                if (jump != null)
                    jump.Select();
            }
        }
    }
}
