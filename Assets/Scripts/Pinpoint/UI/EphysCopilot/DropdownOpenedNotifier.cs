using System;
using UnityEngine;
using UnityEngine.Events;

namespace TrajectoryPlanner.UI.EphysCopilot
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
