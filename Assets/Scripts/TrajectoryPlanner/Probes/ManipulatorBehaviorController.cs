using System;
using UnityEngine;
using UnityEngine.Events;

namespace TrajectoryPlanner.Probes
{
    public class ManipulatorBehaviorController : MonoBehaviour
    {
        #region Components

        [SerializeField] private ProbeManager _probeManager;
        [SerializeField] private ProbeController _probeController;
        private CCFAnnotationDataset _annotationDataset;

        #endregion

        #region Manipulator Properties

        public string ManipulatorID { get; set; }

        private Vector4 _lastManipulatorPosition = Vector4.zero;

        private Vector4 _zeroCoordinateOffset = Vector4.zero;

        /**
         * Getter and setter or the zero coordinate offset of the manipulator.
         * If passed a NaN value, the previous value is kept.
         */
        public Vector4 ZeroCoordinateOffset
        {
            get => _zeroCoordinateOffset;
            set
            {
                _zeroCoordinateOffset = new Vector4(float.IsNaN(value.x) ? _zeroCoordinateOffset.x : value.x,
                    float.IsNaN(value.y) ? _zeroCoordinateOffset.y : value.y,
                    float.IsNaN(value.z) ? _zeroCoordinateOffset.z : value.z,
                    float.IsNaN(value.w) ? _zeroCoordinateOffset.w : value.w);

                ZeroCoordinateOffsetChangedEvent.Invoke(value);
            }
        }

        private float _brainSurfaceOffset;

        public float BrainSurfaceOffset
        {
            get => _brainSurfaceOffset;
            set
            {
                _brainSurfaceOffset = value;
                BrainSurfaceOffsetChangedEvent.Invoke(value);
            }
        }

        public bool CanChangeBrainSurfaceOffsetAxis => BrainSurfaceOffset == 0;
        
        private bool _isSetToDropToSurfaceWithDepth = true;

        public bool IsSetToDropToSurfaceWithDepth
        {
            get => _isSetToDropToSurfaceWithDepth;
            private set
            {
                _isSetToDropToSurfaceWithDepth = value;
                IsSetToDropToSurfaceWithDepthChangedEvent.Invoke(value);
            }
        }

        #endregion

        #region Events

        public UnityEvent<Vector4> ZeroCoordinateOffsetChangedEvent;
        public UnityEvent<float> BrainSurfaceOffsetChangedEvent;
        public UnityEvent<bool> IsSetToDropToSurfaceWithDepthChangedEvent;

        #endregion

        #region Unity

        /// <summary>
        /// Setup this instance
        /// </summary>
        private void OnEnable()
        {
            _annotationDataset = VolumeDatasetManager.AnnotationDataset;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Set manipulator space offset from brain surface as Depth from manipulator or probe coordinates.
        /// </summary>
        public void ComputeBrainSurfaceOffset()
        {
            if (_probeManager.IsProbeInBrain())
            {
                // Just calculate the distance from the probe tip position to the brain surface            
                BrainSurfaceOffset -= _probeManager.GetSurfaceCoordinateT().depthT;
            }
            else
            {
                // We need to calculate the surface coordinate ourselves
                var tipExtensionDirection =
                    IsSetToDropToSurfaceWithDepth ? _probeController.GetTipWorldU().tipUpWorldU : Vector3.up;

                var brainSurfaceCoordinate = _annotationDataset.FindSurfaceCoordinate(
                    _annotationDataset.CoordinateSpace.World2Space(_probeController.GetTipWorldU().tipCoordWorldU -
                                                                   tipExtensionDirection * 5),
                    _annotationDataset.CoordinateSpace.World2SpaceAxisChange(tipExtensionDirection));

                if (float.IsNaN(brainSurfaceCoordinate.x))
                {
                    Debug.LogWarning("Could not find brain surface! Canceling set brain offset.");
                    return;
                }

                var brainSurfaceToTransformed =
                    _probeController.Insertion.World2Transformed(
                        _annotationDataset.CoordinateSpace.Space2World(brainSurfaceCoordinate));

                BrainSurfaceOffset += Vector3.Distance(brainSurfaceToTransformed,
                    _probeController.Insertion.apmldv);
                ;
            }
        }

        /// <summary>
        ///     Manual adjustment of brain surface offset.
        /// </summary>
        /// <param name="increment">Amount to change the brain surface offset by</param>
        public void IncrementBrainSurfaceOffset(float increment)
        {
            BrainSurfaceOffset += increment;
        }
        
        /// <summary>
        ///     Set if the probe should be dropped to the surface with depth or with DV.
        /// </summary>
        /// <param name="dropToSurfaceWithDepth">Use depth if true, use DV if false</param>
        public void SetDropToSurfaceWithDepth(bool dropToSurfaceWithDepth)
        {
            // Only make changes to brain surface offset axis if the offset is 0
            if (!CanChangeBrainSurfaceOffsetAxis) return;
        
            // Apply change (if eligible)
            IsSetToDropToSurfaceWithDepth = dropToSurfaceWithDepth;
        }

        #endregion
    }
}