using bet_slum.Games.MapGame.Simulation;
using bet_slum.Games.MapGame.Simulation.EntityArchetypes;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Burst.Intrinsics;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace bet_slum.Games.MapGame.View
{
    public class ViewSystem
    {
        private ObjectPool<PlotInfoUI> _plotInfoWidgetPool;
        private Dictionary<Color32, PlotInfoUI> _plotInfoUIWidgets = new();

        private ObjectPool<CompetitorInfoUI> _competitorWidgetPool;
        private Dictionary<string, CompetitorInfoUI> _competitorLabels = new();

        private ObjectPool<ArmyView> _armyViewPool;
        private Dictionary<Army, ArmyView> _armyViews = new();

        private List<LineRenderer> _roadViews = new();

        public void Initialize(MapGameMatchRunner mainSystem)
        {
            if (_plotInfoWidgetPool == null)
                _plotInfoWidgetPool = new(() => GameObject.Instantiate(mainSystem.PlotInfoUIPrototype, mainSystem.PlotInfoUIPrototype.transform.parent), widget => widget.gameObject.SetActive(true), widget => widget.gameObject.SetActive(false), widget => GameObject.Destroy(widget.gameObject));

            if (_competitorWidgetPool == null)
                _competitorWidgetPool = new(() => GameObject.Instantiate(mainSystem.CompetitorLabelPrototype, mainSystem.CompetitorLabelPrototype.transform.parent), label => label.gameObject.SetActive(true), label => label.gameObject.SetActive(false), label => GameObject.Destroy(label.gameObject));

            if (_armyViewPool == null)
                _armyViewPool = new(() =>
                {
                    var view = GameObject.Instantiate(mainSystem.ArmyViewPrototype);
                    view.ArmyModelRenderer.material = new Material(view.ArmyModelRenderer.material);
                    return view;
                }, view => view.gameObject.SetActive(true), view => view.gameObject.SetActive(false), view => GameObject.Destroy(view.gameObject));

            // TODO - do this for those cylinders wherever they are
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
                    var road = GameObject.Instantiate(mainSystem.RoadPrototype);
                    road.SetPosition(0, mainSystem.SimulationSystem.Plots[plotNeighbors.Key].PlotWorldCenter);
                    road.SetPosition(1, mainSystem.SimulationSystem.Plots[target].PlotWorldCenter);
                    _roadViews.Add(road);
                    shownPairs.Add((plotNeighbors.Key, target));
                }
            }


            mainSystem.RoadPrototype.gameObject.SetActive(false);
            mainSystem.PlotInfoUIPrototype.gameObject.SetActive(false);
            mainSystem.CompetitorLabelPrototype.gameObject.SetActive(false);
            mainSystem.ArmyViewPrototype.gameObject.SetActive(false);

            mainSystem.MapMesh.Initialize(new int2(mainSystem.Map.width, mainSystem.Map.height), mainSystem.Map);
        }

        public void OnUpdate(MapGameMatchRunner mainSystem)
        {
            foreach (var plot in mainSystem.SimulationSystem.Plots)
            {
                if (plot.Key.Equals(SimulationSystem.DeadPlotID)) continue;

                if (!_plotInfoUIWidgets.TryGetValue(plot.Key, out var infoWidget))
                {
                    infoWidget = _plotInfoWidgetPool.Get();
                    _plotInfoUIWidgets.Add(plot.Key, infoWidget);
                }

                infoWidget.transform.position = Camera.main.WorldToScreenPoint(plot.Value.PlotWorldCenter);
                infoWidget.transform.position += Vector3.up * 50f;

                infoWidget.CountLabel.SetText($"{plot.Value.Population}");
                infoWidget.GrowthProgressMeter.value = plot.Value.CurrentGrowthProgress / 100f/*TODO extract*/;
            }

            foreach (var commander in mainSystem.SimulationSystem.Commanders)
            {
                var competitor = mainSystem.CompetitorData.GetCompetitorData(commander.CompetitorID);

                if (!_competitorLabels.TryGetValue(commander.CompetitorID, out var widget))
                {
                    widget = _competitorWidgetPool.Get();
                    _competitorLabels.Add(commander.CompetitorID, widget);

                    widget.Text.SetText($"<color=#{ColorUtility.ToHtmlStringRGBA(commander.TestData.DisplayColor)}>{competitor.name}</color>");
                    widget.StatDisplay_STD.Label.SetText($"{Mathf.RoundToInt(commander.StewardshipSkill)}");
                    widget.StatDisplay_ATT.Label.SetText($"{Mathf.RoundToInt(commander.AttentionSkill)}");
                }

                var population = mainSystem.SimulationSystem.GetTotalPopulation(commander);
                if (population <= 0)
                {
                    widget.gameObject.SetActive(false);
                    continue;
                }

                widget.gameObject.SetActive(true);

                var capitalWorldPosition = mainSystem.SimulationSystem.Plots[commander.CapitalPlotID].PlotWorldCenter;
                widget.transform.position = Camera.main.WorldToScreenPoint(capitalWorldPosition);
                widget.transform.position -= Vector3.up * 25f;
                widget.PopDisplay.Label.SetText($"{population}");
            }

            // need to rework this wholly to account for armies being nulled out
            foreach (var indicator in _armyViews)
                _armyViewPool.Release(indicator.Value);
            _armyViews.Clear();
            foreach (var army in mainSystem.SimulationSystem.Armies)
            {
                var view = _armyViewPool.Get();

                // Set Position
                var currentPlotCenter = mainSystem.SimulationSystem.Plots[army.Path[army.PathIndex]].PlotWorldCenter;
                var targetPlotCenter = army.PathIndex == army.Path.Count - 1 ? currentPlotCenter : mainSystem.SimulationSystem.Plots[army.Path[army.PathIndex + 1]].PlotWorldCenter;
                var progressPosition = Vector3.Lerp(currentPlotCenter, targetPlotCenter, army.PlotProgress / 1f);
                view.transform.position = progressPosition;
                var scale = 2f;
                view.transform.localScale = Vector3.one * scale;
                view.transform.position += Vector3.up * scale;
                view.PopulationLabel.SetText($"{army.Population}");

                // Set Color
                var color = army.Commander.TestData.DisplayColor;
                Renderer renderer = view.GetComponent<Renderer>();
                renderer.material.color = color;

                _armyViews.Add(army, view);
            }
        }
    }
}