using bet_slum.CombatArena;
using UnityEngine;

namespace bet_slum
{
    // todo: abstract
    public class MatchRunner: MonoBehaviour
    {
        [SerializeField] private CombatAgent _agentPrefab;

        [SerializeField] private Transform _agent1SpawnPosition;
        [SerializeField] private Transform _agent2SpawnPosition;

        private CombatAgent _agentA;
        private CombatAgent _agentB;

        public void Initialize()
        {
            _agentA = Instantiate(_agentPrefab, _agent1SpawnPosition.position, _agent1SpawnPosition.rotation);
            _agentA.name = "Agent A";
            _agentB = Instantiate(_agentPrefab, _agent2SpawnPosition.position, _agent2SpawnPosition.rotation);
            _agentB.name = "Agent B";

            _agentA.Initialize();
            _agentB.Initialize();
        }

        public void StartMatch()
        {
            _agentA.StartMatch();
            _agentB.StartMatch();
        }
    }
}