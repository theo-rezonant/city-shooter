using UnityEngine;
using CityShooter.Camera;
using CityShooter.Weapon;

namespace CityShooter.Player
{
    /// <summary>
    /// Component for setting up the FPS Player hierarchy at runtime.
    /// Ensures correct parent-child relationships for camera, weapon, and character.
    ///
    /// Expected Hierarchy:
    /// Player (CharacterController)
    ///   ├── Character Model (Soldier.fbx with Animator)
    ///   ├── Main Camera (at eye level)
    ///   │   ├── Camera Shake
    ///   │   └── Laser Gun (weapon model)
    ///   └── Ground Check (for physics detection)
    /// </summary>
    public class PlayerSetup : MonoBehaviour
    {
        [Header("Prefabs/Models")]
        [SerializeField] private GameObject characterModelPrefab;
        [SerializeField] private GameObject weaponModelPrefab;

        [Header("Camera Settings")]
        [SerializeField] private float eyeHeight = 1.6f;
        [SerializeField] private float nearClipPlane = 0.1f;
        [SerializeField] private float farClipPlane = 1000f;
        [SerializeField] private float fieldOfView = 75f;

        [Header("Character Controller Settings")]
        [SerializeField] private float controllerHeight = 1.8f;
        [SerializeField] private float controllerRadius = 0.3f;
        [SerializeField] private float stepOffset = 0.3f;
        [SerializeField] private float slopeLimit = 45f;

        [Header("Weapon Position (adjusted for Blender Z-up export)")]
        [SerializeField] private Vector3 weaponLocalPosition = new Vector3(0.2f, -0.15f, 0.4f);
        [SerializeField] private Vector3 weaponLocalRotation = new Vector3(0f, 0f, 0f);

        [Header("Ground Check")]
        [SerializeField] private float groundCheckOffset = 0.1f;

        // Runtime references
        private UnityEngine.Camera mainCamera;
        private FPSCharacterController fpsController;
        private PlayerAnimationController animationController;
        private CameraShake cameraShake;
        private WeaponHandler weaponHandler;
        private Transform groundCheckTransform;

        public UnityEngine.Camera MainCamera => mainCamera;
        public FPSCharacterController FPSController => fpsController;
        public PlayerAnimationController AnimationController => animationController;
        public CameraShake CameraShake => cameraShake;
        public WeaponHandler WeaponHandler => weaponHandler;

        private void Awake()
        {
            SetupCharacterController();
            SetupCamera();
            SetupGroundCheck();
            SetupCharacterModel();
            SetupWeapon();
            LinkComponents();
        }

        private void SetupCharacterController()
        {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = gameObject.AddComponent<CharacterController>();
            }

            cc.height = controllerHeight;
            cc.radius = controllerRadius;
            cc.center = new Vector3(0f, controllerHeight / 2f, 0f);
            cc.stepOffset = stepOffset;
            cc.slopeLimit = slopeLimit;

            // Add FPS Controller if not present
            fpsController = GetComponent<FPSCharacterController>();
            if (fpsController == null)
            {
                fpsController = gameObject.AddComponent<FPSCharacterController>();
            }
        }

        private void SetupCamera()
        {
            // Find or create Main Camera
            Transform existingCameraTransform = transform.Find("Main Camera");
            GameObject cameraObj;

            if (existingCameraTransform != null)
            {
                cameraObj = existingCameraTransform.gameObject;
            }
            else
            {
                cameraObj = new GameObject("Main Camera");
                cameraObj.transform.SetParent(transform);
            }

            // Position at eye level
            cameraObj.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
            cameraObj.transform.localRotation = Quaternion.identity;

            // Setup camera component
            mainCamera = cameraObj.GetComponent<UnityEngine.Camera>();
            if (mainCamera == null)
            {
                mainCamera = cameraObj.AddComponent<UnityEngine.Camera>();
            }

            mainCamera.nearClipPlane = nearClipPlane;
            mainCamera.farClipPlane = farClipPlane;
            mainCamera.fieldOfView = fieldOfView;
            cameraObj.tag = "MainCamera";

            // Add AudioListener if not present
            if (cameraObj.GetComponent<AudioListener>() == null)
            {
                cameraObj.AddComponent<AudioListener>();
            }

            // Add Camera Shake
            cameraShake = cameraObj.GetComponent<CameraShake>();
            if (cameraShake == null)
            {
                cameraShake = cameraObj.AddComponent<CameraShake>();
            }
        }

        private void SetupGroundCheck()
        {
            Transform existingGroundCheck = transform.Find("GroundCheck");

            if (existingGroundCheck != null)
            {
                groundCheckTransform = existingGroundCheck;
            }
            else
            {
                GameObject groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(transform);
                groundCheckTransform = groundCheckObj.transform;
            }

            groundCheckTransform.localPosition = new Vector3(0f, groundCheckOffset, 0f);
        }

        private void SetupCharacterModel()
        {
            if (characterModelPrefab != null)
            {
                // Check if character model already exists
                Transform existingModel = transform.Find("CharacterModel");
                if (existingModel == null)
                {
                    GameObject modelInstance = Instantiate(characterModelPrefab, transform);
                    modelInstance.name = "CharacterModel";
                    modelInstance.transform.localPosition = Vector3.zero;
                    modelInstance.transform.localRotation = Quaternion.identity;
                }
            }

            // Get or add animation controller
            Transform characterModel = transform.Find("CharacterModel");
            if (characterModel != null)
            {
                animationController = characterModel.GetComponent<PlayerAnimationController>();
                if (animationController == null)
                {
                    Animator animator = characterModel.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animationController = characterModel.gameObject.AddComponent<PlayerAnimationController>();
                    }
                }
            }
            else
            {
                // Check if there's an Animator on this object or children
                Animator animator = GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animationController = animator.GetComponent<PlayerAnimationController>();
                    if (animationController == null)
                    {
                        animationController = animator.gameObject.AddComponent<PlayerAnimationController>();
                    }
                }
            }
        }

        private void SetupWeapon()
        {
            if (mainCamera == null) return;

            Transform existingWeapon = mainCamera.transform.Find("Weapon");
            GameObject weaponObj;

            if (existingWeapon != null)
            {
                weaponObj = existingWeapon.gameObject;
            }
            else if (weaponModelPrefab != null)
            {
                weaponObj = Instantiate(weaponModelPrefab, mainCamera.transform);
                weaponObj.name = "Weapon";
            }
            else
            {
                // Create placeholder
                weaponObj = new GameObject("Weapon");
                weaponObj.transform.SetParent(mainCamera.transform);
            }

            // Apply position and rotation (compensating for Blender Z-up)
            weaponObj.transform.localPosition = weaponLocalPosition;
            weaponObj.transform.localRotation = Quaternion.Euler(weaponLocalRotation);

            // Add weapon handler
            weaponHandler = weaponObj.GetComponent<WeaponHandler>();
            if (weaponHandler == null)
            {
                weaponHandler = weaponObj.AddComponent<WeaponHandler>();
            }
        }

        private void LinkComponents()
        {
            // Link weapon handler to animation controller and camera shake
            if (weaponHandler != null)
            {
                // These will be set via the inspector or at runtime
                // weaponHandler has its own references
            }
        }

        /// <summary>
        /// Teleport the player to a specific position.
        /// </summary>
        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                // Disable controller temporarily to allow teleport
                cc.enabled = false;
                transform.position = position;
                transform.rotation = rotation;
                cc.enabled = true;
            }
            else
            {
                transform.position = position;
                transform.rotation = rotation;
            }
        }

        /// <summary>
        /// Reset the player to their spawn position.
        /// </summary>
        public void ResetPlayer()
        {
            // Reset position
            TeleportTo(Vector3.zero, Quaternion.identity);

            // Reset any state
            if (fpsController != null)
            {
                fpsController.LockCursor(true);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Validate Setup")]
        private void ValidateSetup()
        {
            Debug.Log("=== Player Setup Validation ===");
            Debug.Log($"CharacterController: {(GetComponent<CharacterController>() != null ? "OK" : "MISSING")}");
            Debug.Log($"FPSCharacterController: {(fpsController != null ? "OK" : "MISSING")}");
            Debug.Log($"Main Camera: {(mainCamera != null ? "OK" : "MISSING")}");
            Debug.Log($"Camera Shake: {(cameraShake != null ? "OK" : "MISSING")}");
            Debug.Log($"Animation Controller: {(animationController != null ? "OK" : "MISSING")}");
            Debug.Log($"Weapon Handler: {(weaponHandler != null ? "OK" : "MISSING")}");
            Debug.Log($"Ground Check: {(groundCheckTransform != null ? "OK" : "MISSING")}");
        }
#endif
    }
}
