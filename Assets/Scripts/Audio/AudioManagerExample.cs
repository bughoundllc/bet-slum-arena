using UnityEngine;

/// <summary>
/// Example script demonstrating how to use the AudioManager.
/// This is for reference only and can be deleted when no longer needed.
/// </summary>
public class AudioManagerExample : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip _bettingAndBattleMusic;
    [SerializeField] private AudioClip _scoreScreenMusic;

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float _musicVolume = 0.7f;
    
    [Range(0f, 1f)]
    [SerializeField] private float _sfxVolume = 1f;

    private void Start()
    {
        // Set initial volumes
        AudioManager.Instance.SetMusicVolume(_musicVolume);
        AudioManager.Instance.SetSFXVolume(_sfxVolume);
    }

    // Example of how to use volume controls
    public void SetMusicVolume(float volume)
    {
        _musicVolume = volume;
        AudioManager.Instance.SetMusicVolume(volume);
    }

    public void SetSFXVolume(float volume)
    {
        _sfxVolume = volume;
        AudioManager.Instance.SetSFXVolume(volume);
    }

    // Example of stopping all sounds
    public void StopAllSounds()
    {
        AudioManager.Instance.StopAllSFX();
        AudioManager.Instance.StopMusic(fadeTime: 1f);
    }

    // Example of clearing cache when scene is unloaded
    private void OnDestroy()
    {
        AudioUtility.ClearCache();
    }
} 