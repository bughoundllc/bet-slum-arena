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
using UnityEngine.Profiling;

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
        public GameObject PacketViewPrototype;
        public GameObject BuildingViewPrototype;
        public LineRenderer RoadPrototype;
        public GameObject HeroViewUI;
        public GameObject MonsterView;

        public override int MaxCompetitorCount => 32;

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
            SimulationSystem.InitializeMap(this);

            await base.InitializeMatchCompetitors();

            _competitorToTeamIDLookup.Clear();
            for (int i = 0; i < _competitorData.competitionTeams.Count; i++)
            {
                var competitor = _competitorData.competitionTeams[i].competitors[0/*assumes 1 competitor per team*/];
                _competitorToTeamIDLookup.Add(competitor.id, i);
            }

            SimulationSystem.InitializeCompetitors(this);

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

            Profiler.BeginSample("View System Update");
            ViewSystem.OnUpdate(this);
            Profiler.EndSample();

            if (_endRoundFired) return;
            if (!_running) return;

            Profiler.BeginSample("Simulation System Update");
            SimulationSystem.OnUpdate(this);
            Profiler.EndSample();

            Profiler.BeginSample("Map View Update");
            MapMesh.UpdateControlledMapView(this); // this sucks
            Profiler.EndSample();

            var survivingCommanders = SimulationSystem.Commanders
                .Count(commander => SimulationSystem.Plots.Count(plot => plot.Value.Building != null && plot.Value.Building.CommanderID == commander.Key) > 0);
            
            if (survivingCommanders <= 1) // endgame conditions
            {
                Debug.Log($"Winner! {SimulationSystem.Commanders.First(commander => SimulationSystem.Plots.Count(plot => plot.Value.Building != null && plot.Value.Building.CommanderID == commander.Key) > 0)}");
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
                try
                {
                    // .First() will throw err if none exists
                    return _competitorToTeamIDLookup[
                        SimulationSystem.Commanders.First(
                            commander => SimulationSystem.Plots.Count(
                                plot => plot.Value.Building != null && plot.Value.Building.CommanderID == commander.Key) > 0).Key];
                }
                catch
                {
                    return -1;
                }
            }
        }
    }
}

