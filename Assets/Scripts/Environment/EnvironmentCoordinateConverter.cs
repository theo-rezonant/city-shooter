using UnityEngine;

namespace CityShooter.Environment
{
    /// <summary>
    /// Handles coordinate system conversion from Blender (Z-up) to Unity (Y-up).
    /// Apply this component to the root of imported GLB assets.
    /// </summary>
    public class EnvironmentCoordinateConverter : MonoBehaviour
    {
        [Header("Conversion Settings")]
        [SerializeField] private bool applyOnStart = true;
        [SerializeField] private CoordinateSystem sourceSystem = CoordinateSystem.Blender;

        [Header("Manual Offset Override")]
        [SerializeField] private bool useManualOffset = false;
        [SerializeField] private Vector3 manualRotationOffset = new Vector3(-90f, 0f, 0f);
        [SerializeField] private Vector3 manualScaleMultiplier = Vector3.one;

        /// <summary>
        /// Supported coordinate systems for conversion.
        /// </summary>
        public enum CoordinateSystem
        {
            Blender,    // Z-up, -Y forward
            Max3DS,     // Z-up, Y forward
            Maya,       // Y-up, Z forward
            Unity       // Y-up, Z forward (no conversion needed)
        }

        private static readonly Vector3 BlenderToUnityRotation = new Vector3(-90f, 0f, 0f);
        private static readonly Vector3 MaxToUnityRotation = new Vector3(-90f, 0f, 0f);

        private Quaternion _originalRotation;
        private bool _conversionApplied;

        private void Start()
        {
            if (applyOnStart && !_conversionApplied)
            {
                ApplyCoordinateConversion();
            }
        }

        /// <summary>
        /// Applies the coordinate system conversion to this transform.
        /// </summary>
        public void ApplyCoordinateConversion()
        {
            if (_conversionApplied)
            {
                Debug.LogWarning("[EnvironmentCoordinateConverter] Conversion already applied.");
                return;
            }

            _originalRotation = transform.localRotation;

            Vector3 rotationOffset;
            Vector3 scaleMultiplier = Vector3.one;

            if (useManualOffset)
            {
                rotationOffset = manualRotationOffset;
                scaleMultiplier = manualScaleMultiplier;
            }
            else
            {
                rotationOffset = GetConversionRotation(sourceSystem);
            }

            // Apply rotation conversion
            transform.localRotation = Quaternion.Euler(rotationOffset) * transform.localRotation;

            // Apply scale if needed
            if (scaleMultiplier != Vector3.one)
            {
                transform.localScale = Vector3.Scale(transform.localScale, scaleMultiplier);
            }

            _conversionApplied = true;
            Debug.Log($"[EnvironmentCoordinateConverter] Applied {sourceSystem} to Unity conversion. Rotation offset: {rotationOffset}");
        }

        /// <summary>
        /// Reverts the coordinate conversion back to original.
        /// </summary>
        public void RevertConversion()
        {
            if (!_conversionApplied)
            {
                Debug.LogWarning("[EnvironmentCoordinateConverter] No conversion to revert.");
                return;
            }

            transform.localRotation = _originalRotation;
            _conversionApplied = false;
            Debug.Log("[EnvironmentCoordinateConverter] Conversion reverted.");
        }

        /// <summary>
        /// Gets the rotation needed to convert from the source system to Unity.
        /// </summary>
        private Vector3 GetConversionRotation(CoordinateSystem source)
        {
            switch (source)
            {
                case CoordinateSystem.Blender:
                    return BlenderToUnityRotation;
                case CoordinateSystem.Max3DS:
                    return MaxToUnityRotation;
                case CoordinateSystem.Maya:
                case CoordinateSystem.Unity:
                default:
                    return Vector3.zero;
            }
        }

        /// <summary>
        /// Converts a position from Blender space to Unity space.
        /// </summary>
        public static Vector3 BlenderPositionToUnity(Vector3 blenderPos)
        {
            // Blender: (X, Y, Z) -> Unity: (X, Z, -Y)
            return new Vector3(blenderPos.x, blenderPos.z, -blenderPos.y);
        }

        /// <summary>
        /// Converts a rotation from Blender space to Unity space.
        /// </summary>
        public static Quaternion BlenderRotationToUnity(Quaternion blenderRot)
        {
            // Apply the -90 degree X rotation
            return Quaternion.Euler(-90f, 0f, 0f) * blenderRot;
        }

        /// <summary>
        /// Gets whether the conversion has been applied.
        /// </summary>
        public bool ConversionApplied => _conversionApplied;
    }
}
