using System.Threading;

namespace Unisave.Editor.BackendUploading.States
{
    public class StateUploading : BaseState
    {
        /// <summary>
        /// Can be used to cancel the uploading job
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; }
        
        /// <summary>
        /// How many steps need to be performed in total
        /// </summary>
        public int TotalSteps { get; set; }
        
        /// <summary>
        /// How many steps have been performed so far
        /// </summary>
        public int PerformedSteps { get; set; }

        /// <summary>
        /// Uploading progress in [0, 1] range
        /// </summary>
        public float Progress => PerformedSteps / (float) TotalSteps;

        public StateUploading()
        {
            CancellationTokenSource = new CancellationTokenSource();
            TotalSteps = 2; // minimum
        }
    }
}