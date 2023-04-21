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

        private Vector4 _lastManipulatorPosition = Vector4.negativeInfinity;

        private Vector4 _zeroCoordinateOffset = Vector4.negativeInfinity;

        public Vector4 ZeroCoordinateOffset
        {
            get => _zeroCoordinateOffset;
            set
            {
                _zeroCoordinateOffset = value;
                ZeroCoordinateOffsetChangedEvent.Invoke(value);
            }
        }

        public UnityEvent<Vector4> ZeroCoordinateOffsetChangedEvent;

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

        public UnityEvent<float> BrainSurfaceOffsetChangedEvent;

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
        public void SetBrainSurfaceOffset()
        {
            if (_probeManager.IsProbeInBrain())
            {
                // Just calculate the distance from the probe tip position to the brain surface            
                BrainSurfaceOffset -=
                    Vector3.Distance(_probeManager.GetBrainSurface(), _probeController.Insertion.apmldv);
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

        #endregion
    }
}