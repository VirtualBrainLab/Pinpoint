using System;
using System.Globalization;
using System.Linq;
using BrainAtlas;
using BrainAtlas.CoordinateSystems;
using EphysLink;
using Pinpoint.CoordinateSystems;
using UnityEngine;
using UnityEngine.Events;

namespace Pinpoint.Probes
{
    public class ManipulatorBehaviorController : MonoBehaviour
    {
        #region Constants

        // Default movement speed: 0.5 mm/s
        public const float AUTOMATIC_MOVEMENT_SPEED = 0.5f;

        #endregion

        #region Components

        [SerializeField] private ProbeManager _probeManager;
        [SerializeField] private ProbeController _probeController;

        #endregion

        #region Properties

        public string ManipulatorID { get; private set; }

        public int NumAxes { get; set; }

        public Vector3 Dimensions { get; private set; }

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
        private CoordinateTransform CoordinateTransform { get; set; }

        public bool IsRightHanded
        {
            get => _isRightHanded;
            set
            {
                _isRightHanded = value;
                UpdateSpaceAndTransform();
            }
        }

        // Helper functions to create and destroy a probe
        public Action<ProbeProperties.ProbeType> CreatePathfinderProbe { private get; set; }
        public Action DestroyThisProbe { private get; set; }

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

        #region Unity

        /// <summary>
        ///     Setup this instance
        /// </summary>
        private void Awake()
        {
            // Start off as disabled
            enabled = false;

            // Update manipulator inside brain state
            // _probeController.MovedThisFrameEvent.AddListener(() =>
            // {
            //     if (_isSetToInsideBrain != _probeManager.IsProbeInBrain())
            //         CommunicationManager.Instance.SetInsideBrain(ManipulatorID, _probeManager.IsProbeInBrain(),
            //             insideBrain =>
            //             {
            //                 _isSetToInsideBrain = insideBrain;
            //                 _probeController.UnlockedDir = insideBrain ? new Vector4(0, 0, 0, 1) : Vector4.one;
            //             });
            // });
        }

        private void OnDisable()
        {
            ManipulatorID = null;
            _zeroCoordinateOffset = Vector4.zero;
            _brainSurfaceOffset = 0;
        }

        #endregion


        #region Public Methods

        public void Initialize(string manipulatorID, bool calibrated)
        {
            CommunicationManager.Instance.GetManipulators((ids, numAxes, dimensions) =>
            {
                // Shortcut exit if we have an invalid manipulator ID
                if (!ids.Contains(manipulatorID)) return;

                // Set manipulator ID, number of axes, and dimensions
                ManipulatorID = manipulatorID;
                NumAxes = numAxes;
                Dimensions = new Vector3(dimensions[0], dimensions[1], dimensions[2]);

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

        private void UpdateSpaceAndTransform()
        {
            CoordinateSpace = NumAxes switch
            {
                -1 => new PathfinderSpace(),
                _ => new ManipulatorSpace(Dimensions)
            };
            CoordinateTransform = NumAxes switch
            {
                4 => IsRightHanded
                    ? new FourAxisRightHandedManipulatorTransform(_probeController.Insertion.Yaw)
                    : new FourAxisLeftHandedManipulatorTransform(_probeController.Insertion.Yaw),
                3 => new ThreeAxisLeftHandedTransform(_probeController.Insertion.Yaw, _probeController.Insertion.Pitch),
                _ => CoordinateTransform
            };
        }

        public Vector4 ConvertInsertionAPMLDVToManipulatorPosition(Vector3 insertionAPMLDV)
        {
            // Convert apmldv to world coordinate
            var convertToWorld = _probeManager.ProbeController.Insertion.T2World_Vector(insertionAPMLDV);

            // Convert to Manipulator space
            var posInManipulatorSpace = CoordinateSpace.World2Space(convertToWorld);
            Vector4 posInManipulatorTransform = CoordinateTransform.U2T(posInManipulatorSpace);

            // Apply brain surface offset
            var brainSurfaceAdjustment = float.IsNaN(BrainSurfaceOffset)
                ? 0
                : BrainSurfaceOffset;
            if (_probeManager.ManipulatorBehaviorController.IsSetToDropToSurfaceWithDepth)
                posInManipulatorTransform.w -= brainSurfaceAdjustment;
            else
                posInManipulatorTransform.z += brainSurfaceAdjustment;

            // Apply coordinate offsets and return result
            return posInManipulatorTransform + ZeroCoordinateOffset;
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
                var (brainSurfaceCoordinateIdx, _) =
                    _probeManager.CalculateEntryCoordinate(!IsSetToDropToSurfaceWithDepth);

                if (float.IsNaN(brainSurfaceCoordinateIdx.x))
                {
                    Debug.LogError("Could not find brain surface! Canceling set brain offset.");
                    return;
                }

                var brainSurfaceToTransformed =
                    _probeController.Insertion.World2T(
                        BrainAtlasManager.ActiveReferenceAtlas.AtlasIdx2World(brainSurfaceCoordinateIdx));

                BrainSurfaceOffset += Vector3.Distance(brainSurfaceToTransformed,
                    _probeController.Insertion.APMLDV);
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
            var manipulatorSpaceDelta = CoordinateSpace.World2Space_Vector(worldSpaceDelta);
            var manipulatorTransformDelta = CoordinateTransform.U2T(manipulatorSpaceDelta);
            var manipulatorSpaceDepth = worldSpaceDelta.w;

            print("World space delta: " + worldSpaceDelta + "; Manipulator space delta: " + manipulatorSpaceDelta +
                  "; Manipulator transform delta: " + manipulatorTransformDelta + "; Manipulator space depth: " +
                  manipulatorSpaceDepth);

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

        #region Private Methods

        private void EchoPosition(Vector4 pos)
        {
            if (!enabled && _probeController == null) return;

            // Check for special Pathfinder mode (directly set probe position, no calculations needed)
            if (NumAxes == -1)
            {
                // Check if probe type changed
                CommunicationManager.Instance.GetShankCount(ManipulatorID, shankCount =>
                {
                    // Use 2.4 if 4 shank, otherwise default to 1
                    var probeType = shankCount == 4
                        ? ProbeProperties.ProbeType.Neuropixels24
                        : ProbeProperties.ProbeType.Neuropixels1;

                    // print("Read type: " + probeType + "; Current type: " + _probeManager.ProbeType);
                    // Check if change is needed
                    if (probeType != _probeManager.ProbeType)
                    {
                        // Unregister manipulator
                        _probeManager.SetIsEphysLinkControlled(false, ManipulatorID, true, () =>
                        {

                            // Create new probe
                            CreatePathfinderProbe.Invoke(probeType);

                            // Destroy current probe
                            DestroyThisProbe.Invoke();
                        }, Debug.LogError);

                        // Exit early as this probe no longer exists
                        return;
                    }

                    // Otherwise, update probe angles
                    CommunicationManager.Instance.GetAngles(ManipulatorID, angles =>
                    {
                        _probeController.SetProbeAngles(new Vector3(angles.x, 90 - angles.y, angles.z));

                        // If only the DV axis moved, then we drop on DV. Otherwise, we drop on depth.
                        if (Math.Abs(pos.z - _lastManipulatorPosition.z) > 0.0001)
                            IsSetToDropToSurfaceWithDepth = Math.Abs(pos.x - _lastManipulatorPosition.x) > 0.0001 ||
                                                            Math.Abs(pos.y - _lastManipulatorPosition.y) > 0.0001;

                        // Copy in new 3-axis position into saved 4-axis position
                        _lastManipulatorPosition = new Vector4(pos.x, pos.y, pos.z, _lastManipulatorPosition.w);

                        // Apply brain surface offset on correct axis
                        var brainSurfaceAdjustment = float.IsNaN(BrainSurfaceOffset) ? 0 : BrainSurfaceOffset;
                        if (IsSetToDropToSurfaceWithDepth)
                            _lastManipulatorPosition.w -= brainSurfaceAdjustment;
                        else
                            _lastManipulatorPosition.z -= brainSurfaceAdjustment;

                        // Convert Pathfinder space coordinates into active atlas space
                        var convertedPos =
                            _probeController.Insertion.World2T_Vector(
                                CoordinateSpace.Space2World_Vector(_lastManipulatorPosition));

                        // Copy and add 4th axis back in
                        _probeController.SetProbePosition(new Vector4(convertedPos.x, convertedPos.y,
                            convertedPos.z,
                            _lastManipulatorPosition.w));
                    }, Debug.LogError);
                }, Debug.LogError);
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
                var manipulatorSpacePosition = CoordinateTransform.T2U(zeroCoordinateAdjustedManipulatorPosition);

                // Brain surface adjustment
                var brainSurfaceAdjustment = float.IsNaN(BrainSurfaceOffset) ? 0 : BrainSurfaceOffset;
                if (IsSetToDropToSurfaceWithDepth)
                    zeroCoordinateAdjustedManipulatorPosition.w += brainSurfaceAdjustment;
                else
                    manipulatorSpacePosition.y -= brainSurfaceAdjustment;

                // Convert to world space
                var zeroCoordinateAdjustedWorldPosition =
                    CoordinateSpace.Space2World(manipulatorSpacePosition);

                // Set probe position (change axes to match probe)
                var transformedApmldv =
                    _probeController.Insertion.World2T_Vector(zeroCoordinateAdjustedWorldPosition);

                // Split between 3 and 4 axis assignments
                if (CoordinateTransform.Prefix == "3lhm")
                    _probeController.SetProbePosition(transformedApmldv);
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
    }
}