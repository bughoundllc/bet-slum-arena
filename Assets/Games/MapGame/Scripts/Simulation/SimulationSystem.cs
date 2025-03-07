using bet_slum.Games.MapGame.Simulation.EntityArchetypes;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using bet_slum.Utility;
using bet_slum.Data;
using static UnityEngine.GraphicsBuffer;
using Unity.VisualScripting;
using UnityEngine.Rendering;

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
        private float _creepSentPerPacket = 10f;

        // Entities
        public Dictionary<Color32, Plot> Plots => _plots;
        private Dictionary<Color32, Plot> _plots = new();
        public Dictionary<string, Commander> Commanders => _commanders;
        private Dictionary<string, Commander> _commanders = new();
        public Dictionary<string, Hero> Heroes => _heroes;
        private Dictionary<string, Hero> _heroes = new();
        public List<Packet> Packets => _packets;
        private List<Packet> _packets = new();
        public List<Monster> Monsters => _monsters;
        private List<Monster> _monsters = new();


        // Lookups
        private Dictionary<Plot, Color32> _plotKeyLookup = new();

        public Dictionary<Color32, List<int2>> PlotPixelLookup => _plotPixelLookup;
        private Dictionary<Color32, List<int2>> _plotPixelLookup = new();
        public Dictionary<int2, Color32> PixelPlotLookup => _pixelPlotLookup;
        private Dictionary<int2, Color32> _pixelPlotLookup = new();
        public Dictionary<Color32, List<Color32>> PlotNeighborLookup => _plotNeighborLookup;
        private Dictionary<Color32, List<Color32>> _plotNeighborLookup = new();
        public Dictionary<string, List<Color32>> CommanderPlotLookup => _commanderPlotLookup;
        private Dictionary<string, List<Color32>> _commanderPlotLookup = new();


        public void InitializeMap(MapGameMatchRunner mainSystem)
        {
            _plots.Clear();
            _commanders.Clear();
            _packets.Clear();
            _heroes.Clear();

            _plotKeyLookup.Clear();
            _plotPixelLookup.Clear();
            _pixelPlotLookup.Clear();
            _plotNeighborLookup.Clear();
            _commanderPlotLookup.Clear();

            _plotMap = mainSystem.Map;

            CalculateMapPlotData();

            CalculateNeighborLookup();

            // remove neighbors for funsies
            //RandomlyRemoveEdges(GetEdges().Count / 2);
        }

        public void InitializeCompetitors(MapGameMatchRunner mainSystem)
        {
            var spawnPlotIndices = new List<Color32>();
            foreach (var plot in _plots)
            {
                spawnPlotIndices.Add(plot.Key);
            };
            var shuffledIndices = spawnPlotIndices.Shuffle();

            var competitionInfo = mainSystem.CompetitorData;
            for (int i = 0; i < competitionInfo.competitionTeams.Count; i++)
            {
                var plotIdex = shuffledIndices
                    // first plot that has
                    .FirstOrDefault(pIdx => 
                        // 0 neighbors with >0 other heroes
                        _plotNeighborLookup[pIdx].Count(nIdx => _heroes.Count(h => h.Value.CurrentPlotIdx.Equals(nIdx)) > 0) == 0);

                var competitor = competitionInfo.competitionTeams[i].competitors[0];
                var commanderKey = competitor.id;
                
                var maxStatScalar = 3f;
                var atkStat = 1f;// competitionInfo.GetCompetitorStatValue(competitor.id, "STAT_Attack", defaultValue: UnityEngine.Random.Range(0.25f, WorldConsts.MaxStat));
                var defStat = 1f;// competitionInfo.GetCompetitorStatValue(competitor.id, "STAT_Defense", defaultValue: UnityEngine.Random.Range(0.25f, WorldConsts.MaxStat));
                var stdStat = 1f;// competitionInfo.GetCompetitorStatValue(competitor.id, "STAT_Stewardship", defaultValue: UnityEngine.Random.Range(0.25f, WorldConsts.MaxStat));
                
                var commander = new Commander
                {
                    TestData = new(plotIdex, competitor.name),
                    CompetitorID = commanderKey,
                    CapitalID = plotIdex,
                    ProductionStat = 1f,
                    AttackDamageMultiplierStat = (atkStat / WorldConsts.MaxStat) * maxStatScalar,
                    PacketSpeedStat = (defStat / WorldConsts.MaxStat) * maxStatScalar,
                    ConstructionSupplyMultiplierStat = (stdStat/ WorldConsts.MaxStat) * maxStatScalar
                };
                _commanders.Add(commanderKey, commander);
                
                _commanderPlotLookup.Add(commanderKey, new());
                _commanderPlotLookup[commanderKey].Add(plotIdex);

                _plots[plotIdex].Building = new Building { CommanderID = commanderKey, HP = _buildingLevel1HP, MaxHP = _buildingLevel1HP };
                //_plots[plotIdex].CreepAmount = _plots[plotIdex].MaxCreepAmount;
                //_plots[plotIdex].CreepControllerID = commanderKey;

                var hero = new Hero { };
                hero.HP = hero.HPMax;
                hero.CurrentPlotIdx = plotIdex;
                hero.AttackDamage = UnityEngine.Random.Range(0.1f, 10f);
                _heroes.Add(commanderKey, hero);
            }

            // spawn field entities
            var monstersToSpawn = shuffledIndices.Count() * 0.1f;
            for(int i = 0; i < monstersToSpawn; i++)
            {
                var spawnPosition = shuffledIndices
                    .FirstOrDefault(pIdx =>
                        // No other monsters
                        _monsters.Count(m => m.PlotIndex.Equals(pIdx)) == 0
                        // No heroes
                        && _heroes.Count(h => h.Value.CurrentPlotIdx.Equals(pIdx)) == 0
                        // No neighbor heroes
                        && _plotNeighborLookup[pIdx].Count(nIdx => _monsters.Count(m => m.PlotIndex.Equals(nIdx)) > 0) == 0
                        // No neighbor monsters
                        && _plotNeighborLookup[pIdx].Count(nIdx => _heroes.Count(h => h.Value.CurrentPlotIdx.Equals(nIdx)) > 0) == 0
                        );
                
                if (spawnPosition.Equals(default(Color32))) break;

                var monster = new Monster { PlotIndex = spawnPosition };
                _monsters.Add(monster);
            }
        }

        private void CalculateMapPlotData()
        {
            // Gather Pixels to Plots
            var pixels32 = _plotMap.GetPixels32();
            for (int i = 0; i < pixels32.Length; i++)
            {
                var color = pixels32[i];
                if (color.Equals(DeadPlotID)) continue;

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
                var plotID = plotPixelGroup.Key;
                _plotNeighborLookup.Add(plotID, new());

                foreach (var plotPixel in plotPixelGroup.Value)
                {
                    // check 4-dir neighbors with bounds clamping for potential neighbors

                    // left
                    if (plotPixel.x > 0
                        && PixelPlotLookup.ContainsKey(new int2(plotPixel.x - 1, plotPixel.y)))
                    {
                        var neighborPixelPlotID = _pixelPlotLookup[new int2(plotPixel.x - 1, plotPixel.y)];
                        if (!_plotNeighborLookup[plotID].Contains(neighborPixelPlotID))
                            _plotNeighborLookup[plotID].Add(neighborPixelPlotID);
                    }
                    // right
                    if (plotPixel.x < _plotMap.width - 1
                        && PixelPlotLookup.ContainsKey(new int2(plotPixel.x + 1, plotPixel.y)))
                    {
                        var neighborPixelPlotID = _pixelPlotLookup[new int2(plotPixel.x + 1, plotPixel.y)];
                        if (!_plotNeighborLookup[plotID].Contains(neighborPixelPlotID))
                            _plotNeighborLookup[plotID].Add(neighborPixelPlotID);
                    }
                    // up
                    if (plotPixel.y < _plotMap.height - 1
                        && PixelPlotLookup.ContainsKey(new int2(plotPixel.x, plotPixel.y + 1)))
                    {
                        var neighborPixelPlotID = _pixelPlotLookup[new int2(plotPixel.x, plotPixel.y + 1)];
                        if (!_plotNeighborLookup[plotID].Contains(neighborPixelPlotID))
                            _plotNeighborLookup[plotID].Add(neighborPixelPlotID);
                    }
                    // down
                    if (plotPixel.y > 0
                        && PixelPlotLookup.ContainsKey(new int2(plotPixel.x, plotPixel.y - 1)))
                    {
                        var neighborPixelPlotID = _pixelPlotLookup[new int2(plotPixel.x, plotPixel.y - 1)];
                        if (!_plotNeighborLookup[plotID].Contains(neighborPixelPlotID))
                            _plotNeighborLookup[plotID].Add(neighborPixelPlotID);
                    }
                }
            }
        }

        private bool IsGraphConnected(Color32 startNode)
        {
            var visited = new HashSet<Color32>();
            var queue = new Queue<Color32>();
            queue.Enqueue(startNode);
            visited.Add(startNode);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                foreach (var neighbor in _plotNeighborLookup[node])
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return visited.Count == _plotNeighborLookup.Count;
        }

        private List<(Color32 u, Color32 v)> GetEdges()
        {
            var edges = new List<(Color32 u, Color32 v)>();

            foreach (var kvp in _plotNeighborLookup)
            {
                var u = kvp.Key;
                foreach (var v in kvp.Value)
                {
                    if (!u.Equals(v)) // Avoid duplicate edges in undirected graph
                        edges.Add((u, v));
                }
            }
            return edges;
        }

        private void RandomlyRemoveEdges(int numEdgesToRemove)
        {
            var edges = GetEdges();

            var random = new System.Random();
            edges = edges.OrderBy(_ => random.Next()).ToList();

            foreach (var edge in edges)
            {
                if (numEdgesToRemove == 0) break;

                _plotNeighborLookup[edge.u].Remove(edge.v);
                _plotNeighborLookup[edge.v].Remove(edge.u);


                if (!IsGraphConnected(edge.u)) // Check connectivity
                {
                    _plotNeighborLookup[edge.u].Add(edge.v);
                    _plotNeighborLookup[edge.v].Add(edge.u); // Restore edge
                }
                else
                {
                    numEdgesToRemove--;
                }
            }
        }

        private void TravelHeroTo(string commanderKey, Hero hero, Color32 source, Color32 target)
        {
            hero.MovementState = Hero.State.Traveling;

            hero.PathProgress = 0f;
            hero.PathIndex = 0;
            hero.Path = GetPathAStar(source, target, commanderKey);

        }

        // fortnite pass
            // no creep
            // spawn in random position
            // collect nearest weapon
            // fight nearest

        float lastPulseTime = 0f;
        public void OnUpdate(MapGameMatchRunner mainSystem)
        {
            // all capitals start with an emitter
            // emitters are idle across the map
            // a hero capturing a plot captures the emitter
            // emitters emit control/creep passively


            //if (Time.time - lastPulseTime > 0.25f)
            //{
            //    lastPulseTime = Time.time;
            //    var colorList = new List<Color32>();
            //    foreach(var plotKV in _plots)
            //    {
            //        var plotID = plotKV.Key;
            //        var plot = plotKV.Value;

            //        if (plot.CreepControllerID == null) continue;
            //        if (plot.CreepAmount < plot.MaxCreepAmount * 0.75f /*determines the "looseness" of the creep*/) continue;
                    
            //        if(plot.IsEmitter)
            //            plotKV.Value.CreepAmount = math.clamp(plotKV.Value.CreepAmount + plotKV.Value.MaxCreepAmount * 0.2f, 0f, plotKV.Value.MaxCreepAmount) ;

            //        colorList.Clear();
            //        var toPropagate = plot.CreepAmount * 0.1f /*determines propagation throughput*/;
            //        colorList = _plotNeighborLookup[plotID]
            //            .Where(pIdx => _plots[pIdx].CreepControllerID == null 
            //                            || IsAtWar(plot.CreepControllerID, _plots[pIdx].CreepControllerID)
            //                            || (_plots[pIdx].CreepAmount < _plots[pIdx].MaxCreepAmount && _plots[pIdx].CreepControllerID == plot.CreepControllerID)).ToList();

            //        foreach (var neighborPlotIdx in colorList)
            //        {
            //            var neighborPlot = _plots[neighborPlotIdx];

            //            var packet = new Packet { CommanderID = plot.CreepControllerID };
            //            _packets.Add(packet);

            //            packet.MissionLogic = Packet.Mission.AddCreep;
            //            packet.Path = GetPathAStar(plotID, neighborPlotIdx, plot.CreepControllerID);
            //            packet.AuxString = (toPropagate / colorList.Count+).ToString();
            //        }

            //    }
            //}

            foreach (var hero in _heroes)
            {
                if(hero.Value.MovementState == Hero.State.Traveling)
                {
                    if (hero.Value.PathIndex == hero.Value.Path.Count - 1)
                    {
                        // arrived
                        hero.Value.MovementState = Hero.State.Stationed;
                    }
                    else
                    {
                        hero.Value.PathProgress += 1f * Time.deltaTime;
                        if (hero.Value.PathProgress >= 1f)
                        {
                            hero.Value.PathProgress = 0f;
                            hero.Value.PathIndex++;
                            hero.Value.CurrentPlotIdx = hero.Value.Path[hero.Value.PathIndex];
                        }
                    }

                    continue;
                }
                
                var currentPlotKey = hero.Value.CurrentPlotIdx;
                var currentPlot = _plots[currentPlotKey];
                // use supply here or nah?
                if(Time.time - hero.Value.LastActionTime > 0.5f)
                {
                    hero.Value.LastActionTime = Time.time;

                    //// account for packets in transit
                    //var creepSent = new Dictionary<Color32, float>();
                    //foreach (var existingPacket in _packets)
                    //{
                    //    if (existingPacket.CommanderID == hero.Key)
                    //    {
                    //        var target = existingPacket.Path[existingPacket.Path.Count - 1];

                    //        if (existingPacket.MissionLogic == Packet.Mission.AddCreep)
                    //        {
                    //            if (!creepSent.ContainsKey(target))
                    //                creepSent.Add(target, 0f);
                    //            creepSent[target] += float.Parse(existingPacket.AuxString);//* commander.Value.ConstructionSupplyMultiplierStat;
                    //        }
                    //    }
                    //}

                    var didAction = false;

                    // how to chase an enemy
                    // case 1) assume they're moving
                    // set target to an unstationed neighbor of their current position
                    // EN ROUTE: what if their position changes?
                    // reset case 1
                    // EN ROUTE: what if they stop moving?
                    // reset, change to case 2
                    // case 2) assume they're stationary
                    // move to one of the neighboring unstationed plots
                    // what if all the neighboring plots are filled?
                    // for now, just cancel. later, maybe neighbor of a neighbor? or we could introduce ranges
                    // EN ROUTE: what if they start moving?
                    // reset, change to case 1


                    // Try to attack enemies neighboring your current position
                    //if (!didAction)
                    //{
                    //    var targets = _plotNeighborLookup[currentPlotKey].Where(pIdx => _heroes.Count(kv => kv.Key != hero.Key && kv.Value.CurrentPlotIdx.Equals(pIdx)) > 0);
                    //    if (targets.Count() > 0)
                    //    {
                    //        var target = targets.First();

                    //        var packet = new Packet { CommanderID = hero.Key };
                    //        _packets.Add(packet);

                    //        packet.MissionLogic = Packet.Mission.AttackHero;
                    //        packet.Path = GetPathAStar(currentPlotKey, target, hero.Key);
                    //        packet.AuxString = (1f * hero.Value.AttackDamage).ToString();

                    //        didAction = true;
                    //    }
                    //}

                    // Move to weaker target
                    //if (!didAction)
                    //{
                    //    var stationaryTargetNeighbors = _heroes
                    //        .Where(h => h.Key != hero.Key) /* not me */
                    //        .Where(h => h.Value.AttackDamage < hero.Value.AttackDamage) /* weaker than me */
                    //        .Where(h => h.Value.MovementState != Hero.State.Traveling) /* stationary */
                    //        .Where(h => _plotNeighborLookup[h.Value.CurrentPlotIdx] /* has unstationed neighbors */
                    //            .Count(nIdx => _heroes
                    //                            .Count(h =>  (h.Value.MovementState == Hero.State.Stationed && h.Value.CurrentPlotIdx.Equals(nIdx))
                    //                                || h.Value.MovementState == Hero.State.Traveling && h.Value.Path[h.Value.Path.Count-1].Equals(nIdx)) == 0) > 0)
                    //        .SelectMany(h => _plotNeighborLookup[h.Value.CurrentPlotIdx] /* get those neighbors */
                    //            .Where(nIdx => _heroes
                    //                            .Count(h => (h.Value.MovementState == Hero.State.Stationed && h.Value.CurrentPlotIdx.Equals(nIdx))
                    //                                || h.Value.MovementState == Hero.State.Traveling && h.Value.Path[h.Value.Path.Count - 1].Equals(nIdx)) == 0))
                    //        .OrderBy(pIdx => Vector3.Distance(_plots[pIdx].PlotWorldCenter, _plots[hero.Value.CurrentPlotIdx].PlotWorldCenter)) /* order by distance */
                    //        ;

                    //    if(stationaryTargetNeighbors.Count() > 0)
                    //    {
                    //        var target = stationaryTargetNeighbors.First();

                    //        TravelHeroTo(hero.Key, hero.Value, currentPlotKey, target);

                    //        didAction = true;
                    //    }
                    //}

                    // RELOCATE - Toward a weak enemy
                    //if (!didAction)
                    //{
                    //    var toHunt = _heroes
                    //        .Where(h => h.Value.AttackDamage < hero.Value.AttackDamage)
                    //        .OrderBy(h => Vector3.Distance(_plots[h.Value.CurrentPlotIdx].PlotWorldCenter, currentPlot.PlotWorldCenter))
                    //        .Select(h => h.Value)
                    //        .FirstOrDefault();

                    //    if (toHunt != null)
                    //    {
                    //        var target = toHunt.CurrentPlotIdx;

                    //        TravelHeroTo(hero.Key, hero.Value, currentPlotKey, target);

                    //        didAction = true;
                    //    }
                    //}

                    // Stationed Actions

                    // Priority 1
                    // Increase control in your current position
                    //if (!didAction)
                    //{
                    //    if (currentPlot.CreepControllerID != hero.Key
                    //        || currentPlot.CreepAmount < currentPlot.MaxCreepAmount)
                    //    {
                    //        var packet = new Packet { CommanderID = hero.Key };
                    //        _packets.Add(packet);

                    //        packet.MissionLogic = Packet.Mission.AddCreep;
                    //        packet.Path = GetPathAStar(currentPlotKey, currentPlotKey, hero.Key);
                    //        packet.AuxString = _creepSentPerPacket.ToString();

                    //        didAction = true;
                    //    }
                    //}

                    // Relocation Actions

                    // Priority 1 - Protect the Capital - Relocate to the capital if control < threshold
                    //if (!didAction)
                    //{
                    //    var capitalPlotID = _commanders[hero.Key].CapitalID;
                    //    var capitalPlot = _plots[capitalPlotID];
                    //    if(!hero.Value.CurrentPlotIdx.Equals(capitalPlotID)
                    //        && capitalPlot.CreepAmount < capitalPlot.MaxCreepAmount * 0.9f)
                    //    {
                    //        TravelHeroTo(hero.Key, hero.Value, currentPlotKey, capitalPlotID);

                    //        didAction = true;
                    //    }
                    //}

                    // Priority 2 - Declare War
                    //if (!didAction)
                    //{
                    //    var totalCreep = _commanderPlotLookup[hero.Key].Sum(pIdx => _plots[pIdx].CreepAmount);
                    //    foreach(var otherCommander in _commanders)
                    //    {
                    //        if (otherCommander.Key == hero.Key) continue;
                    //        if (IsAtWar(otherCommander.Key, hero.Key)) continue;

                    //        var totalEnemyCreep = _commanderPlotLookup[otherCommander.Key].Sum(pIdx => _plots[pIdx].CreepAmount);

                    //        if(totalCreep >= totalEnemyCreep * 0.75f)
                    //        {
                    //            var war = new War { AggressorID = hero.Key, DefenderID = otherCommander.Key };
                    //            _wars.Add(war);
                    //            break;
                    //        }
                    //    }
                    //    var capitalPlotID = _commanders[hero.Key].CapitalID;
                    //    var capitalPlot = _plots[capitalPlotID];
                    //    if (!hero.Value.CurrentPlotIdx.Equals(capitalPlotID)
                    //        && capitalPlot.CreepAmount < capitalPlot.MaxCreepAmount * 0.9f)
                    //    {
                    //        TravelHeroTo(hero.Key, hero.Value, currentPlotKey, capitalPlotID);

                    //        didAction = true;
                    //    }
                    //}

                    // Priority x - uhhhh let's just go top off non-capital
                    //if (!didAction)
                    //{
                    //    var plotIndices = _commanderPlotLookup[hero.Key].Where(pIdx => !pIdx.Equals(_commanders[hero.Key].CapitalID));
                    //    foreach(var plotIdx in plotIndices)
                    //    {
                    //        var plot = _plots[plotIdx];
                    //        if (!hero.Value.CurrentPlotIdx.Equals(plotIdx)
                    //            && plot.CreepAmount < plot.MaxCreepAmount * 0.75f)
                    //        {
                    //            TravelHeroTo(hero.Key, hero.Value, currentPlotKey, plotIdx);

                    //            didAction = true;
                    //        }
                    //    }
                    //}



                    // Try to send creep to empty neighbor
                    //if (!didAction)
                    //{
                    //    var targets = _plotNeighborLookup[currentPlotKey]
                    //        .Where(pIdx => (_plots[pIdx].CreepControllerID == null || _plots[pIdx].CreepControllerID == hero.Key) && _plots[pIdx].CreepAmount < _plots[pIdx].MaxCreepAmount)
                    //        .Where(pIdx => !creepSent.TryGetValue(pIdx, out var sentAmount) || sentAmount + _plots[pIdx].CreepAmount < _plots[pIdx].MaxCreepAmount)
                    //        .OrderByDescending(pIdx => {
                    //            var amount = _plots[pIdx].CreepAmount;
                    //            if (creepSent.TryGetValue(pIdx, out var sent))
                    //                amount += sent;
                    //            var creepAmountFactor = amount / _plots[pIdx].MaxCreepAmount;
                    //            var distanceFactor = Vector3.Distance(_plots[pIdx].PlotWorldCenter, currentPlot.PlotWorldCenter) / mainSystem.Map.width;

                    //            return (creepAmountFactor * 0.25f) + (distanceFactor * 0.75f); 
                    //        });

                    //    if(targets.Count() > 0)
                    //    {
                    //        var target = targets.First();

                    //        var packet = new Packet { CommanderID = hero.Key };
                    //        _packets.Add(packet);

                    //        packet.MissionLogic = Packet.Mission.AddCreep;
                    //        packet.Path = GetPathAStar(currentPlotKey, target, hero.Key);
                    //        TODO amount

                    //        didAction = true;
                    //    }
                    //}

                    // Try to send creep to occupied neighbor
                    //if (!didAction)
                    //{
                    //    var targets = _plotNeighborLookup[currentPlotKey]
                    //        .Where(pIdx => _plots[pIdx].CreepControllerID != hero.Key)
                    //        //.Where(pIdx => !creepSent.TryGetValue(pIdx, out var sentAmount) || sentAmount + _plots[pIdx].CreepAmount < _plots[pIdx].MaxCreepAmount)
                    //        .OrderByDescending(pIdx => {
                    //            var amount = _plots[pIdx].CreepAmount;
                    //            if (creepSent.TryGetValue(pIdx, out var sent))
                    //                amount += sent;
                    //            var creepAmountFactor = amount / _plots[pIdx].MaxCreepAmount;
                    //            var distanceFactor = Vector3.Distance(_plots[pIdx].PlotWorldCenter, currentPlot.PlotWorldCenter) / mainSystem.Map.width;

                    //            return (creepAmountFactor * 0.75f) + (distanceFactor * 0.25f);
                    //        });

                    //    if (targets.Count() > 0)
                    //    {
                    //        var target = targets.First();

                    //        var packet = new Packet { CommanderID = hero.Key };
                    //        _packets.Add(packet);

                    //        packet.MissionLogic = Packet.Mission.AddCreep;
                    //        packet.Path = GetPathAStar(currentPlotKey, target, hero.Key);
                    //        TODO amount  

                    //        didAction = true;
                    //    }
                    //}

                    // RELOCATE - To a virgin plot
                    //if (!didAction)
                    //{
                    //    // account for another hero traveling to that plot
                    //    // account for another hero present at that plot
                    //    var potentialTargets = _commanderPlotLookup[hero.Key]
                    //        .SelectMany(pIdx => _plotNeighborLookup[pIdx])
                    //        .Distinct()
                    //        .Where(pIdx => !pIdx.Equals(hero.Value.CurrentPlotIdx)
                    //            && _plots[pIdx].CreepControllerID == null
                    //            // no hero traveling to this location
                    //            && _heroes.Count(h => h.Value.MovementState == Hero.State.Traveling && pIdx.Equals(h.Value.Path[h.Value.Path.Count-1])) == 0
                    //            // hero stationed at this location
                    //            && _heroes.Count(h => h.Value.MovementState == Hero.State.Stationed && h.Value.CurrentPlotIdx.Equals(pIdx)) == 0);

                    //    if (potentialTargets.Count() > 0)
                    //    {
                    //        var target = potentialTargets.First();

                    //        TravelHeroTo(hero.Key, hero.Value, currentPlotKey, target);

                    //        didAction = true;
                    //    }
                    //}

                    // RELOCATE - To a random empty plot
                    //if (!didAction)
                    //{
                    //    var potentialTargets = _plots
                    //        .Select(kv => kv.Key)
                    //        .Where(pIdx => !pIdx.Equals(hero.Value.CurrentPlotIdx)
                    //            // no hero traveling to this location
                    //            && _heroes.Count(h => h.Value.MovementState == Hero.State.Traveling && pIdx.Equals(h.Value.Path[h.Value.Path.Count - 1])) == 0
                    //            // hero stationed at this location
                    //            && _heroes.Count(h => h.Value.MovementState == Hero.State.Stationed && h.Value.CurrentPlotIdx.Equals(pIdx)) == 0
                    //            )
                    //        .Shuffle();

                    //    if (potentialTargets.Count() > 0)
                    //    {
                    //        var target = potentialTargets.First();

                    //        TravelHeroTo(hero.Key, hero.Value, currentPlotKey, target);

                    //        didAction = true;
                    //    }
                    //}

                    //// RELOCATE - To an enemy plot
                    //if (!didAction)
                    //{
                    //    var potentialTargets = _commanderPlotLookup[hero.Key]
                    //        .SelectMany(pIdx => _plotNeighborLookup[pIdx])
                    //        .Distinct()
                    //        .Where(pIdx => !pIdx.Equals(hero.Value.CurrentPlotIdx)
                    //            && _plots[pIdx].CreepControllerID != null && _plots[pIdx].CreepControllerID != hero.Key);

                    //    if (potentialTargets.Count() > 0)
                    //    {
                    //        var target = potentialTargets.First();

                    //        TravelHeroTo(hero.Key, hero.Value, currentPlotKey, target);

                    //        didAction = true;
                    //    }
                    //}

                    //// RELOCATE - To a controlled plot we can attack neighboring creep from
                    //if (!didAction)
                    //{
                    //    var potentialTargets = _commanderPlotLookup[hero.Key]
                    //        .Where(pIdx => 
                    //        _plotNeighborLookup[pIdx].Count(nIdx => _plots[nIdx].CreepControllerID != null 
                    //            && !_plots[nIdx].CreepControllerID.Equals(hero.Key)
                    //            && _heroes.Count(h => h.Value.CurrentPlotIdx.Equals(nIdx))  == 0) > 0);

                    //    if (potentialTargets.Count() > 0)
                    //    {
                    //        var target = potentialTargets.First();

                    //        TravelHeroTo(hero.Key, hero.Value, currentPlotKey, target);

                    //        didAction = true;
                    //    }
                    //}
                }



                // what does a hero actually do?
                // creates and sends attack packets
                // will attack any enemy in a neighboring plot

                // how often should heroes make decisions?
                // continuously
                // may get hyper-frequent behavior changes
                // at x rate
                // smoother
                // manipulable rate

                // i stand in my starting province
                // i have no enemy neighbors
                // i want to capture neighbor empty territory        
                // what does capturing land give me?
                // - increased "decision speed" (fmrly Prodction)





                // later...
                // i have enemy neighbors
                // i want to destroy my enemies
            }

            var toDestroy = new List<Packet>();
            var toAdd = new List<Packet>();
            var heroesToDestroy = new List<string>();
            foreach (var packet in _packets)
            {
                // arrived
                if (packet.PathIndex == packet.Path.Count - 1)
                {
                    toDestroy.Add(packet);

                    var currentPlotIndex = packet.Path[packet.PathIndex];
                    var currentPlot = _plots[currentPlotIndex];
                    if (packet.MissionLogic == Packet.Mission.EstablishConstructionProject)
                    {
                        if (currentPlot.Building != null && packet.CommanderID != currentPlot.Building.CommanderID)
                        {
                            Debug.Log("Trying to establish a construction project in controlled enemy territory");
                            continue;
                        }

                        var supplyNeeded = _buildingLevel1ConstructionSupply;
                        if (currentPlot.Building != null && currentPlot.Building.CommanderID == packet.CommanderID)
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
                        currentPlot.ConstructionProjects.Add(project);
                    }
                    else if (packet.MissionLogic == Packet.Mission.SupplyConstructionProject)
                    {
                        foreach (var project in currentPlot.ConstructionProjects)
                        {
                            if (project.CommanderID != packet.CommanderID) continue;

                            project.Supply = math.clamp(project.Supply + _constructionSupplyPerPacket * _commanders[project.CommanderID].ConstructionSupplyMultiplierStat, 0f, project.MaxSupply);
                            if (project.Supply >= project.MaxSupply)
                            {
                                currentPlot.ConstructionProjects.Remove(project);
                                if (currentPlot.Building == null) // spawn level 1
                                {
                                    _commanderPlotLookup[project.CommanderID].Add(currentPlotIndex);
                                    currentPlot.Building = new Building 
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
                                    foreach (var otherProject in currentPlot.ConstructionProjects)
                                    {
                                        if (otherProject.CommanderID == project.CommanderID) continue;

                                        projectsToDestroy.Add(otherProject);
                                    }
                                    foreach (var otherProject in projectsToDestroy)
                                        currentPlot.ConstructionProjects.Remove(otherProject);
                                }
                                else // upgrade to level 2
                                {
                                    UpgradePlotBuilding(currentPlot);
                                }
                            }
                            break;
                        }
                    }
                    else if (packet.MissionLogic == Packet.Mission.SupplyBuilding)
                    {
                        if (currentPlot.Building != null)
                        {
                            var building = currentPlot.Building;
                            if (building.Supply >= building.MaxSupply || building.CommanderID != packet.CommanderID) continue;

                            building.Supply = math.clamp(building.Supply + _buildingSupplyPerPacket * _commanders[packet.CommanderID].ConstructionSupplyMultiplierStat, 0f, building.MaxSupply);
                            continue;
                        }
                    }
                    else if (packet.MissionLogic == Packet.Mission.AttackHero)
                    {
                        // note - this is not checking the actual hero's position
                        if(_heroes.Count(h => h.Key != packet.CommanderID && h.Value.CurrentPlotIdx.Equals(currentPlotIndex)) > 0)
                        {
                            var heroToAttack = _heroes.FirstOrDefault(h => h.Key != packet.CommanderID && h.Value.CurrentPlotIdx.Equals(currentPlotIndex));
                            heroToAttack.Value.HP = math.clamp(heroToAttack.Value.HP - float.Parse(packet.AuxString), 0f, heroToAttack.Value.HPMax);
                            if(heroToAttack.Value.HP <= 0)
                            {
                                // DIE
                                //heroToAttack.Value.Disabled = true;
                                heroesToDestroy.Add(heroToAttack.Key);
                            }
                        }
                        continue;
                    }
                    else if(packet.MissionLogic == Packet.Mission.AddCreep)
                    {
                        var amountToAdd = float.Parse(packet.AuxString);
                        // if we control it, just add to count
                        // if not, we subtract to 0. if we have any left, we take control w that amount

                        // it's neutral
                        if (currentPlot.CreepControllerID == null) 
                        {
                            currentPlot.CreepControllerID = packet.CommanderID;
                            _commanderPlotLookup[packet.CommanderID].Add(currentPlotIndex);
                        }
                        // it's yours
                        else if (packet.CommanderID == currentPlot.CreepControllerID)
                        {
                            currentPlot.CreepAmount = math.clamp(currentPlot.CreepAmount + amountToAdd, 0f, currentPlot.MaxCreepAmount);
                        }
                        else
                        {
                            if(currentPlot.CreepAmount > amountToAdd)
                            {
                                currentPlot.CreepAmount = math.clamp(currentPlot.CreepAmount - amountToAdd, 0f, currentPlot.MaxCreepAmount);
                            }
                            else
                            {
                                _commanderPlotLookup[currentPlot.CreepControllerID].Remove(currentPlotIndex);
                                amountToAdd -= currentPlot.CreepAmount;
                                if(amountToAdd > 0f)
                                {
                                    currentPlot.CreepAmount = amountToAdd;
                                    currentPlot.CreepControllerID = packet.CommanderID;
                                    _commanderPlotLookup[packet.CommanderID].Add(currentPlotIndex);
                                }
                                else
                                {
                                    currentPlot.CreepAmount = 0f;
                                    currentPlot.CreepControllerID = null;
                                }
                            }
                        }
                        continue;
                    }
                    else if(packet.MissionLogic == Packet.Mission.DowngradeAndRedeployBuildingLevel)
                    {
                        if(currentPlot.Building != null && packet.CommanderID == currentPlot.Building.CommanderID)
                        {
                            var targetPlotID = packet.AuxColor;
                            var turretPlot = _plots[currentPlotIndex];
                            DowngradePlotBuilding(turretPlot);

                            var newPacket = new Packet { CommanderID = packet.CommanderID };
                            newPacket.MissionLogic = Packet.Mission.UpgradeBuildingLevel;
                            newPacket.Path = GetPathAStar(currentPlotIndex, targetPlotID, packet.CommanderID);
                            
                            toAdd.Add(newPacket);
                        }
                        
                    }
                    else if (packet.MissionLogic == Packet.Mission.UpgradeBuildingLevel)
                    {
                        if(currentPlot.Building != null && currentPlot.Building.CommanderID == packet.CommanderID)
                        {
                            UpgradePlotBuilding(currentPlot);
                        }
                    }
                }
                else
                {
                    var relevantStat = packet.MissionLogic == Packet.Mission.AttackHero ? _commanders[packet.CommanderID].AttackDamageMultiplierStat : _commanders[packet.CommanderID].PacketSpeedStat;
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
            foreach(var commanderKey in heroesToDestroy)
                _heroes.Remove(commanderKey);
        }

        private List<Color32> GetPathAStar(
            Color32 startPlotKey,
            Color32 destinationPlotKey,
            string commander = null,
            bool forceFriendly = true)
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

                        if (forceFriendly 
                            && commander != null
                            && _plots[neighborIdx].CreepControllerID != commander)
                            newCost = float.MaxValue;
                        
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