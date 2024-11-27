using UnityEngine;
using UnityEngine.AI;

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

        public void Initialize()
        {
        }

        public void StartMatch()
        {
            _active = true;
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
                        Debug.Log($"{gameObject.name} hit {enemyAgent.gameObject.name}");
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

