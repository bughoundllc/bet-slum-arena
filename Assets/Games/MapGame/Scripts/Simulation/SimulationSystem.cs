using bet_slum.Games.MapGame.Simulation.EntityArchetypes;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
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

        // Consts
        private float _constructionSupplyPerPacket = 1f;
        private float _buildingSupplyPerPacket = 3f;
        private float _buildingDamagePerPacket = 5f;
        private float _buildingLevel1ConstructionSupply = 10;
        private float _buildingLevel2ConstructionSupply = 20;
        private float _buildingLevel1HP = 100f;
        private float _buildingLevel2HP = 200f;
        private float _buildingLevel2SupplyCap = 12;
        private float _buildingLevel2FireRate = 1f;
        private float _productionGenerationPerControlledPlot = 0.4f;

        // Entities
        public Dictionary<Color32, Plot> Plots => _plots;
        private Dictionary<Color32, Plot> _plots = new();
        public Dictionary<string, Commander> Commanders => _commanders;
        private Dictionary<string, Commander> _commanders = new();
        public List<Packet> Packets => _packets;
        private List<Packet> _packets = new();

        // Lookups
        private Dictionary<Plot, Color32> _plotKeyLookup = new();

        public Dictionary<Color32, List<int2>> PlotPixelLookup => _plotPixelLookup;
        private Dictionary<Color32, List<int2>> _plotPixelLookup = new();
        public Dictionary<int2, Color32> PixelPlotLookup => _pixelPlotLookup;
        private Dictionary<int2, Color32> _pixelPlotLookup = new();
        public Dictionary<Color32, HashSet<Color32>> PlotNeighborLookup => _plotNeighborLookup;
        private Dictionary<Color32, HashSet<Color32>> _plotNeighborLookup = new();
        public Dictionary<string, List<Color32>> CommanderPlotLookup => _commanderPlotLookup;
        private Dictionary<string, List<Color32>> _commanderPlotLookup = new();


        public void InitializeMap(MapGameMatchRunner mainSystem)
        {
            _plots.Clear();
            _commanders.Clear();
            _packets.Clear();

            _plotKeyLookup.Clear();
            _plotPixelLookup.Clear();
            _pixelPlotLookup.Clear();
            _plotNeighborLookup.Clear();
            _commanderPlotLookup.Clear();

            _plotMap = mainSystem.Map;

            CalculateMapPlotData();

            CalculateNeighborLookup();
        }

        public void InitializeCompetitors(MapGameMatchRunner mainSystem)
        {
            var spawnPlotIndices = new List<Color32>();
            foreach (var plot in _plots)
                spawnPlotIndices.Add(plot.Key);
            var shuffledIndicesQueue = new Queue<Color32>(spawnPlotIndices.Shuffle());

            var competitionInfo = mainSystem.CompetitorData;
            for (int i = 0; i < competitionInfo.competitionTeams.Count; i++)
            {
                if(!shuffledIndicesQueue.TryDequeue(out var nextPlotIdx))
                {
                    throw new System.Exception($"Not enough plots ({_plots.Count}) for allotted competition teams ({competitionInfo.competitionTeams.Count})");
                }
                if (nextPlotIdx.Equals(DeadPlotID)) continue;

                var competitor = competitionInfo.competitionTeams[i].competitors[0];
                var commanderKey = competitor.id;
                
                var maxStatScalar = 3f;
                var atkStat = competitionInfo.GetCompetitorStatValue(competitor.id, "STAT_Attack", defaultValue: UnityEngine.Random.Range(0.25f, WorldConsts.MaxStat));
                var defStat = competitionInfo.GetCompetitorStatValue(competitor.id, "STAT_Defense", defaultValue: UnityEngine.Random.Range(0.25f, WorldConsts.MaxStat));
                var stdStat = competitionInfo.GetCompetitorStatValue(competitor.id, "STAT_Stewardship", defaultValue: UnityEngine.Random.Range(0.25f, WorldConsts.MaxStat));
                
                var commander = new Commander
                {
                    TestData = new(nextPlotIdx, competitor.name),
                    CompetitorID = commanderKey,
                    CapitalID = nextPlotIdx,
                    ProductionStat = 1f,
                    AttackDamageMultiplierStat = (atkStat / WorldConsts.MaxStat) * maxStatScalar,
                    PacketSpeedStat = (defStat / WorldConsts.MaxStat) * maxStatScalar,
                    ConstructionSupplyMultiplierStat = (stdStat/ WorldConsts.MaxStat) * maxStatScalar
                };
                _commanders.Add(commanderKey, commander);
                
                _commanderPlotLookup.Add(commanderKey, new());
                _commanderPlotLookup[commanderKey].Add(nextPlotIdx);

                _plots[nextPlotIdx].Building = new Building { CommanderID = commanderKey, HP = _buildingLevel1HP, MaxHP = _buildingLevel1HP };
            }
        }

        private void CalculateMapPlotData()
        {
            // Gather Pixels to Plots
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

            // Initialize Plot Data
            var ix = 0;
            foreach (var color in _plots.Keys)
            {
                if (color.Equals(DeadPlotID)) continue;

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

                ix++;
            }
        }

        private void CalculateNeighborLookup()
        {
            foreach (var plotPixelGroup in _plotPixelLookup)
            {
                if (plotPixelGroup.Key.Equals(DeadPlotID)) continue;

                var plotID = plotPixelGroup.Key;
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
            // TODO - Level X building upgrades?
            // lvl3 should be the "tank" - it should punch through frontlines
            
            foreach (var commander in _commanders)
            {
                var buildingGeneration = 0f;
                foreach (var controlledPlot in _commanderPlotLookup[commander.Key])
                {
                    var baseBuildingCount = _plots[controlledPlot].Building.CommanderID == commander.Key ? 1 : 0;
                    buildingGeneration += baseBuildingCount;
                }
                commander.Value.ProductionProgress +=
                    (commander.Value.ProductionStat / WorldConsts.MaxStat)
                    * (buildingGeneration * _productionGenerationPerControlledPlot)
                    * Time.deltaTime;

                if (commander.Value.ProductionProgress >= 1f)
                {
                    commander.Value.ProductionProgress = 0f;

                    // account for packets in transit
                    var sentConstructionSupply = new Dictionary<Color32, float>();
                    var sentBuildingSupply = new Dictionary<Color32, float>();
                    var sentConstructionBegin = new Dictionary<Color32, bool>();
                    foreach (var existingPacket in _packets)
                    {
                        if (existingPacket.CommanderID == commander.Key)
                        {
                            var target = existingPacket.Path[existingPacket.Path.Count - 1];

                            if (existingPacket.MissionLogic == Packet.Mission.SupplyConstructionProject)
                            {
                                if (!sentConstructionSupply.ContainsKey(target))
                                    sentConstructionSupply.Add(target, 0f);
                                sentConstructionSupply[target] += _constructionSupplyPerPacket * commander.Value.ConstructionSupplyMultiplierStat;
                            }
                            if (existingPacket.MissionLogic == Packet.Mission.SupplyBuilding)
                            {
                                if (!sentBuildingSupply.ContainsKey(target))
                                    sentBuildingSupply.Add(target, 0f);
                                sentBuildingSupply[target] += _buildingSupplyPerPacket * commander.Value.ConstructionSupplyMultiplierStat;
                            }
                            if (existingPacket.MissionLogic == Packet.Mission.EstablishConstructionProject
                                || existingPacket.MissionLogic == Packet.Mission.UpgradeBuildingLevel)
                            {
                                if (!sentConstructionBegin.ContainsKey(target))
                                    sentConstructionBegin.Add(target, false);
                                sentConstructionBegin[target] = true;
                            }
                            // we "lock" sending to both so that we don't end up constructing in either in the meantime
                            if(existingPacket.MissionLogic == Packet.Mission.DowngradeAndRedeployBuildingLevel)
                            {
                                if (!sentConstructionBegin.ContainsKey(target))
                                    sentConstructionBegin.Add(target, false);
                                sentConstructionBegin[target] = true;

                                if (!sentConstructionBegin.ContainsKey(existingPacket.AuxColor))
                                    sentConstructionBegin.Add(existingPacket.AuxColor, false);
                                sentConstructionBegin[existingPacket.AuxColor] = true;
                            }
                        }
                    }

                    // Produce Supply Packet
                    var packet = new Packet { CommanderID = commander.Key };
                    
                    var assigned = false;
                    var plot = _plots[commander.Value.CapitalID];

                    // TODO - some sort of structure that lets us extract the conditions/assignment logic and preferably prioritize them without depending on code order

                    // Supply Capital Construction
                    if (plot.ConstructionProjects.Count >= 1)
                    {
                        packet.MissionLogic = Packet.Mission.SupplyConstructionProject;
                        packet.Path = GetPathAStar(commander.Value.CapitalID, commander.Value.CapitalID);

                        assigned = true;
                    }

                    // Supply Capital Building
                    if (!assigned)
                    {
                        if (plot.Building.Supply < plot.Building.MaxSupply)
                        {
                            packet.MissionLogic = Packet.Mission.SupplyBuilding;
                            packet.Path = GetPathAStar(commander.Value.CapitalID, commander.Value.CapitalID);

                            assigned = true;
                        }
                    }

                    // Send Plot Building Supply
                    if (!assigned)
                    {
                        // all commander's + neighbor plots with commander's construction sorted by supply progress
                        var sortedPlots = _commanderPlotLookup[commander.Key]
                            .OrderBy(pIdx =>
                            {
                                var building = _plots[pIdx].Building;

                                if (!sentBuildingSupply.TryGetValue(pIdx, out var sentSupply))
                                    sentSupply = 0f;

                                return sentSupply / (building.MaxSupply - building.Supply);
                            });

                        foreach (var plotIdx in sortedPlots)
                        {
                            var buildingSupplyNeeded = 0f;
                            if (_plots[plotIdx].Building != null)
                            {
                                if (_plots[plotIdx].Building.CommanderID == commander.Key
                                && _plots[plotIdx].Building.Supply < _plots[plotIdx].Building.MaxSupply / 2f /*refill at half*/ )
                                    buildingSupplyNeeded += _plots[plotIdx].Building.MaxSupply - _plots[plotIdx].Building.Supply;
                            }

                            if ((!sentBuildingSupply.TryGetValue(plotIdx, out var nodeSentSupplies) && buildingSupplyNeeded > 0f)
                                || buildingSupplyNeeded > nodeSentSupplies)
                            {
                                packet.MissionLogic = Packet.Mission.SupplyBuilding;
                                packet.Path = GetPathAStar(commander.Value.CapitalID, plotIdx);

                                assigned = true;
                                break;
                            }
                        }
                    }

                    // Construct Plot Turret
                    if (!assigned)
                    {
                        var upgradeNeededPlots = _commanderPlotLookup[commander.Key]
                            .Where(pIdx =>
                                _plots[pIdx].Building.Level < 2
                                && (!sentConstructionBegin.TryGetValue(pIdx, out var hasSent) || !hasSent)
                                && _plots[pIdx].ConstructionProjects.Count(p => p.CommanderID == commander.Key) < 1)
                            .Where(pIdx => {
                                foreach (var neighborIdx in _plotNeighborLookup[pIdx])
                                {
                                    if (_plots[neighborIdx].Building != null
                                        && _plots[neighborIdx].Building.CommanderID != commander.Key)
                                    {
                                        return true;                                        
                                    }
                                }
                                return false;
                            });

                        var unusedTurretPlots = _commanderPlotLookup[commander.Key]
                            .Where(pIdx =>
                                _plots[pIdx].Building.Level >= 2
                                && (!sentConstructionBegin.TryGetValue(pIdx, out var hasSent) || !hasSent))
                            .Where(pIdx => {
                                foreach (var neighborIdx in _plotNeighborLookup[pIdx])
                                {
                                    if (_plots[neighborIdx].Building != null
                                        && _plots[neighborIdx].Building.CommanderID != commander.Key)
                                    {
                                        return false;
                                    }
                                }
                                return true;
                            });


                        foreach (var plotIdx in upgradeNeededPlots)
                        {
                            // if we have a spare backline turret, we can send that instead
                            if (unusedTurretPlots.Count() > 0)
                            {
                                var turretToMovePlotIdx = unusedTurretPlots.First();

                                packet.MissionLogic = Packet.Mission.DowngradeAndRedeployBuildingLevel;
                                packet.Path = GetPathAStar(commander.Value.CapitalID, turretToMovePlotIdx);
                                packet.AuxColor = plotIdx;

                                assigned = true;
                                break;
                            }

                            packet.MissionLogic = Packet.Mission.EstablishConstructionProject;
                            packet.Path = GetPathAStar(commander.Value.CapitalID, plotIdx);

                            assigned = true;
                            break;
                        }
                    }

                    // Send Plot Construction Supply
                    if (!assigned)
                    {
                        // all commander's + neighbor plots with commander's construction sorted by supply progress
                        var sortedPlots = _commanderPlotLookup[commander.Key]
                            .SelectMany(pIdx =>
                            {
                                var toAdd = new List<Color32>() { pIdx };
                                toAdd.AddRange(_plotNeighborLookup[pIdx]);
                                return toAdd;
                            })
                            .Distinct()
                            .Where(pIdx => _plots[pIdx].ConstructionProjects.Count(project => project.CommanderID == commander.Key) > 0)
                            .OrderBy(pIdx =>
                            {
                                var project = _plots[pIdx].ConstructionProjects.First(project => project.CommanderID == commander.Key);

                                if (!sentConstructionSupply.TryGetValue(pIdx, out var sentSupply))
                                    sentSupply = 0f;
                                
                                return sentSupply / (project.MaxSupply - project.Supply);
                            });

                        foreach (var plotIdx in sortedPlots)
                        {
                            var project = _plots[plotIdx].ConstructionProjects.First(p => p.CommanderID == commander.Key);
                            var supplyNeeded = project.MaxSupply - project.Supply;
                            if ((!sentConstructionSupply.TryGetValue(plotIdx, out var sentSupply) && supplyNeeded > 0)
                                || supplyNeeded > sentSupply)
                            {
                                packet.MissionLogic = Packet.Mission.SupplyConstructionProject;
                                packet.Path = GetPathAStar(commander.Value.CapitalID, plotIdx);

                                assigned = true;

                                break;
                            }
                        }
                    }

                    // Construct HQ In Empty Plot
                    if (!assigned)
                    {
                        // collect all empty neighbor plots, order by # hostile plot neighbors
                        var sortedNeighberEmptyPlots = _commanderPlotLookup[commander.Key]
                            .SelectMany(pIdx => _plotNeighborLookup[pIdx])
                            .Where(pIdx => _plots[pIdx].Building == null)
                            .Distinct()
                            .OrderBy(pIdx =>
                            {
                                var distanceRaw = Vector3.Distance(_plots[pIdx].PlotWorldCenter, plot.PlotWorldCenter);
                                var maxDistanceForScaling = mainSystem.Map.width;
                                var distanceFactor = (distanceRaw / maxDistanceForScaling);

                                var maxEnemyNeighbors = 8;
                                var enemyNeighors = 0;
                                foreach (var neighborIdx in _plotNeighborLookup[pIdx])
                                {
                                    var building = _plots[neighborIdx].Building;
                                    if (building == null) continue;
                                    if (building.CommanderID == commander.Key) continue;

                                    enemyNeighors += building.Level; // make settling near cannons less desirable
                                }
                                var enemyFactor = (enemyNeighors / _plotNeighborLookup[pIdx].Count);

                                var distanceWeight = 0.7f;
                                var enemyNeighborWeight = 0.3f;
                                return (enemyFactor * enemyNeighborWeight)
                                        + (distanceFactor * distanceWeight);
                            });

                        foreach (var plotIdx in sortedNeighberEmptyPlots)
                        {
                            // already sent packet
                            if (sentConstructionBegin.TryGetValue(plotIdx, out var hasSent) && hasSent) continue;
                            // already constructing
                            if (_plots[plotIdx].ConstructionProjects.Count(p => p.CommanderID == commander.Key) > 0) continue; // akready building there

                            packet.MissionLogic = Packet.Mission.EstablishConstructionProject;
                            packet.Path = GetPathAStar(commander.Value.CapitalID, plotIdx);

                            assigned = true;
                            break;
                        }
                    }

                    if (!assigned)
                    {
                        commander.Value.ProductionProgress = 1f;
                    }
                    else
                    {
                        _packets.Add(packet);
                    }
                }
            }

            foreach(var plot in _plots)
            {
                if (plot.Value.Building != null)
                {
                    var plotBuilding = plot.Value.Building;
                    if (plotBuilding.Level < 2) continue;
                    if (Time.time - plotBuilding.LastActionTime < plotBuilding.ActionRate) continue;

                    var supplyToUse = 1f;
                    if (plotBuilding.Supply < supplyToUse) continue;

                    // find a target

                    var potentialTargets = _plotNeighborLookup[plot.Key]
                        .Where(pIdx =>
                                _plots[pIdx].Building != null
                                && _plots[pIdx].Building.CommanderID != plotBuilding.CommanderID)
                        .OrderByDescending(pIdx => _plots[pIdx].Building.Level);

                    if (potentialTargets.Count() > 0)
                    {
                        var target = potentialTargets.First();
                        plotBuilding.LastActionTime = Time.time;
                        plotBuilding.Supply -= supplyToUse;

                        var packet = new Packet { CommanderID = plotBuilding.CommanderID };
                        _packets.Add(packet);

                        packet.MissionLogic = Packet.Mission.AttackBuilding;
                        packet.Path = GetPathAStar(plot.Key, target );
                    }
                }
            }

            var toDestroy = new List<Packet>();
            var toAdd = new List<Packet>();
            foreach (var packet in _packets)
            {
                // arrived
                if (packet.PathIndex == packet.Path.Count - 1)
                {
                    toDestroy.Add(packet);

                    var plotIndex = packet.Path[packet.PathIndex];
                    var plot = _plots[plotIndex];
                    if (packet.MissionLogic == Packet.Mission.EstablishConstructionProject)
                    {
                        if (plot.Building != null && packet.CommanderID != plot.Building.CommanderID)
                        {
                            Debug.Log("Trying to establish a construction project in controlled enemy territory");
                            continue;
                        }

                        var supplyNeeded = _buildingLevel1ConstructionSupply;
                        if (plot.Building != null && plot.Building.CommanderID == packet.CommanderID)
                        {
                            // level 2
                            supplyNeeded = _buildingLevel2ConstructionSupply;
                        }

                        // Establish Construction Project
                        var project = new ConstructionProject
                        {
                            CommanderID = packet.CommanderID,
                            MaxSupply = supplyNeeded
                        };
                        plot.ConstructionProjects.Add(project);
                    }
                    else if (packet.MissionLogic == Packet.Mission.SupplyConstructionProject)
                    {
                        foreach (var project in plot.ConstructionProjects)
                        {
                            if (project.CommanderID != packet.CommanderID) continue;

                            project.Supply = math.clamp(project.Supply + _constructionSupplyPerPacket * _commanders[project.CommanderID].ConstructionSupplyMultiplierStat, 0f, project.MaxSupply);
                            if (project.Supply >= project.MaxSupply)
                            {
                                plot.ConstructionProjects.Remove(project);
                                if (plot.Building == null) // spawn level 1
                                {
                                    _commanderPlotLookup[project.CommanderID].Add(plotIndex);
                                    plot.Building = new Building 
                                    { 
                                        CommanderID = packet.CommanderID, 
                                        HP = _buildingLevel1HP, 
                                        MaxHP = _buildingLevel1HP, 
                                        ActionRate= 0f, 
                                        MaxSupply = 0f, 
                                        LastActionTime = Time.time, 
                                        Supply = 0f };

                                    // Destroy others constructing in plot
                                    var projectsToDestroy = new List<ConstructionProject>();
                                    foreach (var otherProject in plot.ConstructionProjects)
                                    {
                                        if (otherProject.CommanderID == project.CommanderID) continue;

                                        projectsToDestroy.Add(otherProject);
                                    }
                                    foreach (var otherProject in projectsToDestroy)
                                        plot.ConstructionProjects.Remove(otherProject);
                                }
                                else // upgrade to level 2
                                {
                                    UpgradePlotBuilding(plot);
                                }
                            }
                            break;
                        }
                    }
                    else if (packet.MissionLogic == Packet.Mission.SupplyBuilding)
                    {
                        if (plot.Building != null)
                        {
                            var building = plot.Building;
                            if (building.Supply >= building.MaxSupply || building.CommanderID != packet.CommanderID) continue;

                            building.Supply = math.clamp(building.Supply + _buildingSupplyPerPacket * _commanders[packet.CommanderID].ConstructionSupplyMultiplierStat, 0f, building.MaxSupply);
                            break;
                        }
                    }
                    else if (packet.MissionLogic == Packet.Mission.AttackBuilding)
                    {
                        if (plot.Building != null)
                        {
                            var building = plot.Building;
                            if (building.CommanderID == packet.CommanderID) continue;

                            building.HP -= _buildingDamagePerPacket * _commanders[packet.CommanderID].AttackDamageMultiplierStat;
                            if (building.HP <= 0f)
                            {
                                // find them a new capital if necessary
                                if(_commanders[building.CommanderID].CapitalID.Equals(plotIndex))
                                {
                                    foreach (var plotID in _commanderPlotLookup[plot.Building.CommanderID])
                                    {
                                        if (plotID.Equals(plotIndex)) continue;

                                        _commanders[plot.Building.CommanderID].CapitalID = plotID;
                                        break;
                                    }
                                }
                                _commanderPlotLookup[plot.Building.CommanderID].Remove(plotIndex);

                                // Destroy all current construction projects (maybe not needed?)
                                var toRm = new List<ConstructionProject>();
                                foreach (var project in plot.ConstructionProjects)
                                {
                                    toRm.Add(project);
                                }
                                foreach (var project in toRm)
                                    plot.ConstructionProjects.Remove(project);

                                // destroy
                                plot.Building = null;
                            }
                        }
                    }
                    else if(packet.MissionLogic == Packet.Mission.DowngradeAndRedeployBuildingLevel)
                    {
                        if(plot.Building != null && packet.CommanderID == plot.Building.CommanderID)
                        {
                            var targetPlotID = packet.AuxColor;
                            var turretPlot = _plots[plotIndex];
                            DowngradePlotBuilding(turretPlot);

                            var newPacket = new Packet { CommanderID = packet.CommanderID };
                            newPacket.MissionLogic = Packet.Mission.UpgradeBuildingLevel;
                            newPacket.Path = GetPathAStar(plotIndex, targetPlotID);
                            
                            toAdd.Add(newPacket);
                        }
                        
                    }
                    else if (packet.MissionLogic == Packet.Mission.UpgradeBuildingLevel)
                    {
                        if(plot.Building != null && plot.Building.CommanderID == packet.CommanderID)
                        {
                            UpgradePlotBuilding(plot);
                        }
                    }
                }
                else
                {
                    var relevantStat = packet.MissionLogic == Packet.Mission.AttackBuilding ? _commanders[packet.CommanderID].AttackDamageMultiplierStat : _commanders[packet.CommanderID].PacketSpeedStat;
                    packet.PathProgress += relevantStat * Time.deltaTime;
                    if (packet.PathProgress >= 1f)
                    {
                        packet.PathProgress = 0f;
                        packet.PathIndex++;
                    }
                }
            }
            foreach (var packet in toAdd)
                _packets.Add(packet);
            foreach (var packet in toDestroy)
                _packets.Remove(packet);
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

                foreach (var neighborIdx in _plotNeighborLookup[currentPlotKey])
                {
                    if (neighborIdx.Equals(DeadPlotID)) continue;

                    var newCost = costSoFar[currentPlotKey];
                    //+ math.distance(currentPlotKey, neighbor);

                    if (!costSoFar.ContainsKey(neighborIdx)
                        || newCost < costSoFar[neighborIdx])
                    {
                        costSoFar[neighborIdx] = newCost;
                        float heuristic(int2 a, int2 b)
                        {
                            return math.abs(a.x - b.x) +
                                   math.abs((a.y - b.y));
                        };
                        frontier.Enqueue(neighborIdx, newCost /*+ heuristic(destinationCoordinates, neighbor)*/);
                        cameFrom[neighborIdx] = currentPlotKey;
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

        private void UpgradePlotBuilding(Plot plot)
        {
            if(plot.Building.Level == 1)
            {
                plot.Building.Level = 2;
                plot.Building.ActionRate = _buildingLevel2FireRate;
                plot.Building.MaxHP = _buildingLevel2HP;
                plot.Building.HP = plot.Building.MaxHP;
                plot.Building.MaxSupply = _buildingLevel2SupplyCap;
            }
        }
        private void DowngradePlotBuilding(Plot plot)
        {
            if(plot.Building.Level == 2)
            {
                plot.Building.Level = 1;
                plot.Building.ActionRate = 0f;
                plot.Building.MaxHP = _buildingLevel1HP;
                plot.Building.HP = plot.Building.MaxHP;
                plot.Building.MaxSupply = 0f;
            }
        }

    }
}