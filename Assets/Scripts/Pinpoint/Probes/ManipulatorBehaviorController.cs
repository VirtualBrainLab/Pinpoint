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

        [SerializeField]
        private ProbeManager _probeManager;

        [SerializeField]
        private ProbeController _probeController;

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
                _zeroCoordinateOffset = new Vector4(
                    float.IsNaN(value.x) ? _zeroCoordinateOffset.x : value.x,
                    float.IsNaN(value.y) ? _zeroCoordinateOffset.y : value.y,
                    float.IsNaN(value.z) ? _zeroCoordinateOffset.z : value.z,
                    float.IsNaN(value.w) ? _zeroCoordinateOffset.w : value.w
                );

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
                if (BrainSurfaceOffset != 0)
                    return;
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

        #region Automation State

        public bool HasResetBregma;

        public Vector4 SelectedTargetInsertion;

        public bool HasResetDura;

        public float BaseDriveSpeed;
        
        public float DrivePastDistance;

        #endregion

        #region Private internal fields

        private Vector4 _lastManipulatorPosition = Vector4.zero;
        private Vector4 _lastLoggedManipulatorPosition = Vector4.zero;
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
            CommunicationManager.Instance.GetManipulators(response =>
            {
                // Shortcut exit if we have an invalid manipulator ID
                if (!response.Manipulators.Contains(manipulatorID))
                    return;

                // Set manipulator ID, number of axes, and dimensions
                ManipulatorID = manipulatorID;
                NumAxes = response.NumAxes;
                Dimensions = response.Dimensions;

                // Update transform and space
                UpdateSpaceAndTransform();

                // Lock the manipulator from manual control
                _probeController.SetControllerLock(true);

                StartEchoing();
                return;

                void StartEchoing()
                {
                    CommunicationManager.Instance.GetPosition(
                        manipulatorID,
                        pos =>
                        {
                            if (ZeroCoordinateOffset.Equals(Vector4.zero))
                                ZeroCoordinateOffset = pos;
                            EchoPosition(pos);
                        }
                    );
                }
            });
        }

        private void UpdateSpaceAndTransform()
        {
            CoordinateSpace = new ManipulatorSpace(Dimensions);
            CoordinateTransform = NumAxes switch
            {
                4
                    => IsRightHanded
                        ? new FourAxisRightHandedManipulatorTransform(
                            _probeController.Insertion.Yaw
                        )
                        : new FourAxisLeftHandedManipulatorTransform(
                            _probeController.Insertion.Yaw
                        ),
                3
                    => new ThreeAxisLeftHandedTransform(
                        _probeController.Insertion.Yaw,
                        _probeController.Insertion.Pitch
                    ),
                _ => CoordinateTransform
            };
        }

        public Vector4 ConvertInsertionAPMLDVToManipulatorPosition(Vector3 insertionAPMLDV)
        {
            // Convert apmldv to world coordinate
            var convertToWorld = _probeManager.ProbeController.Insertion.T2World_Vector(
                insertionAPMLDV
            );

            // Convert to Manipulator space
            var posInManipulatorSpace = CoordinateSpace.World2Space(convertToWorld);
            Vector4 posInManipulatorTransform = CoordinateTransform.U2T(posInManipulatorSpace);

            // Apply brain surface offset
            var brainSurfaceAdjustment = float.IsNaN(BrainSurfaceOffset) ? 0 : BrainSurfaceOffset;
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
                var (brainSurfaceCoordinateIdx, _) = _probeManager.CalculateEntryCoordinate(
                    !IsSetToDropToSurfaceWithDepth
                );

                if (float.IsNaN(brainSurfaceCoordinateIdx.x))
                {
                    Debug.LogError("Could not find brain surface! Canceling set brain offset.");
                    return;
                }

                var brainSurfaceToTransformed = _probeController.Insertion.World2T(
                    BrainAtlasManager.ActiveReferenceAtlas.AtlasIdx2World(brainSurfaceCoordinateIdx)
                );

                BrainSurfaceOffset += Vector3.Distance(
                    brainSurfaceToTransformed,
                    _probeController.Insertion.APMLDV
                );
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
        public void MoveByWorldSpaceDelta(
            Vector4 worldSpaceDelta,
            Action<Vector4> onSuccessCallback,
            Action<string> onErrorCallback = null
        )
        {
            // Convert to manipulator axes (world -> space -> transform)
            var manipulatorSpaceDelta = CoordinateSpace.World2Space_Vector(worldSpaceDelta);
            var manipulatorTransformDelta = CoordinateTransform.U2T(manipulatorSpaceDelta);
            var manipulatorSpaceDepth = worldSpaceDelta.w;

            print(
                "World space delta: "
                    + worldSpaceDelta
                    + "; Manipulator space delta: "
                    + manipulatorSpaceDelta
                    + "; Manipulator transform delta: "
                    + manipulatorTransformDelta
                    + "; Manipulator space depth: "
                    + manipulatorSpaceDepth
            );

            // Get manipulator position
            CommunicationManager.Instance.GetPosition(
                ManipulatorID,
                pos =>
                {
                    // Apply delta
                    var targetPosition =
                        pos
                        + new Vector4(
                            manipulatorTransformDelta.x,
                            manipulatorTransformDelta.y,
                            manipulatorTransformDelta.z
                        );
                    // Move manipulator
                    CommunicationManager.Instance.SetPosition(
                        new SetPositionRequest(
                            ManipulatorID,
                            targetPosition,
                            AUTOMATIC_MOVEMENT_SPEED
                        ),
                        newPos =>
                        {
                            print("New pos: " + newPos + "; Setting depth...");
                            // Process depth movement
                            var targetDepth = newPos.w + manipulatorSpaceDepth;
                            // Move the manipulator
                            CommunicationManager.Instance.SetDepth(
                                new SetDepthRequest(
                                    ManipulatorID,
                                    targetDepth,
                                    AUTOMATIC_MOVEMENT_SPEED
                                ),
                                _ =>
                                    CommunicationManager.Instance.GetPosition(
                                        ManipulatorID,
                                        onSuccessCallback,
                                        onErrorCallback
                                    ),
                                onErrorCallback
                            );
                        },
                        onErrorCallback
                    );
                }
            );
        }

        /// <summary>
        ///     Drive the manipulator back to the zero coordinate position
        /// </summary>
        /// <param name="onSuccessCallback">Action on success</param>
        /// <param name="onErrorCallBack">Action on failure</param>
        public void MoveBackToZeroCoordinate(
            Action<Vector4> onSuccessCallback,
            Action<string> onErrorCallBack
        )
        {
            // Send move command
            CommunicationManager.Instance.SetPosition(
                new SetPositionRequest(
                    ManipulatorID,
                    ZeroCoordinateOffset,
                    AUTOMATIC_MOVEMENT_SPEED
                ),
                onSuccessCallback,
                onErrorCallBack
            );
        }

        #endregion

        #region Private Methods

        private void EchoPosition(Vector4 pos)
        {
            // Exit if disabled and there is no probe controller.
            if (!enabled && _probeController == null)
                return;

            // Calculate last used direction for dropping to brain surface (between depth and DV)
            var dvDelta = Math.Abs(pos.z - _lastManipulatorPosition.z);
            var depthDelta = Math.Abs(pos.w - _lastManipulatorPosition.w);
            if (dvDelta > 0.0001 || depthDelta > 0.0001)
                IsSetToDropToSurfaceWithDepth = depthDelta > dvDelta;
            _lastManipulatorPosition = pos;

            // Apply zero coordinate offset.
            var zeroCoordinateAdjustedManipulatorPosition = pos - ZeroCoordinateOffset;

            // Convert to coordinate space.
            var manipulatorSpacePosition = CoordinateTransform.T2U(
                zeroCoordinateAdjustedManipulatorPosition
            );

            // Brain surface adjustment.
            var brainSurfaceAdjustment = float.IsNaN(BrainSurfaceOffset) ? 0 : BrainSurfaceOffset;
            if (IsSetToDropToSurfaceWithDepth)
            {
                // Apply depth adjustment to manipulator position for non-3 axis manipulators.
                if (CoordinateTransform.Prefix != "3lhm")
                    zeroCoordinateAdjustedManipulatorPosition.w += brainSurfaceAdjustment;
            }
            else
            {
                manipulatorSpacePosition.y -= brainSurfaceAdjustment;
            }

            // Convert to world space.
            var zeroCoordinateAdjustedWorldPosition = CoordinateSpace.Space2World(
                manipulatorSpacePosition
            );

            // Set probe position (change axes to match probe).
            var transformedApmldv = BrainAtlasManager.World2T_Vector(
                zeroCoordinateAdjustedWorldPosition
            );

            // Set probe position.
            // For 3-axis manipulators, use depth to adjust brain offset if applying offset on depth.
            if (CoordinateTransform.Prefix == "3lhm")
            {
                if (IsSetToDropToSurfaceWithDepth)
                    _probeController.SetProbePosition(
                        new Vector4(
                            transformedApmldv.x,
                            transformedApmldv.y,
                            transformedApmldv.z,
                            brainSurfaceAdjustment
                        )
                    );
                else
                    _probeController.SetProbePosition(transformedApmldv);
            }
            else
            {
                _probeController.SetProbePosition(
                    new Vector4(
                        transformedApmldv.x,
                        transformedApmldv.y,
                        transformedApmldv.z,
                        zeroCoordinateAdjustedManipulatorPosition.w
                    )
                );
            }

            // Log and continue echoing
            LogAndContinue();
            return;

            void LogAndContinue()
            {
                // Don't log if the last position is the same.
                var positionDifference = _lastLoggedManipulatorPosition - pos;
                if (
                    Mathf.Abs(positionDifference.x) > 0.0001
                    || Mathf.Abs(positionDifference.y) > 0.0001
                    || Mathf.Abs(positionDifference.z) > 0.0001
                    || Mathf.Abs(positionDifference.w) > 0.0001
                )
                    // Log every 4 hz
                    if (Time.time - _lastLoggedTime >= 0.25)
                    {
                        _lastLoggedTime = Time.time;
                        var tipPos = _probeController.ProbeTipT.position;

                        // ["ephys_link", Real time stamp, Manipulator ID, X, Y, Z, W, Phi, Theta, Spin, TipX, TipY, TipZ]
                        OutputLog.Log(
                            new[]
                            {
                                "ephys_link",
                                DateTime.Now.ToString(CultureInfo.InvariantCulture),
                                ManipulatorID,
                                pos.x.ToString(CultureInfo.InvariantCulture),
                                pos.y.ToString(CultureInfo.InvariantCulture),
                                pos.z.ToString(CultureInfo.InvariantCulture),
                                pos.w.ToString(CultureInfo.InvariantCulture),
                                _probeController.Insertion.Yaw.ToString(
                                    CultureInfo.InvariantCulture
                                ),
                                _probeController.Insertion.Pitch.ToString(
                                    CultureInfo.InvariantCulture
                                ),
                                _probeController.Insertion.Roll.ToString(
                                    CultureInfo.InvariantCulture
                                ),
                                tipPos.x.ToString(CultureInfo.InvariantCulture),
                                tipPos.y.ToString(CultureInfo.InvariantCulture),
                                tipPos.z.ToString(CultureInfo.InvariantCulture)
                            }
                        );

                        // Update last logged position
                        _lastLoggedManipulatorPosition = pos;
                    }

                // Continue echoing position
                CommunicationManager.Instance.GetPosition(ManipulatorID, EchoPosition);
            }
        }

        #endregion
    }
}
