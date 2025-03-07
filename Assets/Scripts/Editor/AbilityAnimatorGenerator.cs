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
            // Initialize the ability animations array
            abilityAnimations = new AnimationClip[0];
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
            
            // Display fields for each ability animation
            for (int i = 0; i < abilityCount; i++)
            {
                abilityAnimations[i] = (AnimationClip)EditorGUILayout.ObjectField(
                    $"Ability {i} Animation", abilityAnimations[i], typeof(AnimationClip), false);
            }
            
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
                
                // Assign animation if provided
                if (i < abilityAnimations.Length && abilityAnimations[i] != null)
                {
                    abilityState.motion = abilityAnimations[i];
                }
                
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
            
            Debug.Log($"Animator Controller created at {fullPath}");
        }
    }
} 