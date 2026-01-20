using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace CityShooter.Editor
{
    /// <summary>
    /// Editor utility for creating and configuring the Soldier Animator Controller.
    /// This script creates the animator controller with all required states and transitions.
    /// </summary>
    public static class SoldierAnimatorSetup
    {
        private const string ANIMATOR_PATH = "Assets/Animations/SoldierAnimatorController.controller";
        private const string ANIMATIONS_FOLDER = "Assets/Animations";

        [MenuItem("CityShooter/Setup/Create Soldier Animator Controller")]
        public static void CreateSoldierAnimatorController()
        {
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder(ANIMATIONS_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets", "Animations");
            }

            // Create animator controller
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ANIMATOR_PATH);

            if (controller == null)
            {
                Debug.LogError("Failed to create Animator Controller");
                return;
            }

            // Add parameters
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("React", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsAlive", AnimatorControllerParameterType.Bool);

            // Get the root state machine
            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

            // Create states
            AnimatorState idleState = rootStateMachine.AddState("Idle", new Vector3(0, 0, 0));
            AnimatorState moveState = rootStateMachine.AddState("Move", new Vector3(200, 0, 0));
            AnimatorState attackState = rootStateMachine.AddState("Attack", new Vector3(400, 0, 0));
            AnimatorState deathState = rootStateMachine.AddState("Death", new Vector3(200, 200, 0));

            // Set Idle as default
            rootStateMachine.defaultState = idleState;

            // Create transitions

            // Idle -> Move (when moving)
            AnimatorStateTransition idleToMove = idleState.AddTransition(moveState);
            idleToMove.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToMove.hasExitTime = false;
            idleToMove.duration = 0.1f;

            // Move -> Idle (when stopped)
            AnimatorStateTransition moveToIdle = moveState.AddTransition(idleState);
            moveToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            moveToIdle.hasExitTime = false;
            moveToIdle.duration = 0.1f;

            // Any -> Attack (trigger)
            AnimatorStateTransition anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "IsAlive");
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0.1f;
            anyToAttack.canTransitionToSelf = false;

            // Attack -> Idle (exit time)
            AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 0.9f;
            attackToIdle.duration = 0.1f;

            // Any -> Death (trigger)
            AnimatorStateTransition anyToDeath = rootStateMachine.AddAnyStateTransition(deathState);
            anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "Death");
            anyToDeath.hasExitTime = false;
            anyToDeath.duration = 0.1f;
            anyToDeath.canTransitionToSelf = false;

            // Create a separate layer for hit reactions (interrupt layer)
            CreateReactionLayer(controller);

            // Save the asset
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Soldier Animator Controller created at: {ANIMATOR_PATH}");
            Debug.Log("Remember to assign animation clips to the states!");
        }

        private static void CreateReactionLayer(AnimatorController controller)
        {
            // Add a new layer for reactions
            controller.AddLayer("Reactions");
            AnimatorControllerLayer reactionLayer = controller.layers[1];

            // Set layer properties for override blending
            AnimatorControllerLayer[] layers = controller.layers;
            layers[1].defaultWeight = 1f;
            layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
            controller.layers = layers;

            AnimatorStateMachine reactionStateMachine = reactionLayer.stateMachine;

            // Create empty state (default)
            AnimatorState emptyState = reactionStateMachine.AddState("Empty", new Vector3(0, 0, 0));
            reactionStateMachine.defaultState = emptyState;

            // Create react state
            AnimatorState reactState = reactionStateMachine.AddState("React", new Vector3(200, 0, 0));

            // Empty -> React (trigger)
            AnimatorStateTransition emptyToReact = emptyState.AddTransition(reactState);
            emptyToReact.AddCondition(AnimatorConditionMode.If, 0, "React");
            emptyToReact.hasExitTime = false;
            emptyToReact.duration = 0.05f; // Very fast transition for immediate response

            // React -> Empty (exit time)
            AnimatorStateTransition reactToEmpty = reactState.AddTransition(emptyState);
            reactToEmpty.hasExitTime = true;
            reactToEmpty.exitTime = 0.95f;
            reactToEmpty.duration = 0.1f;
        }

        [MenuItem("CityShooter/Setup/Validate Soldier Setup")]
        public static void ValidateSoldierSetup()
        {
            bool allValid = true;
            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine("=== Soldier Setup Validation Report ===\n");

            // Check for Soldier.fbx
            string soldierPath = "Assets/Soldier.fbx";
            if (File.Exists(Application.dataPath + "/Soldier.fbx"))
            {
                report.AppendLine("[OK] Soldier.fbx found");
            }
            else
            {
                report.AppendLine("[WARNING] Soldier.fbx not found at expected location");
                allValid = false;
            }

            // Check for Reaction.fbx (in root or parent)
            string[] reactionPaths = new string[]
            {
                "Assets/Reaction.fbx",
                "Reaction.fbx"
            };
            bool reactionFound = false;
            foreach (var path in reactionPaths)
            {
                string fullPath = path.StartsWith("Assets/")
                    ? Application.dataPath + path.Substring(6)
                    : Application.dataPath + "/../" + path;

                if (File.Exists(fullPath))
                {
                    report.AppendLine($"[OK] Reaction.fbx found at {path}");
                    reactionFound = true;
                    break;
                }
            }
            if (!reactionFound)
            {
                report.AppendLine("[WARNING] Reaction.fbx not found");
                allValid = false;
            }

            // Check for Animator Controller
            if (File.Exists(Application.dataPath + "/Animations/SoldierAnimatorController.controller"))
            {
                report.AppendLine("[OK] SoldierAnimatorController.controller found");
            }
            else
            {
                report.AppendLine("[INFO] SoldierAnimatorController.controller not yet created");
                report.AppendLine("       Run 'CityShooter/Setup/Create Soldier Animator Controller' to create it");
            }

            // Check for required scripts
            string[] requiredScripts = new string[]
            {
                "Assets/Scripts/Enemy/SoldierAI.cs",
                "Assets/Scripts/Enemy/EnemyHealth.cs",
                "Assets/Scripts/Interfaces/IDamageable.cs"
            };

            foreach (var script in requiredScripts)
            {
                string fullPath = Application.dataPath + script.Substring(6);
                if (File.Exists(fullPath))
                {
                    report.AppendLine($"[OK] {Path.GetFileName(script)} found");
                }
                else
                {
                    report.AppendLine($"[ERROR] {Path.GetFileName(script)} not found");
                    allValid = false;
                }
            }

            // Summary
            report.AppendLine("\n" + (allValid ? "All checks passed!" : "Some issues found. See above for details."));

            Debug.Log(report.ToString());
        }
    }
}
