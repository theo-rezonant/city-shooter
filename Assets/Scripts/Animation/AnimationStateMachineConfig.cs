using UnityEngine;

namespace CityShooter.Animation
{
    /// <summary>
    /// Configuration and documentation for the Animation State Machine setup.
    /// This ScriptableObject defines the expected structure and can be used
    /// by editor tools to auto-configure the Animator Controller.
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationStateMachineConfig", menuName = "City Shooter/Animation State Machine Config")]
    public class AnimationStateMachineConfig : ScriptableObject
    {
        [Header("State Names")]
        public string idleStateName = "Idle";
        public string walkStateName = "Walk";
        public string strafeStateName = "Strafe";
        public string staticFireStateName = "StaticFire";
        public string movingFireStateName = "MovingFire";
        public string hitReactionStateName = "HitReaction";

        [Header("Parameter Names")]
        public string velocityParam = "Velocity";
        public string horizontalParam = "Horizontal";
        public string verticalParam = "Vertical";
        public string isGroundedParam = "IsGrounded";
        public string isSprintingParam = "IsSprinting";
        public string isFiringParam = "IsFiring";
        public string isMovingParam = "IsMoving";
        public string hitReactionTrigger = "HitReaction";

        [Header("Layer Configuration")]
        public LayerConfig[] layers = new LayerConfig[]
        {
            new LayerConfig("Base Layer", 0, 1f, AvatarMaskBodyPart.Root | AvatarMaskBodyPart.Body | AvatarMaskBodyPart.LeftLeg | AvatarMaskBodyPart.RightLeg),
            new LayerConfig("Upper Body", 1, 1f, AvatarMaskBodyPart.Head | AvatarMaskBodyPart.LeftArm | AvatarMaskBodyPart.RightArm)
        };

        [Header("Transition Settings")]
        public float defaultTransitionDuration = 0.1f;
        public float idleToWalkThreshold = 0.1f;
        public float walkToIdleThreshold = 0.05f;

        [Header("Animation Clip Assignments")]
        [Tooltip("Reference: Strafe.fbx")]
        public string strafeAnimationPath = "Strafe";

        [Tooltip("Reference: Reaction.fbx")]
        public string reactionAnimationPath = "Reaction";

        [Tooltip("Reference: static_fire.fbx")]
        public string staticFireAnimationPath = "static_fire";

        [Tooltip("Reference: moving fire.fbx")]
        public string movingFireAnimationPath = "moving fire";

        /// <summary>
        /// Get the expected animator parameter definitions.
        /// </summary>
        public ParameterDefinition[] GetParameterDefinitions()
        {
            return new ParameterDefinition[]
            {
                new ParameterDefinition(velocityParam, AnimatorControllerParameterType.Float, 0f),
                new ParameterDefinition(horizontalParam, AnimatorControllerParameterType.Float, 0f),
                new ParameterDefinition(verticalParam, AnimatorControllerParameterType.Float, 0f),
                new ParameterDefinition(isGroundedParam, AnimatorControllerParameterType.Bool, true),
                new ParameterDefinition(isSprintingParam, AnimatorControllerParameterType.Bool, false),
                new ParameterDefinition(isFiringParam, AnimatorControllerParameterType.Bool, false),
                new ParameterDefinition(isMovingParam, AnimatorControllerParameterType.Bool, false),
                new ParameterDefinition(hitReactionTrigger, AnimatorControllerParameterType.Trigger, false)
            };
        }

        [System.Serializable]
        public struct LayerConfig
        {
            public string layerName;
            public int layerIndex;
            public float defaultWeight;
            public AvatarMaskBodyPart bodyParts;

            public LayerConfig(string name, int index, float weight, AvatarMaskBodyPart parts)
            {
                layerName = name;
                layerIndex = index;
                defaultWeight = weight;
                bodyParts = parts;
            }
        }

        [System.Serializable]
        public struct ParameterDefinition
        {
            public string name;
            public AnimatorControllerParameterType type;
            public object defaultValue;

            public ParameterDefinition(string name, AnimatorControllerParameterType type, object defaultValue)
            {
                this.name = name;
                this.type = type;
                this.defaultValue = defaultValue;
            }
        }

        [System.Flags]
        public enum AvatarMaskBodyPart
        {
            Root = 1,
            Body = 2,
            Head = 4,
            LeftLeg = 8,
            RightLeg = 16,
            LeftArm = 32,
            RightArm = 64,
            LeftHand = 128,
            RightHand = 256,
            LeftFootIK = 512,
            RightFootIK = 1024,
            LeftHandIK = 2048,
            RightHandIK = 4096
        }
    }
}
