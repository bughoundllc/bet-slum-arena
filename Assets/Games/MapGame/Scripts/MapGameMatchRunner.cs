using bet_slum.Games.MapGame.Simulation;
using bet_slum.Games.MapGame.View;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace bet_slum.Games.MapGame
{
    // This currently functions as GameSystem/MainSystem
    public class MapGameMatchRunner : MatchRunner
    {
        public MapRenderer MapMesh;

        public ViewSystem ViewSystem;
        public SimulationSystem SimulationSystem;
        private bool _running = false;
        private float _lastTickTime = 0f;
        private float tickInterval = .5f;

        private Dictionary<string, int> _competitorToTeamIDLookup = new Dictionary<string, int>();

        // TODO - abstract to MatchRunner or trash
        private bool _endRoundFired = false;

        // TODO - some other config method
        [HideInInspector] public Texture2D Map;

        public override Awaitable InitializeGameEnvironment(ShowRunner runner)
        {
            Map = Resources.Load<Texture2D>("map");

            SimulationSystem = new SimulationSystem();

            ViewSystem = new ViewSystem();

            return base.InitializeGameEnvironment(runner);
        }

        public override async Awaitable InitializeMatchEnvironment()
        {
            await base.InitializeMatchEnvironment();
            await InitializeMatchCompetitors();
        }

        public async override Awaitable InitializeMatchCompetitors()
        {
            SimulationSystem.Initialize(this);
            ViewSystem.Initialize(this);
            _competitorToTeamIDLookup.Clear();

            // DEBUG/WIP - undo me once we support plot count > commander count
            // DO NOT GO LIVE WITH THIS - it wont work
            if (_competitorData.CompetitionTeams.Count < SimulationSystem.Commanders.Count)
            {
                while (_competitorData.CompetitionTeams.Count < SimulationSystem.Commanders.Count)
                    _competitorData.CompetitionTeams.Add(new() { new() { id = $"DUMMY{_competitorData.CompetitionTeams.Count}", name = $"DUMMY{_competitorData.CompetitionTeams.Count}" } });
            }

            for(int i = 0; i < _competitorData.CompetitionTeams.Count; i++)
            {
                var commander = SimulationSystem.Commanders[i];
                commander.CompetitorID = _competitorData.CompetitionTeams[i][0].id;
                _competitorToTeamIDLookup.Add(commander.CompetitorID, i);
            }
        }

        public override void StartMatch()
        {
            _running = true;
        }

        public async override Awaitable EndMatch()
        {
            _running = false;
            await Task.Delay(5000); // to see the end map
            await base.EndMatch();
        }

        private async void Update()
        {
            ViewSystem.OnUpdate(this);

            if (_endRoundFired) return;
            if (!_running) return;

            if(Time.time - _lastTickTime >= tickInterval)
            {
                SimulationSystem.OnUpdate(this);
                MapMesh.UpdateControlledMapView(this); // this sucks

                _lastTickTime = Time.time;

                if(SimulationSystem.Commanders.Count == 1) // endgame conditions
                {
                    Debug.Log($"Winner! {SimulationSystem.Commanders[0].TestData.name}");
                    // this is bad
                    _endRoundFired = true;
                    await EndMatch();
                    _endRoundFired = false;
                }
            }
        }

        // TODO - map competitors to team IDs
        protected override int GetWinnerID => _competitorToTeamIDLookup[SimulationSystem.Commanders[0].CompetitorID];
    }
}

