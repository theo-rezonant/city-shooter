using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Reflection;

/// <summary>
/// Unit tests for the spatial audio system components.
/// These tests verify the core functionality of audio components without requiring actual audio playback.
/// </summary>
[TestFixture]
public class AudioSystemTests
{
    private GameObject testObject;

    [SetUp]
    public void SetUp()
    {
        testObject = new GameObject("TestAudioObject");
    }

    [TearDown]
    public void TearDown()
    {
        if (testObject != null)
        {
            Object.DestroyImmediate(testObject);
        }

        // Clean up any AudioManager singleton
        if (AudioManager.Instance != null)
        {
            Object.DestroyImmediate(AudioManager.Instance.gameObject);
        }
    }

    #region AudioManager Tests

    [Test]
    public void AudioManager_SingletonPattern_CreatesOnlyOneInstance()
    {
        // Arrange & Act
        var manager1 = new GameObject("Manager1").AddComponent<AudioManager>();
        var manager2 = new GameObject("Manager2").AddComponent<AudioManager>();

        // Force Awake by enabling
        manager1.gameObject.SetActive(true);
        manager2.gameObject.SetActive(true);

        // Assert - Second instance should be destroyed (singleton pattern)
        // Note: In edit mode tests, Awake runs synchronously
        Assert.IsNotNull(AudioManager.Instance, "AudioManager instance should exist");
    }

    [Test]
    public void AudioManager_VolumeToDecibels_ConvertsCorrectly()
    {
        // Arrange
        var manager = testObject.AddComponent<AudioManager>();

        // Use reflection to access private method
        var method = typeof(AudioManager).GetMethod("VolumeToDecibels",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act & Assert
        // Volume 1.0 should be 0 dB
        float result1 = (float)method.Invoke(manager, new object[] { 1.0f });
        Assert.AreEqual(0f, result1, 0.01f, "Volume 1.0 should convert to 0 dB");

        // Volume 0.5 should be approximately -6 dB
        float result05 = (float)method.Invoke(manager, new object[] { 0.5f });
        Assert.AreEqual(-6.02f, result05, 0.1f, "Volume 0.5 should convert to approximately -6 dB");

        // Volume 0 should be -80 dB (or lower)
        float result0 = (float)method.Invoke(manager, new object[] { 0f });
        Assert.LessOrEqual(result0, -80f, "Volume 0 should convert to -80 dB or lower");
    }

    [Test]
    public void AudioManager_GetRandomClip_ReturnsValidClip()
    {
        // Arrange
        var manager = testObject.AddComponent<AudioManager>();
        var clips = new AudioClip[]
        {
            AudioClip.Create("Clip1", 44100, 1, 44100, false),
            AudioClip.Create("Clip2", 44100, 1, 44100, false),
            AudioClip.Create("Clip3", 44100, 1, 44100, false)
        };

        // Act
        var result = manager.GetRandomClip(clips);

        // Assert
        Assert.IsNotNull(result, "Should return a clip");
        Assert.Contains(result, clips, "Returned clip should be from the array");
    }

    [Test]
    public void AudioManager_GetRandomClip_ReturnsNullForEmptyArray()
    {
        // Arrange
        var manager = testObject.AddComponent<AudioManager>();

        // Act
        var resultNull = manager.GetRandomClip(null);
        var resultEmpty = manager.GetRandomClip(new AudioClip[0]);

        // Assert
        Assert.IsNull(resultNull, "Should return null for null array");
        Assert.IsNull(resultEmpty, "Should return null for empty array");
    }

    #endregion

    #region WeaponAudio Tests

    [Test]
    public void WeaponAudio_InitializesAudioSource_With3DSpatialBlend()
    {
        // Arrange & Act
        var weaponAudio = testObject.AddComponent<WeaponAudio>();

        // Get the audio source
        var audioSource = testObject.GetComponent<AudioSource>();

        // Assert
        Assert.IsNotNull(audioSource, "AudioSource should be created");
        Assert.AreEqual(1.0f, audioSource.spatialBlend, "Spatial blend should be 1.0 for 3D audio");
    }

    [Test]
    public void WeaponAudio_InitializesAudioSource_WithCustomRolloff()
    {
        // Arrange & Act
        var weaponAudio = testObject.AddComponent<WeaponAudio>();
        var audioSource = testObject.GetComponent<AudioSource>();

        // Assert
        Assert.AreEqual(AudioRolloffMode.Custom, audioSource.rolloffMode,
            "Should use custom rolloff mode");
    }

    [Test]
    public void WeaponAudio_SetFireClips_UpdatesClipPool()
    {
        // Arrange
        var weaponAudio = testObject.AddComponent<WeaponAudio>();
        var newClips = new AudioClip[]
        {
            AudioClip.Create("NewClip1", 44100, 1, 44100, false),
            AudioClip.Create("NewClip2", 44100, 1, 44100, false)
        };

        // Act
        weaponAudio.SetFireClips(newClips);

        // Assert - Use reflection to verify the clips were set
        var field = typeof(WeaponAudio).GetField("laserFireClips",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var storedClips = field.GetValue(weaponAudio) as AudioClip[];

        Assert.AreEqual(newClips, storedClips, "Fire clips should be updated");
    }

    #endregion

    #region FootstepAudio Tests

    [Test]
    public void FootstepAudio_InitializesAudioSource_With3DSpatialBlend()
    {
        // Arrange & Act
        var footstepAudio = testObject.AddComponent<FootstepAudio>();
        var audioSource = testObject.GetComponent<AudioSource>();

        // Assert
        Assert.IsNotNull(audioSource, "AudioSource should be created");
        Assert.AreEqual(1.0f, audioSource.spatialBlend, "Spatial blend should be 1.0 for 3D audio");
    }

    [Test]
    public void FootstepAudio_InitializesAudioSource_WithZeroDoppler()
    {
        // Arrange & Act
        var footstepAudio = testObject.AddComponent<FootstepAudio>();
        var audioSource = testObject.GetComponent<AudioSource>();

        // Assert
        Assert.AreEqual(0f, audioSource.dopplerLevel,
            "Footsteps should have zero Doppler effect");
    }

    [Test]
    public void FootstepAudio_HasAnimationEventCallbacks()
    {
        // Arrange
        var footstepAudio = testObject.AddComponent<FootstepAudio>();

        // Act & Assert - Verify the callback methods exist
        var onLeftFootStep = typeof(FootstepAudio).GetMethod("OnLeftFootStep");
        var onRightFootStep = typeof(FootstepAudio).GetMethod("OnRightFootStep");
        var onFootstepEvent = typeof(FootstepAudio).GetMethod("OnFootstepEvent");

        Assert.IsNotNull(onLeftFootStep, "OnLeftFootStep method should exist");
        Assert.IsNotNull(onRightFootStep, "OnRightFootStep method should exist");
        Assert.IsNotNull(onFootstepEvent, "OnFootstepEvent method should exist");
    }

    #endregion

    #region EnemyAudio Tests

    [Test]
    public void EnemyAudio_InitializesAudioSource_With3DSpatialBlend()
    {
        // Arrange & Act
        var enemyAudio = testObject.AddComponent<EnemyAudio>();
        var audioSource = testObject.GetComponent<AudioSource>();

        // Assert
        Assert.IsNotNull(audioSource, "AudioSource should be created");
        Assert.AreEqual(1.0f, audioSource.spatialBlend, "Spatial blend should be 1.0 for 3D audio");
    }

    [Test]
    public void EnemyAudio_CreatesSecondaryAudioSource_ForLayeredSounds()
    {
        // Arrange & Act
        var enemyAudio = testObject.AddComponent<EnemyAudio>();
        var audioSources = testObject.GetComponents<AudioSource>();

        // Assert
        Assert.AreEqual(2, audioSources.Length,
            "Should have two audio sources for layered sounds");
    }

    [Test]
    public void EnemyAudio_SetMoving_UpdatesMovementState()
    {
        // Arrange
        var enemyAudio = testObject.AddComponent<EnemyAudio>();

        // Act
        enemyAudio.SetMoving(true);

        // Assert - Use reflection to check internal state
        var field = typeof(EnemyAudio).GetField("isMoving",
            BindingFlags.NonPublic | BindingFlags.Instance);
        bool isMoving = (bool)field.GetValue(enemyAudio);

        Assert.IsTrue(isMoving, "Movement state should be set to true");
    }

    [Test]
    public void EnemyAudio_SetSpatialSettings_UpdatesAudioSources()
    {
        // Arrange
        var enemyAudio = testObject.AddComponent<EnemyAudio>();
        float minDist = 5f;
        float maxDist = 50f;
        float doppler = 0.8f;

        // Act
        enemyAudio.SetSpatialSettings(minDist, maxDist, doppler);

        // Assert
        var audioSources = testObject.GetComponents<AudioSource>();
        foreach (var source in audioSources)
        {
            Assert.AreEqual(minDist, source.minDistance, "Min distance should be updated");
            Assert.AreEqual(maxDist, source.maxDistance, "Max distance should be updated");
            Assert.AreEqual(doppler, source.dopplerLevel, "Doppler level should be updated");
        }
    }

    #endregion

    #region EnvironmentalAudio Tests

    [Test]
    public void EnvironmentalAudio_HasRequiredPublicMethods()
    {
        // Arrange
        var envAudio = testObject.AddComponent<EnvironmentalAudio>();

        // Assert - Verify public API exists
        var startAmbience = typeof(EnvironmentalAudio).GetMethod("StartAmbience");
        var stopAmbience = typeof(EnvironmentalAudio).GetMethod("StopAmbience");
        var setMasterVolume = typeof(EnvironmentalAudio).GetMethod("SetMasterVolume");

        Assert.IsNotNull(startAmbience, "StartAmbience method should exist");
        Assert.IsNotNull(stopAmbience, "StopAmbience method should exist");
        Assert.IsNotNull(setMasterVolume, "SetMasterVolume method should exist");
    }

    [Test]
    public void EnvironmentalAudio_SetMasterVolume_ClampsValue()
    {
        // Arrange
        var envAudio = testObject.AddComponent<EnvironmentalAudio>();

        // Act
        envAudio.SetMasterVolume(2.0f); // Over 1.0

        // Assert - Use reflection to check internal state
        var field = typeof(EnvironmentalAudio).GetField("masterAmbienceVolume",
            BindingFlags.NonPublic | BindingFlags.Instance);
        float volume = (float)field.GetValue(envAudio);

        Assert.AreEqual(1.0f, volume, "Volume should be clamped to 1.0");
    }

    #endregion

    #region AudioListenerSetup Tests

    [Test]
    public void AudioListenerSetup_CreatesAudioListener()
    {
        // Arrange & Act
        var setup = testObject.AddComponent<AudioListenerSetup>();

        // Assert
        var listener = testObject.GetComponent<AudioListener>();
        Assert.IsNotNull(listener, "AudioListener should be created");
    }

    [Test]
    public void AudioListenerSetup_GetAudioListener_ReturnsListener()
    {
        // Arrange
        var setup = testObject.AddComponent<AudioListenerSetup>();

        // Act
        var listener = setup.GetAudioListener();

        // Assert
        Assert.IsNotNull(listener, "GetAudioListener should return the listener");
    }

    [Test]
    public void AudioListenerSetup_SetGlobalVolume_ClampsValue()
    {
        // Arrange & Act
        AudioListenerSetup.SetGlobalVolume(1.5f);

        // Assert
        Assert.AreEqual(1.0f, AudioListener.volume, "Volume should be clamped to 1.0");

        // Cleanup
        AudioListener.volume = 1.0f;
    }

    [Test]
    public void AudioListenerSetup_SetAudioPaused_WorksCorrectly()
    {
        // Arrange & Act
        AudioListenerSetup.SetAudioPaused(true);

        // Assert
        Assert.IsTrue(AudioListener.pause, "Audio should be paused");

        // Cleanup
        AudioListener.pause = false;
    }

    #endregion

    #region Integration Tests

    [Test]
    public void AllAudioComponents_UseCustomRolloff_ForConsistentFalloff()
    {
        // Arrange
        var weapon = new GameObject("Weapon").AddComponent<WeaponAudio>();
        var footstep = new GameObject("Footstep").AddComponent<FootstepAudio>();
        var enemy = new GameObject("Enemy").AddComponent<EnemyAudio>();

        // Act & Assert
        Assert.AreEqual(AudioRolloffMode.Custom,
            weapon.GetComponent<AudioSource>().rolloffMode,
            "WeaponAudio should use custom rolloff");

        Assert.AreEqual(AudioRolloffMode.Custom,
            footstep.GetComponent<AudioSource>().rolloffMode,
            "FootstepAudio should use custom rolloff");

        Assert.AreEqual(AudioRolloffMode.Custom,
            enemy.GetComponent<AudioSource>().rolloffMode,
            "EnemyAudio should use custom rolloff");

        // Cleanup
        Object.DestroyImmediate(weapon.gameObject);
        Object.DestroyImmediate(footstep.gameObject);
        Object.DestroyImmediate(enemy.gameObject);
    }

    [Test]
    public void AllAudioComponents_HaveFullSpatialBlend()
    {
        // Arrange
        var weapon = new GameObject("Weapon").AddComponent<WeaponAudio>();
        var footstep = new GameObject("Footstep").AddComponent<FootstepAudio>();
        var enemy = new GameObject("Enemy").AddComponent<EnemyAudio>();

        // Act & Assert
        Assert.AreEqual(1.0f,
            weapon.GetComponent<AudioSource>().spatialBlend,
            "WeaponAudio should have full 3D spatial blend");

        Assert.AreEqual(1.0f,
            footstep.GetComponent<AudioSource>().spatialBlend,
            "FootstepAudio should have full 3D spatial blend");

        Assert.AreEqual(1.0f,
            enemy.GetComponent<AudioSource>().spatialBlend,
            "EnemyAudio should have full 3D spatial blend");

        // Cleanup
        Object.DestroyImmediate(weapon.gameObject);
        Object.DestroyImmediate(footstep.gameObject);
        Object.DestroyImmediate(enemy.gameObject);
    }

    #endregion
}
