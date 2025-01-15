using bet_slum.Games.MapGame.Simulation.EntityArchetypes;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using bet_slum.Utility;
using bet_slum.Data;

namespace bet_slum.Games.MapGame.Simulation
{
    public class SimulationSystem
    {
        public static Color32 DeadPlotID = new Color32(0, 0, 0, 255);

        // State
        private Texture2D _plotMap;

        // Config Vars
        public int MaxPopulationCap => _maxPopulationCap;
        private int _maxPopulationCap = 100000;
        private float _basePopulationGrowthRate = 10f;
        private float _commanderDecisionCooldownTime = 5f;

        // Entities
        public Dictionary<Color32, Plot> Plots => _plots;
        private Dictionary<Color32, Plot> _plots = new();
        public List<Commander> Commanders => _commanders; // TODO - keyed lookup
        private List<Commander> _commanders = new(); // TODO - keyed lookup
        public List<Army> Armies => _armies; // TODO - keyed lookup
        private List<Army> _armies = new(); // TODO - keyed lookup

        // Lookups
        private Dictionary<Plot, Color32> _plotKeyLookup = new();

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
            _armies.Clear();

            _plotKeyLookup.Clear();
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
                    var plot = new Plot();
                    _plots.Add(color, plot);
                    _plotKeyLookup.Add(plot, color);
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
                    CapitalPlotID = colorPlot.Key
                    // Competitor items to be assigned later
                });
            }


            var ix = 0;
            foreach (var color in _plots.Keys)
            {
                if (color.Equals(DeadPlotID)) continue;

                _plots[color].Controller = _commanders[ix];
                _plots[color].BasePopulationCap = 1000;// UnityEngine.Random.Range(_maxPopulationCap / 10, _maxPopulationCap);
                _plots[color].Population = 10; //UnityEngine.Random.Range((int)(_plots[color].BasePopulationCap * 0.5f), _plots[color].BasePopulationCap);
                _plots[color].GrowthRate = 1f; //UnityEngine.Random.Range(0.1f, 1f);
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
                float growth = _basePopulationGrowthRate;
                growth *= plot.Value.GrowthRate;
                growth *= commander.StewardshipSkill;
                growth *= Time.deltaTime;

                plot.Value.CurrentGrowthProgress += growth;

                if (plot.Value.CurrentGrowthProgress >= 100f)
                {
                    plot.Value.Population = math.min(plot.Value.BasePopulationCap, plot.Value.Population + 1);
                    plot.Value.CurrentGrowthProgress = 0f;
                }
            }

            float assaultForceRatioRequired = 2.5f;
            float forceToSendPercent = 0.5f;
            var toDeduct = new List<(Plot, int)>(); // dont want to remove pop until decision making finished, otherwise second turn always has preference against first
            foreach (var commander in _commanders)
            {
                var decisionFrequency = Competitor.MAX_LEVEL - commander.AttentionSkill;
                if (Time.time - commander.LastDecisionTime < decisionFrequency) continue;

                // look for minimum source node combo that could overwhelm a potential neighbor
                var potentialTargetPlots = new List<Plot>();
                foreach (var commanderPlotID in _commanderPlotLookup[commander])
                {
                    var plot = _plots[commanderPlotID];
                    foreach (var neighborPlotID in _plotNeighborLookup[commanderPlotID])
                    {
                        var neighborPlot = _plots[neighborPlotID];
                        if (neighborPlot.Controller == commander) continue;

                        potentialTargetPlots.Add(neighborPlot);
                    }
                }

                var potentialAssaults = new List<(List<Plot> sources, Plot target)>();
                var commanderPlots = new List<Color32>(_commanderPlotLookup[commander]);
                foreach (var target in potentialTargetPlots)
                {
                    var targetCommander = target.Controller;
                    var defenseForce = target.Population;

                    var sources = new List<Plot>();
                    var runningPop = 0;

                    commanderPlots.Shuffle(); // shuffle for source combo variety

                    // if any combination of sources has >= population, it's a valid assault decision
                    foreach (var commanderPlotID in commanderPlots)
                    {
                        if (_plots[commanderPlotID].Population * forceToSendPercent < 1f) continue; // can't send <1

                        sources.Add(_plots[commanderPlotID]);
                        runningPop += _plots[commanderPlotID].Population;

                        var attackForce = runningPop;

                        if (attackForce >= defenseForce * assaultForceRatioRequired)
                        {
                            potentialAssaults.Add((sources, target));
                            break;
                        }
                    }
                }

                if (potentialAssaults.Count > 0)
                {
                    var assault = potentialAssaults.OrderByDescending(assault =>
                    {
                        var sourcesPop = 0;
                        foreach (var source in assault.sources)
                            sourcesPop += source.Population;

                        return sourcesPop - assault.target.Population;
                    })
                        // randomly choose from the top ordered half
                        .ToList()[UnityEngine.Random.Range(0, potentialAssaults.Count / 2)];

                    foreach (var source in assault.sources)
                    {
                        var toSend = Mathf.FloorToInt(source.Population * forceToSendPercent);
                        toDeduct.Add((source, toSend));

                        // launch assault
                        var path = GetPathAStar(_plotKeyLookup[source], _plotKeyLookup[assault.target]);
                        _armies.Add(new Army { MoveSpeed = 0.25f, Commander = commander, Path = path, Population = toSend });
                    }

                    commander.LastDecisionTime = Time.time;
                }
            }
            foreach (var popRemove in toDeduct)
            {
                popRemove.Item1.Population -= popRemove.Item2;
            }

            // TODO - armies should take/deal damage when passing through unfriendly territory
            // TODO - unfriendly territory should have higher pathfinding cost and slower move speed

            var toDestroy = new List<Army>();
            foreach (var army in _armies)
            {
                // arrived
                if (army.PathIndex == army.Path.Count - 1)
                {
                    var targetPlotKey = army.Path[army.PathIndex];
                    var targetPlot = _plots[targetPlotKey];

                    // Enemy-held plot
                    if (targetPlot.Controller != army.Commander)
                    {
                        var attackForce = army.Population;
                        var defenseForce = targetPlot.Population;
                        if (attackForce >= defenseForce)
                        {
                            army.Population = math.max(0, army.Population - (int)defenseForce);
                            targetPlot.Population = army.Population;

                            // Choose new capital if necessary
                            var plotKey = _plotKeyLookup[targetPlot];
                            if (targetPlot.Controller.CapitalPlotID.Equals(plotKey))
                            {
                                foreach (var otherCommanderPlotKey in _commanderPlotLookup[targetPlot.Controller])
                                {
                                    if (otherCommanderPlotKey.Equals(plotKey)) continue;

                                    targetPlot.Controller.CapitalPlotID = otherCommanderPlotKey; break;
                                }
                            }

                            // Transfer control
                            _commanderPlotLookup[targetPlot.Controller].Remove(_plotKeyLookup[targetPlot]);
                            // "dead" commanders (0 plots) still exist - might be attached to armies
                            targetPlot.Controller = army.Commander;
                            _commanderPlotLookup[army.Commander].Add(_plotKeyLookup[targetPlot]);

                        }
                        else
                        {
                            targetPlot.Population = math.max(0, targetPlot.Population - (int)attackForce);
                        }
                    }
                    else
                        targetPlot.Population += army.Population;

                    toDestroy.Add(army);
                }
                else
                {
                    army.PlotProgress += army.MoveSpeed * Time.deltaTime;
                    if (army.PlotProgress >= 1f)
                    {
                        army.PlotProgress = 0f;
                        army.PathIndex++;
                    }
                }
            }
            foreach (var army in toDestroy)
                _armies.Remove(army);
        }

        private List<Color32> GetPathAStar(
            Color32 startPlotKey,
            Color32 destinationPlotKey)
        {
            var frontier = new PriorityQueue<Color32>();
            frontier.Enqueue(startPlotKey, 0f);

            var cameFrom = new Dictionary<Color32, Color32>();
            cameFrom.Add(startPlotKey, default);

            var costSoFar = new Dictionary<Color32, float>();
            costSoFar.Add(startPlotKey, 0f);

            while (frontier.TryDequeue(out var currentPlotKey))
            {
                if (currentPlotKey.Equals(destinationPlotKey)) break;

                foreach (var neighbor in _plotNeighborLookup[currentPlotKey])
                {
                    var newCost = costSoFar[currentPlotKey];
                    //+ math.distance(currentPlotKey, neighbor);

                    if (!costSoFar.ContainsKey(neighbor)
                        || newCost < costSoFar[neighbor])
                    {
                        costSoFar[neighbor] = newCost;
                        float heuristic(int2 a, int2 b)
                        {
                            return math.abs(a.x - b.x) +
                                   math.abs((a.y - b.y));
                        };
                        frontier.Enqueue(neighbor, newCost /*+ heuristic(destinationCoordinates, neighbor)*/);
                        cameFrom[neighbor] = currentPlotKey;
                    }
                }
            }

            var path = new List<Color32>();
            var current = destinationPlotKey;
            while (!current.Equals(startPlotKey))
            {
                path.Add(current);
                current = cameFrom[current];
            }
            path.Add(startPlotKey);

            // reverse path - TODO: extension method
            Color32 temp;
            int n = path.Count;
            for (int i = 0; i < n / 2; i++)
            {
                temp = path[i];
                path[i] = path[n - i - 1];
                path[n - i - 1] = temp;
            }

            return path;
        }

        private void ProcessCommanders()
        {
            foreach (var commander in _commanders)
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

                        var alliedPopulation = GetTotalPopulation(commander);
                        var enemyPopulation = GetTotalPopulation(neighborPlotCommander);

                        // They appear stronger than us
                        if (alliedPopulation < enemyPopulation)
                        {
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
                        return capitalDistance + GetTotalPopulation(enemyCommander);
                    }).ToList()[0];

                    //Debug.Log($"Declaring War! {commander.TestData.name} vs {enemyToDeclare.TestData.name}");
                    //_wars.Add(new War
                    //{
                    //    StartTick = _tick,
                    //    Attacker = commander,
                    //    Defender = enemyToDeclare
                    //});
                }
            }
        }

        public int GetTotalPopulation(Commander commander)
        {
            var population = 0;
            foreach (var plotID in _commanderPlotLookup[commander])
                population += _plots[plotID].Population;

            return population;
        }
    }
}