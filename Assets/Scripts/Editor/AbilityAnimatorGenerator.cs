using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace NET_SLUM.Editor
{
    public class AbilityAnimatorGenerator : EditorWindow
    {
        // Fields for user configuration
        private string animatorName = "NewAbilityAnimator";
        private string savePath = "Assets/Animations";
        private int abilityCount = 3;
        private AnimationClip idleAnimation;
        private AnimationClip[] abilityAnimations;

        [MenuItem("Tools/NET SLUM/Ability Animator Generator")]
        public static void ShowWindow()
        {
            GetWindow<AbilityAnimatorGenerator>("Ability Animator Generator");
        }

        private void OnEnable()
        {
            // Initialize the ability animations array to match the ability count
            abilityAnimations = new AnimationClip[abilityCount];
        }

        private void OnGUI()
        {
            GUILayout.Label("Ability Animator Generator", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(10);

            // Config section
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            
            animatorName = EditorGUILayout.TextField("Animator Name", animatorName);
            savePath = EditorGUILayout.TextField("Save Path", savePath);
            
            // Ability count with resize of animation array when changed
            EditorGUI.BeginChangeCheck();
            abilityCount = EditorGUILayout.IntField("Number of Abilities", abilityCount);
            if (EditorGUI.EndChangeCheck())
            {
                // Ensure abilityCount is at least 1
                abilityCount = Mathf.Max(1, abilityCount);
                
                // Resize the ability animations array
                System.Array.Resize(ref abilityAnimations, abilityCount);
            }
            
            EditorGUILayout.Space(10);
            
            // Animation clips
            EditorGUILayout.LabelField("Animation Clips (Optional)", EditorStyles.boldLabel);
            
            idleAnimation = (AnimationClip)EditorGUILayout.ObjectField(
                "Idle Animation", idleAnimation, typeof(AnimationClip), false);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Placeholder animation clips will be generated for each ability state. These can be replaced at runtime.", MessageType.Info);
            
            EditorGUILayout.Space(20);
            
            // Generate button
            if (GUILayout.Button("Generate Animator Controller", GUILayout.Height(30)))
            {
                GenerateAnimatorController();
            }
        }
        
        private void GenerateAnimatorController()
        {
            // Ensure the directory exists
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            
            // Create subdirectory for animation clips
            string animationsSubPath = Path.Combine(savePath, $"{animatorName}_Animations");
            if (!Directory.Exists(animationsSubPath))
            {
                Directory.CreateDirectory(animationsSubPath);
            }
            
            // Create the animator controller asset
            string fullPath = Path.Combine(savePath, $"{animatorName}.controller");
            AnimatorController animatorController = AnimatorController.CreateAnimatorControllerAtPath(fullPath);
            
            // Get the root state machine
            AnimatorStateMachine rootStateMachine = animatorController.layers[0].stateMachine;
            
            // Create the Idle state
            AnimatorState idleState = rootStateMachine.AddState("Idle");
            if (idleAnimation != null)
            {
                idleState.motion = idleAnimation;
            }
            else
            {
                // Create a placeholder idle animation if none provided
                AnimationClip idleClip = CreatePlaceholderAnimation("Idle", animationsSubPath);
                idleState.motion = idleClip;
            }
            
            // Set Idle as the default state
            rootStateMachine.defaultState = idleState;
            
            // Add the parameters (trigger and int for ability index)
            animatorController.AddParameter("UseAbility", AnimatorControllerParameterType.Trigger);
            animatorController.AddParameter("AbilityIndex", AnimatorControllerParameterType.Int);
            
            // Create ability states and transitions
            for (int i = 0; i < abilityCount; i++)
            {
                // Create the ability state
                AnimatorState abilityState = rootStateMachine.AddState($"Ability{i}");
                
                // Create a placeholder animation for this ability
                AnimationClip abilityClip = CreatePlaceholderAnimation($"Ability{i}", animationsSubPath);
                abilityState.motion = abilityClip;
                
                // Create transition from Any State to this ability state
                AnimatorStateTransition anyStateTransition = rootStateMachine.AddAnyStateTransition(abilityState);
                anyStateTransition.hasExitTime = false;
                anyStateTransition.duration = 0.1f; // Short transition duration
                
                // Set up the conditions
                anyStateTransition.AddCondition(AnimatorConditionMode.Equals, i, "AbilityIndex");
                anyStateTransition.AddCondition(AnimatorConditionMode.If, 0, "UseAbility");
                
                // Create transition from ability state back to idle
                AnimatorStateTransition returnToIdleTransition = abilityState.AddTransition(idleState);
                returnToIdleTransition.hasExitTime = true;
                returnToIdleTransition.exitTime = 1f; // Exit time of 1 as requested
                returnToIdleTransition.duration = 0.1f; // Short transition duration
            }
            
            // Save asset changes
            EditorUtility.SetDirty(animatorController);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Select and ping the created controller in the project view
            Selection.activeObject = animatorController;
            EditorGUIUtility.PingObject(animatorController);
            
            Debug.Log($"Animator Controller created at {fullPath} with animation clips in {animationsSubPath}");
        }
        
        // Helper method to create a placeholder animation clip
        private AnimationClip CreatePlaceholderAnimation(string clipName, string savePath)
        {
            // Create a new animation clip
            AnimationClip clip = new AnimationClip();
            clip.name = clipName;
            
            // Add a simple property curve (just a dummy animation)
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0f);
            curve.AddKey(1f, 0f);
            clip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            
            // Save the animation clip as an asset
            string clipPath = Path.Combine(savePath, $"{clipName}.anim");
            AssetDatabase.CreateAsset(clip, clipPath);
            
            Debug.Log($"Created placeholder animation: {clipPath}");
            return clip;
        }
    }
} 