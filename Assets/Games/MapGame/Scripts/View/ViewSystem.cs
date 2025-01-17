using bet_slum.Games.MapGame.Simulation.EntityArchetypes;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace bet_slum.Games.MapGame.View
{
    public class ViewSystem
    {
        private ObjectPool<PlotInfoUI> _plotInfoWidgetPool;
        private List<PlotInfoUI> _plotInfoUIWidgets = new();

        private ObjectPool<CompetitorInfoUI> _competitorWidgetPool;
        private Dictionary<string, CompetitorInfoUI> _competitorLabels = new();

        private ObjectPool<GameObject> _plotBuildingViewPool;
        private Dictionary<Color32, GameObject> _plotBuildingViews = new();

        private ObjectPool<GameObject> _packetViewPool;
        private List<GameObject> _packetViews = new();

        private List<LineRenderer> _roadViews = new();

        public void Initialize(MapGameMatchRunner mainSystem)
        {
            Debug.Log("Initializing View");
            if (_plotInfoWidgetPool == null)
                _plotInfoWidgetPool = new(() => GameObject.Instantiate(mainSystem.PlotInfoUIPrototype, mainSystem.PlotInfoUIPrototype.transform.parent), widget => widget.gameObject.SetActive(true), widget => widget.gameObject.SetActive(false), widget => GameObject.Destroy(widget.gameObject));

            if (_competitorWidgetPool == null)
                _competitorWidgetPool = new(() => GameObject.Instantiate(mainSystem.CompetitorLabelPrototype, mainSystem.CompetitorLabelPrototype.transform.parent), label => label.gameObject.SetActive(true), label => label.gameObject.SetActive(false), label => GameObject.Destroy(label.gameObject));

            if (_packetViewPool == null)
            {
                _packetViewPool = new(() =>
                {
                    var go = GameObject.Instantiate(mainSystem.PacketViewPrototype, mainSystem.PacketViewPrototype.transform.parent);
                    go.GetComponent<Renderer>().material = new Material(go.GetComponent<Renderer>().material);
                    return go;
                }, go => go.gameObject.SetActive(true), go => go.gameObject.SetActive(false), go => GameObject.Destroy(go.gameObject));
            }

            if (_plotBuildingViewPool == null)
            {
                _plotBuildingViewPool = new(() =>
                {
                    var go = GameObject.Instantiate(mainSystem.BuildingViewPrototype, mainSystem.BuildingViewPrototype.transform.parent);
                    go.GetComponent<Renderer>().material = new Material(go.GetComponent<Renderer>().material);
                    return go;
                }, go => go.gameObject.SetActive(true), go => go.gameObject.SetActive(false), go => GameObject.Destroy(go.gameObject));
            }

            foreach (var view in _roadViews)
                GameObject.Destroy(view.gameObject);
            _roadViews.Clear();
            var shownPairs = new List<(Color32 a, Color32 b)>();
            foreach (var plotNeighbors in mainSystem.SimulationSystem.PlotNeighborLookup)
            {
                var sourcePlot = mainSystem.SimulationSystem.Plots[plotNeighbors.Key];
                foreach (var target in plotNeighbors.Value)
                {
                    // todo - some kinda comparer here - this whole imp is trash cba
                    if (shownPairs.Count(p => (p.a.Equals(plotNeighbors.Key) && p.b.Equals(target))
                        || (p.b.Equals(plotNeighbors.Key) && p.a.Equals(target))) > 0)
                        continue;

                    // spawn pair road
                    var road = GameObject.Instantiate(mainSystem.RoadPrototype, mainSystem.RoadPrototype.transform.parent);
                    road.SetPosition(0, mainSystem.SimulationSystem.Plots[plotNeighbors.Key].PlotWorldCenter);
                    road.SetPosition(1, mainSystem.SimulationSystem.Plots[target].PlotWorldCenter);
                    _roadViews.Add(road);
                    shownPairs.Add((plotNeighbors.Key, target));
                }
            }


            mainSystem.RoadPrototype.gameObject.SetActive(false);
            mainSystem.PlotInfoUIPrototype.gameObject.SetActive(false);
            mainSystem.CompetitorLabelPrototype.gameObject.SetActive(false); 
            mainSystem.PacketViewPrototype.gameObject.SetActive(false);
            mainSystem.BuildingViewPrototype.gameObject.SetActive(false);

            mainSystem.MapMesh.Initialize(new int2(mainSystem.Map.width, mainSystem.Map.height), mainSystem.Map);
        }

        // move me
        private List<Vector3> CalculateOffsetPositions(int count, float radius = 1f)
        {
            List<Vector3> offsets = new List<Vector3>();
            if (count == 0) return offsets;

            float angleStep = 360f / count;
            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                offsets.Add(new Vector3(x, 0f, z));
            }
            return offsets;
        }

        public void OnUpdate(MapGameMatchRunner mainSystem)
        {
            //Debug.Log("View Update");
            foreach(var widget in _plotInfoUIWidgets)
                _plotInfoWidgetPool.Release(widget);
            _plotInfoUIWidgets.Clear();
            foreach(var plot in mainSystem.SimulationSystem.Plots)
            {
                var buildingWorldPositions = CalculateOffsetPositions(plot.Value.ConstructionProjects.Count, radius: 0.25f);
                var buildingPositionIdx = 0;
                foreach (var project in plot.Value.ConstructionProjects)
                {
                    var widget = _plotInfoWidgetPool.Get();
                    _plotInfoUIWidgets.Add(widget);

                    widget.Slider.fillRect.transform.GetComponentInChildren<Image>().color = mainSystem.SimulationSystem.Commanders[project.CommanderID].TestData.DisplayColor;
                    widget.Slider.value = project.Supply / project.MaxSupply;

                    widget.transform.position = Camera.main.WorldToScreenPoint(plot.Value.PlotWorldCenter + buildingWorldPositions[buildingPositionIdx++]);
                }

                if(!_plotBuildingViews.TryGetValue(plot.Key, out var view))
                {
                    view = _plotBuildingViewPool.Get();
                    view.transform.position = plot.Value.PlotWorldCenter;
                    _plotBuildingViews.Add(plot.Key, view);
                }
                
                if(plot.Value.Building == null
                    || plot.Value.Building.Level < 2)
                {
                    view.gameObject.SetActive(false);
                }
                else
                {
                    view.gameObject.SetActive(true); 
                    view.GetComponent<Renderer>().material.color = Color.Lerp(new Color(249f / 256f, 180f / 256f, 45f / 256f), Color.white, math.clamp((Time.time - plot.Value.Building.LastActionTime) / plot.Value.Building.ActionRate, 0f, 1f));

                }
            }

            foreach (var view in _packetViews)
                _packetViewPool.Release(view);
            _packetViews.Clear();
            foreach (var packet in mainSystem.SimulationSystem.Packets)
            {
                var view = _packetViewPool.Get();
                _packetViews.Add(view);

                var currentPlotCenter = mainSystem.SimulationSystem.Plots[packet.Path[packet.PathIndex]].PlotWorldCenter;
                var targetPlotCenter = packet.PathIndex == packet.Path.Count - 1 ? currentPlotCenter : mainSystem.SimulationSystem.Plots[packet.Path[packet.PathIndex + 1]].PlotWorldCenter;
                var progressPosition = Vector3.Lerp(currentPlotCenter, targetPlotCenter, packet.PathProgress / 1f);
                view.transform.position = progressPosition;
                view.GetComponent<Renderer>().material.color = packet.MissionLogic == Packet.Mission.AttackBuilding
                    ? Color.red
                    : (packet.MissionLogic == Packet.Mission.DowngradeAndRedeployBuildingLevel || packet.MissionLogic == Packet.Mission.UpgradeBuildingLevel)
                        ? Color.Lerp(Color.yellow, mainSystem.SimulationSystem.Commanders[packet.CommanderID].TestData.DisplayColor, 0.5f)
                        : Color.Lerp(Color.white, mainSystem.SimulationSystem.Commanders[packet.CommanderID].TestData.DisplayColor, 0.7f);
                view.transform.localScale = Vector3.one * 
                    (packet.MissionLogic == Packet.Mission.AttackBuilding 
                    ? 10f 
                    : packet.MissionLogic == Packet.Mission.UpgradeBuildingLevel || packet.MissionLogic == Packet.Mission.DowngradeAndRedeployBuildingLevel 
                        ? 8.5f
                        : 7f);
            }

            foreach (var commander in mainSystem.SimulationSystem.Commanders)
            {
                var competitor = mainSystem.CompetitorData.GetCompetitorData(commander.Value.CompetitorID);

                if (!_competitorLabels.TryGetValue(commander.Value.CompetitorID, out var widget))
                {
                    widget = _competitorWidgetPool.Get();
                    _competitorLabels.Add(commander.Value.CompetitorID, widget);

                    widget.Text.SetText($"<color=#{ColorUtility.ToHtmlStringRGBA(commander.Value.TestData.DisplayColor)}>{competitor.name}</color>");
                    widget.StatDisplay_STD.Label.SetText($"{Mathf.RoundToInt(commander.Value.ConstructionSupplyMultiplierStat)}");
                    widget.StatDisplay_ATT.Label.SetText($"{Mathf.RoundToInt(commander.Value.AttackDamageMultiplierStat)}");
                    widget.StatDisplay_DEF.Label.SetText($"{Mathf.RoundToInt(commander.Value.PacketSpeedStat)}");
                    widget.ProductionDisplay.fillRect.transform.GetComponentInChildren<Image>().color 
                        = Color.Lerp(Color.white, commander.Value.TestData.DisplayColor, 0.5f);

                }

                if (mainSystem.SimulationSystem.CommanderPlotLookup[commander.Key].Count <= 0)
                {
                    widget.gameObject.SetActive(false);
                    continue;
                }

                var capitalWorldPosition = mainSystem.SimulationSystem.Plots[commander.Value.CapitalID].PlotWorldCenter;
                widget.transform.position = Camera.main.WorldToScreenPoint(capitalWorldPosition);
                widget.transform.position -= Vector3.up * 25f;

                widget.ProductionDisplay.value = commander.Value.ProductionProgress / 1f;
            }
        }
    }
}