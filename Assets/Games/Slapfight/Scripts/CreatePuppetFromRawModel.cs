using RootMotion.Dynamics;
using Unity.VisualScripting;
using UnityEngine;

namespace bet_slum.Games.Slapfight
{
    public class CreatePuppetFromRawModel
    {
        public static Transform CreatePuppetFromRagdoll(Transform ragdoll, int characterControllerLayer = 8, int ragdollLayer = 9)
        {
            //Transform ragdollInstance = GameObject.Instantiate(ragdoll, transform.position, transform.rotation) as Transform;

            // This will duplicate the ragdoll character, remove the ragdoll components from the original and use it as the animated target.
            PuppetMaster.SetUp(ragdoll, characterControllerLayer, ragdollLayer);

            Debug.Log("A ragdoll was successfully converted to a Puppet.");
            return ragdoll;
        }

        public static GameObject CreateRagdollFromModel(GameObject Model, Transform parent)
        {
            // Instantiate the character
            GameObject instance = GameObject.Instantiate(Model, parent);

            // TODO - handle scaling

            // Find bones (Humanoids)
            BipedRagdollReferences r = BipedRagdollReferences.FromAvatar(instance.GetComponent<Animator>());

            // How would you like your ragdoll?
            BipedRagdollCreator.Options options = BipedRagdollCreator.AutodetectOptions(r);

            // Edit options here if you need to
            //options.headCollider = RagdollCreator.ColliderType.Box;
            //options.weight *= 2f;
            //options.joints = RagdollCreator.JointType.Character;

            // Create the ragdoll
            BipedRagdollCreator.Create(r, options);

            Debug.Log("A ragdoll was successfully created.");
            return instance;
        }
    }
}