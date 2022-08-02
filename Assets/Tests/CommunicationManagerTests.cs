using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests
{
    public class CommunicationManagerTests
    {
        // A Test behaves as an ordinary method
        [Test]
        public void CommunicationManagerTestsSimplePasses()
        {
            const string input = "Hello";
            Assert.That(input, Is.EqualTo("Hello"));
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator CommunicationManagerTestsWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}
