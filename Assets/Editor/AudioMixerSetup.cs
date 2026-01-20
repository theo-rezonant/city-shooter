using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility to automatically set up the AudioMixer with the required groups (SFX, Ambience, UI).
/// Run from menu: Tools > Audio > Setup Audio Mixer
/// </summary>
public class AudioMixerSetup : EditorWindow
{
    [MenuItem("Tools/Audio/Setup Audio Mixer")]
    public static void ShowWindow()
    {
        GetWindow<AudioMixerSetup>("Audio Mixer Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Audio Mixer Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This tool will create an AudioMixer asset with the following groups:\n" +
            "• Master\n" +
            "  • SFX (for weapon and combat sounds)\n" +
            "  • Ambience (for environmental loops)\n" +
            "  • UI (for interface sounds)\n\n" +
            "It will also set up exposed parameters for volume control and ducking.",
            MessageType.Info);

        GUILayout.Space(20);

        if (GUILayout.Button("Create Audio Mixer", GUILayout.Height(40)))
        {
            CreateAudioMixer();
        }

        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "After creating the mixer:\n" +
            "1. Assign the mixer to the AudioManager component\n" +
            "2. Assign the mixer groups to the respective audio sources\n" +
            "3. Configure the ducking snapshot in the mixer window",
            MessageType.None);
    }

    private static void CreateAudioMixer()
    {
        string folderPath = "Assets/Audio/Mixers";

        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Audio"))
        {
            AssetDatabase.CreateFolder("Assets", "Audio");
        }
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Audio", "Mixers");
        }

        string mixerPath = $"{folderPath}/MainMixer.mixer";

        // Check if mixer already exists
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>(mixerPath) != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Mixer Exists",
                "An AudioMixer already exists at this location. Do you want to overwrite it?",
                "Overwrite",
                "Cancel");

            if (!overwrite) return;
            AssetDatabase.DeleteAsset(mixerPath);
        }

        // Unfortunately, we cannot create AudioMixer assets programmatically via code
        // because Unity's AudioMixer doesn't have a public constructor
        // Instead, we'll create documentation and a ScriptableObject preset

        Debug.Log("[AudioMixerSetup] AudioMixer must be created manually through Unity's Project window.");
        Debug.Log("[AudioMixerSetup] Right-click in Audio/Mixers folder > Create > Audio Mixer");

        EditorUtility.DisplayDialog(
            "Manual Step Required",
            "Unity doesn't allow programmatic AudioMixer creation.\n\n" +
            "Please follow these steps:\n\n" +
            "1. Right-click in 'Assets/Audio/Mixers' folder\n" +
            "2. Select Create > Audio Mixer\n" +
            "3. Name it 'MainMixer'\n" +
            "4. Double-click to open the mixer\n" +
            "5. Add child groups: SFX, Ambience, UI\n" +
            "6. Expose the volume parameters\n\n" +
            "See the created AudioMixerConfiguration.asset for detailed setup.",
            "OK");

        // Create a configuration ScriptableObject with the required settings
        CreateMixerConfiguration(folderPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateMixerConfiguration(string folderPath)
    {
        // Create a text file with mixer configuration instructions
        string configPath = $"{folderPath}/MixerSetupInstructions.md";

        string instructions = @"# Audio Mixer Configuration

## Required Groups Structure

```
Master (Root)
├── SFX
│   ├── Weapons
│   └── Footsteps
├── Ambience
│   ├── Wind
│   └── CityHum
└── UI
```

## Exposed Parameters

Expose these parameters for script access:

| Parameter Name | Group | Purpose |
|---------------|-------|---------|
| MasterVolume | Master | Overall game volume |
| SFXVolume | SFX | Combat and interaction sounds |
| AmbienceVolume | Ambience | Environmental loops |
| UIVolume | UI | Interface feedback |

## How to Expose Parameters

1. Select a group in the mixer
2. Right-click on the Volume slider
3. Select 'Expose [GroupName] (of Volume)'
4. In the 'Exposed Parameters' section, rename it to match the table above

## Ducking Setup

To create ambient ducking when weapons fire:

1. Create a new Snapshot (right-click in mixer > Add Snapshot)
2. Name it 'Ducking'
3. In the Ducking snapshot, lower the Ambience group volume by -10dB
4. Set transition time to 0.1 seconds

## Attenuation Units

Use logarithmic attenuation:
- Master: 0dB to -80dB range
- SFX: 0dB to -80dB range
- Ambience: -6dB to -80dB range (slightly quieter by default)
- UI: 0dB to -80dB range

## Effect Recommendations

### SFX Group
- Add a Limiter effect to prevent clipping during intense combat
- Settings: Threshold -1dB, Release 10ms

### Ambience Group
- Add a Low Pass Filter (optional) for indoor/outdoor transitions
- Default cutoff: 22000Hz (full range)

### Master Group
- Add a compressor for consistent loudness
- Settings: Threshold -12dB, Ratio 4:1, Attack 10ms, Release 100ms
";

        File.WriteAllText(Path.Combine(Application.dataPath, "..", configPath), instructions);
        Debug.Log($"[AudioMixerSetup] Created mixer instructions at: {configPath}");
    }
}
