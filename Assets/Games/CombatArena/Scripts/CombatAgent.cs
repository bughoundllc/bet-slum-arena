using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace bet_slum.CombatArena
{
    public class CombatAgent : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent _nav;

        private CombatAgent _target;
        private bool _active = false;

        private float _lastAttackTime;
        [SerializeField] private float _attackCooldown = 1f;
        [SerializeField] private float _stoppingDistance = 1f;

        public float HP { get { return _HP; } }
        private float _HP;
        private float _MaxHP = 100;
        private float _AttackDamage = 25f;


        public void Initialize(Vector3 initialPosition, Quaternion initialRotation)
        {
            _HP = _MaxHP;
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }

        public void Activate()
        {
            _active = true;
        }

        public void Deactivate()
        {
            _active = false;
        }

        public void TakeDamage(float amount)
        {
            _HP = Mathf.Clamp(_HP - amount, 0f, _MaxHP);
        }

        private void Update()
        {
            if (!_active) return;
            
            if(_target == null)
            {
                // Find new target
                var agents = FindObjectsByType<CombatAgent>(FindObjectsSortMode.InstanceID);
                foreach (var agent in agents)
                {
                    if (agent == this) continue;

                    _target = agent;
                    break;
                }

                if (_target == null) { Debug.LogError($"No target for {gameObject.name}"); return; };
            }

            if (Vector3.Distance(transform.position, _target.transform.position) <= _stoppingDistance)
            {
                _nav.isStopped = true;
                if(Time.time - _lastAttackTime > _attackCooldown)
                {
                    // Attack
                    _lastAttackTime = Time.time;
                    foreach(var hit in Physics.BoxCastAll(transform.position + transform.forward*2f, Vector3.one, transform.forward))
                    {
                        if (hit.transform.gameObject == gameObject) continue;
                        if (!hit.transform.TryGetComponent<CombatAgent>(out var enemyAgent)) return;
                        enemyAgent.TakeDamage(_AttackDamage);
                    }
                }
            }
            else // Update for target movement
            {
                _nav.isStopped = false;
                _nav.SetDestination(_target.transform.position); 
            }
        }
    }
}

