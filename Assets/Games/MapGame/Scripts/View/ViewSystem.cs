using bet_slum.Games.MapGame.Simulation.EntityArchetypes;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;

namespace bet_slum.Games.MapGame.View
{
    public class ViewSystem
    {
        private float _maxIndicatorHeight = 10f;

        private ObjectPool<GameObject> _popIndicatorPool;
        private Dictionary<Color32, GameObject> _plotPopIndicators = new();

        private enum BorderMode
        {
            None,
            Plot,
            Commander
        }
        private BorderMode _currentBorderMode = BorderMode.None;

        public void Initialize(MapGameMatchRunner mainSystem)
        {
            if(_popIndicatorPool == null)
                _popIndicatorPool = new(() => GameObject.CreatePrimitive(PrimitiveType.Cube), indicator => indicator.SetActive(true), indicator => indicator.SetActive(false), indicator => GameObject.Destroy(indicator));

            var pixels32 = mainSystem.Map.GetPixels32();
            for (int i = 0; i < pixels32.Length; i++)
            {
                var color = pixels32[i];

                if (!_plotPopIndicators.ContainsKey(color))
                    _plotPopIndicators.Add(color, _popIndicatorPool.Get());
            }

            mainSystem.MapMesh.Initialize(new int2(mainSystem.Map.width, mainSystem.Map.height), mainSystem.Map);
        }

        public void OnUpdate(MapGameMatchRunner mainSystem)
        {
            // Draw Plot Borders
            var borderHeight = 0.01f;
            // Draw Population stacks
            switch (_currentBorderMode)
            {
                case BorderMode.Commander:
                    foreach (var plotCoordinates in mainSystem.SimulationSystem.PlotPixelLookup)
                    {
                        var plotCommander = mainSystem.SimulationSystem.Plots[plotCoordinates.Key].Controller;
                        foreach (var coordinate in plotCoordinates.Value)
                        {
                            var leftCoordinate = new int2(coordinate.x - 1, coordinate.y);
                            if (coordinate.x > 0
                                && !mainSystem.SimulationSystem.PixelPlotLookup[leftCoordinate].Equals(plotCoordinates.Key)
                                && mainSystem.SimulationSystem.Plots[mainSystem.SimulationSystem.PixelPlotLookup[leftCoordinate]].Controller != plotCommander)
                            {
                                Debug.DrawLine(new Vector3(coordinate.x, borderHeight, coordinate.y), new Vector3(coordinate.x, borderHeight, coordinate.y + 1f), Color.black);
                            }

                            var downCoordinate = new int2(coordinate.x, coordinate.y - 1);
                            if (coordinate.y > 0
                                && !mainSystem.SimulationSystem.PixelPlotLookup[downCoordinate].Equals(plotCoordinates.Key)
                                && mainSystem.SimulationSystem.Plots[mainSystem.SimulationSystem.PixelPlotLookup[downCoordinate]].Controller != plotCommander)
                            {
                                Debug.DrawLine(new Vector3(coordinate.x, borderHeight, coordinate.y), new Vector3(coordinate.x + 1f, borderHeight, coordinate.y), Color.black);
                            }

                            var upCoordinate = new int2(coordinate.x, coordinate.y + 1);
                            if (coordinate.y < mainSystem.Map.height - 1
                                && !mainSystem.SimulationSystem.PixelPlotLookup[upCoordinate].Equals(plotCoordinates.Key)
                                && mainSystem.SimulationSystem.Plots[mainSystem.SimulationSystem.PixelPlotLookup[upCoordinate]].Controller != plotCommander)
                            {
                                Debug.DrawLine(new Vector3(coordinate.x, borderHeight, coordinate.y + 1f), new Vector3(coordinate.x + 1f, borderHeight, coordinate.y + 1f), Color.black);
                            }

                            var rightCoordinate = new int2(coordinate.x + 1, coordinate.y);
                            if (coordinate.x < mainSystem.Map.width - 1
                                && !mainSystem.SimulationSystem.PixelPlotLookup[new int2(rightCoordinate)].Equals(plotCoordinates.Key)
                                && mainSystem.SimulationSystem.Plots[mainSystem.SimulationSystem.PixelPlotLookup[rightCoordinate]].Controller != plotCommander)
                            {
                                Debug.DrawLine(new Vector3(coordinate.x + 1f, borderHeight, coordinate.y), new Vector3(coordinate.x + 1f, borderHeight, coordinate.y + 1f), Color.black);
                            }
                        }
                    }
                    break;
                case BorderMode.Plot:
                    foreach (var plotCoordinates in mainSystem.SimulationSystem.PlotPixelLookup)
                    {
                        foreach (var coordinate in plotCoordinates.Value)
                        {
                            if (coordinate.x > 0
                                && !mainSystem.SimulationSystem.PixelPlotLookup[new int2(coordinate.x - 1, coordinate.y)].Equals(plotCoordinates.Key))
                            {
                                Debug.DrawLine(new Vector3(coordinate.x, borderHeight, coordinate.y), new Vector3(coordinate.x, borderHeight, coordinate.y + 1f), Color.black);
                            }
                            if (coordinate.y > 0
                                && !mainSystem.SimulationSystem.PixelPlotLookup[new int2(coordinate.x, coordinate.y - 1)].Equals(plotCoordinates.Key))
                            {
                                Debug.DrawLine(new Vector3(coordinate.x, borderHeight, coordinate.y), new Vector3(coordinate.x + 1f, borderHeight, coordinate.y), Color.black);
                            }
                            if (coordinate.y < mainSystem.Map.height - 1
                                && !mainSystem.SimulationSystem.PixelPlotLookup[new int2(coordinate.x, coordinate.y + 1)].Equals(plotCoordinates.Key))
                            {
                                Debug.DrawLine(new Vector3(coordinate.x, borderHeight, coordinate.y + 1f), new Vector3(coordinate.x + 1f, borderHeight, coordinate.y + 1f), Color.black);
                            }
                            if (coordinate.x < mainSystem.Map.width - 1
                                && !mainSystem.SimulationSystem.PixelPlotLookup[new int2(coordinate.x + 1, coordinate.y)].Equals(plotCoordinates.Key))
                            {
                                Debug.DrawLine(new Vector3(coordinate.x + 1f, borderHeight, coordinate.y), new Vector3(coordinate.x + 1f, borderHeight, coordinate.y + 1f), Color.black);
                            }
                        }
                    }
                    break;
                case BorderMode.None:
                default:
                    break;
            }

            foreach (var plot in mainSystem.SimulationSystem.Plots)
            {
                var indicator = _plotPopIndicators[plot.Key];
                var height = (plot.Value.Population / (float)mainSystem.SimulationSystem.MaxPopulationCap) * _maxIndicatorHeight;
                indicator.transform.localScale = new Vector3(height, height, height);
                indicator.transform.position = new Vector3(plot.Value.PlotWorldCenter.x, height / 2f, plot.Value.PlotWorldCenter.z);
            }

            foreach (var war in mainSystem.SimulationSystem.Wars)
            {
                Debug.DrawLine(
                        new Vector3(mainSystem.SimulationSystem.Plots[war.Attacker.CapitalPlotID].PlotWorldCenter.x, 1f, mainSystem.SimulationSystem.Plots[war.Attacker.CapitalPlotID].PlotWorldCenter.z),
                        new Vector3(mainSystem.SimulationSystem.Plots[war.Defender.CapitalPlotID].PlotWorldCenter.x, 1f, mainSystem.SimulationSystem.Plots[war.Defender.CapitalPlotID].PlotWorldCenter.z),
                        Color.red);
            }
        }
    }
}