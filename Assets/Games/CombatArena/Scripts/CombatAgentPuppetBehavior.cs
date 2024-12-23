using bet_slum.CombatArena.Agents;
using RootMotion.Dynamics;
using UnityEngine;

namespace bet_slum.CombatArena
{
    public class CombatAgentPuppetBase : BehaviourBase
    {
        [SerializeField] private float _minimumDamageThreshold = 5f;
        [SerializeField] private CombatAgent _agent;
        [SerializeField] private PopupText _popupPrefab;

        public override void OnReactivate()
        {
        }

        protected override void OnMuscleCollisionBehaviour(MuscleCollision collision)
        {
            if (collision.collision.impulse.magnitude < _minimumDamageThreshold) return; // too small to care
            if (!CollisionHelper.IsPrimaryForceInCollision(puppetMaster.muscles[collision.muscleIndex].rigidbody, collision.collision)) return;

            var damageToTake = collision.collision.impulse.magnitude;
            _agent.TakeDamage(damageToTake);
            var popup = Instantiate(_popupPrefab, collision.collision.GetContact(0).point, Quaternion.identity);
            popup.Initialize($"{damageToTake}");
        }
    }
}