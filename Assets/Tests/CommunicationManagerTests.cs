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
            string error = null;

            _communicationManager.GetManipulators(
                returnedManipulators => _manipulators = returnedManipulators,
                returnedError => error = returnedError);

            yield return new WaitWhile(() => _manipulators == null && error == null);

            if (error == null && _manipulators != null) yield break;
            TearDown();
            Assert.Fail(error);
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
                var state = -1; // -1 = no state, 0 = failed both, 1 = failed unregister, 2 = success

                _communicationManager.RegisterManipulator(id,
                    () => _communicationManager.UnregisterManipulator(id, () => state = 2, _ => state = 1),
                    _ => state = 0);

                yield return new WaitUntil(() => state != -1);
                Assert.That(state, Is.EqualTo(2));
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
                var position = Vector4.positiveInfinity; // + = no state, position = success, - = failed

                _communicationManager.RegisterManipulator(id, () => _communicationManager.BypassCalibration(id, () =>
                        _communicationManager.GetPos(id,
                            returnedPos => position = returnedPos,
                            _ => position = Vector4.negativeInfinity), _ => position = Vector4.negativeInfinity),
                    _ => position = Vector4.negativeInfinity);


                yield return new WaitUntil(() => position != Vector4.positiveInfinity);
                Assert.That(position, Is.Not.EqualTo(Vector4.negativeInfinity));
            }
        }

        #endregion
    }
}