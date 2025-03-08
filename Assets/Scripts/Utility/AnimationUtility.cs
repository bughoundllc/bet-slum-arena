using UnityEngine;
using System.Reflection;

/// <summary>
/// Utility class for operations related to animation clips.
/// </summary>
public static class AnimationUtility
{
    /// <summary>
    /// Creates a runtime clone of an animation clip.
    /// This method provides the best available clone of an animation clip at runtime.
    /// Note: Due to Unity's limitations, a true deep clone is not possible at runtime.
    /// </summary>
    /// <param name="originalClip">The original animation clip to clone</param>
    /// <returns>A new instance of the animation clip with copied properties</returns>
    public static AnimationClip CloneAnimationClip(AnimationClip originalClip)
    {
        if (originalClip == null)
            return null;
            
        // Create a new animation clip
        AnimationClip newClip = new AnimationClip();
        
        // Copy basic properties
        newClip.name = originalClip.name + " (Clone)";
        newClip.frameRate = originalClip.frameRate;
        newClip.legacy = originalClip.legacy;
        newClip.wrapMode = originalClip.wrapMode;
        newClip.localBounds = originalClip.localBounds;
        
        // Copy animation events
        if (originalClip.events != null && originalClip.events.Length > 0)
        {
            AnimationEvent[] newEvents = new AnimationEvent[originalClip.events.Length];
            for (int i = 0; i < originalClip.events.Length; i++)
            {
                AnimationEvent evt = originalClip.events[i];
                AnimationEvent newEvent = new AnimationEvent
                {
                    functionName = evt.functionName,
                    time = evt.time,
                    floatParameter = evt.floatParameter,
                    intParameter = evt.intParameter,
                    stringParameter = evt.stringParameter,
                    objectReferenceParameter = evt.objectReferenceParameter
                };
                newEvents[i] = newEvent;
            }
            newClip.events = newEvents;
        }
        
        // Although we can't access the animation curves directly at runtime,
        // Unity's AnimationClip.CopyFrom method can be used to copy the animation data
        // This is undocumented but works in many cases
        // Note: This might not work for all custom curves and properties
        try
        {
            System.Type type = newClip.GetType();
            System.Reflection.MethodInfo method = type.GetMethod("CopyFrom", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            if (method != null)
            {
                method.Invoke(newClip, new object[] { originalClip });
                Debug.Log($"Successfully cloned animation clip: {newClip.name} using internal CopyFrom method");
                return newClip;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to clone animation data: {e.Message}");
        }
        
        // If the reflection approach fails, we use AnimationClip.SampleAnimation
        // via a more complex approach (not implemented here)
        
        Debug.LogWarning("Created partial animation clip clone with same properties but without complete curve data. " +
                         "A complete runtime clone requires a more complex implementation or pre-processing.");
        
        return newClip;
    }
    
    /// <summary>
    /// Creates an AnimatorOverrideController that overrides the specified clip.
    /// This is useful for runtime animation modifications.
    /// </summary>
    /// <param name="originalController">The original controller to base the override on</param>
    /// <param name="clipName">The name of the clip to override</param>
    /// <param name="newClip">The new clip to use</param>
    /// <returns>A new AnimatorOverrideController with the specified clip replaced</returns>
    public static AnimatorOverrideController CreateAnimationOverride(RuntimeAnimatorController originalController, string clipName, AnimationClip newClip)
    {
        if (originalController == null || string.IsNullOrEmpty(clipName) || newClip == null)
            return null;
            
        // Create a new override controller based on the original
        AnimatorOverrideController overrideController = new AnimatorOverrideController(originalController);
        
        // Override the clip
        overrideController[clipName] = newClip;
        
        return overrideController;
    }
} 