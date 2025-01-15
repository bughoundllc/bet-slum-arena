using bet_slum.Data;
using bet_slum.Games.MapGame.Simulation;
using bet_slum.Games.MapGame.Simulation.EntityArchetypes;
using bet_slum.Games.MapGame.View;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
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
        private bool _isInitialized = false;

        private Dictionary<string, int> _competitorToTeamIDLookup = new Dictionary<string, int>();

        // TODO - abstract to MatchRunner or trash
        private bool _endRoundFired = false;

        // TODO - some other config method
        [HideInInspector] public Texture2D Map;
        [SerializeField] private TMP_Text _auxText;

        public PlotInfoUI PlotInfoUIPrototype;
        public CompetitorInfoUI CompetitorLabelPrototype;
        public ArmyView ArmyViewPrototype;
        public LineRenderer RoadPrototype;

        public override int MaxCompetitorCount => SimulationSystem.Commanders.Count;

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

            await base.InitializeMatchCompetitors();

            _competitorToTeamIDLookup.Clear();

            // DEBUG/WIP - undo me once we support plot count > commander count
            // DO NOT GO LIVE WITH THIS - it wont work
            //if (_competitorData.competitionTeams.Count < SimulationSystem.Commanders.Count)
            //{
            //    while (_competitorData.competitionTeams.Count < SimulationSystem.Commanders.Count)
            //        _competitorData.competitionTeams.Add(new() { new() { id = $"DUMMY{_competitorData.CompetitorData.Count}", name = $"DUMMY{_competitorData.CompetitorData.Count}" } });
            //}

            for (int i = 0; i < _competitorData.competitionTeams.Count; i++)
            {
                var commander = SimulationSystem.Commanders[i];
                var competitor = _competitorData.competitionTeams[i].competitors[0/*assumes 1 competitor per team*/];

                commander.CompetitorID = competitor.id;
                commander.StewardshipSkill = _competitorData.GetCompetitorStatValue(competitor.id, "STAT_Stewardship");
                commander.AttentionSkill = _competitorData.GetCompetitorStatValue(competitor.id, "STAT_Attention", defaultValue: Random.Range(0f, Competitor.MAX_LEVEL));

                _competitorToTeamIDLookup.Add(commander.CompetitorID, i);
            }

            ViewSystem.Initialize(this);

            _isInitialized = true;
        }

        public override void StartMatch()
        {
            _running = true;
        }

        public async override Awaitable EndMatch()
        {
            _running = false;
            _isInitialized = false;
            ViewSystem.OnUpdate(this);

            await Task.Delay(3000); // to see the end map
            await base.EndMatch();
        }

        private async void Update()
        {
            if (!_isInitialized) return; // this is sloppy

            ViewSystem.OnUpdate(this);

            if (_endRoundFired) return;
            if (!_running) return;

            SimulationSystem.OnUpdate(this);
            MapMesh.UpdateControlledMapView(this); // this sucks

            var survivingCommanders = 0;
            foreach (var commander in SimulationSystem.Commanders)
            {
                if (SimulationSystem.GetTotalPopulation(commander) > 0) survivingCommanders++;
            }

            if (survivingCommanders <= 1) // endgame conditions
            {
                Debug.Log($"Winner! {SimulationSystem.Commanders[0].TestData.name}");
                // this is bad
                _endRoundFired = true;
                await EndMatch();
                _endRoundFired = false;
            }
        }

        // TODO - map competitors to team IDs
        protected override int WinnerID
        {
            get
            {
                for (int i = 0; i < SimulationSystem.Commanders.Count; i++)
                {
                    if (SimulationSystem.GetTotalPopulation(SimulationSystem.Commanders[i]) > 0)
                        return i;
                }
                return -1;
            }
        }
    }
}

