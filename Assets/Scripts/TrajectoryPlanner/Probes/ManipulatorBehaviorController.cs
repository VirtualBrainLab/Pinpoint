using System;
using CoordinateSpaces;
using CoordinateTransforms;
using EphysLink;
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

        public float BrainSurfaceOffset
        {
            get => _brainSurfaceOffset;
            set
            {
                _brainSurfaceOffset = value;
                BrainSurfaceOffsetChangedEvent.Invoke(value);
            }
        }

        public bool IsSetToDropToSurfaceWithDepth
        {
            get => _isSetToDropToSurfaceWithDepth;
            set
            {
                if (BrainSurfaceOffset != 0) return;
                _isSetToDropToSurfaceWithDepth = value;
                IsSetToDropToSurfaceWithDepthChangedEvent.Invoke(value);
            }
        }

        public CoordinateSpace CoordinateSpace { get; set; }
        public AffineTransform Transform { get; set; }

        public bool IsRightHanded { get; set; }

        #region Private internal fields

        private Vector4 _lastManipulatorPosition = Vector4.zero;
        private Vector4 _zeroCoordinateOffset = Vector4.zero;
        private float _brainSurfaceOffset;
        private bool _isSetToDropToSurfaceWithDepth = true;

        #endregion

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
        private void Awake()
        {
            _annotationDataset = VolumeDatasetManager.AnnotationDataset;
        }


        private void OnEnable()
        {
        }

        #endregion

        #region Public Methods

        public void Initialize(string manipulatorID, bool calibrated)
        {
            ManipulatorID = manipulatorID;
            CoordinateSpace = new SensapexSpace();
            Transform = IsRightHanded
                ? new SensapexRightTransform(_probeController.Insertion.phi)
                : new SensapexLeftTransform(_probeController.Insertion.phi);
            _probeController.Locked = true;

            if (calibrated)
                // Bypass calibration and start echoing
                CommunicationManager.Instance.BypassCalibration(manipulatorID, StartEchoing);
            else
                CommunicationManager.Instance.SetCanWrite(manipulatorID, true, 1,
                    _ =>
                    {
                        CommunicationManager.Instance.Calibrate(manipulatorID,
                            () =>
                            {
                                CommunicationManager.Instance.SetCanWrite(manipulatorID, false, 0, _ => StartEchoing());
                            });
                    });

            void StartEchoing()
            {
                CommunicationManager.Instance.GetPos(manipulatorID, pos =>
                {
                    if (ZeroCoordinateOffset.Equals(Vector4.zero)) ZeroCoordinateOffset = pos;
                    EchoPosition(pos);
                });
            }
        }

        public void Disable()
        {
            ManipulatorID = null;
            _zeroCoordinateOffset = Vector4.zero;
            _brainSurfaceOffset = 0;
            enabled = false;
        }

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

        #endregion

        #region Private Methods

        private void EchoPosition(Vector4 pos)
        {
            if (_probeController == null) return;
            // Calculate last used direction for dropping to brain surface (between depth and DV)
            var dvDelta = Math.Abs(pos.z - _lastManipulatorPosition.z);
            var depthDelta = Math.Abs(pos.w - _lastManipulatorPosition.w);
            if (dvDelta > 0.0001 || depthDelta > 0.0001) IsSetToDropToSurfaceWithDepth = depthDelta >= dvDelta;
            _lastManipulatorPosition = pos;

            // Apply zero coordinate offset
            var zeroCoordinateAdjustedManipulatorPosition = pos - ZeroCoordinateOffset;

            // Convert to sensapex space
            var sensapexSpacePosition = Transform.Transform2Space(zeroCoordinateAdjustedManipulatorPosition);

            // Brain surface adjustment
            var brainSurfaceAdjustment = float.IsNaN(BrainSurfaceOffset) ? 0 : BrainSurfaceOffset;
            if (IsSetToDropToSurfaceWithDepth)
                zeroCoordinateAdjustedManipulatorPosition.w += brainSurfaceAdjustment;
            else
                sensapexSpacePosition.z += brainSurfaceAdjustment;

            // Convert to world space
            var zeroCoordinateAdjustedWorldPosition =
                CoordinateSpace.Space2WorldAxisChange(sensapexSpacePosition);

            // Set probe position (change axes to match probe)
            var transformedApmldv =
                _probeController.Insertion.World2TransformedAxisChange(zeroCoordinateAdjustedWorldPosition);
            _probeController.SetProbePosition(new Vector4(transformedApmldv.x, transformedApmldv.y,
                transformedApmldv.z, zeroCoordinateAdjustedManipulatorPosition.w));

            // Continue echoing position
            CommunicationManager.Instance.GetPos(ManipulatorID, EchoPosition);
        }

        #endregion
    }
}