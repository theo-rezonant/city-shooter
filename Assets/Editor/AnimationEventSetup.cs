using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor utility to set up Animation Events on FBX files for footstep audio.
/// Specifically designed for Strafe.fbx and similar movement animations.
/// Run from menu: Tools > Audio > Setup Animation Events
/// </summary>
public class AnimationEventSetup : EditorWindow
{
    private AnimationClip selectedClip;
    private float leftFootFrame = 0f;
    private float rightFootFrame = 0f;
    private string functionName = "OnFootstepEvent";
    private bool autoDetectFrames = true;

    // Common animation names that should have footstep events
    private static readonly string[] FootstepAnimationNames = new string[]
    {
        "strafe",
        "walk",
        "run",
        "sprint",
        "jog",
        "move"
    };

    [MenuItem("Tools/Audio/Setup Animation Events")]
    public static void ShowWindow()
    {
        GetWindow<AnimationEventSetup>("Animation Event Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Animation Event Setup for Footsteps", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This tool helps configure Animation Events for footstep sounds.\n\n" +
            "For Strafe.fbx:\n" +
            "1. Select the FBX file in the Project window\n" +
            "2. Click 'Auto Setup Strafe.fbx' below\n" +
            "3. Or manually configure frames for custom animations",
            MessageType.Info);

        GUILayout.Space(15);

        // Quick setup for Strafe.fbx
        GUILayout.Label("Quick Setup", EditorStyles.boldLabel);

        if (GUILayout.Button("Auto Setup Strafe.fbx", GUILayout.Height(30)))
        {
            AutoSetupStrafeAnimation();
        }

        GUILayout.Space(20);

        // Manual setup section
        GUILayout.Label("Manual Setup", EditorStyles.boldLabel);

        selectedClip = EditorGUILayout.ObjectField("Animation Clip", selectedClip, typeof(AnimationClip), false) as AnimationClip;

        functionName = EditorGUILayout.TextField("Function Name", functionName);

        EditorGUILayout.HelpBox(
            "Function options:\n" +
            "• OnFootstepEvent - Generic footstep\n" +
            "• OnLeftFootStep - Left foot specific\n" +
            "• OnRightFootStep - Right foot specific\n" +
            "• PlayFootstep - Direct playback call",
            MessageType.None);

        GUILayout.Space(10);

        autoDetectFrames = EditorGUILayout.Toggle("Auto Detect Foot Contacts", autoDetectFrames);

        if (!autoDetectFrames)
        {
            leftFootFrame = EditorGUILayout.FloatField("Left Foot Frame (normalized 0-1)", leftFootFrame);
            rightFootFrame = EditorGUILayout.FloatField("Right Foot Frame (normalized 0-1)", rightFootFrame);
        }

        GUILayout.Space(10);

        EditorGUI.BeginDisabledGroup(selectedClip == null);
        if (GUILayout.Button("Add Footstep Events", GUILayout.Height(30)))
        {
            AddFootstepEventsToClip();
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(20);

        // Batch processing
        GUILayout.Label("Batch Processing", EditorStyles.boldLabel);

        if (GUILayout.Button("Process All Movement Animations", GUILayout.Height(25)))
        {
            ProcessAllMovementAnimations();
        }

        GUILayout.Space(10);

        // Info section
        DrawInfoSection();
    }

    private void AutoSetupStrafeAnimation()
    {
        // Find Strafe.fbx
        string[] guids = AssetDatabase.FindAssets("Strafe t:Model");

        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Not Found", "Could not find Strafe.fbx in the project.", "OK");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

        if (importer == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not get ModelImporter for Strafe.fbx", "OK");
            return;
        }

        // Get existing clips
        ModelImporterClipAnimation[] clips = importer.clipAnimations;

        if (clips.Length == 0)
        {
            // Use default clips
            clips = importer.defaultClipAnimations;
        }

        // Add events to each clip
        List<ModelImporterClipAnimation> modifiedClips = new List<ModelImporterClipAnimation>();

        foreach (var clip in clips)
        {
            var newClip = clip;
            var events = new List<AnimationEvent>();

            // Keep existing events
            if (clip.events != null)
            {
                events.AddRange(clip.events);
            }

            // Calculate typical footstep positions for a strafe animation
            // Assuming a standard walk/strafe cycle with 2 steps
            float clipLength = clip.lastFrame - clip.firstFrame;
            float frameRate = 30f; // Standard FBX frame rate

            // Add events at 25% and 75% of the animation (typical foot contact points)
            // Left foot at start, right foot at middle
            AnimationEvent leftFootEvent = new AnimationEvent
            {
                functionName = "OnLeftFootStep",
                time = 0.15f, // 15% into the animation
                messageOptions = SendMessageOptions.DontRequireReceiver
            };

            AnimationEvent rightFootEvent = new AnimationEvent
            {
                functionName = "OnRightFootStep",
                time = 0.65f, // 65% into the animation
                messageOptions = SendMessageOptions.DontRequireReceiver
            };

            // Check if events already exist at similar times
            bool hasLeftEvent = false;
            bool hasRightEvent = false;

            foreach (var existingEvent in events)
            {
                if (Mathf.Abs(existingEvent.time - leftFootEvent.time) < 0.1f)
                    hasLeftEvent = true;
                if (Mathf.Abs(existingEvent.time - rightFootEvent.time) < 0.1f)
                    hasRightEvent = true;
            }

            if (!hasLeftEvent) events.Add(leftFootEvent);
            if (!hasRightEvent) events.Add(rightFootEvent);

            newClip.events = events.ToArray();
            modifiedClips.Add(newClip);
        }

        // Apply changes
        importer.clipAnimations = modifiedClips.ToArray();
        importer.SaveAndReimport();

        EditorUtility.DisplayDialog(
            "Success",
            $"Added footstep events to Strafe.fbx\n\nEvents added:\n" +
            "• OnLeftFootStep at 15%\n" +
            "• OnRightFootStep at 65%\n\n" +
            "You can fine-tune these in the Animation tab of the FBX import settings.",
            "OK");

        Debug.Log($"[AnimationEventSetup] Successfully configured footstep events for: {path}");
    }

    private void AddFootstepEventsToClip()
    {
        if (selectedClip == null) return;

        string path = AssetDatabase.GetAssetPath(selectedClip);
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

        if (importer == null)
        {
            // It might be a standalone animation clip
            AddEventsToStandaloneClip();
            return;
        }

        ModelImporterClipAnimation[] clips = importer.clipAnimations;

        if (clips.Length == 0)
        {
            clips = importer.defaultClipAnimations;
        }

        List<ModelImporterClipAnimation> modifiedClips = new List<ModelImporterClipAnimation>();

        foreach (var clip in clips)
        {
            if (clip.name == selectedClip.name || clips.Length == 1)
            {
                var newClip = clip;
                var events = new List<AnimationEvent>();

                if (clip.events != null)
                {
                    events.AddRange(clip.events);
                }

                float leftTime = autoDetectFrames ? 0.15f : leftFootFrame;
                float rightTime = autoDetectFrames ? 0.65f : rightFootFrame;

                events.Add(new AnimationEvent
                {
                    functionName = functionName,
                    time = leftTime,
                    messageOptions = SendMessageOptions.DontRequireReceiver
                });

                events.Add(new AnimationEvent
                {
                    functionName = functionName,
                    time = rightTime,
                    messageOptions = SendMessageOptions.DontRequireReceiver
                });

                newClip.events = events.ToArray();
                modifiedClips.Add(newClip);
            }
            else
            {
                modifiedClips.Add(clip);
            }
        }

        importer.clipAnimations = modifiedClips.ToArray();
        importer.SaveAndReimport();

        Debug.Log($"[AnimationEventSetup] Added footstep events to: {selectedClip.name}");
        EditorUtility.DisplayDialog("Success", $"Added footstep events to {selectedClip.name}", "OK");
    }

    private void AddEventsToStandaloneClip()
    {
        // For standalone .anim files
        AnimationEvent leftEvent = new AnimationEvent
        {
            functionName = functionName,
            time = autoDetectFrames ? selectedClip.length * 0.15f : leftFootFrame * selectedClip.length
        };

        AnimationEvent rightEvent = new AnimationEvent
        {
            functionName = functionName,
            time = autoDetectFrames ? selectedClip.length * 0.65f : rightFootFrame * selectedClip.length
        };

        AnimationUtility.SetAnimationEvents(selectedClip, new[] { leftEvent, rightEvent });

        EditorUtility.SetDirty(selectedClip);
        AssetDatabase.SaveAssets();

        Debug.Log($"[AnimationEventSetup] Added events to standalone clip: {selectedClip.name}");
        EditorUtility.DisplayDialog("Success", $"Added footstep events to {selectedClip.name}", "OK");
    }

    private void ProcessAllMovementAnimations()
    {
        string[] fbxGuids = AssetDatabase.FindAssets("t:Model");
        int processedCount = 0;

        foreach (string guid in fbxGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();

            bool isMovementAnimation = false;
            foreach (string keyword in FootstepAnimationNames)
            {
                if (fileName.Contains(keyword))
                {
                    isMovementAnimation = true;
                    break;
                }
            }

            if (!isMovementAnimation) continue;

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips.Length == 0) clips = importer.defaultClipAnimations;

            List<ModelImporterClipAnimation> modifiedClips = new List<ModelImporterClipAnimation>();
            bool wasModified = false;

            foreach (var clip in clips)
            {
                var newClip = clip;
                var events = new List<AnimationEvent>();

                if (clip.events != null)
                {
                    // Check if already has footstep events
                    bool hasFootstepEvent = false;
                    foreach (var evt in clip.events)
                    {
                        if (evt.functionName.Contains("Foot") || evt.functionName.Contains("Step"))
                        {
                            hasFootstepEvent = true;
                        }
                        events.Add(evt);
                    }

                    if (hasFootstepEvent)
                    {
                        modifiedClips.Add(clip);
                        continue;
                    }
                }

                // Add footstep events
                events.Add(new AnimationEvent
                {
                    functionName = "OnLeftFootStep",
                    time = 0.15f,
                    messageOptions = SendMessageOptions.DontRequireReceiver
                });

                events.Add(new AnimationEvent
                {
                    functionName = "OnRightFootStep",
                    time = 0.65f,
                    messageOptions = SendMessageOptions.DontRequireReceiver
                });

                newClip.events = events.ToArray();
                modifiedClips.Add(newClip);
                wasModified = true;
            }

            if (wasModified)
            {
                importer.clipAnimations = modifiedClips.ToArray();
                importer.SaveAndReimport();
                processedCount++;
                Debug.Log($"[AnimationEventSetup] Processed: {path}");
            }
        }

        EditorUtility.DisplayDialog(
            "Batch Processing Complete",
            $"Processed {processedCount} movement animations.\n\n" +
            "Check the Console for details.",
            "OK");
    }

    private void DrawInfoSection()
    {
        GUILayout.Label("Important Notes", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "After adding Animation Events:\n\n" +
            "1. The FootstepAudio component must be on the same GameObject " +
            "as the Animator (or a child with the animation events properly routed)\n\n" +
            "2. The function names must match exactly:\n" +
            "   • OnFootstepEvent\n" +
            "   • OnLeftFootStep\n" +
            "   • OnRightFootStep\n" +
            "   • PlayFootstep\n\n" +
            "3. Fine-tune event timing in the FBX Import Settings > Animation tab\n\n" +
            "4. Preview the animation in the Animation window to verify event placement",
            MessageType.Warning);
    }
}
