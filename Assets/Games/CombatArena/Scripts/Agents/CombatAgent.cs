using bet_slum.Data;
using RootMotion.Dynamics;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace bet_slum.CombatArena.Agents
{
    public class CombatAgent : ArenaAgent
    {
        [SerializeField] private NavMeshAgent _nav;
        [SerializeField] private Animator _animator;
        [SerializeField] private PuppetMaster _puppet;

        private ArenaAgent _target;

        private float _lastAttackTime;
        [SerializeField] private float _attackCooldown = 1f;
        [SerializeField] private float _stoppingDistance = 1f;

        public override float CurrentHP => _HP;
        private float _HP;
        public override float MaxHP => _HP;
        private float _MaxHP = 250f;

        private float _MaxRotationSpeed = 100f;

        private void Awake()
        {
            // Prevent early Win condition 
            _HP = _MaxHP;
        }

        public override void Initialize(Competitor competitor, Vector3 initialPosition, Quaternion initialRotation)
        {
            _puppet.Teleport(initialPosition, initialRotation, true);

            _HP = _MaxHP;

            base.Initialize(competitor, initialPosition, initialRotation);
        }


        public void TakeDamage(float amount)
        {
            _HP = Mathf.Clamp(_HP - amount, 0f, _MaxHP);
            OnDamageTaken?.Invoke(this);
        }

        private void Update()
        {
            if (!_active) return;

            _animator.SetFloat("MoveSpeed", _nav.velocity.magnitude / _nav.speed);

            
            if(_target == null)
            {
                // Find new target
                var agents = FindObjectsByType<ArenaAgent>(FindObjectsSortMode.InstanceID);
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

                // Rotate to face target
                if (Vector3.Dot(transform.forward, (_target.transform.position - transform.position).normalized) <= 0.99f)
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation((_target.transform.position - transform.position).normalized, transform.up), _MaxRotationSpeed * Time.deltaTime);
                }

                // Fire Attack
                if (Time.time - _lastAttackTime > _attackCooldown)
                {
                    _lastAttackTime = Time.time;
                    if(Random.Range(0f, 1f) > 0.5f)
                        _animator.SetTrigger("Attack");
                    else
                        _animator.SetTrigger("Attack2");
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

