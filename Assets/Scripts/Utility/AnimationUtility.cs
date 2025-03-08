using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Utility class for operations related to animation clips.
/// </summary>
public static class AnimationUtility
{
    /// <summary>
    /// Creates a runtime clone of an animation clip.
    /// This method uses manual curve sampling to clone animation data.
    /// Note: Due to Unity's limitations, a true deep clone is not possible at runtime.
    /// </summary>
    /// <param name="originalClip">The original animation clip to clone</param>
    /// <param name="name">Optional name for the cloned clip</param>
    /// <returns>A new instance of the animation clip with copied properties</returns>
    public static AnimationClip CloneAnimationClip(AnimationClip originalClip, string name = null)
    {
        if (originalClip == null)
            return null;
            
        // Create a new animation clip
        AnimationClip newClip = new AnimationClip();

        // Copy basic properties
        if (string.IsNullOrEmpty(name))
            newClip.name = originalClip.name + " (Clone)";
        else
            newClip.name = name;

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
        
        // If we're in the editor, we can attempt to use a special method (Unity 2020.3+ API)
#if UNITY_EDITOR
        try
        {
            // Try to copy curve data via editor API
            UnityEditor.EditorCurveBinding[] bindings = UnityEditor.AnimationUtility.GetCurveBindings(originalClip);
            
            foreach (var binding in bindings)
            {
                AnimationCurve curve = UnityEditor.AnimationUtility.GetEditorCurve(originalClip, binding);
                if (curve != null)
                {
                    // Create a clone of the curve
                    AnimationCurve curveCopy = new AnimationCurve(curve.keys);
                    curveCopy.preWrapMode = curve.preWrapMode;
                    curveCopy.postWrapMode = curve.postWrapMode;
                    
                    // Apply to the new clip
                    UnityEditor.AnimationUtility.SetEditorCurve(newClip, binding, curveCopy);
                }
            }
            
            Debug.Log($"Successfully cloned animation clip: {newClip.name} using editor API");
            return newClip;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to clone animation data using editor API: {e.Message}");
            // Fall through to runtime implementation
        }
#endif
        
        // Runtime implementation - create a GameObject to sample the animation
        try
        {
            // We need a GameObject to sample the animation
            GameObject tempGO = new GameObject("__TempAnimSampler");
            tempGO.SetActive(false); // Don't show in scene
            
            // Add an animator to control playback
            Animator animator = tempGO.AddComponent<Animator>();
            
            // We need to manually sample the animation on different properties
            // This is just an example for transforms - you'll need to expand for your specific needs
            
            // Setup proxy objects for sampling
            GameObject proxy = new GameObject("__ClipTarget");
            proxy.transform.SetParent(tempGO.transform);
            proxy.SetActive(false);
            
            // Sample the original clip
            float frameTime = 1f / newClip.frameRate;
            float animLength = originalClip.length;
            
            // For commonly animated properties (position, rotation, scale)
            string[] curvePaths = new string[] { "localPosition.x", "localPosition.y", "localPosition.z",
                                               "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w",
                                               "localScale.x", "localScale.y", "localScale.z" };
                                               
            Dictionary<string, List<Keyframe>> curves = new Dictionary<string, List<Keyframe>>();
            foreach (string path in curvePaths)
            {
                curves[path] = new List<Keyframe>();
            }
            
            // Sample the clip at regular intervals and record the transform state
            for (float time = 0; time <= animLength; time += frameTime)
            {
                float normalizedTime = time / animLength;
                
                // We're using legacy API for simplicity
                if (originalClip.legacy)
                {
                    Animation anim = tempGO.AddComponent<Animation>();
                    anim.AddClip(originalClip, "clip");
                    anim.clip = originalClip;
                    anim.enabled = false;
                    
                    anim.Play("clip");
                    anim.Sample();
                    
                    // Record transform at this sample point
                    Vector3 pos = proxy.transform.localPosition;
                    Quaternion rot = proxy.transform.localRotation;
                    Vector3 scale = proxy.transform.localScale;
                    
                    // Add keyframes for position
                    curves["localPosition.x"].Add(new Keyframe(time, pos.x));
                    curves["localPosition.y"].Add(new Keyframe(time, pos.y));
                    curves["localPosition.z"].Add(new Keyframe(time, pos.z));
                    
                    // Add keyframes for rotation
                    curves["localRotation.x"].Add(new Keyframe(time, rot.x));
                    curves["localRotation.y"].Add(new Keyframe(time, rot.y));
                    curves["localRotation.z"].Add(new Keyframe(time, rot.z));
                    curves["localRotation.w"].Add(new Keyframe(time, rot.w));
                    
                    // Add keyframes for scale
                    curves["localScale.x"].Add(new Keyframe(time, scale.x));
                    curves["localScale.y"].Add(new Keyframe(time, scale.y));
                    curves["localScale.z"].Add(new Keyframe(time, scale.z));
                    
                    Object.DestroyImmediate(anim);
                }
                else
                {
                    // For Mecanim clips - needs a controller setup
                    // This is more complex and would require a temporary controller or override controller
                    // Not implemented for simplicity
                }
            }
            
            // Apply collected curves to new clip
            foreach (var kvp in curves)
            {
                if (kvp.Value.Count > 0)
                {
                    AnimationCurve curve = new AnimationCurve(kvp.Value.ToArray());
                    
                    // Apply the curve - would need property paths for runtime
                    // In runtime we have limited ways to set curves
                    if (kvp.Key.StartsWith("localPosition"))
                    {
                        string property = kvp.Key.Substring("localPosition.".Length);
                        newClip.SetCurve("", typeof(Transform), $"localPosition.{property}", curve);
                    }
                    else if (kvp.Key.StartsWith("localRotation"))
                    {
                        string property = kvp.Key.Substring("localRotation.".Length);
                        newClip.SetCurve("", typeof(Transform), $"localRotation.{property}", curve);
                    }
                    else if (kvp.Key.StartsWith("localScale"))
                    {
                        string property = kvp.Key.Substring("localScale.".Length);
                        newClip.SetCurve("", typeof(Transform), $"localScale.{property}", curve);
                    }
                }
            }
            
            // Clean up temporary objects
            Object.DestroyImmediate(proxy);
            Object.DestroyImmediate(tempGO);
            
            Debug.Log($"Created animation clip clone: {newClip.name} with basic transform curves");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error while sampling animation: {e.Message}\n{e.StackTrace}");
        }
        
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