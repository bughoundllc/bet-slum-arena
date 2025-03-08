# AudioManager for NET-SLUM

This simple AudioManager provides a clean API for handling audio in the NET-SLUM project. It's designed to be easily extendable by audio engineers in the future.

## Features

- Singleton pattern for easy access from anywhere
- Separate control for music and sound effects
- Volume controls (master, music, SFX)
- Crossfading between music tracks
- Spatial audio support
- Audio source pooling for efficiency
- Utility functions for common operations

## How to Set Up

1. Create an empty GameObject in your persistent scene (e.g., _BOOT scene)
2. Add the `AudioManager` component to it
3. (Optional) Assign AudioMixerGroups for music and SFX
4. Adjust the initial pool size if needed

## Basic Usage

### Playing Sound Effects

```csharp
// Play a sound effect
AudioClip sfxClip = Resources.Load<AudioClip>("Sounds/MySoundEffect");
AudioManager.Instance.PlaySFX(sfxClip);

// Play with custom volume and pitch
AudioManager.Instance.PlaySFX(sfxClip, volume: 0.5f, pitch: 1.2f);

// Play looping sound effect and store reference to stop it later
AudioSource loopingSource = AudioManager.Instance.PlaySFX(sfxClip, loop: true);
// ...later...
if (loopingSource != null && loopingSource.isPlaying)
    loopingSource.Stop();

// Play spatial sound effect
Vector3 position = new Vector3(1, 0, 2);
AudioManager.Instance.PlaySFXAtPosition(sfxClip, position);
```

### Playing Music

```csharp
// Play background music with crossfade
AudioClip musicClip = Resources.Load<AudioClip>("Music/BackgroundTheme");
AudioManager.Instance.PlayMusic(musicClip);

// Custom fade time and volume
AudioManager.Instance.PlayMusic(musicClip, fadeTime: 2.5f, volume: 0.7f);

// Stop the music with fade out
AudioManager.Instance.StopMusic(fadeTime: 3.0f);
```

### Volume Control

```csharp
// Adjust master volume (affects both music and SFX)
AudioManager.Instance.SetMasterVolume(0.8f);

// Adjust specific volumes
AudioManager.Instance.SetMusicVolume(0.6f);
AudioManager.Instance.SetSFXVolume(1.0f);
```

## Using the AudioUtility Class

The AudioUtility class provides additional helper methods:

```csharp
// Play sound directly from Resources path
AudioUtility.PlaySFX("Sounds/MySoundEffect");

// Play music directly from Resources path
AudioUtility.PlayMusic("Music/BackgroundTheme", fadeTime: 2.0f);

// Clear audio cache on scene transitions
private void OnDestroy()
{
    AudioUtility.ClearCache();
}

// Convert between linear and decibel volume scales
float dbValue = AudioUtility.LinearToDecibel(sliderValue);
float linearValue = AudioUtility.DecibelToLinear(dbValue);
```

## Extending the System

The AudioManager is designed to be easily extendable. Audio engineers can:

1. Add more sophisticated audio mixer setup
2. Implement dynamic audio groups for different types of sounds
3. Add sound prioritization
4. Implement distance-based audio culling
5. Add occlusion and obstruction effects
6. Implement runtime effect processing
7. Add FMOD or Wwise integration

## Best Practices

1. Organize audio clips in the Resources folder (e.g., "Music", "SFX", "Ambient")
2. Use descriptive filenames for audio clips
3. Adjust volume levels in the Unity editor rather than in code when possible
4. For optimal memory management, clear the audio cache during scene transitions
5. Use AudioMixerGroups for better control over different audio categories 