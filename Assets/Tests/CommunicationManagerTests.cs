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
        private CommunicationManager _communicationManager;
        private int[] _manipulators;

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

        [TearDown]
        public void TearDown()
        {
            _communicationManager.DisconnectFromServer();
        }

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
    }
}