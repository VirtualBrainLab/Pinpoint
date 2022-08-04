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

        [UnitySetUp]
        public IEnumerator Setup()
        {
            SceneManager.LoadScene("Scenes/TrajectoryPlanner");
            yield return null;
            _communicationManager = GameObject.Find("SensapexLink").GetComponent<CommunicationManager>();
            yield return new WaitUntil(_communicationManager.IsConnected);
        }

        [TearDown]
        public void TearDown()
        {
            _communicationManager.DisconnectFromServer();
        }

        /// <summary>
        /// Check if the communication manager is instantiated
        /// </summary>
        [Test]
        public void TestHasAccessToManager()
        {
            Assert.That(_communicationManager, Is.Not.Null);
            Assert.True(_communicationManager.IsConnected());
        }

        /// <summary>
        /// Test that a success response is received from the server when getting manipulators
        /// </summary>
        [UnityTest]
        public IEnumerator TestGetManipulators()
        {
            int[] manipulators = null;
            string error = null;

            _communicationManager.GetManipulators(
                returnedManipulators => manipulators = returnedManipulators,
                returnedError => error = returnedError);

            yield return new WaitWhile(() => manipulators == null && error == null);

            Assert.NotNull(manipulators);
            Assert.Null(error);
        }

        [Test]
        public void TestRegisterAndUnRegister()
        {
        }
    }
}