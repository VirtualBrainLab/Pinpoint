using System;
using UnityEngine;

namespace Unisave.Foundation
{
    /// <summary>
    /// Triggers the disposal of the Unisave client application instance
    /// </summary>
    public class UnisaveDisposalTrigger : MonoBehaviour
    {
        public event Action OnDisposalTriggered;
        
        private void OnDestroy()
        {
            OnDisposalTriggered?.Invoke();
        }
    }
}