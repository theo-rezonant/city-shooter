#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using CityShooter.Environment;
using CityShooter.Navigation;

namespace CityShooter.Editor
{
    /// <summary>
    /// Custom editor tools for setting up and configuring the town environment.
    /// Provides menu items and inspectors for physics and NavMesh setup.
    /// </summary>
    public static class TownEnvironmentEditor
    {
        private const string MENU_ROOT = "City Shooter/Environment/";

        [MenuItem(MENU_ROOT + "Setup Town Environment")]
        public static void SetupTownEnvironment()
        {
            // Find or create the town environment root
            GameObject townRoot = GameObject.Find("Town_Environment");
            if (townRoot == null)
            {
                townRoot = new GameObject("Town_Environment");
                Debug.Log("[TownEnvironmentEditor] Created Town_Environment root object.");
            }

            // Add required components
            if (townRoot.GetComponent<EnvironmentPhysicsSetup>() == null)
            {
                townRoot.AddComponent<EnvironmentPhysicsSetup>();
            }

            if (townRoot.GetComponent<EnvironmentCoordinateConverter>() == null)
            {
                townRoot.AddComponent<EnvironmentCoordinateConverter>();
            }

            if (townRoot.GetComponent<NavMeshSetup>() == null)
            {
                townRoot.AddComponent<NavMeshSetup>();
            }

            if (townRoot.GetComponent<NavMeshSurface>() == null)
            {
                townRoot.AddComponent<NavMeshSurface>();
            }

            // Mark as static
            GameObjectUtility.SetStaticEditorFlags(townRoot,
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.NavigationStatic |
                StaticEditorFlags.OccludeeStatic |
                StaticEditorFlags.OccluderStatic);

            Selection.activeGameObject = townRoot;
            EditorUtility.SetDirty(townRoot);

            Debug.Log("[TownEnvironmentEditor] Town environment setup complete.");
        }

        [MenuItem(MENU_ROOT + "Generate Mesh Colliders")]
        public static void GenerateMeshColliders()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select the environment root object.", "OK");
                return;
            }

            int count = 0;
            MeshFilter[] meshFilters = selected.GetComponentsInChildren<MeshFilter>(true);

            Undo.RecordObjects(meshFilters, "Generate Mesh Colliders");

            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;

                MeshCollider existing = mf.GetComponent<MeshCollider>();
                if (existing == null)
                {
                    MeshCollider mc = Undo.AddComponent<MeshCollider>(mf.gameObject);
                    mc.sharedMesh = mf.sharedMesh;
                    count++;
                }
            }

            Debug.Log($"[TownEnvironmentEditor] Generated {count} mesh colliders.");
            EditorUtility.DisplayDialog("Complete", $"Generated {count} mesh colliders.", "OK");
        }

        [MenuItem(MENU_ROOT + "Mark Selection as Static")]
        public static void MarkSelectionAsStatic()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select objects to mark as static.", "OK");
                return;
            }

            int count = 0;
            foreach (GameObject go in selected)
            {
                MarkObjectAndChildrenStatic(go);
                count++;
            }

            Debug.Log($"[TownEnvironmentEditor] Marked {count} root objects (and children) as static.");
        }

        private static void MarkObjectAndChildrenStatic(GameObject root)
        {
            Undo.RecordObject(root, "Mark Static");

            GameObjectUtility.SetStaticEditorFlags(root,
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.NavigationStatic |
                StaticEditorFlags.OccludeeStatic |
                StaticEditorFlags.OccluderStatic);

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.gameObject != root)
                {
                    Undo.RecordObject(child.gameObject, "Mark Static");
                    GameObjectUtility.SetStaticEditorFlags(child.gameObject,
                        StaticEditorFlags.BatchingStatic |
                        StaticEditorFlags.NavigationStatic |
                        StaticEditorFlags.OccludeeStatic |
                        StaticEditorFlags.OccluderStatic);
                }
            }
        }

        [MenuItem(MENU_ROOT + "Bake NavMesh")]
        public static void BakeNavMesh()
        {
            NavMeshSurface surface = Object.FindObjectOfType<NavMeshSurface>();

            if (surface == null)
            {
                EditorUtility.DisplayDialog("No NavMesh Surface",
                    "Please add a NavMeshSurface component to your environment first.", "OK");
                return;
            }

            Undo.RecordObject(surface, "Bake NavMesh");
            surface.BuildNavMesh();

            // Validate the NavMesh
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            if (triangulation.vertices.Length > 0)
            {
                Debug.Log($"[TownEnvironmentEditor] NavMesh baked successfully. " +
                    $"Vertices: {triangulation.vertices.Length}, " +
                    $"Triangles: {triangulation.indices.Length / 3}");
                EditorUtility.DisplayDialog("Success",
                    $"NavMesh baked successfully.\nVertices: {triangulation.vertices.Length}\nTriangles: {triangulation.indices.Length / 3}", "OK");
            }
            else
            {
                Debug.LogWarning("[TownEnvironmentEditor] NavMesh bake completed but no navigation data generated.");
                EditorUtility.DisplayDialog("Warning",
                    "NavMesh bake completed but no navigation data was generated.\n\nMake sure your environment geometry is marked as Navigation Static.", "OK");
            }
        }

        [MenuItem(MENU_ROOT + "Clear NavMesh")]
        public static void ClearNavMesh()
        {
            NavMeshSurface surface = Object.FindObjectOfType<NavMeshSurface>();

            if (surface != null)
            {
                Undo.RecordObject(surface, "Clear NavMesh");
                surface.RemoveData();
                Debug.Log("[TownEnvironmentEditor] NavMesh data cleared.");
            }
        }

        [MenuItem(MENU_ROOT + "Validate Environment Setup")]
        public static void ValidateEnvironmentSetup()
        {
            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine("=== Town Environment Validation Report ===\n");

            // Check for environment root
            GameObject townRoot = GameObject.Find("Town_Environment");
            if (townRoot != null)
            {
                report.AppendLine("[OK] Town_Environment root found.");
            }
            else
            {
                report.AppendLine("[WARNING] Town_Environment root not found.");
            }

            // Check for mesh colliders
            MeshCollider[] colliders = Object.FindObjectsOfType<MeshCollider>();
            report.AppendLine($"[INFO] Found {colliders.Length} mesh colliders.");

            // Check for NavMesh
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            if (triangulation.vertices.Length > 0)
            {
                report.AppendLine($"[OK] NavMesh exists with {triangulation.vertices.Length} vertices.");
            }
            else
            {
                report.AppendLine("[WARNING] No NavMesh data found. Please bake the NavMesh.");
            }

            // Check for static objects
            int staticCount = 0;
            MeshRenderer[] renderers = Object.FindObjectsOfType<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                if (GameObjectUtility.AreStaticEditorFlagsSet(renderer.gameObject, StaticEditorFlags.NavigationStatic))
                {
                    staticCount++;
                }
            }
            report.AppendLine($"[INFO] {staticCount}/{renderers.Length} mesh renderers are Navigation Static.");

            // Check for required components
            NavMeshSurface surface = Object.FindObjectOfType<NavMeshSurface>();
            report.AppendLine(surface != null ? "[OK] NavMeshSurface component found." : "[WARNING] NavMeshSurface component not found.");

            Debug.Log(report.ToString());
            EditorUtility.DisplayDialog("Validation Report", report.ToString(), "OK");
        }

        [MenuItem(MENU_ROOT + "Configure GLB Import Settings")]
        public static void ConfigureGLBImportSettings()
        {
            string path = EditorUtility.OpenFilePanel("Select GLB File", "map/source", "glb");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            // Convert to relative path
            string relativePath = "Assets" + path.Substring(Application.dataPath.Length);

            ModelImporter importer = AssetImporter.GetAtPath(relativePath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[TownEnvironmentEditor] Could not get importer for: {relativePath}");
                return;
            }

            // Configure import settings
            Undo.RecordObject(importer, "Configure GLB Import");

            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = true;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.addCollider = true;

            // Handle coordinate conversion (Blender Z-up to Unity Y-up)
            importer.bakeAxisConversion = true;

            importer.SaveAndReimport();

            Debug.Log($"[TownEnvironmentEditor] Configured import settings for: {relativePath}");
            EditorUtility.DisplayDialog("Complete",
                $"Import settings configured for:\n{relativePath}\n\nSettings applied:\n- Generate Colliders: ON\n- Axis Conversion: ON\n- Mesh Compression: Medium", "OK");
        }
    }

    /// <summary>
    /// Custom inspector for EnvironmentPhysicsSetup component.
    /// </summary>
    [CustomEditor(typeof(EnvironmentPhysicsSetup))]
    public class EnvironmentPhysicsSetupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EnvironmentPhysicsSetup setup = (EnvironmentPhysicsSetup)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Generate Colliders Now"))
            {
                setup.SetupEnvironmentPhysics();
            }

            if (GUILayout.Button("Remove All Colliders"))
            {
                if (EditorUtility.DisplayDialog("Confirm", "Remove all generated colliders?", "Yes", "No"))
                {
                    setup.RemoveAllColliders();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Generated Colliders: {setup.ColliderCount}", EditorStyles.helpBox);
        }
    }

    /// <summary>
    /// Custom inspector for NavMeshSetup component.
    /// </summary>
    [CustomEditor(typeof(NavMeshSetup))]
    public class NavMeshSetupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            NavMeshSetup setup = (NavMeshSetup)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Setup and Bake NavMesh"))
            {
                setup.SetupAndBuildNavMesh();
            }

            if (GUILayout.Button("Validate NavMesh"))
            {
                if (setup.ValidateNavMesh())
                {
                    EditorUtility.DisplayDialog("Valid", "NavMesh is valid and has navigation data.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid", "NavMesh has no navigation data. Please bake it first.", "OK");
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Ensure all walkable surfaces are marked as Navigation Static before baking.", MessageType.Info);
        }
    }
}
#endif
