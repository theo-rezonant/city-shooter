#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using CityShooter.Environment;

namespace CityShooter.Tests.Editor
{
    /// <summary>
    /// Unit tests for EnvironmentCoordinateConverter component.
    /// </summary>
    [TestFixture]
    public class EnvironmentCoordinateConverterTests
    {
        private GameObject _testObject;
        private EnvironmentCoordinateConverter _converter;

        [SetUp]
        public void Setup()
        {
            _testObject = new GameObject("TestConverter");
            _converter = _testObject.AddComponent<EnvironmentCoordinateConverter>();
        }

        [TearDown]
        public void Teardown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }

        [Test]
        public void CoordinateConverter_InitializesCorrectly()
        {
            Assert.IsNotNull(_converter);
            Assert.IsFalse(_converter.ConversionApplied);
        }

        [Test]
        public void CoordinateConverter_ConversionNotAppliedByDefault()
        {
            Assert.IsFalse(_converter.ConversionApplied);
        }

        [Test]
        public void CoordinateConverter_ApplyConversion_SetsFlag()
        {
            _converter.ApplyCoordinateConversion();
            Assert.IsTrue(_converter.ConversionApplied);
        }

        [Test]
        public void CoordinateConverter_DoubleApply_LogsWarning()
        {
            _converter.ApplyCoordinateConversion();
            Assert.IsTrue(_converter.ConversionApplied);

            // Second apply should not throw
            Assert.DoesNotThrow(() => _converter.ApplyCoordinateConversion());
        }

        [Test]
        public void CoordinateConverter_RevertConversion_ClearsFlag()
        {
            _converter.ApplyCoordinateConversion();
            Assert.IsTrue(_converter.ConversionApplied);

            _converter.RevertConversion();
            Assert.IsFalse(_converter.ConversionApplied);
        }

        [Test]
        public void CoordinateConverter_RevertWhenNotApplied_LogsWarning()
        {
            // Should not throw when reverting without prior conversion
            Assert.DoesNotThrow(() => _converter.RevertConversion());
        }

        [Test]
        public void CoordinateConverter_BlenderPositionToUnity_CorrectConversion()
        {
            Vector3 blenderPos = new Vector3(1f, 2f, 3f);
            Vector3 unityPos = EnvironmentCoordinateConverter.BlenderPositionToUnity(blenderPos);

            // Blender (X, Y, Z) -> Unity (X, Z, -Y)
            Assert.AreEqual(1f, unityPos.x, 0.001f);
            Assert.AreEqual(3f, unityPos.y, 0.001f);
            Assert.AreEqual(-2f, unityPos.z, 0.001f);
        }

        [Test]
        public void CoordinateConverter_BlenderPositionToUnity_OriginUnchanged()
        {
            Vector3 blenderPos = Vector3.zero;
            Vector3 unityPos = EnvironmentCoordinateConverter.BlenderPositionToUnity(blenderPos);

            Assert.AreEqual(Vector3.zero, unityPos);
        }

        [Test]
        public void CoordinateConverter_BlenderRotationToUnity_AppliesOffset()
        {
            Quaternion blenderRot = Quaternion.identity;
            Quaternion unityRot = EnvironmentCoordinateConverter.BlenderRotationToUnity(blenderRot);

            // The result should have a -90 degree X rotation applied
            Vector3 euler = unityRot.eulerAngles;

            // Unity represents -90 as 270
            Assert.AreEqual(270f, euler.x, 0.1f);
        }
    }
}
#endif
