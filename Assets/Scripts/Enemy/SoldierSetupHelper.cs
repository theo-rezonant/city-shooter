using UnityEngine;
using UnityEngine.AI;

namespace CityShooter.Enemy
{
    /// <summary>
    /// Helper component for setting up Soldier prefabs.
    /// Automatically configures required components and settings.
    /// Attach this to the Soldier.fbx root object when creating the prefab.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class SoldierSetupHelper : MonoBehaviour
    {
        [Header("Auto-Setup on Start")]
        [SerializeField] private bool autoSetup = true;

        [Header("Collider Settings")]
        [SerializeField] private float colliderHeight = 1.8f;
        [SerializeField] private float colliderRadius = 0.3f;
        [SerializeField] private Vector3 colliderCenter = new Vector3(0, 0.9f, 0);

        [Header("NavMesh Settings")]
        [SerializeField] private float agentSpeed = 3.5f;
        [SerializeField] private float agentAngularSpeed = 120f;
        [SerializeField] private float agentAcceleration = 8f;
        [SerializeField] private float agentStoppingDistance = 1.5f;
        [SerializeField] private float agentRadius = 0.5f;
        [SerializeField] private float agentHeight = 2f;

        [Header("Physics Layer")]
        [SerializeField] private string enemyLayerName = "Enemy";

        private void Start()
        {
            if (autoSetup)
            {
                SetupSoldier();
            }
        }

        /// <summary>
        /// Configures all components for the Soldier.
        /// Can be called from editor scripts or at runtime.
        /// </summary>
        public void SetupSoldier()
        {
            SetupCollider();
            SetupNavMeshAgent();
            SetupPhysicsLayer();
            EnsureRequiredComponents();

            Debug.Log($"Soldier setup complete for {gameObject.name}");
        }

        private void SetupCollider()
        {
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.height = colliderHeight;
                capsule.radius = colliderRadius;
                capsule.center = colliderCenter;
            }
        }

        private void SetupNavMeshAgent()
        {
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = agentSpeed;
                agent.angularSpeed = agentAngularSpeed;
                agent.acceleration = agentAcceleration;
                agent.stoppingDistance = agentStoppingDistance;
                agent.radius = agentRadius;
                agent.height = agentHeight;
                agent.autoTraverseOffMeshLink = true;
            }
        }

        private void SetupPhysicsLayer()
        {
            int enemyLayer = LayerMask.NameToLayer(enemyLayerName);

            if (enemyLayer == -1)
            {
                Debug.LogWarning($"SoldierSetupHelper: Layer '{enemyLayerName}' not found. " +
                    "Please create an 'Enemy' layer in Project Settings > Tags and Layers.");
                return;
            }

            // Set layer for this object and all children
            SetLayerRecursively(gameObject, enemyLayer);
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private void EnsureRequiredComponents()
        {
            // Ensure SoldierAI is present
            if (GetComponent<SoldierAI>() == null)
            {
                gameObject.AddComponent<SoldierAI>();
                Debug.Log("Added SoldierAI component");
            }

            // Ensure EnemyHealth is present
            if (GetComponent<EnemyHealth>() == null)
            {
                gameObject.AddComponent<EnemyHealth>();
                Debug.Log("Added EnemyHealth component");
            }

            // Ensure Rigidbody is present for physics interactions (optional)
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            // Configure rigidbody for NavMesh AI
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        #region Editor Utilities

#if UNITY_EDITOR
        [ContextMenu("Run Setup")]
        private void EditorSetup()
        {
            SetupSoldier();
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }

        [ContextMenu("Validate Setup")]
        private void ValidateSetup()
        {
            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine("=== Soldier Setup Validation ===");

            // Check components
            report.AppendLine(GetComponent<NavMeshAgent>() != null ? "[OK] NavMeshAgent" : "[MISSING] NavMeshAgent");
            report.AppendLine(GetComponent<Animator>() != null ? "[OK] Animator" : "[MISSING] Animator");
            report.AppendLine(GetComponent<CapsuleCollider>() != null ? "[OK] CapsuleCollider" : "[MISSING] CapsuleCollider");
            report.AppendLine(GetComponent<SoldierAI>() != null ? "[OK] SoldierAI" : "[MISSING] SoldierAI");
            report.AppendLine(GetComponent<EnemyHealth>() != null ? "[OK] EnemyHealth" : "[MISSING] EnemyHealth");

            // Check layer
            int enemyLayer = LayerMask.NameToLayer(enemyLayerName);
            if (enemyLayer != -1 && gameObject.layer == enemyLayer)
            {
                report.AppendLine("[OK] Enemy Layer");
            }
            else
            {
                report.AppendLine("[WARNING] Not on Enemy layer");
            }

            // Check animator controller
            Animator animator = GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                report.AppendLine("[OK] Animator Controller assigned");
            }
            else
            {
                report.AppendLine("[WARNING] Animator Controller not assigned");
            }

            Debug.Log(report.ToString());
        }
#endif

        #endregion
    }
}
