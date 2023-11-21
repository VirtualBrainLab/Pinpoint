using UnityEngine;
using UnityEngine.Events;

namespace Pinpoint.UI.EphysCopilot
{
    public class DropdownOpenedNotifier : MonoBehaviour
    {
        [SerializeField] private UnityEvent _dropdownOpenedEvent;

        private void OnEnable()
        {
            _dropdownOpenedEvent.Invoke();
        }
    }
}
