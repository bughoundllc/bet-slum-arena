using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Audio utility methods to complement the AudioManager.
/// This class provides helper methods for common audio operations.
/// </summary>
public static class AudioUtility
{
    // Dictionary to cache AudioClips loaded by path
    private static Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();

    /// <summary>
    /// Loads an AudioClip from Resources folder with caching for efficiency.
    /// </summary>
    /// <param name="path">Path to the audio clip within Resources folder</param>
    /// <returns>The loaded AudioClip or null if not found</returns>
    public static AudioClip GetClip(string path)
    {
        // Check if clip is already in cache
        if (_clipCache.TryGetValue(path, out AudioClip clip))
        {
            return clip;
        }

        // Load clip from Resources
        clip = Resources.Load<AudioClip>(path);
        
        if (clip != null)
        {
            // Add to cache
            _clipCache[path] = clip;
        }
        else
        {
            Debug.LogWarning($"AudioUtility: Failed to load clip at path: {path}");
        }

        return clip;
    }

    /// <summary>
    /// Plays a sound effect from a Resources path.
    /// </summary>
    /// <param name="path">Path to the audio clip within Resources folder</param>
    /// <param name="volume">Volume scale (0-1)</param>
    /// <param name="pitch">Pitch adjustment (default 1)</param>
    /// <returns>The AudioSource playing the sound</returns>
    public static AudioSource PlaySFX(string path, float volume = 1f, float pitch = 1f)
    {
        AudioClip clip = GetClip(path);
        if (clip != null)
        {
            return AudioManager.Instance.PlaySFX(clip, volume, pitch);
        }
        return null;
    }

    /// <summary>
    /// Plays a sound effect at a specific position from a Resources path.
    /// </summary>
    /// <param name="path">Path to the audio clip within Resources folder</param>
    /// <param name="position">The position in 3D space</param>
    /// <param name="volume">Volume scale (0-1)</param>
    /// <param name="pitch">Pitch adjustment (default 1)</param>
    /// <returns>The AudioSource playing the sound</returns>
    public static AudioSource PlaySFXAtPosition(string path, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        AudioClip clip = GetClip(path);
        if (clip != null)
        {
            return AudioManager.Instance.PlaySFXAtPosition(clip, position, volume, pitch);
        }
        return null;
    }

    /// <summary>
    /// Plays music from a Resources path with crossfade.
    /// </summary>
    /// <param name="path">Path to the audio clip within Resources folder</param>
    /// <param name="fadeTime">How long to fade between tracks (in seconds)</param>
    /// <param name="volume">Volume scale (0-1)</param>
    public static void PlayMusic(string path, float fadeTime = 1f, float volume = 1f)
    {
        AudioClip clip = GetClip(path);
        if (clip != null)
        {
            AudioManager.Instance.PlayMusic(clip, fadeTime, volume);
        }
    }

    /// <summary>
    /// Clear the audio clip cache to free memory.
    /// Call this during scene transitions or when you need to free memory.
    /// </summary>
    public static void ClearCache()
    {
        _clipCache.Clear();
        Resources.UnloadUnusedAssets();
    }

    /// <summary>
    /// Adjusts a float value to be used for logarithmic volume scaling.
    /// This provides a more natural volume curve for UI sliders.
    /// </summary>
    /// <param name="value">Linear value (0-1)</param>
    /// <returns>Logarithmic value for better volume control</returns>
    public static float LinearToDecibel(float value)
    {
        // Convert linear scale to logarithmic for better volume perception
        value = Mathf.Clamp01(value);
        return value == 0 ? -80f : Mathf.Log10(value) * 20f;
    }

    /// <summary>
    /// Converts decibel value to linear (0-1) range.
    /// </summary>
    /// <param name="dB">Decibel value</param>
    /// <returns>Linear value (0-1)</returns>
    public static float DecibelToLinear(float dB)
    {
        return Mathf.Clamp01(Mathf.Pow(10f, dB / 20f));
    }
} 