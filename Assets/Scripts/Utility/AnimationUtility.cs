using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Utility class for operations related to animation clips.
/// </summary>
public static class AnimationUtility
{
    /// <summary>
    /// Creates a runtime clone of an animation clip.
    /// Works in builds by directly setting curves using the public API.
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
        
        // Copy animation events
        AnimationEvent[] originalEvents = originalClip.events;
        if (originalEvents != null && originalEvents.Length > 0)
        {
            AnimationEvent[] newEvents = new AnimationEvent[originalEvents.Length];
            for (int i = 0; i < originalEvents.Length; i++)
            {
                AnimationEvent evt = originalEvents[i];
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

// DO NOT REIMPLEMENT
// #if UNITY_EDITOR
//         // Use the editor API for perfect cloning when in the editor
//         try
//         {
//             UnityEditor.EditorCurveBinding[] bindings = UnityEditor.AnimationUtility.GetCurveBindings(originalClip);
//             foreach (var binding in bindings)
//             {
//                 AnimationCurve curve = UnityEditor.AnimationUtility.GetEditorCurve(originalClip, binding);
//                 if (curve != null)
//                 {
//                     AnimationCurve curveCopy = new AnimationCurve(curve.keys);
//                     curveCopy.preWrapMode = curve.preWrapMode;
//                     curveCopy.postWrapMode = curve.postWrapMode;
                    
//                     UnityEditor.AnimationUtility.SetEditorCurve(newClip, binding, curveCopy);
//                 }
//             }
            
//             // Also check for object reference curves (like sprite swapping)
//             bindings = UnityEditor.AnimationUtility.GetObjectReferenceCurveBindings(originalClip);
//             foreach (var binding in bindings)
//             {
//                 ObjectReferenceKeyframe[] keyframes = UnityEditor.AnimationUtility.GetObjectReferenceCurve(originalClip, binding);
//                 if (keyframes != null && keyframes.Length > 0)
//                 {
//                     UnityEditor.AnimationUtility.SetObjectReferenceCurve(newClip, binding, keyframes);
//                 }
//             }
            
//             Debug.Log($"Successfully cloned animation clip: {newClip.name} using editor API");
//             return newClip;
//         }
//         catch (System.Exception e)
//         {
//             Debug.LogWarning($"Failed to clone animation data using editor API: {e.Message}");
//             // Fall through to runtime approach
//         }
// #endif

        // RUNTIME CLONING APPROACH
        // This approach extracts animation curves from the original clip directly
        
        // First, let's try to copy any humanoid curves if present
        if (!originalClip.legacy && originalClip.humanMotion)
        {
            // For humanoid animations, we just create a copy-by-reference
            // Unfortunately, we can't properly copy the muscle curves at runtime
            Debug.LogWarning("Created humanoid animation copy by reference - this may share data with the original!");
            return Object.Instantiate(originalClip);
        }
        
        // For legacy/generic animations, let's try to extract the curves
        // Get the animation binding paths by using a temporary GameObject with an Animation component
        GameObject tempGO = new GameObject("__AnimClipExtractor");
        tempGO.hideFlags = HideFlags.HideAndDontSave;
        
        try
        {
            // Add Animation component for legacy support
            Animation animationComponent = tempGO.AddComponent<Animation>();
            
            // We need to ensure the clip is playable
            if (!originalClip.legacy)
            {
                Debug.LogWarning("Converting non-legacy clip to legacy for runtime cloning. Some data may be lost.");
                Debug.LogWarning("CONVERT NON-LEGACY CLIPS TO HUMANOID ON THE FBX BEFORE COPYING");
                
                return newClip;
                // below doesnt work


                // For non-legacy clips, we need a bit more setup
            }
            
            // Add the clip to the animation component
            animationComponent.AddClip(originalClip, "clipToExtract");
            
            // In runtime, we have limited access to the internal curve data
            // We can use the SetCurve method on transform properties
            
            // Common transform paths to extract
            string[] boneNames = GetGameObjectPaths(tempGO);
            string[] properties = new string[] 
            { 
                "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z",
                "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w",
                "localEulerAnglesRaw.x", "localEulerAnglesRaw.y", "localEulerAnglesRaw.z",
                "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z"
            };

            // For each possible path, try to set the curve
            foreach (string path in boneNames)
            {
                foreach (string property in properties)
                {
                    // Try to get the curve from the original clip
                    // This is a bit of a hack, but it's the only way at runtime
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(originalClip, path, typeof(Transform), property);
                    if (curve != null && curve.keys.Length > 0)
                    {
                        AnimationCurve newCurve = new AnimationCurve(curve.keys);
                        newCurve.preWrapMode = curve.preWrapMode;
                        newCurve.postWrapMode = curve.postWrapMode;
                        
                        // Set the curve in the new clip
                        newClip.SetCurve(path, typeof(Transform), property, newCurve);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error cloning animation clip at runtime: {e.Message}");
        }
        finally
        {
            // Clean up
            Object.DestroyImmediate(tempGO);
        }
        
        return newClip;
    }
    
    /// <summary>
    /// Gets a list of possible GameObject paths that might be animated.
    /// This is a helper for the runtime animator.
    /// </summary>
    private static string[] GetGameObjectPaths(GameObject root)
    {
        // Simple implementation - in a real project, you'd want to extract all bones/objects
        // from your character or object hierarchies that could be animated
        List<string> paths = new List<string>();
        paths.Add(""); // Root object
        
        // Add children recursively (just first level for simplicity)
        foreach (Transform child in root.transform)
        {
            paths.Add(child.name);
        }
        
        return paths.ToArray();
    }
    
    /// <summary>
    /// Gets an Animation curve from an animation clip at runtime.
    /// This is a workaround since Unity doesn't provide direct access to curves at runtime.
    /// </summary>
    private static AnimationCurve GetEditorCurve(AnimationClip clip, string path, System.Type type, string propertyName)
    {
        // This function tries to extract the curve by creating a temporary animation state
        // and then sampling it at intervals
        
        if (clip == null)
            return null;
            
        // We need at least 2 keyframes for a valid curve
        if (clip.length <= 0)
            return null;
            
        // Create a curve with keys at regular intervals
        float duration = clip.length;
        int samples = Mathf.Max(10, Mathf.FloorToInt(duration * 30)); // Sample at ~30fps
        
        Keyframe[] keys = new Keyframe[samples];
        float deltaTime = duration / (samples - 1);
        
        // Create a temporary GameObject to sample the animation
        GameObject tempGO = new GameObject("__CurveSampler");
        tempGO.hideFlags = HideFlags.HideAndDontSave;
        
        // Add animation component
        Animation anim = tempGO.AddComponent<Animation>();
        anim.AddClip(clip, "clipToSample");
        anim.clip = clip;
        anim.playAutomatically = false;
        
        // Create the hierarchy that matches the expected path
        GameObject targetObject = tempGO;
        if (!string.IsNullOrEmpty(path))
        {
            string[] pathSegments = path.Split('/');
            foreach (string segment in pathSegments)
            {
                if (string.IsNullOrEmpty(segment)) continue;
                
                Transform child = targetObject.transform.Find(segment);
                if (child == null)
                {
                    GameObject newChild = new GameObject(segment);
                    newChild.transform.SetParent(targetObject.transform);
                    targetObject = newChild;
                }
                else
                {
                    targetObject = child.gameObject;
                }
            }
        }
        
        // Now sample the animation at each time point
        try
        {
            Component component = null;
            
            // Find the component to sample
            if (type == typeof(Transform))
            {
                component = targetObject.transform;
            }
            else 
            {
                component = targetObject.GetComponent(type);
                if (component == null)
                {
                    component = targetObject.AddComponent(type);
                }
            }
            
            // We can't reliably get property values at runtime without reflection
            // But this is just a basic implementation example
            for (int i = 0; i < samples; i++)
            {
                float time = i * deltaTime;
                
                // Sample the animation at this time - FIXED: Access animation state properly
                anim.Play("clipToSample");
                // Instead of anim.time = time (which doesn't exist), get the animation state:
                AnimationState animState = anim["clipToSample"];
                animState.time = time;
                anim.Sample();
                
                // Get the value - this would need reflection to be generic
                float value = 0;
                
                // For common transform properties we can hardcode the extraction
                if (component is Transform)
                {
                    Transform transform = component as Transform;
                    
                    if (propertyName == "m_LocalPosition.x") value = transform.localPosition.x;
                    else if (propertyName == "m_LocalPosition.y") value = transform.localPosition.y;
                    else if (propertyName == "m_LocalPosition.z") value = transform.localPosition.z;
                    else if (propertyName == "m_LocalRotation.x") value = transform.localRotation.x;
                    else if (propertyName == "m_LocalRotation.y") value = transform.localRotation.y;
                    else if (propertyName == "m_LocalRotation.z") value = transform.localRotation.z;
                    else if (propertyName == "m_LocalRotation.w") value = transform.localRotation.w;
                    else if (propertyName == "m_LocalScale.x") value = transform.localScale.x;
                    else if (propertyName == "m_LocalScale.y") value = transform.localScale.y;
                    else if (propertyName == "m_LocalScale.z") value = transform.localScale.z;
                    else if (propertyName == "localEulerAnglesRaw.x") value = transform.localEulerAngles.x;
                    else if (propertyName == "localEulerAnglesRaw.y") value = transform.localEulerAngles.y;
                    else if (propertyName == "localEulerAnglesRaw.z") value = transform.localEulerAngles.z;
                }
                
                // Create a key
                keys[i] = new Keyframe(time, value);
            }
        }
        finally
        {
            // Clean up
            Object.DestroyImmediate(tempGO);
        }
        
        // Check if we have any actual animation data
        bool hasAnimation = false;
        float firstValue = keys[0].value;
        for (int i = 1; i < keys.Length; i++)
        {
            if (!Mathf.Approximately(keys[i].value, firstValue))
            {
                hasAnimation = true;
                break;
            }
        }
        
        if (!hasAnimation)
        {
            return null; // No actual animation found
        }
        
        // Create and return the curve
        return new AnimationCurve(keys);
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