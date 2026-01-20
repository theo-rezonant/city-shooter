using UnityEngine;

/// <summary>
/// ScriptableObject to store audio clip collections for different audio types.
/// Allows easy swapping and configuration of audio assets without modifying scripts.
/// </summary>
[CreateAssetMenu(fileName = "AudioClipPreset", menuName = "City Shooter/Audio Clip Preset")]
public class AudioClipPreset : ScriptableObject
{
    [Header("Weapon Audio Clips")]
    [SerializeField] private AudioClip[] laserFireClips;
    [SerializeField] private AudioClip[] laserImpactClips;
    [SerializeField] private AudioClip weaponReadyClip;
    [SerializeField] private AudioClip weaponReloadClip;
    [SerializeField] private AudioClip weaponEmptyClip;

    [Header("Footstep Audio Clips")]
    [SerializeField] private AudioClip[] defaultFootsteps;
    [SerializeField] private AudioClip[] metalFootsteps;
    [SerializeField] private AudioClip[] concreteFootsteps;
    [SerializeField] private AudioClip[] gravelFootsteps;

    [Header("Enemy Audio Clips")]
    [SerializeField] private AudioClip[] enemyFootsteps;
    [SerializeField] private AudioClip[] enemyHitReactions;
    [SerializeField] private AudioClip[] enemyDeathClips;
    [SerializeField] private AudioClip[] enemyAlertClips;
    [SerializeField] private AudioClip[] enemyPainVocals;

    [Header("Environmental Audio Clips")]
    [SerializeField] private AudioClip[] windClips;
    [SerializeField] private AudioClip cityHumClip;
    [SerializeField] private AudioClip[] distantSoundClips;

    // Public accessors
    public AudioClip[] LaserFireClips => laserFireClips;
    public AudioClip[] LaserImpactClips => laserImpactClips;
    public AudioClip WeaponReadyClip => weaponReadyClip;
    public AudioClip WeaponReloadClip => weaponReloadClip;
    public AudioClip WeaponEmptyClip => weaponEmptyClip;

    public AudioClip[] DefaultFootsteps => defaultFootsteps;
    public AudioClip[] MetalFootsteps => metalFootsteps;
    public AudioClip[] ConcreteFootsteps => concreteFootsteps;
    public AudioClip[] GravelFootsteps => gravelFootsteps;

    public AudioClip[] EnemyFootsteps => enemyFootsteps;
    public AudioClip[] EnemyHitReactions => enemyHitReactions;
    public AudioClip[] EnemyDeathClips => enemyDeathClips;
    public AudioClip[] EnemyAlertClips => enemyAlertClips;
    public AudioClip[] EnemyPainVocals => enemyPainVocals;

    public AudioClip[] WindClips => windClips;
    public AudioClip CityHumClip => cityHumClip;
    public AudioClip[] DistantSoundClips => distantSoundClips;

    /// <summary>
    /// Validates that required clips are assigned.
    /// </summary>
    public bool ValidatePreset(out string errorMessage)
    {
        errorMessage = "";

        if (laserFireClips == null || laserFireClips.Length == 0)
        {
            errorMessage = "Laser fire clips are not assigned";
            return false;
        }

        return true;
    }
}
