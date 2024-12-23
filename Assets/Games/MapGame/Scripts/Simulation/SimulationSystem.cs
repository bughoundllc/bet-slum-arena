using bet_slum.Games.MapGame.Simulation.EntityArchetypes;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace bet_slum.Games.MapGame.Simulation
{
    public class SimulationSystem
    {
        private static Color32 DeadPlotID = new Color32(0, 0, 0, 255);

        // State
        private uint _tick;
        private Texture2D _plotMap;

        // Config Vars
        public int MaxPopulationCap => _maxPopulationCap;
        private int _maxPopulationCap = 100000;
        private int _basePopulationGrowth = 100;
        private float _minRandomForceScalar = 0.1f;
        private float _maxRandomForceScalar = 0.5f;

        // Entities
        public Dictionary<Color32, Plot> Plots => _plots;
        private Dictionary<Color32, Plot> _plots = new();
        public List<Commander> Commanders => _commanders; // TODO - keyed lookup
        private List<Commander> _commanders = new(); // TODO - keyed lookup
        public List<War> Wars => _wars; // TODO - keyed lookup
        private List<War> _wars = new(); // TODO - keyed lookup

        // Lookups
        public Dictionary<Color32, List<int2>> PlotPixelLookup => _plotPixelLookup;
        private Dictionary<Color32, List<int2>> _plotPixelLookup = new();
        public Dictionary<int2, Color32> PixelPlotLookup => _pixelPlotLookup;
        private Dictionary<int2, Color32> _pixelPlotLookup = new();
        public Dictionary<Color32, HashSet<Color32>> PlotNeighborLookup => _plotNeighborLookup;
        private Dictionary<Color32, HashSet<Color32>> _plotNeighborLookup = new();
        public Dictionary<Commander, List<Color32>> CommanderPlotLookup => _commanderPlotLookup;
        private Dictionary<Commander, List<Color32>> _commanderPlotLookup = new();

        public void Initialize(MapGameMatchRunner mainSystem)
        {
            _plots.Clear(); 
            _commanders.Clear();
            _wars.Clear();
            _plotPixelLookup.Clear(); 
            _pixelPlotLookup.Clear(); 
            _plotNeighborLookup.Clear();
            _commanderPlotLookup.Clear();

            _plotMap = mainSystem.Map;

            var pixels32 = _plotMap.GetPixels32();
            for (int i = 0; i < pixels32.Length; i++)
            {
                var color = pixels32[i];

                if (!_plotPixelLookup.ContainsKey(color))
                {
                    _plotPixelLookup.Add(color, new List<int2>());
                    _plots.Add(color, new Plot());
                    //_plotPopIndicators.Add(color, _popIndicatorPool.Get()); // TODO
                }
                var coordinate = new int2(i % _plotMap.width, i / _plotMap.width);
                _plotPixelLookup[color].Add(coordinate);
                _pixelPlotLookup.Add(coordinate, color);
            }

            // Initialize Commanders
            // TODO - move to InitializeCompetitors
            foreach (var colorPlot in _plots)
            {
                if (colorPlot.Key.Equals(DeadPlotID)) continue;

                _commanders.Add(new Commander
                {
                    TestData =
                {
                    DisplayColor = colorPlot.Key,
                    name = $"{colorPlot.Key.r}|{colorPlot.Key.g}|{colorPlot.Key.b}"
                },
                    CapitalPlotID = colorPlot.Key,
                    AttackSkill = UnityEngine.Random.Range(0.25f, 4f),
                    DefenseSkill = UnityEngine.Random.Range(0.25f, 4f),
                    StewardshipSkill = UnityEngine.Random.Range(0.25f, 4f)
                });
            }


            var ix = 0;
            foreach (var color in _plots.Keys)
            {
                if (color.Equals(DeadPlotID)) continue;

                _plots[color].Controller = _commanders[ix];
                _plots[color].BasePopulationCap = UnityEngine.Random.Range(_maxPopulationCap / 10, _maxPopulationCap);
                _plots[color].Population = UnityEngine.Random.Range((int)(_plots[color].BasePopulationCap * 0.5f), _plots[color].BasePopulationCap);
                _plots[color].GrowthRate = UnityEngine.Random.Range(0.1f, 1f);
                if (color.Equals(DeadPlotID))
                {
                    _plots[color].Navigable = false;
                }

                var maxExtents = int2.zero;
                var minExtents = new int2(int.MaxValue, int.MaxValue);
                foreach (var coordinate in _plotPixelLookup[color])
                {
                    if (coordinate.x < minExtents.x)
                        minExtents.x = coordinate.x;
                    if (coordinate.y < minExtents.y)
                        minExtents.y = coordinate.y;
                    if (coordinate.x > maxExtents.x)
                        maxExtents.x = coordinate.x;
                    if (coordinate.y > maxExtents.y)
                        maxExtents.y = coordinate.y;
                }
                _plots[color].PlotWorldCenter = new Vector3((maxExtents.x + minExtents.x) / 2f, 0f, (maxExtents.y + minExtents.y) / 2f);
                var centerMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                centerMarker.transform.position = _plots[color].PlotWorldCenter;

                if (!_commanderPlotLookup.ContainsKey(_commanders[ix]))
                    _commanderPlotLookup.Add(_commanders[ix], new List<Color32>());

                _commanderPlotLookup[_commanders[ix]].Add(color);

                ix++;
            }

            CalculateNeighborLookup();
        }

        private void CalculateNeighborLookup()
        {
            foreach (var plotPixelGroup in _plotPixelLookup)
            {
                if (plotPixelGroup.Key.Equals(DeadPlotID)) continue;

                var plotID = plotPixelGroup.Key;
                var commander = _plots[plotID].Controller;
                _plotNeighborLookup.Add(plotID, new());

                foreach (var plotPixel in plotPixelGroup.Value)
                {
                    // check 4-dir neighbors with bounds clamping for potential neighbors

                    // left
                    if (plotPixel.x > 0)
                    {
                        var neighborPixelPlotID = _pixelPlotLookup[new int2(plotPixel.x - 1, plotPixel.y)];
                        if (
                            !_plotNeighborLookup[plotID].Contains(neighborPixelPlotID)
                            && _plots[neighborPixelPlotID].Controller != commander
                            && !neighborPixelPlotID.Equals(DeadPlotID))
                        {
                            _plotNeighborLookup[plotID].Add(neighborPixelPlotID);
                        }
                    }
                    // right
                    if (plotPixel.x < _plotMap.width - 1)
                    {
                        var neighborPixelPlotID = _pixelPlotLookup[new int2(plotPixel.x + 1, plotPixel.y)];
                        if (
                            !_plotNeighborLookup[plotID].Contains(neighborPixelPlotID)
                            && _plots[neighborPixelPlotID].Controller != commander
                            && !neighborPixelPlotID.Equals(DeadPlotID))
                        {
                            _plotNeighborLookup[plotID].Add(neighborPixelPlotID);
                        }
                    }
                    // up
                    if (plotPixel.y < _plotMap.height - 1)
                    {
                        var neighborPixelPlotID = _pixelPlotLookup[new int2(plotPixel.x, plotPixel.y + 1)];
                        if (
                            !_plotNeighborLookup[plotID].Contains(neighborPixelPlotID)
                            && _plots[neighborPixelPlotID].Controller != commander
                            && !neighborPixelPlotID.Equals(DeadPlotID))
                        {
                            _plotNeighborLookup[plotID].Add(neighborPixelPlotID);
                        }
                    }
                    // down
                    if (plotPixel.y > 0)
                    {
                        var neighborPixelPlotID = _pixelPlotLookup[new int2(plotPixel.x, plotPixel.y - 1)];
                        if (
                            !_plotNeighborLookup[plotID].Contains(neighborPixelPlotID)
                            && _plots[neighborPixelPlotID].Controller != commander
                            && !neighborPixelPlotID.Equals(DeadPlotID))
                        {
                            _plotNeighborLookup[plotID].Add(neighborPixelPlotID);
                        }
                    }
                }
            }
        }


        public void OnUpdate(MapGameMatchRunner mainSystem)
        {
            foreach (var plot in _plots)
            {
                if (plot.Key.Equals(DeadPlotID)) continue;

                var commander = plot.Value.Controller;
                float growth = _basePopulationGrowth;
                growth *= plot.Value.GrowthRate;
                growth *= commander.StewardshipSkill;

                plot.Value.Population = math.min(plot.Value.BasePopulationCap, plot.Value.Population + (int)growth);
            }
            ProcessCommanders();
            ProcessWars();

            _tick++;
        }

        private void ProcessCommanders()
        {
            foreach (var commander in _commanders)
            {
                // i might want to declare a war
                // look at all neighboring states

                var inActiveWar = false;
                foreach (var war in _wars)
                {
                    if (war.Attacker == commander || war.Defender == commander)
                    {
                        inActiveWar = true;
                        break;
                    }
                }

                // if in war, dont declare a new one
                if (!inActiveWar)
                {
                    var potentialEnemies = new List<Commander>();
                    foreach (var plotID in _commanderPlotLookup[commander])
                    {
                        // look at each neighboring plot
                        foreach (var neighborPlotID in _plotNeighborLookup[plotID])
                        {
                            var neighborPlotCommander = _plots[neighborPlotID].Controller;

                            // It's us
                            if (neighborPlotCommander == commander) continue;

                            var alliedPopulation = GetPopulation(commander);
                            var enemyPopulation = GetPopulation(neighborPlotCommander);

                            // They appear stronger than us
                            if (alliedPopulation * commander.AttackSkill < enemyPopulation * neighborPlotCommander.DefenseSkill)
                            {
                                // ... and we can't overwhelm them with numbers
                                if (alliedPopulation < enemyPopulation)
                                    continue;
                            }

                            // confirmed potential target
                            potentialEnemies.Add(_plots[neighborPlotID].Controller);

                        }
                    }
                    if (potentialEnemies.Count > 0)
                    {
                        // order by easiest to conquer
                        var enemyToDeclare = potentialEnemies.OrderBy(enemyCommander =>
                        {
                            var capitalDistance = Vector3.Distance(_plots[enemyCommander.CapitalPlotID].PlotWorldCenter, _plots[commander.CapitalPlotID].PlotWorldCenter); // (math.sqrt(math.square(_plotMap.width) + math.square(_plotMap.height));
                            return capitalDistance + GetPopulation(enemyCommander) * enemyCommander.DefenseSkill;
                        }).ToList()[0];

                        //Debug.Log($"Declaring War! {commander.TestData.name} vs {enemyToDeclare.TestData.name}");
                        _wars.Add(new War
                        {
                            StartTick = _tick,
                            Attacker = commander,
                            Defender = enemyToDeclare
                        });
                    }
                }
            }
        }

        private void ProcessWars()
        {
            Debug.Log($"Processing {_wars.Count} wars");
            var warsToDestroy = new List<War>();
            foreach (var war in _wars)
            {
                // Prematurely ended by conclusion of other wars
                if (warsToDestroy.Contains(war)) continue;

                // Dont declare and attack same turn
                if (_tick == war.StartTick) continue;

                // for now, war is just battle rounds between the total pop of all commanders' plots
                var attacker = war.Attacker;
                var defender = war.Defender;

                // Note - Defender attacks first
                // Note - 'Force' is an abstract value that represents overall damage dealt; 
                // for now, 1 Force == 1 Death
                // TODO - Force should be applied to Plots proportionally to their Population

                // Calculate starting populations
                var attackerPopulation = GetPopulation(attacker);
                var defenderPopulation = GetPopulation(defender);

                //Debug.Log($"War {attacker.TestData.name} ({attackerPopulation}) vs {defender.TestData.name} ({defenderPopulation})-----------");

                // Calculate Defense Force
                var defenseForce = defenderPopulation;
                defenseForce = (int)(defenseForce * defender.DefenseSkill * 0.75f/*defense is OP right now*/);
                defenseForce = (int)(defenseForce * UnityEngine.Random.Range(_minRandomForceScalar, _maxRandomForceScalar));
                defenseForce = math.max(defenseForce, 1);// minimum force
                                                         //Debug.Log($"{defender.TestData.name} is applying {defenseForce} Force to {attacker.TestData.name} in Defense");

                // Apply Defense Force
                foreach (var attackerPlot in _commanderPlotLookup[attacker])
                {
                    var toKill = (int)math.ceil(defenseForce * (_plots[attackerPlot].Population / (float)attackerPopulation));
                    _plots[attackerPlot].Population = math.max(0, _plots[attackerPlot].Population - toKill);
                }

                // Calculate Attack Force
                var attackForce = GetPopulation(attacker);
                attackForce = (int)(attackForce * attacker.AttackSkill);
                attackForce = (int)(attackForce * UnityEngine.Random.Range(_minRandomForceScalar, _maxRandomForceScalar));
                attackForce = math.max(attackForce, 1);
                //Debug.Log($"{attacker.TestData.name} is applying {attackForce} Force to {defender.TestData.name} in Offense");

                // Apply Attack Force
                foreach (var defenderPlot in _commanderPlotLookup[defender])
                {
                    var toKill = (int)math.ceil(attackForce * (_plots[defenderPlot].Population / (float)defenderPopulation));
                    _plots[defenderPlot].Population = math.max(0, _plots[defenderPlot].Population - toKill);
                }

                // Re-Calculate current Population after Force applied - ends when either has 0 Population left
                attackerPopulation = GetPopulation(attacker);
                defenderPopulation = GetPopulation(defender);

                // End War
                if (attackerPopulation == 0 || defenderPopulation == 0)
                {
                    var isDraw = attackerPopulation == 0 && defenderPopulation == 0;
                    if (!isDraw)
                    {
                        // War of extermination lol
                        var victor = attackerPopulation > 0 ? attacker : defender;
                        // The Other Guy
                        var loser = victor == attacker ? defender : attacker;

                        // take n of their plots
                        // prioritze plots "more" adjacent to yours
                        var sortedPlots = new List<Color32>(_commanderPlotLookup[loser]).OrderByDescending(plotID =>
                        {
                            var victorNeighboringPlots = 0;
                            foreach (var neighborPlotID in _plotNeighborLookup[plotID])
                            {
                                if (_plots[neighborPlotID].Controller == victor)
                                    victorNeighboringPlots++;
                            }
                            return victorNeighboringPlots;
                        }).ToList();

                        // if it's the last duo, just take it all
                        var plotsToTake = _commanders.Count == 2 ? sortedPlots.Count : math.max(1, sortedPlots.Count / 2);
                        var plotsToRemove = new List<Color32>();
                        for (int p = 0; p < plotsToTake; p++)
                        {
                            var plotID = sortedPlots[p];
                            _plots[plotID].Controller = victor;
                            _commanderPlotLookup[victor].Add(plotID);
                            plotsToRemove.Add(plotID);
                        }
                        foreach (var toRemove in plotsToRemove)
                            _commanderPlotLookup[loser].Remove(toRemove);

                        // Destroyed!
                        if (_commanderPlotLookup[loser].Count == 0)
                        {
                            foreach (var otherWar in _wars)
                            {
                                if (otherWar == war) continue;

                                if (otherWar.Defender == loser)
                                    otherWar.Defender = victor;
                                else if (otherWar.Attacker == loser)
                                    warsToDestroy.Add(otherWar);
                            }

                            _commanderPlotLookup.Remove(loser);// we can do this safely for now because all wars are 1:1
                            _commanders.Remove(loser);
                        }
                        // capital taken - find a new one
                        else if (_plots[loser.CapitalPlotID].Controller != loser)
                        {
                            loser.CapitalPlotID = _commanderPlotLookup[loser][0];
                        }
                    }
                    else
                    {
                        // Stalemate - nothing to do for now
                    }

                    warsToDestroy.Add(war);
                }
            }

            foreach (var war in warsToDestroy)
                _wars.Remove(war);
        }

        // TODO - move me
        private int GetPopulation(Commander commander)
        {
            var population = 0;
            foreach (var plotID in _commanderPlotLookup[commander])
                population += _plots[plotID].Population;

            return population;
        }
    }
}