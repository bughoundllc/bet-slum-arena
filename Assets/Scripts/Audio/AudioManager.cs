using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// AudioManager singleton that handles all audio playback in the application.
/// This system is designed to be simple and easily extendable by audio engineers.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;

    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Find existing instance
                _instance = FindObjectOfType<AudioManager>();

                // If no instance exists, create one
                if (_instance == null)
                {
                    GameObject audioManagerObj = new GameObject("AudioManager");
                    _instance = audioManagerObj.AddComponent<AudioManager>();
                    DontDestroyOnLoad(audioManagerObj);
                    Debug.Log("AudioManager: Created new instance");
                }
            }
            return _instance;
        }
    }

    // Audio source pools
    [SerializeField] private int initialPoolSize = 5;
    private List<AudioSource> _musicSources = new List<AudioSource>();
    private List<AudioSource> _sfxSources = new List<AudioSource>();
    
    // Audio mixer settings (can be configured in the Unity Editor)
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    // Volume controls
    [Range(0f, 1f)]
    [SerializeField] private float _masterVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float _musicVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float _sfxVolume = 1f;

    // Currently playing background music
    private AudioSource _currentMusicSource;

    private void Awake()
    {
        // Singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize audio source pools
        InitializeAudioSources();
    }

    private void InitializeAudioSources()
    {
        // Create initial pool of audio sources
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateAudioSource("MusicSource_" + i, ref _musicSources, musicMixerGroup);
            CreateAudioSource("SFXSource_" + i, ref _sfxSources, sfxMixerGroup);
        }
    }

    private void CreateAudioSource(string name, ref List<AudioSource> sourceList, AudioMixerGroup mixerGroup)
    {
        GameObject sourceObj = new GameObject(name);
        sourceObj.transform.parent = transform;
        
        AudioSource source = sourceObj.AddComponent<AudioSource>();
        source.playOnAwake = false;
        
        if (mixerGroup != null)
            source.outputAudioMixerGroup = mixerGroup;
            
        sourceList.Add(source);
    }

    #region Public API

    /// <summary>
    /// Play a sound effect once.
    /// </summary>
    /// <param name="clip">The audio clip to play</param>
    /// <param name="volume">Volume scale (0-1)</param>
    /// <param name="pitch">Pitch adjustment (default 1)</param>
    /// <param name="loop">Whether the sound should loop</param>
    /// <returns>The AudioSource playing the sound (can be used to stop it later)</returns>
    public AudioSource PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (clip == null)
            return null;

        // Get available audio source
        AudioSource source = GetAvailableSource(_sfxSources);
        if (source == null)
        {
            // Create new source if needed
            CreateAudioSource("SFXSource_" + _sfxSources.Count, ref _sfxSources, sfxMixerGroup);
            source = _sfxSources[_sfxSources.Count - 1];
        }

        // Configure and play
        source.clip = clip;
        source.volume = volume * _sfxVolume * _masterVolume;
        source.pitch = pitch;
        source.loop = loop;
        source.Play();

        return source;
    }

    /// <summary>
    /// Play a sound effect at a specific position in 3D space.
    /// </summary>
    /// <param name="clip">The audio clip to play</param>
    /// <param name="position">The position in 3D space</param>
    /// <param name="volume">Volume scale (0-1)</param>
    /// <param name="pitch">Pitch adjustment (default 1)</param>
    /// <param name="spatialBlend">How much the sound is 3D (0=2D, 1=3D)</param>
    /// <returns>The AudioSource playing the sound</returns>
    public AudioSource PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = 1f)
    {
        if (clip == null)
            return null;

        // Get available audio source
        AudioSource source = GetAvailableSource(_sfxSources);
        if (source == null)
        {
            // Create new source if needed
            CreateAudioSource("SFXSource_" + _sfxSources.Count, ref _sfxSources, sfxMixerGroup);
            source = _sfxSources[_sfxSources.Count - 1];
        }

        // Move to position and configure
        source.gameObject.transform.position = position;
        source.clip = clip;
        source.volume = volume * _sfxVolume * _masterVolume;
        source.pitch = pitch;
        source.spatialBlend = spatialBlend;
        source.Play();

        return source;
    }

    /// <summary>
    /// Play background music with optional crossfade.
    /// </summary>
    /// <param name="clip">The music clip to play</param>
    /// <param name="fadeTime">How long to fade between tracks (in seconds)</param>
    /// <param name="volume">Volume scale (0-1)</param>
    /// <param name="loop">Whether to loop the music (default true)</param>
    public void PlayMusic(AudioClip clip, float fadeTime = 1f, float volume = 1f, bool loop = true)
    {
        if (clip == null)
            return;

        // If already playing this music, just return
        if (_currentMusicSource != null && _currentMusicSource.clip == clip && _currentMusicSource.isPlaying)
            return;

        // Start coroutine to crossfade
        StartCoroutine(CrossfadeMusic(clip, fadeTime, volume, loop));
    }

    /// <summary>
    /// Stop all sound effects.
    /// </summary>
    public void StopAllSFX()
    {
        foreach (AudioSource source in _sfxSources)
        {
            if (source.isPlaying)
                source.Stop();
        }
    }

    /// <summary>
    /// Stop the currently playing music.
    /// </summary>
    /// <param name="fadeTime">How long to fade out (in seconds)</param>
    public void StopMusic(float fadeTime = 1f)
    {
        if (_currentMusicSource != null && _currentMusicSource.isPlaying)
        {
            StartCoroutine(FadeOut(_currentMusicSource, fadeTime));
        }
    }

    /// <summary>
    /// Set the master volume (affects all audio).
    /// </summary>
    /// <param name="volume">Volume level (0-1)</param>
    public void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    /// <summary>
    /// Set the music volume.
    /// </summary>
    /// <param name="volume">Volume level (0-1)</param>
    public void SetMusicVolume(float volume)
    {
        _musicVolume = Mathf.Clamp01(volume);
        UpdateMusicVolumes();
    }

    /// <summary>
    /// Set the SFX volume.
    /// </summary>
    /// <param name="volume">Volume level (0-1)</param>
    public void SetSFXVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        UpdateSFXVolumes();
    }

    #endregion

    #region Helper Methods

    private AudioSource GetAvailableSource(List<AudioSource> sourceList)
    {
        // First look for an audio source that's not playing
        foreach (AudioSource source in sourceList)
        {
            if (!source.isPlaying)
                return source;
        }

        // If all are playing, create a new one or return null
        return null;
    }

    private void UpdateAllVolumes()
    {
        UpdateMusicVolumes();
        UpdateSFXVolumes();
    }

    private void UpdateMusicVolumes()
    {
        foreach (AudioSource source in _musicSources)
        {
            if (source.isPlaying)
                source.volume = source.volume / (_musicVolume * _masterVolume) * _musicVolume * _masterVolume;
        }
    }

    private void UpdateSFXVolumes()
    {
        foreach (AudioSource source in _sfxSources)
        {
            if (source.isPlaying)
                source.volume = source.volume / (_sfxVolume * _masterVolume) * _sfxVolume * _masterVolume;
        }
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip, float fadeTime, float volume, bool loop)
    {
        // Get a new music source
        AudioSource newSource = GetAvailableSource(_musicSources);
        if (newSource == null)
        {
            // Create new source if needed
            CreateAudioSource("MusicSource_" + _musicSources.Count, ref _musicSources, musicMixerGroup);
            newSource = _musicSources[_musicSources.Count - 1];
        }

        // Set up the new music source
        newSource.clip = newClip;
        newSource.loop = loop;
        newSource.volume = 0f;
        newSource.Play();

        // Fade out old music if it exists
        AudioSource oldSource = _currentMusicSource;
        float startVolume = oldSource != null ? oldSource.volume : 0f;
        float timer = 0f;

        // Perform crossfade
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float t = timer / fadeTime;

            // Fade in new music
            newSource.volume = Mathf.Lerp(0f, volume * _musicVolume * _masterVolume, t);

            // Fade out old music
            if (oldSource != null && oldSource.isPlaying)
                oldSource.volume = Mathf.Lerp(startVolume, 0f, t);

            yield return null;
        }

        // Ensure final volumes are set correctly
        newSource.volume = volume * _musicVolume * _masterVolume;
        
        // Stop the old source
        if (oldSource != null && oldSource.isPlaying)
            oldSource.Stop();

        // Set new source as current
        _currentMusicSource = newSource;
    }

    private IEnumerator FadeOut(AudioSource source, float fadeTime)
    {
        float startVolume = source.volume;
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, timer / fadeTime);
            yield return null;
        }

        source.Stop();
        source.volume = startVolume;
    }

    #endregion
} 