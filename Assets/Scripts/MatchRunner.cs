using bet_slum.CombatArena;
using bet_slum.Data;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

        public UnityEvent OnRoundEnd;

        private ShowRunner _runner;

        // TODO - abtract to shared class lib
        public class MatchCompetitorInfo
        {
            public List<Competitor> CompetitorsA;
            public List<Competitor> CompetitorsB;
        }
        public async Awaitable InitializeMatch(ShowRunner runner)
        {
            Debug.Log("MR: Initialize Match");
            _runner = runner;
            
            _agentA = Instantiate(_agentPrefab);
            _agentB = Instantiate(_agentPrefab);

            await InitializeRound();
        }

        public async Awaitable InitializeRound()
        {
            Debug.Log("MR: Initialize Round");
            var competitorResponse = await NetworkController.GET("game", "get-competitors");
            var competitorData = JsonUtility.FromJson<MatchCompetitorInfo>(competitorResponse.Text);
            _agentA.name = competitorData.competitorAName;
            _agentB.name = competitorData.competitorBName;
            _agentA.Initialize(_agent1SpawnPosition.position, _agent1SpawnPosition.rotation);
            _agentB.Initialize(_agent2SpawnPosition.position, _agent2SpawnPosition.rotation);
        }

        public void StartRound()
        {
            Debug.Log("MR: Start Round");
            _agentA.Activate();
            _agentB.Activate();
        }

        private async Awaitable EndRound(CombatAgent victor)
        {
            Debug.Log("MR: End Round");
            _agentA.Deactivate();
            _agentB.Deactivate();

            await _runner.OnRoundEnd(victor == _agentA ? 0 : victor == _agentB ? 1 : -1);
        }

        private bool _endRoundFired = false;
        private async void Update()
        {
            if(_endRoundFired) return;

            // Check for victory conditions
            if(_agentA.HP <= 0f || _agentB.HP <= 0f)
            {
                _endRoundFired = true;
                await EndRound(_agentA.HP <= 0f 
                    ? _agentB.HP <= 0f
                        ? null
                        : _agentB
                    : _agentA);
                _endRoundFired = false;
            }
        }
    }
}