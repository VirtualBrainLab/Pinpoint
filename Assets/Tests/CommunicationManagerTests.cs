using System.Collections;
using NUnit.Framework;
using SensapexLink;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests
{
    public class CommunicationManagerTests
    {
        #region Variables

        private CommunicationManager _communicationManager;
        private int[] _manipulators;

        private enum State
        {
            None,
            Success,
            Failed,
            FailedLevel2,
            FailedLevel3,
            FailedLevel4,
            FailedLevel5
        }

        #endregion


        #region Setup and Teardown

        /// <summary>
        /// Setup each test.
        /// Ensures a connection to the server and gets the manipulators.
        /// </summary>
        /// <returns></returns>
        [UnitySetUp]
        public IEnumerator Setup()
        {
            SceneManager.LoadScene("Scenes/TrajectoryPlanner");
            yield return null;

            // Connect to server
            _communicationManager = GameObject.Find("SensapexLink").GetComponent<CommunicationManager>();
            yield return new WaitUntil(_communicationManager.IsConnected);

            // Get manipulators
            var state = State.None;

            _communicationManager.GetManipulators(
                returnedManipulators =>
                {
                    _manipulators = returnedManipulators;
                    state = State.Success;
                },
                returnedError => state = State.Failed);

            yield return new WaitWhile(() => state == State.None);

            if (state == State.Success) yield break;
            TearDown();
            Assert.Fail("Could not get manipulators");
        }

        /// <summary>
        /// Disconnect from server after each test
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            _communicationManager.DisconnectFromServer();
        }

        #endregion

        #region Tests

        /// <summary>
        /// Register and then unregister each manipulator
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestRegisterAndUnregister()
        {
            foreach (var id in _manipulators)
            {
                var state = State.None;

                _communicationManager.RegisterManipulator(id,
                    () => _communicationManager.UnregisterManipulator(id, () => state = State.Success,
                        _ => state = State.FailedLevel2),
                    _ => state = State.Failed);

                yield return new WaitWhile(() => state == State.None);
                Assert.That(state, Is.EqualTo(State.Success));
            }
        }

        /// <summary>
        /// Register -> Bypass calibration -> Get position
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestGetPos()
        {
            foreach (var id in _manipulators)
            {
                var state = State.None;

                _communicationManager.RegisterManipulator(id, () => _communicationManager.BypassCalibration(id, () =>
                        _communicationManager.GetPos(id,
                            _ => state = State.Success,
                            _ => state = State.FailedLevel3), _ => state = State.FailedLevel2),
                    _ => state = State.Failed);


                yield return new WaitWhile(() => state == State.None);
                Assert.That(state, Is.EqualTo(State.Success));
            }
        }

        [UnityTest]
        public IEnumerator TestCalibrateAndMovement()
        {
            foreach (var id in _manipulators)
            {
                var state = State.None;

                _communicationManager.RegisterManipulator(id, () => _communicationManager.SetCanWrite(id, true, 1, _ =>
                        _communicationManager.Calibrate(id, () =>
                            _communicationManager.GotoPos(id, new Vector4(0, 0, 0, 0), 5000,
                                _ => _communicationManager.GotoPos(id, new Vector4(10000, 10000, 10000, 10000), 5000,
                                    _ => state = State.Success, _ => state = State.FailedLevel5),
                                _ => state = State.FailedLevel4), _ => state = State.FailedLevel3),
                    _ => state = State.FailedLevel2), _ => state = State.Failed);

                yield return new WaitWhile(() => state == State.None);
                Assert.That(state, Is.EqualTo(State.Success));
            }
        }

        #endregion
    }
}