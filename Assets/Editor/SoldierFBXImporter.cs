using UnityEditor;
using UnityEngine;

namespace CityShooter.Editor
{
    /// <summary>
    /// Asset post-processor for Soldier FBX files.
    /// Automatically configures import settings for proper Z-up to Y-up conversion
    /// and animation setup.
    /// </summary>
    public class SoldierFBXImporter : AssetPostprocessor
    {
        // Define paths that should use Soldier import settings
        private static readonly string[] SoldierAssetPaths = new string[]
        {
            "Assets/Soldier",
            "Assets/Reaction",
            "Assets/Strafe",
            "Assets/moving fire",
            "Assets/static_fire"
        };

        private bool IsSoldierRelatedAsset(string path)
        {
            foreach (var soldierPath in SoldierAssetPaths)
            {
                if (path.ToLower().Contains(soldierPath.ToLower()))
                {
                    return true;
                }
            }
            return false;
        }

        private void OnPreprocessModel()
        {
            if (!IsSoldierRelatedAsset(assetPath)) return;

            ModelImporter modelImporter = (ModelImporter)assetImporter;

            // Model settings
            modelImporter.globalScale = 1f;
            modelImporter.useFileScale = true;
            modelImporter.meshCompression = ModelImporterMeshCompression.Medium;
            modelImporter.isReadable = false;
            modelImporter.optimizeMeshPolygons = true;
            modelImporter.optimizeMeshVertices = true;

            // Convert from Blender's Z-up to Unity's Y-up coordinate system
            modelImporter.bakeAxisConversion = true;

            // Rig settings for humanoid animations
            if (assetPath.Contains("Soldier"))
            {
                modelImporter.animationType = ModelImporterAnimationType.Human;
                modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            }
            else
            {
                // Animation files should copy avatar from Soldier
                modelImporter.animationType = ModelImporterAnimationType.Human;
                modelImporter.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                // Note: Source avatar must be set manually in Unity Editor
            }

            // Material settings
            modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            modelImporter.materialLocation = ModelImporterMaterialLocation.InPrefab;

            Debug.Log($"SoldierFBXImporter: Configured import settings for {assetPath}");
        }

        private void OnPreprocessAnimation()
        {
            if (!IsSoldierRelatedAsset(assetPath)) return;

            ModelImporter modelImporter = (ModelImporter)assetImporter;

            // Animation settings
            ModelImporterClipAnimation[] clipAnimations = modelImporter.defaultClipAnimations;

            if (clipAnimations != null && clipAnimations.Length > 0)
            {
                for (int i = 0; i < clipAnimations.Length; i++)
                {
                    // Configure animation clips
                    clipAnimations[i].lockRootRotation = false;
                    clipAnimations[i].lockRootHeightY = false;
                    clipAnimations[i].lockRootPositionXZ = false;
                    clipAnimations[i].keepOriginalOrientation = true;
                    clipAnimations[i].keepOriginalPositionY = true;
                    clipAnimations[i].keepOriginalPositionXZ = true;

                    // Set loop time based on animation name
                    string clipName = clipAnimations[i].name.ToLower();
                    if (clipName.Contains("idle") || clipName.Contains("walk") ||
                        clipName.Contains("run") || clipName.Contains("strafe"))
                    {
                        clipAnimations[i].loopTime = true;
                    }
                    else
                    {
                        // Reaction, attack, death animations should not loop
                        clipAnimations[i].loopTime = false;
                    }
                }

                modelImporter.clipAnimations = clipAnimations;
            }

            Debug.Log($"SoldierFBXImporter: Configured animation settings for {assetPath}");
        }

        private void OnPostprocessModel(GameObject root)
        {
            if (!IsSoldierRelatedAsset(assetPath)) return;

            // Add necessary components or configure the imported model
            Debug.Log($"SoldierFBXImporter: Post-processed model {root.name}");
        }
    }

    /// <summary>
    /// Editor window for managing Soldier import settings.
    /// </summary>
    public class SoldierImportWindow : EditorWindow
    {
        private string soldierFBXPath = "Assets/Soldier.fbx";
        private string reactionFBXPath = "";
        private Avatar sourceAvatar;

        [MenuItem("CityShooter/Setup/Soldier Import Settings")]
        public static void ShowWindow()
        {
            GetWindow<SoldierImportWindow>("Soldier Import Settings");
        }

        private void OnEnable()
        {
            // Try to find Reaction.fbx path
            string[] reactionGuids = AssetDatabase.FindAssets("Reaction t:Model");
            if (reactionGuids.Length > 0)
            {
                reactionFBXPath = AssetDatabase.GUIDToAssetPath(reactionGuids[0]);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Soldier FBX Import Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This tool helps configure FBX import settings for the Soldier model and animations.\n\n" +
                "The Soldier.fbx should be imported as Humanoid to enable animation retargeting.\n" +
                "Animation files (Reaction.fbx, Strafe.fbx) should copy the avatar from the Soldier model.",
                MessageType.Info);

            EditorGUILayout.Space();

            // Soldier FBX path
            EditorGUILayout.LabelField("Soldier Model", EditorStyles.boldLabel);
            soldierFBXPath = EditorGUILayout.TextField("Path", soldierFBXPath);

            if (GUILayout.Button("Browse Soldier.fbx"))
            {
                string path = EditorUtility.OpenFilePanel("Select Soldier FBX", "Assets", "fbx");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        soldierFBXPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                }
            }

            EditorGUILayout.Space();

            // Reaction FBX path
            EditorGUILayout.LabelField("Reaction Animation", EditorStyles.boldLabel);
            reactionFBXPath = EditorGUILayout.TextField("Path", reactionFBXPath);

            if (GUILayout.Button("Browse Reaction.fbx"))
            {
                string path = EditorUtility.OpenFilePanel("Select Reaction FBX", "Assets", "fbx");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        reactionFBXPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                }
            }

            EditorGUILayout.Space();

            // Source Avatar for animation files
            EditorGUILayout.LabelField("Animation Retargeting", EditorStyles.boldLabel);
            sourceAvatar = (Avatar)EditorGUILayout.ObjectField("Source Avatar", sourceAvatar, typeof(Avatar), false);

            EditorGUILayout.Space();

            if (GUILayout.Button("Apply Settings to Soldier.fbx"))
            {
                ApplySoldierSettings();
            }

            if (GUILayout.Button("Apply Settings to Reaction.fbx"))
            {
                ApplyAnimationSettings(reactionFBXPath);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Reimport All Soldier Assets"))
            {
                ReimportAll();
            }
        }

        private void ApplySoldierSettings()
        {
            if (string.IsNullOrEmpty(soldierFBXPath))
            {
                Debug.LogError("Soldier FBX path is not set!");
                return;
            }

            ModelImporter importer = AssetImporter.GetAtPath(soldierFBXPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"Could not find ModelImporter for {soldierFBXPath}");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.bakeAxisConversion = true;

            importer.SaveAndReimport();
            Debug.Log($"Applied Humanoid settings to {soldierFBXPath}");
        }

        private void ApplyAnimationSettings(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Animation FBX path is not set!");
                return;
            }

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"Could not find ModelImporter for {path}");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;

            if (sourceAvatar != null)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = sourceAvatar;
            }

            importer.bakeAxisConversion = true;

            importer.SaveAndReimport();
            Debug.Log($"Applied animation settings to {path}");
        }

        private void ReimportAll()
        {
            string[] paths = new string[] { soldierFBXPath, reactionFBXPath };

            foreach (var path in paths)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("Reimported all Soldier assets");
        }
    }
}
