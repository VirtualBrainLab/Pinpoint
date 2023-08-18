using System;
using System.Globalization;
using System.Linq;
using CoordinateSpaces;
using CoordinateTransforms;
using EphysLink;
using UnityEngine;
using UnityEngine.Events;

namespace TrajectoryPlanner.Probes
{
    public class ManipulatorBehaviorController : MonoBehaviour
    {
        #region Constants

        public const int AUTOMATIC_MOVEMENT_SPEED = 500;

        #endregion

        #region Private Methods

        private void EchoPosition(Vector4 pos)
        {
            if (!enabled && _probeController == null) return;

            // Check for special pathfinder mode (directly set probe position, no calculations needed)
            if (ManipulatorType.Contains("pathfinder"))
            {
                CommunicationManager.Instance.GetAngles(ManipulatorID, angles =>
                {
                    _probeController.SetProbeAngles(angles);
                    _probeController.SetProbePosition(new Vector3(pos.y, pos.x, pos.z));
                });
            }
            else
            {
                // Calculate last used direction for dropping to brain surface (between depth and DV)
                var dvDelta = Math.Abs(pos.z - _lastManipulatorPosition.z);
                var depthDelta = Math.Abs(pos.w - _lastManipulatorPosition.w);
                if (dvDelta > 0.0001 || depthDelta > 0.0001) IsSetToDropToSurfaceWithDepth = depthDelta > dvDelta;
                _lastManipulatorPosition = pos;

                // Apply zero coordinate offset
                var zeroCoordinateAdjustedManipulatorPosition = pos - ZeroCoordinateOffset;

                // Convert to coordinate space
                var manipulatorSpacePosition =
                    Transform.Transform2Space(zeroCoordinateAdjustedManipulatorPosition);

                // Brain surface adjustment
                // FIXME: Dependent on CoordinateSpace direction. Should be standardized by Ephys Link.
                var brainSurfaceAdjustment = float.IsNaN(BrainSurfaceOffset) ? 0 : BrainSurfaceOffset;
                if (IsSetToDropToSurfaceWithDepth)
                    zeroCoordinateAdjustedManipulatorPosition.w +=
                        CoordinateSpace.World2SpaceAxisChange(Vector3.down).z * brainSurfaceAdjustment;
                else
                    manipulatorSpacePosition.z +=
                        CoordinateSpace.World2SpaceAxisChange(Vector3.down).z * brainSurfaceAdjustment;

                // Convert to world space
                var zeroCoordinateAdjustedWorldPosition =
                    CoordinateSpace.Space2World(manipulatorSpacePosition);

                // Set probe position (change axes to match probe)
                var transformedApmldv =
                    _probeController.Insertion.World2TransformedAxisChange(zeroCoordinateAdjustedWorldPosition);

                // FIXME: Dependent on Manipulator Type. Should be standardized by Ephys Link.
                if (ManipulatorType == "new_scale")
                    _probeController.SetProbePosition(new Vector4(transformedApmldv.x, transformedApmldv.y,
                        transformedApmldv.z, 0));
                else
                    _probeController.SetProbePosition(new Vector4(transformedApmldv.x, transformedApmldv.y,
                        transformedApmldv.z, zeroCoordinateAdjustedManipulatorPosition.w));
            }

            // Log every 5 hz
            if (Time.time - _lastLoggedTime >= 0.2)
            {
                _lastLoggedTime = Time.time;
                var tipPos = _probeController.ProbeTipT.position;

                // ["ephys_link", Real time stamp, Manipulator ID, X, Y, Z, W, Phi, Theta, Spin, TipX, TipY, TipZ]
                string[] data =
                {
                    "ephys_link", Time.realtimeSinceStartup.ToString(CultureInfo.InvariantCulture), ManipulatorID,
                    pos.x.ToString(CultureInfo.InvariantCulture), pos.y.ToString(CultureInfo.InvariantCulture),
                    pos.z.ToString(CultureInfo.InvariantCulture), pos.w.ToString(CultureInfo.InvariantCulture),
                    _probeController.Insertion.yaw.ToString(CultureInfo.InvariantCulture),
                    _probeController.Insertion.pitch.ToString(CultureInfo.InvariantCulture),
                    _probeController.Insertion.roll.ToString(CultureInfo.InvariantCulture),
                    tipPos.x.ToString(CultureInfo.InvariantCulture), tipPos.y.ToString(CultureInfo.InvariantCulture),
                    tipPos.z.ToString(CultureInfo.InvariantCulture)
                };
                OutputLog.Log(data);
            }

            // Continue echoing position
            CommunicationManager.Instance.GetPos(ManipulatorID, EchoPosition);
        }

        #endregion

        #region Unity

        /// <summary>
        ///     Setup this instance
        /// </summary>
        private void Awake()
        {
            // Start off as disabled
            enabled = false;
        }

        private void OnDisable()
        {
            ManipulatorID = null;
            _zeroCoordinateOffset = Vector4.zero;
            _brainSurfaceOffset = 0;
        }

        #endregion

        #region Components

        [SerializeField] private ProbeManager _probeManager;
        [SerializeField] private ProbeController _probeController;
        private readonly CCFAnnotationDataset _annotationDataset = VolumeDatasetManager.AnnotationDataset;

        #endregion

        #region Properties

        public string ManipulatorID { get; private set; }

        public string ManipulatorType { get; set; }

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

                ZeroCoordinateOffsetChangedEvent.Invoke(_zeroCoordinateOffset);
            }
        }

        public float BrainSurfaceOffset
        {
            get => _brainSurfaceOffset;
            set
            {
                _brainSurfaceOffset = value;
                BrainSurfaceOffsetChangedEvent.Invoke(_brainSurfaceOffset);
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

        public CoordinateSpace CoordinateSpace { get; private set; }
        private CoordinateTransform Transform { get; set; }

        public bool IsRightHanded
        {
            get => _isRightHanded;
            set
            {
                _isRightHanded = value;
                Transform = IsRightHanded
                    ? new SensapexRightTransform(_probeController.Insertion.yaw)
                    : new SensapexLeftTransform(_probeController.Insertion.yaw);
            }
        }

        #region Private internal fields

        private Vector4 _lastManipulatorPosition = Vector4.zero;
        private Vector4 _zeroCoordinateOffset = Vector4.zero;
        private float _brainSurfaceOffset;
        private bool _isSetToDropToSurfaceWithDepth = true;
        private bool _isRightHanded;
        private float _lastLoggedTime;

        #endregion

        #endregion

        #region Events

        public UnityEvent<Vector4> ZeroCoordinateOffsetChangedEvent;
        public UnityEvent<float> BrainSurfaceOffsetChangedEvent;
        public UnityEvent<bool> IsSetToDropToSurfaceWithDepthChangedEvent;

        #endregion

        #region Public Methods

        public void Initialize(string manipulatorID, bool calibrated)
        {
            // FIXME: Dependent on Manipulator Type. Should be standardized by Ephys Link.
            CommunicationManager.Instance.GetManipulators((ids, type) =>
            {
                if (!ids.Contains(manipulatorID)) return;

                ManipulatorID = manipulatorID;
                if (type == "sensapex")
                {
                    CoordinateSpace = new SensapexSpace();
                    Transform = IsRightHanded
                        ? new SensapexRightTransform(_probeController.Insertion.yaw)
                        : new SensapexLeftTransform(_probeController.Insertion.yaw);
                }
                else
                {
                    CoordinateSpace = new NewScaleSpace();
                    Transform = new NewScaleLeftTransform(_probeController.Insertion.yaw,
                        _probeController.Insertion.pitch);
                }

                _probeController.UnlockedDir = Vector4.zero;

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
                                    CommunicationManager.Instance.SetCanWrite(manipulatorID, false, 0,
                                        _ => StartEchoing());
                                });
                        });
                return;

                void StartEchoing()
                {
                    CommunicationManager.Instance.GetPos(manipulatorID, pos =>
                    {
                        if (ZeroCoordinateOffset.Equals(Vector4.zero)) ZeroCoordinateOffset = pos;
                        EchoPosition(pos);
                    });
                }
            });
        }

        public Vector4 ConvertInsertionToManipulatorPosition(Vector3 insertionAPMLDV)
        {
            // Convert apmldv to world coordinate
            var convertToWorld = _probeManager.ProbeController.Insertion.Transformed2WorldAxisChange(insertionAPMLDV);

            // Convert to Sensapex space
            var posInSensapexSpace =
                _probeManager.ManipulatorBehaviorController.CoordinateSpace.World2Space(convertToWorld);
            Vector4 posInSensapexTransform =
                _probeManager.ManipulatorBehaviorController.Transform.Space2Transform(posInSensapexSpace);

            // Apply brain surface offset
            var brainSurfaceAdjustment = float.IsNaN(_probeManager.ManipulatorBehaviorController.BrainSurfaceOffset)
                ? 0
                : _probeManager.ManipulatorBehaviorController.BrainSurfaceOffset;
            if (_probeManager.ManipulatorBehaviorController.IsSetToDropToSurfaceWithDepth)
                posInSensapexTransform.w -= brainSurfaceAdjustment;
            else
                posInSensapexTransform.z -= brainSurfaceAdjustment;

            // Apply coordinate offsets and return result
            return posInSensapexTransform + _probeManager.ManipulatorBehaviorController.ZeroCoordinateOffset;
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
        ///     Move manipulator by a given delta in world space
        /// </summary>
        /// <param name="worldSpaceDelta">Delta (X, Y, Z, D) to move by in world space coordinates</param>
        /// <param name="onSuccessCallback">Action on success</param>
        /// <param name="onErrorCallback">Action on error</param>
        public void MoveByWorldSpaceDelta(Vector4 worldSpaceDelta, Action<bool> onSuccessCallback,
            Action<string> onErrorCallback = null)
        {
            // Convert to manipulator axes (world -> space -> transform)
            var manipulatorSpaceDelta = CoordinateSpace.World2SpaceAxisChange(worldSpaceDelta);
            var manipulatorTransformDelta = Transform.Space2Transform(manipulatorSpaceDelta);
            var manipulatorSpaceDepth = CoordinateSpace
                .World2SpaceAxisChange(Vector3.down).z * worldSpaceDelta.w;

            // Get manipulator position
            CommunicationManager.Instance.GetPos(ManipulatorID, pos =>
            {
                // Apply delta
                var targetPosition = pos + new Vector4(manipulatorTransformDelta.x, manipulatorTransformDelta.y,
                    manipulatorTransformDelta.z);
                // Move manipulator
                CommunicationManager.Instance.SetCanWrite(ManipulatorID, true, 1, b =>
                {
                    if (!b) return;
                    CommunicationManager.Instance.GotoPos(ManipulatorID, targetPosition, AUTOMATIC_MOVEMENT_SPEED,
                        newPos =>
                        {
                            // Process depth movement
                            var targetDepth = newPos.w + manipulatorSpaceDepth;
                            CommunicationManager.Instance.SetInsideBrain(
                                ManipulatorID, true, _ =>
                                {
                                    // Move the manipulator
                                    CommunicationManager.Instance.DriveToDepth(
                                        ManipulatorID, targetDepth, AUTOMATIC_MOVEMENT_SPEED,
                                        _ =>
                                        {
                                            CommunicationManager.Instance.SetInsideBrain(
                                                ManipulatorID, false,
                                                _ =>
                                                {
                                                    CommunicationManager.Instance.SetCanWrite(ManipulatorID, false, 0,
                                                        onSuccessCallback, onErrorCallback);
                                                }, onErrorCallback);
                                        }, onErrorCallback);
                                }, onErrorCallback);
                        }, onErrorCallback);
                }, onErrorCallback);
            });
        }

        /// <summary>
        ///     Drive the manipulator back to the zero coordinate position
        /// </summary>
        /// <param name="onSuccessCallback">Action on success</param>
        /// <param name="onErrorCallBack">Action on failure</param>
        public void MoveBackToZeroCoordinate(Action<Vector4> onSuccessCallback, Action<string> onErrorCallBack)
        {
            // Send move command
            CommunicationManager.Instance.SetCanWrite(ManipulatorID, true, 1, b =>
            {
                if (!b) return;
                CommunicationManager.Instance.GotoPos(ManipulatorID, ZeroCoordinateOffset, AUTOMATIC_MOVEMENT_SPEED,
                    pos =>
                    {
                        CommunicationManager.Instance.SetCanWrite(ManipulatorID, false, 0, _ => onSuccessCallback(pos),
                            onErrorCallBack);
                    }, onErrorCallBack);
            }, onErrorCallBack);
        }

        #endregion
    }
}