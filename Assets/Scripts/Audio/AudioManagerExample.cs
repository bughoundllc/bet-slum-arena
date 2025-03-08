using UnityEngine;

/// <summary>
/// Example script demonstrating how to use the AudioManager.
/// This is for reference only and can be deleted when no longer needed.
/// </summary>
public class AudioManagerExample : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip _backgroundMusic;
    [SerializeField] private AudioClip _buttonClickSound;
    [SerializeField] private AudioClip _explosionSound;

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
        
        // Start background music
        if (_backgroundMusic != null)
        {
            AudioManager.Instance.PlayMusic(_backgroundMusic, fadeTime: 2f);
        }
        else
        {
            // Example of using AudioUtility to play from Resources
            AudioUtility.PlayMusic("Audio/Music/BackgroundTheme", fadeTime: 2f);
        }
    }

    // Example method that could be called from a UI button
    public void OnButtonClick()
    {
        if (_buttonClickSound != null)
        {
            AudioManager.Instance.PlaySFX(_buttonClickSound);
        }
        else
        {
            // Example of using AudioUtility to play from Resources
            AudioUtility.PlaySFX("Audio/SFX/ButtonClick");
        }
    }

    // Example method to play a 3D sound at a position
    public void PlayExplosionAt(Vector3 position)
    {
        if (_explosionSound != null)
        {
            AudioManager.Instance.PlaySFXAtPosition(_explosionSound, position);
        }
        else
        {
            // Example of using AudioUtility to play from Resources
            AudioUtility.PlaySFXAtPosition("Audio/SFX/Explosion", position);
        }
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