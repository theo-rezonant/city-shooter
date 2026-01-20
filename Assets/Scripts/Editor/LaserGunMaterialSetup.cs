#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CityShooter.Editor
{
    /// <summary>
    /// Editor tool for setting up laser gun materials from the texture assets.
    /// Creates URP Lit materials with proper PBR texture assignments and emission settings.
    /// </summary>
    public class LaserGunMaterialSetup : EditorWindow
    {
        private const string TextureFolder = "Assets/laser-gun/textures";
        private const string MaterialOutputFolder = "Assets/Materials/LaserGun";

        private string[] _subObjectNames = { "barrel", "chain", "fuel", "handle", "pin", "screw" };
        private bool _enableEmission = true;
        private Color _emissionColor = Color.cyan;
        private float _emissionIntensity = 2f;
        private bool _useEmissionForFuelAndBarrel = true;

        [MenuItem("Tools/City Shooter/Setup Laser Gun Materials")]
        public static void ShowWindow()
        {
            GetWindow<LaserGunMaterialSetup>("Laser Gun Material Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Laser Gun Material Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This tool creates URP Lit materials for the laser gun model using the PBR textures in the textures folder.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Settings
            _enableEmission = EditorGUILayout.Toggle("Enable Emission", _enableEmission);

            if (_enableEmission)
            {
                _emissionColor = EditorGUILayout.ColorField("Emission Color", _emissionColor);
                _emissionIntensity = EditorGUILayout.FloatField("Emission Intensity", _emissionIntensity);
                _useEmissionForFuelAndBarrel = EditorGUILayout.Toggle("Only Fuel & Barrel Emission", _useEmissionForFuelAndBarrel);
            }

            EditorGUILayout.Space();

            // Sub-object list
            EditorGUILayout.LabelField("Sub-Objects to Process:", EditorStyles.boldLabel);
            for (int i = 0; i < _subObjectNames.Length; i++)
            {
                EditorGUILayout.LabelField($"  - {_subObjectNames[i]}");
            }

            EditorGUILayout.Space();

            // Action buttons
            if (GUILayout.Button("Create Materials", GUILayout.Height(40)))
            {
                CreateMaterials();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Validate Textures"))
            {
                ValidateTextures();
            }
        }

        private void CreateMaterials()
        {
            // Ensure output folder exists
            if (!AssetDatabase.IsValidFolder(MaterialOutputFolder))
            {
                string[] folders = MaterialOutputFolder.Split('/');
                string currentPath = folders[0];
                for (int i = 1; i < folders.Length; i++)
                {
                    string parentPath = currentPath;
                    currentPath = $"{currentPath}/{folders[i]}";
                    if (!AssetDatabase.IsValidFolder(currentPath))
                    {
                        AssetDatabase.CreateFolder(parentPath, folders[i]);
                    }
                }
            }

            int createdCount = 0;

            foreach (string subObject in _subObjectNames)
            {
                Material mat = CreateMaterialForSubObject(subObject);
                if (mat != null)
                {
                    string materialPath = $"{MaterialOutputFolder}/{subObject}_Mat.mat";
                    AssetDatabase.CreateAsset(mat, materialPath);
                    createdCount++;
                    Debug.Log($"[LaserGunMaterialSetup] Created material: {materialPath}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Material Setup Complete",
                $"Created {createdCount} materials in {MaterialOutputFolder}",
                "OK"
            );
        }

        private Material CreateMaterialForSubObject(string subObjectName)
        {
            // Find URP Lit shader
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
                Debug.LogWarning("[LaserGunMaterialSetup] URP Lit shader not found, using Standard shader.");
            }

            if (shader == null)
            {
                Debug.LogError("[LaserGunMaterialSetup] No suitable shader found!");
                return null;
            }

            Material mat = new Material(shader);
            mat.name = $"{subObjectName}_Mat";

            // Map texture names (handle typo in "barell")
            string textureName = subObjectName == "barrel" ? "barell" : subObjectName;

            // Load textures
            Texture2D baseColor = LoadTexture($"{textureName}_Base_color_sRGB.png");
            Texture2D metallic = LoadTexture($"{textureName}_Metallic_Raw.png");
            Texture2D normal = LoadTexture($"{textureName}_Normal_OpenGL_Raw.png");
            Texture2D roughness = LoadTexture($"{textureName}_Roughness_Raw.png");

            // Assign textures
            if (baseColor != null)
            {
                mat.SetTexture("_BaseMap", baseColor);
                mat.SetTexture("_MainTex", baseColor); // Standard shader fallback
            }

            if (metallic != null)
            {
                mat.SetTexture("_MetallicGlossMap", metallic);
            }

            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }

            // Note: URP Lit uses smoothness (inverted roughness) in the metallic map's alpha
            // For separate roughness texture, we'd need a custom shader or conversion
            // For now, set smoothness value based on assumption
            mat.SetFloat("_Smoothness", 0.5f);

            // Setup emission for fuel and barrel
            if (_enableEmission)
            {
                bool shouldHaveEmission = !_useEmissionForFuelAndBarrel ||
                                          subObjectName == "fuel" ||
                                          subObjectName == "barrel";

                if (shouldHaveEmission)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", _emissionColor * _emissionIntensity);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
            }

            return mat;
        }

        private Texture2D LoadTexture(string textureName)
        {
            string path = $"{TextureFolder}/{textureName}";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (texture == null)
            {
                Debug.LogWarning($"[LaserGunMaterialSetup] Texture not found: {path}");
            }

            return texture;
        }

        private void ValidateTextures()
        {
            int found = 0;
            int missing = 0;

            Debug.Log("[LaserGunMaterialSetup] Validating textures...");

            foreach (string subObject in _subObjectNames)
            {
                string textureName = subObject == "barrel" ? "barell" : subObject;

                string[] textureTypes = {
                    $"{textureName}_Base_color_sRGB.png",
                    $"{textureName}_Metallic_Raw.png",
                    $"{textureName}_Normal_OpenGL_Raw.png",
                    $"{textureName}_Roughness_Raw.png"
                };

                foreach (string texType in textureTypes)
                {
                    string path = $"{TextureFolder}/{texType}";
                    if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null)
                    {
                        Debug.Log($"  [OK] {texType}");
                        found++;
                    }
                    else
                    {
                        Debug.LogWarning($"  [MISSING] {texType}");
                        missing++;
                    }
                }
            }

            EditorUtility.DisplayDialog(
                "Texture Validation",
                $"Found: {found}\nMissing: {missing}",
                "OK"
            );
        }
    }

    /// <summary>
    /// Editor tool for setting up the weapon prefab with all required components.
    /// </summary>
    public class LaserGunPrefabSetup : EditorWindow
    {
        private GameObject _targetWeapon;
        private bool _addAnimator = true;
        private bool _addAudioSource = true;

        [MenuItem("Tools/City Shooter/Setup Laser Gun Prefab")]
        public static void ShowWindow()
        {
            GetWindow<LaserGunPrefabSetup>("Laser Gun Prefab Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Laser Gun Prefab Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Drag the laser gun model here to automatically add all required combat system components.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            _targetWeapon = (GameObject)EditorGUILayout.ObjectField(
                "Target Weapon",
                _targetWeapon,
                typeof(GameObject),
                true
            );

            EditorGUILayout.Space();

            _addAnimator = EditorGUILayout.Toggle("Add Animator", _addAnimator);
            _addAudioSource = EditorGUILayout.Toggle("Add Audio Source", _addAudioSource);

            EditorGUILayout.Space();

            GUI.enabled = _targetWeapon != null;
            if (GUILayout.Button("Setup Weapon Components", GUILayout.Height(40)))
            {
                SetupWeaponComponents();
            }
            GUI.enabled = true;

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Empty Laser Gun Prefab"))
            {
                CreateEmptyPrefab();
            }
        }

        private void SetupWeaponComponents()
        {
            if (_targetWeapon == null)
                return;

            Undo.RecordObject(_targetWeapon, "Setup Laser Gun Components");

            // Add LaserGunController
            if (_targetWeapon.GetComponent<CityShooter.Weapons.LaserGunController>() == null)
            {
                _targetWeapon.AddComponent<CityShooter.Weapons.LaserGunController>();
            }

            // Add LaserBoltVFX
            if (_targetWeapon.GetComponent<CityShooter.Weapons.LaserBoltVFX>() == null)
            {
                _targetWeapon.AddComponent<CityShooter.Weapons.LaserBoltVFX>();
            }

            // Add EmissiveFlashController
            if (_targetWeapon.GetComponent<CityShooter.Weapons.EmissiveFlashController>() == null)
            {
                _targetWeapon.AddComponent<CityShooter.Weapons.EmissiveFlashController>();
            }

            // Add WeaponAnimationController
            if (_targetWeapon.GetComponent<CityShooter.Weapons.WeaponAnimationController>() == null)
            {
                _targetWeapon.AddComponent<CityShooter.Weapons.WeaponAnimationController>();
            }

            // Add ImpactEffectController
            if (_targetWeapon.GetComponent<CityShooter.Weapons.ImpactEffectController>() == null)
            {
                _targetWeapon.AddComponent<CityShooter.Weapons.ImpactEffectController>();
            }

            // Add Animator if requested
            if (_addAnimator && _targetWeapon.GetComponent<Animator>() == null)
            {
                _targetWeapon.AddComponent<Animator>();
            }

            // Add AudioSource if requested
            if (_addAudioSource && _targetWeapon.GetComponent<AudioSource>() == null)
            {
                AudioSource source = _targetWeapon.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f; // 3D sound
            }

            // Create muzzle point
            Transform muzzlePoint = _targetWeapon.transform.Find("MuzzlePoint");
            if (muzzlePoint == null)
            {
                GameObject muzzle = new GameObject("MuzzlePoint");
                muzzle.transform.SetParent(_targetWeapon.transform);
                muzzle.transform.localPosition = Vector3.forward;
                muzzle.transform.localRotation = Quaternion.identity;
            }

            EditorUtility.SetDirty(_targetWeapon);

            EditorUtility.DisplayDialog(
                "Setup Complete",
                "All laser gun components have been added to the weapon.",
                "OK"
            );
        }

        private void CreateEmptyPrefab()
        {
            // Create empty game object with all components
            GameObject laserGun = new GameObject("LaserGun");

            // Add required components
            laserGun.AddComponent<CityShooter.Weapons.LaserGunController>();
            laserGun.AddComponent<CityShooter.Weapons.LaserBoltVFX>();
            laserGun.AddComponent<CityShooter.Weapons.EmissiveFlashController>();
            laserGun.AddComponent<CityShooter.Weapons.WeaponAnimationController>();
            laserGun.AddComponent<CityShooter.Weapons.ImpactEffectController>();

            if (_addAnimator)
            {
                laserGun.AddComponent<Animator>();
            }

            if (_addAudioSource)
            {
                AudioSource source = laserGun.AddComponent<AudioSource>();
                source.playOnAwake = false;
            }

            // Create muzzle point
            GameObject muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(laserGun.transform);
            muzzle.transform.localPosition = Vector3.forward;

            // Ensure prefab folder exists
            string prefabFolder = "Assets/Prefabs/Weapons";
            if (!AssetDatabase.IsValidFolder(prefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Weapons");
            }

            // Save as prefab
            string prefabPath = $"{prefabFolder}/LaserGun.prefab";
            prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

            PrefabUtility.SaveAsPrefabAsset(laserGun, prefabPath);
            DestroyImmediate(laserGun);

            // Select the new prefab
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            EditorUtility.DisplayDialog(
                "Prefab Created",
                $"Laser gun prefab created at:\n{prefabPath}",
                "OK"
            );
        }
    }
}
#endif
