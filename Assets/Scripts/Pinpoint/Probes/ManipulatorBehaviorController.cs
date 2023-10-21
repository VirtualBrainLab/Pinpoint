using System;
using System.Globalization;
using System.Linq;
using BrainAtlas;
using BrainAtlas.CoordinateSystems;
using EphysLink;
using UnityEngine;
using UnityEngine.Events;
using Urchin.Utils;

namespace TrajectoryPlanner.Probes
{
    public class ManipulatorBehaviorController : MonoBehaviour
    {
        #region Constants

        // Default movement speed: 0.5 mm/s
        public const float AUTOMATIC_MOVEMENT_SPEED = 0.5f;

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
                    AtlasTransform.T2U(zeroCoordinateAdjustedManipulatorPosition);

                // Brain surface adjustment
                // FIXME: Dependent on CoordinateSpace direction. Should be standardized by Ephys Link.
                var brainSurfaceAdjustment = float.IsNaN(BrainSurfaceOffset) ? 0 : BrainSurfaceOffset;
                if (IsSetToDropToSurfaceWithDepth)
                    zeroCoordinateAdjustedManipulatorPosition.w +=
                        ReferenceAtlas.World2Atlas_Vector(Vector3.down).z * brainSurfaceAdjustment;
                else
                    manipulatorSpacePosition.z +=
                        ReferenceAtlas.World2Atlas_Vector(Vector3.down).z * brainSurfaceAdjustment;

                // Convert to world space
                var zeroCoordinateAdjustedWorldPosition =
                    ReferenceAtlas.Atlas2World(manipulatorSpacePosition);

                // Set probe position (change axes to match probe)
                var transformedApmldv =
                    _probeController.Insertion.World2T_Vector(zeroCoordinateAdjustedWorldPosition);

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
                    _probeController.Insertion.Yaw.ToString(CultureInfo.InvariantCulture),
                    _probeController.Insertion.Pitch.ToString(CultureInfo.InvariantCulture),
                    _probeController.Insertion.Roll.ToString(CultureInfo.InvariantCulture),
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

            // Update manipulator inside brain state

            // [NOTE FROM DAN: did I disable this?]
             //_probeController.MovedThisFrameEvent.AddListener(() =>
             //{
             //    if (_isSetToInsideBrain != _probeManager.IsProbeInBrain())
             //        CommunicationManager.Instance.SetInsideBrain(ManipulatorID, _probeManager.IsProbeInBrain(),
             //            insideBrain =>
             //            {
             //                _isSetToInsideBrain = insideBrain;
             //                _probeController.UnlockedDir = insideBrain ? new Vector4(0, 0, 0, 1) : Vector4.one;
             //            });
             //});
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

        public ReferenceAtlas ReferenceAtlas { get; private set; }
        private AtlasTransform AtlasTransform { get; set; }

        public bool IsRightHanded
        {
            get => _isRightHanded;
            set
            {
                _isRightHanded = value;
                UpdateSpaceAndTransform();
            }
        }

        #region Private internal fields

        private Vector4 _lastManipulatorPosition = Vector4.zero;
        private Vector4 _zeroCoordinateOffset = Vector4.zero;
        private float _brainSurfaceOffset;
        private bool _isSetToDropToSurfaceWithDepth = true;
        private bool _isRightHanded;
        private float _lastLoggedTime;
        private bool _isSetToInsideBrain;

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
                // Shortcut exit if we have an invalid manipulator ID
                if (!ids.Contains(manipulatorID)) return;

                // Set manipulator ID and type
                ManipulatorID = manipulatorID;
                ManipulatorType = type;

                // Update transform and space
                UpdateSpaceAndTransform();

                // Lock the manipulator from manual control
                _probeController.SetControllerLock(true);

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

        public void UpdateSpaceAndTransform()
        {
            if (ManipulatorType == "sensapex")
            {
                ReferenceAtlas = new SensapexSpace();
                AtlasTransform = IsRightHanded
                    ? new SensapexRightTransform(_probeController.Insertion.Yaw)
                    : new SensapexLeftTransform(_probeController.Insertion.Yaw);
            }
            else
            {
                ReferenceAtlas = new NewScaleSpace();
                AtlasTransform = new NewScaleLeftTransform(_probeController.Insertion.Yaw,
                    _probeController.Insertion.Pitch);
            }
        }

        public Vector4 ConvertInsertionToManipulatorPosition(Vector3 insertionAPMLDV)
        {
            // Convert apmldv to world coordinate
            var convertToWorld = _probeManager.ProbeController.Insertion.T2World_Vector(insertionAPMLDV);

            // Convert to Sensapex space
            var posInManipulatorSpace =
                _probeManager.ManipulatorBehaviorController.ReferenceAtlas.World2Atlas(convertToWorld);
            Vector4 posInManipulatorTransform =
                _probeManager.ManipulatorBehaviorController.AtlasTransform.U2T(posInManipulatorSpace);

            // Apply brain surface offset
            var brainSurfaceAdjustment = float.IsNaN(_probeManager.ManipulatorBehaviorController.BrainSurfaceOffset)
                ? 0
                : _probeManager.ManipulatorBehaviorController.BrainSurfaceOffset;
            if (_probeManager.ManipulatorBehaviorController.IsSetToDropToSurfaceWithDepth)
                posInManipulatorTransform.w -= brainSurfaceAdjustment;
            else
                posInManipulatorTransform.z -= brainSurfaceAdjustment;

            // Apply coordinate offsets and return result
            return posInManipulatorTransform + _probeManager.ManipulatorBehaviorController.ZeroCoordinateOffset;
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

                var brainSurfaceIdxCoordU = _probeManager.FindSurfaceIdxCoordinate(
                    BrainAtlasManager.ActiveReferenceAtlas.World2AtlasIdx(_probeController.GetTipWorldU().tipCoordWorldU -
                                                                   tipExtensionDirection * 5),
                    BrainAtlasManager.ActiveReferenceAtlas.World2Atlas_Vector(tipExtensionDirection));

                if (float.IsNaN(brainSurfaceIdxCoordU.x))
                {
                    Debug.LogWarning("Could not find brain surface! Canceling set brain offset.");
                    return;
                }

                var brainSurfaceToTransformed =
                    _probeController.Insertion.World2T(
                        BrainAtlasManager.ActiveReferenceAtlas.World2AtlasIdx(brainSurfaceIdxCoordU));

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
            var manipulatorSpaceDelta = ReferenceAtlas.World2Atlas_Vector(worldSpaceDelta);
            var manipulatorTransformDelta = AtlasTransform.U2T(manipulatorSpaceDelta);
            var manipulatorSpaceDepth = ReferenceAtlas
                .World2Atlas_Vector(Vector3.down).z * worldSpaceDelta.w;

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
                            // Move the manipulator
                            CommunicationManager.Instance.DriveToDepth(
                                ManipulatorID, targetDepth, AUTOMATIC_MOVEMENT_SPEED,
                                _ =>
                                {
                                    CommunicationManager.Instance.SetCanWrite(ManipulatorID, false, 0,
                                        onSuccessCallback, onErrorCallback);
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