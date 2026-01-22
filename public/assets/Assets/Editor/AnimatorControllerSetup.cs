#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace CityShooter.Editor
{
    /// <summary>
    /// Editor utility for creating and configuring the FPS Animation State Machine.
    /// Creates an Animator Controller with proper states, parameters, and transitions.
    /// </summary>
    public class AnimatorControllerSetup : EditorWindow
    {
        private const string CONTROLLER_PATH = "Assets/Animations/FPSPlayerController.controller";
        private const string UPPER_BODY_MASK_PATH = "Assets/Animations/UpperBodyMask.mask";
        private const string LOWER_BODY_MASK_PATH = "Assets/Animations/LowerBodyMask.mask";

        // Animation clip references
        private AnimationClip idleClip;
        private AnimationClip strafeClip;
        private AnimationClip staticFireClip;
        private AnimationClip movingFireClip;
        private AnimationClip reactionClip;

        [MenuItem("City Shooter/Create FPS Animator Controller")]
        public static void ShowWindow()
        {
            GetWindow<AnimatorControllerSetup>("FPS Animator Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("FPS Animation State Machine Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This tool creates an Animator Controller configured for the FPS character.\n\n" +
                "Expected animation clips:\n" +
                "- Strafe.fbx (walking/strafing)\n" +
                "- static_fire.fbx (upper body firing while stationary)\n" +
                "- moving fire.fbx (upper body firing while moving)\n" +
                "- Reaction.fbx (hit reaction)",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Animation clip fields
            idleClip = EditorGUILayout.ObjectField("Idle Clip", idleClip, typeof(AnimationClip), false) as AnimationClip;
            strafeClip = EditorGUILayout.ObjectField("Strafe Clip", strafeClip, typeof(AnimationClip), false) as AnimationClip;
            staticFireClip = EditorGUILayout.ObjectField("Static Fire Clip", staticFireClip, typeof(AnimationClip), false) as AnimationClip;
            movingFireClip = EditorGUILayout.ObjectField("Moving Fire Clip", movingFireClip, typeof(AnimationClip), false) as AnimationClip;
            reactionClip = EditorGUILayout.ObjectField("Reaction Clip", reactionClip, typeof(AnimationClip), false) as AnimationClip;

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Animator Controller", GUILayout.Height(30)))
            {
                CreateAnimatorController();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Avatar Masks", GUILayout.Height(25)))
            {
                CreateAvatarMasks();
            }

            if (GUILayout.Button("Auto-Find Animation Clips", GUILayout.Height(25)))
            {
                AutoFindClips();
            }
        }

        private void AutoFindClips()
        {
            // Try to find clips automatically
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

                if (clip == null) continue;

                string clipName = clip.name.ToLower();

                if (clipName.Contains("strafe"))
                    strafeClip = clip;
                else if (clipName.Contains("static") && clipName.Contains("fire"))
                    staticFireClip = clip;
                else if (clipName.Contains("moving") && clipName.Contains("fire"))
                    movingFireClip = clip;
                else if (clipName.Contains("reaction") || clipName.Contains("hit"))
                    reactionClip = clip;
                else if (clipName.Contains("idle"))
                    idleClip = clip;
            }

            Debug.Log("Auto-find complete. Check the assigned clips.");
        }

        private void CreateAnimatorController()
        {
            // Ensure directory exists
            string directory = Path.GetDirectoryName(CONTROLLER_PATH);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create the Animator Controller
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);

            // Add parameters
            AddParameters(controller);

            // Setup base layer (locomotion)
            SetupBaseLayer(controller);

            // Setup upper body layer (firing animations - additive)
            SetupUpperBodyLayer(controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Animator Controller created at: {CONTROLLER_PATH}");
            Selection.activeObject = controller;
        }

        private void AddParameters(AnimatorController controller)
        {
            controller.AddParameter("Velocity", AnimatorControllerParameterType.Float);
            controller.AddParameter("Horizontal", AnimatorControllerParameterType.Float);
            controller.AddParameter("Vertical", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsSprinting", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsFiring", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("HitReaction", AnimatorControllerParameterType.Trigger);
        }

        private void SetupBaseLayer(AnimatorController controller)
        {
            AnimatorControllerLayer baseLayer = controller.layers[0];
            AnimatorStateMachine stateMachine = baseLayer.stateMachine;

            // Create states
            AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(250, 50, 0));
            AnimatorState walkState = stateMachine.AddState("Walk", new Vector3(250, 150, 0));
            AnimatorState strafeState = stateMachine.AddState("Strafe", new Vector3(450, 100, 0));

            // Assign clips if available
            if (idleClip != null) idleState.motion = idleClip;
            if (strafeClip != null)
            {
                walkState.motion = strafeClip;
                strafeState.motion = strafeClip;
            }

            // Set default state
            stateMachine.defaultState = idleState;

            // Create transitions
            // Idle -> Walk (when moving forward/back)
            AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Velocity");
            idleToWalk.duration = 0.1f;
            idleToWalk.hasExitTime = false;

            // Walk -> Idle (when stopped)
            AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "Velocity");
            walkToIdle.duration = 0.1f;
            walkToIdle.hasExitTime = false;

            // Idle -> Strafe (when moving sideways)
            AnimatorStateTransition idleToStrafe = idleState.AddTransition(strafeState);
            idleToStrafe.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Horizontal");
            idleToStrafe.duration = 0.1f;
            idleToStrafe.hasExitTime = false;

            AnimatorStateTransition idleToStrafeNeg = idleState.AddTransition(strafeState);
            idleToStrafeNeg.AddCondition(AnimatorConditionMode.Less, -0.1f, "Horizontal");
            idleToStrafeNeg.duration = 0.1f;
            idleToStrafeNeg.hasExitTime = false;

            // Strafe -> Idle
            AnimatorStateTransition strafeToIdle = strafeState.AddTransition(idleState);
            strafeToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "Velocity");
            strafeToIdle.duration = 0.1f;
            strafeToIdle.hasExitTime = false;

            // Walk <-> Strafe
            AnimatorStateTransition walkToStrafe = walkState.AddTransition(strafeState);
            walkToStrafe.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Horizontal");
            walkToStrafe.duration = 0.1f;
            walkToStrafe.hasExitTime = false;

            AnimatorStateTransition strafeToWalk = strafeState.AddTransition(walkState);
            strafeToWalk.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Vertical");
            strafeToWalk.AddCondition(AnimatorConditionMode.Less, 0.3f, "Horizontal");
            strafeToWalk.AddCondition(AnimatorConditionMode.Greater, -0.3f, "Horizontal");
            strafeToWalk.duration = 0.1f;
            strafeToWalk.hasExitTime = false;
        }

        private void SetupUpperBodyLayer(AnimatorController controller)
        {
            // Add upper body layer for additive firing animations
            controller.AddLayer("Upper Body");

            AnimatorControllerLayer[] layers = controller.layers;
            AnimatorControllerLayer upperBodyLayer = layers[1];

            upperBodyLayer.defaultWeight = 1f;
            upperBodyLayer.blendingMode = AnimatorLayerBlendingMode.Additive;

            // Load or create avatar mask
            AvatarMask upperMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UPPER_BODY_MASK_PATH);
            if (upperMask != null)
            {
                upperBodyLayer.avatarMask = upperMask;
            }

            controller.layers = layers;

            AnimatorStateMachine upperStateMachine = upperBodyLayer.stateMachine;

            // Create states for upper body
            AnimatorState emptyState = upperStateMachine.AddState("Empty", new Vector3(250, 50, 0));
            AnimatorState staticFireState = upperStateMachine.AddState("StaticFire", new Vector3(250, 150, 0));
            AnimatorState movingFireState = upperStateMachine.AddState("MovingFire", new Vector3(450, 150, 0));
            AnimatorState hitReactionState = upperStateMachine.AddState("HitReaction", new Vector3(450, 50, 0));

            // Assign clips
            if (staticFireClip != null) staticFireState.motion = staticFireClip;
            if (movingFireClip != null) movingFireState.motion = movingFireClip;
            if (reactionClip != null) hitReactionState.motion = reactionClip;

            // Set default
            upperStateMachine.defaultState = emptyState;

            // Transitions
            // Empty -> StaticFire (firing while stationary)
            AnimatorStateTransition toStaticFire = emptyState.AddTransition(staticFireState);
            toStaticFire.AddCondition(AnimatorConditionMode.If, 0, "IsFiring");
            toStaticFire.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            toStaticFire.duration = 0.05f;
            toStaticFire.hasExitTime = false;

            // Empty -> MovingFire (firing while moving)
            AnimatorStateTransition toMovingFire = emptyState.AddTransition(movingFireState);
            toMovingFire.AddCondition(AnimatorConditionMode.If, 0, "IsFiring");
            toMovingFire.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            toMovingFire.duration = 0.05f;
            toMovingFire.hasExitTime = false;

            // StaticFire -> Empty
            AnimatorStateTransition staticToEmpty = staticFireState.AddTransition(emptyState);
            staticToEmpty.AddCondition(AnimatorConditionMode.IfNot, 0, "IsFiring");
            staticToEmpty.duration = 0.1f;
            staticToEmpty.hasExitTime = false;

            // MovingFire -> Empty
            AnimatorStateTransition movingToEmpty = movingFireState.AddTransition(emptyState);
            movingToEmpty.AddCondition(AnimatorConditionMode.IfNot, 0, "IsFiring");
            movingToEmpty.duration = 0.1f;
            movingToEmpty.hasExitTime = false;

            // StaticFire <-> MovingFire (transition based on movement state)
            AnimatorStateTransition staticToMoving = staticFireState.AddTransition(movingFireState);
            staticToMoving.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            staticToMoving.duration = 0.1f;
            staticToMoving.hasExitTime = false;

            AnimatorStateTransition movingToStatic = movingFireState.AddTransition(staticFireState);
            movingToStatic.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            movingToStatic.duration = 0.1f;
            movingToStatic.hasExitTime = false;

            // Any State -> HitReaction (triggered)
            AnimatorStateTransition anyToHit = upperStateMachine.AddAnyStateTransition(hitReactionState);
            anyToHit.AddCondition(AnimatorConditionMode.If, 0, "HitReaction");
            anyToHit.duration = 0.05f;
            anyToHit.hasExitTime = false;

            // HitReaction -> Empty (after animation completes)
            AnimatorStateTransition hitToEmpty = hitReactionState.AddTransition(emptyState);
            hitToEmpty.hasExitTime = true;
            hitToEmpty.exitTime = 0.9f;
            hitToEmpty.duration = 0.1f;
        }

        private void CreateAvatarMasks()
        {
            // Ensure directory exists
            string directory = Path.GetDirectoryName(UPPER_BODY_MASK_PATH);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create Upper Body Mask
            AvatarMask upperMask = new AvatarMask();
            upperMask.name = "UpperBodyMask";

            // Configure humanoid body parts
            // Disable legs, enable upper body
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                AvatarMaskBodyPart part = (AvatarMaskBodyPart)i;
                bool active = part == AvatarMaskBodyPart.Head ||
                              part == AvatarMaskBodyPart.LeftArm ||
                              part == AvatarMaskBodyPart.RightArm ||
                              part == AvatarMaskBodyPart.Body;
                upperMask.SetHumanoidBodyPartActive(part, active);
            }

            AssetDatabase.CreateAsset(upperMask, UPPER_BODY_MASK_PATH);

            // Create Lower Body Mask
            AvatarMask lowerMask = new AvatarMask();
            lowerMask.name = "LowerBodyMask";

            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                AvatarMaskBodyPart part = (AvatarMaskBodyPart)i;
                bool active = part == AvatarMaskBodyPart.Root ||
                              part == AvatarMaskBodyPart.LeftLeg ||
                              part == AvatarMaskBodyPart.RightLeg;
                lowerMask.SetHumanoidBodyPartActive(part, active);
            }

            AssetDatabase.CreateAsset(lowerMask, LOWER_BODY_MASK_PATH);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Avatar Masks created at:\n- {UPPER_BODY_MASK_PATH}\n- {LOWER_BODY_MASK_PATH}");
        }
    }
}
#endif
