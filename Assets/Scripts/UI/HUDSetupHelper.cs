using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CityShooter.UI
{
    /// <summary>
    /// Editor helper script to create the complete HUD prefab structure.
    /// Run from context menu: City Shooter > Create HUD Canvas
    /// </summary>
    public class HUDSetupHelper : MonoBehaviour
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("City Shooter/Create HUD Canvas")]
        public static void CreateHUDCanvas()
        {
            // Create main Canvas
            GameObject canvasGO = new GameObject("HUD_Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // Configure CanvasScaler for responsive UI
            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Add HUDManager
            HUDManager hudManager = canvasGO.AddComponent<HUDManager>();

            // Create Crosshair
            GameObject crosshairGO = CreateCrosshair(canvasGO.transform);
            DynamicCrosshair crosshair = crosshairGO.GetComponent<DynamicCrosshair>();

            // Create Health Bar
            GameObject healthGO = CreateHealthBar(canvasGO.transform);
            HealthBar healthBar = healthGO.GetComponent<HealthBar>();

            // Create Ammo Counter
            GameObject ammoGO = CreateAmmoCounter(canvasGO.transform);
            AmmoCounter ammoCounter = ammoGO.GetComponent<AmmoCounter>();

            // Create Hit Marker
            GameObject hitMarkerGO = CreateHitMarker(canvasGO.transform);
            HitMarker hitMarker = hitMarkerGO.GetComponent<HitMarker>();

            // Create Damage Indicator
            GameObject damageGO = CreateDamageIndicator(canvasGO.transform);
            DamageIndicator damageIndicator = damageGO.GetComponent<DamageIndicator>();

            // Wire up references using SerializedObject
            UnityEditor.SerializedObject serializedHUD = new UnityEditor.SerializedObject(hudManager);
            serializedHUD.FindProperty("crosshair").objectReferenceValue = crosshair;
            serializedHUD.FindProperty("healthBar").objectReferenceValue = healthBar;
            serializedHUD.FindProperty("ammoCounter").objectReferenceValue = ammoCounter;
            serializedHUD.FindProperty("hitMarker").objectReferenceValue = hitMarker;
            serializedHUD.FindProperty("damageIndicator").objectReferenceValue = damageIndicator;
            serializedHUD.FindProperty("hudCanvas").objectReferenceValue = canvas;
            serializedHUD.ApplyModifiedProperties();

            // Select the created canvas
            UnityEditor.Selection.activeGameObject = canvasGO;

            Debug.Log("[HUDSetupHelper] HUD Canvas created successfully!");
        }

        private static GameObject CreateCrosshair(Transform parent)
        {
            GameObject container = new GameObject("Crosshair");
            container.transform.SetParent(parent, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(100, 100);

            DynamicCrosshair crosshair = container.AddComponent<DynamicCrosshair>();

            // Center dot
            GameObject centerDot = CreateUIImage("CenterDot", container.transform, Color.cyan, new Vector2(4, 4));

            // Lines
            GameObject topLine = CreateUIImage("TopLine", container.transform, Color.cyan, new Vector2(2, 12));
            GameObject bottomLine = CreateUIImage("BottomLine", container.transform, Color.cyan, new Vector2(2, 12));
            GameObject leftLine = CreateUIImage("LeftLine", container.transform, Color.cyan, new Vector2(12, 2));
            GameObject rightLine = CreateUIImage("RightLine", container.transform, Color.cyan, new Vector2(12, 2));

            // Wire up references
            UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(crosshair);
            serialized.FindProperty("crosshairContainer").objectReferenceValue = containerRect;
            serialized.FindProperty("centerDot").objectReferenceValue = centerDot.GetComponent<Image>();
            serialized.FindProperty("topLine").objectReferenceValue = topLine.GetComponent<Image>();
            serialized.FindProperty("bottomLine").objectReferenceValue = bottomLine.GetComponent<Image>();
            serialized.FindProperty("leftLine").objectReferenceValue = leftLine.GetComponent<Image>();
            serialized.FindProperty("rightLine").objectReferenceValue = rightLine.GetComponent<Image>();
            serialized.ApplyModifiedProperties();

            return container;
        }

        private static GameObject CreateHealthBar(Transform parent)
        {
            GameObject container = new GameObject("HealthBar");
            container.transform.SetParent(parent, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(0, 0);
            containerRect.pivot = new Vector2(0, 0);
            containerRect.anchoredPosition = new Vector2(50, 50);
            containerRect.sizeDelta = new Vector2(300, 40);

            HealthBar healthBar = container.AddComponent<HealthBar>();

            // Background
            GameObject background = CreateUIImage("Background", container.transform,
                new Color(0.1f, 0.1f, 0.1f, 0.6f), new Vector2(300, 40));
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Damage fill (behind main fill)
            GameObject damageFill = CreateUIImage("DamageFill", container.transform,
                new Color(1f, 0.5f, 0f, 0.8f), new Vector2(300, 40));
            Image damageFillImg = damageFill.GetComponent<Image>();
            damageFillImg.type = Image.Type.Filled;
            damageFillImg.fillMethod = Image.FillMethod.Horizontal;
            RectTransform damageRect = damageFill.GetComponent<RectTransform>();
            damageRect.anchorMin = Vector2.zero;
            damageRect.anchorMax = Vector2.one;
            damageRect.sizeDelta = Vector2.zero;

            // Health fill
            GameObject healthFill = CreateUIImage("HealthFill", container.transform, Color.cyan, new Vector2(300, 40));
            Image healthFillImg = healthFill.GetComponent<Image>();
            healthFillImg.type = Image.Type.Filled;
            healthFillImg.fillMethod = Image.FillMethod.Horizontal;
            RectTransform fillRect = healthFill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            // Health text
            GameObject textGO = new GameObject("HealthText");
            textGO.transform.SetParent(container.transform, false);
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI healthText = textGO.AddComponent<TextMeshProUGUI>();
            healthText.text = "100/100";
            healthText.fontSize = 18;
            healthText.alignment = TextAlignmentOptions.Center;
            healthText.color = Color.white;

            // Wire up references
            UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(healthBar);
            serialized.FindProperty("healthBarContainer").objectReferenceValue = containerRect;
            serialized.FindProperty("healthFillImage").objectReferenceValue = healthFillImg;
            serialized.FindProperty("healthBackgroundImage").objectReferenceValue = background.GetComponent<Image>();
            serialized.FindProperty("damageFillImage").objectReferenceValue = damageFillImg;
            serialized.FindProperty("healthText").objectReferenceValue = healthText;
            serialized.ApplyModifiedProperties();

            return container;
        }

        private static GameObject CreateAmmoCounter(Transform parent)
        {
            GameObject container = new GameObject("AmmoCounter");
            container.transform.SetParent(parent, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(1, 0);
            containerRect.anchorMax = new Vector2(1, 0);
            containerRect.pivot = new Vector2(1, 0);
            containerRect.anchoredPosition = new Vector2(-50, 50);
            containerRect.sizeDelta = new Vector2(200, 60);

            AmmoCounter ammoCounter = container.AddComponent<AmmoCounter>();

            // Background
            GameObject background = CreateUIImage("Background", container.transform,
                new Color(0.1f, 0.1f, 0.1f, 0.6f), new Vector2(200, 60));
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Ammo fill
            GameObject ammoFill = CreateUIImage("AmmoFill", container.transform, Color.cyan, new Vector2(200, 60));
            Image ammoFillImg = ammoFill.GetComponent<Image>();
            ammoFillImg.type = Image.Type.Filled;
            ammoFillImg.fillMethod = Image.FillMethod.Horizontal;
            RectTransform fillRect = ammoFill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            // Ammo text
            GameObject textGO = new GameObject("AmmoText");
            textGO.transform.SetParent(container.transform, false);
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI ammoText = textGO.AddComponent<TextMeshProUGUI>();
            ammoText.text = "030";
            ammoText.fontSize = 32;
            ammoText.alignment = TextAlignmentOptions.Center;
            ammoText.color = Color.cyan;

            // Wire up references
            UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(ammoCounter);
            serialized.FindProperty("ammoContainer").objectReferenceValue = containerRect;
            serialized.FindProperty("ammoFillImage").objectReferenceValue = ammoFillImg;
            serialized.FindProperty("ammoBackgroundImage").objectReferenceValue = background.GetComponent<Image>();
            serialized.FindProperty("ammoText").objectReferenceValue = ammoText;
            serialized.ApplyModifiedProperties();

            return container;
        }

        private static GameObject CreateHitMarker(Transform parent)
        {
            GameObject container = new GameObject("HitMarker");
            container.transform.SetParent(parent, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(50, 50);

            HitMarker hitMarker = container.AddComponent<HitMarker>();
            CanvasGroup canvasGroup = container.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // Create X-pattern lines
            Color hitColor = Color.cyan;
            GameObject topLeft = CreateUIImage("TopLeft", container.transform, hitColor, new Vector2(2, 10));
            GameObject topRight = CreateUIImage("TopRight", container.transform, hitColor, new Vector2(2, 10));
            GameObject bottomLeft = CreateUIImage("BottomLeft", container.transform, hitColor, new Vector2(2, 10));
            GameObject bottomRight = CreateUIImage("BottomRight", container.transform, hitColor, new Vector2(2, 10));

            // Position and rotate for X pattern
            float offset = 8f * 0.707f;
            topLeft.GetComponent<RectTransform>().anchoredPosition = new Vector2(-offset, offset);
            topRight.GetComponent<RectTransform>().anchoredPosition = new Vector2(offset, offset);
            bottomLeft.GetComponent<RectTransform>().anchoredPosition = new Vector2(-offset, -offset);
            bottomRight.GetComponent<RectTransform>().anchoredPosition = new Vector2(offset, -offset);

            topLeft.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, -45);
            topRight.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, 45);
            bottomLeft.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, 45);
            bottomRight.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, -45);

            // Wire up references
            UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(hitMarker);
            serialized.FindProperty("hitMarkerContainer").objectReferenceValue = containerRect;
            serialized.FindProperty("topLeftLine").objectReferenceValue = topLeft.GetComponent<Image>();
            serialized.FindProperty("topRightLine").objectReferenceValue = topRight.GetComponent<Image>();
            serialized.FindProperty("bottomLeftLine").objectReferenceValue = bottomLeft.GetComponent<Image>();
            serialized.FindProperty("bottomRightLine").objectReferenceValue = bottomRight.GetComponent<Image>();
            serialized.FindProperty("hitMarkerCanvasGroup").objectReferenceValue = canvasGroup;
            serialized.ApplyModifiedProperties();

            container.SetActive(false);

            return container;
        }

        private static GameObject CreateDamageIndicator(Transform parent)
        {
            GameObject container = new GameObject("DamageIndicator");
            container.transform.SetParent(parent, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;

            DamageIndicator damageIndicator = container.AddComponent<DamageIndicator>();

            // Create indicator template (hidden)
            GameObject template = CreateUIImage("IndicatorTemplate", container.transform,
                new Color(1f, 0.2f, 0.2f, 0.8f), new Vector2(40, 80));
            RectTransform templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0.5f, 0.5f);
            templateRect.anchorMax = new Vector2(0.5f, 0.5f);
            templateRect.pivot = new Vector2(0.5f, 0f);
            template.SetActive(false);

            // Create vignette
            GameObject vignette = new GameObject("Vignette");
            vignette.transform.SetParent(parent, false);
            vignette.transform.SetAsFirstSibling(); // Behind other elements
            RectTransform vignetteRect = vignette.AddComponent<RectTransform>();
            vignetteRect.anchorMin = Vector2.zero;
            vignetteRect.anchorMax = Vector2.one;
            vignetteRect.sizeDelta = Vector2.zero;
            Image vignetteImage = vignette.AddComponent<Image>();
            vignetteImage.color = new Color(1f, 0f, 0f, 0f);

            // Wire up references
            UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(damageIndicator);
            serialized.FindProperty("indicatorContainer").objectReferenceValue = containerRect;
            serialized.FindProperty("indicatorTemplate").objectReferenceValue = template.GetComponent<Image>();
            serialized.FindProperty("vignetteImage").objectReferenceValue = vignetteImage;
            serialized.ApplyModifiedProperties();

            return container;
        }

        private static GameObject CreateUIImage(string name, Transform parent, Color color, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            Image image = go.AddComponent<Image>();
            image.color = color;

            return go;
        }
#endif
    }
}
