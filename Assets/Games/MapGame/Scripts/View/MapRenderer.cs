using bet_slum.Games.MapGame.Simulation;
using bet_slum.Games.MapGame.Simulation.EntityArchetypes;
using bet_slum.Utility.Rendering;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace bet_slum.Games.MapGame.View
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MapRenderer : MonoBehaviour
    {
        private NativeHashMap<int2, float> _height;
        private NativeHashMap<int2, int> _terrain;
        private Mesh _mesh;

        private MeshFilter _filter;
        private MeshRenderer _renderer;
        private Texture2D _modifiedTexture;

        private Dictionary<Color32, Color32> _lastPlotColor = new();
        // TODO - split this logic up
        public void UpdateControlledMapView(MapGameMatchRunner mainSystem)
        {
            return;
            Profiler.BeginSample("Get pixels");
            var pixels = _modifiedTexture.GetPixels32();
            Profiler.EndSample();

            //var mapPixels = GameRunner.Instance.
            //(_renderer.material.mainTexture as Texture2D).SetPixels();
            Profiler.BeginSample("Pixel Update");
            foreach(var plot in mainSystem.SimulationSystem.Plots)
            {
                if (plot.Key.Equals(SimulationSystem.DeadPlotID)) continue;
                // if owner/color has changed, we update those pixels only
                var plotColor = new Color32(0,200,0,255);
                if (plot.Value.CreepControllerID != null) 
                {
                    var hpPct = plot.Value.CreepAmount / plot.Value.MaxCreepAmount;
                    var commanderColor = mainSystem.SimulationSystem.Commanders[plot.Value.CreepControllerID].TestData.DisplayColor;

                    var minColor = Color.Lerp(Color.white, commanderColor, 0.35f);
                    plotColor = Color.Lerp(minColor, commanderColor, math.clamp(hpPct, 0f, 1f));
                }

                var shouldUpdate = false;
                if(!_lastPlotColor.TryGetValue(plot.Key, out var lastColor))
                {
                    _lastPlotColor[plot.Key] = lastColor;
                    // TODO - UPDATE
                    shouldUpdate = true;
                }

                shouldUpdate |= !_lastPlotColor[plot.Key].Equals(plotColor);
                if (shouldUpdate)
                {
                    foreach(var pixelCoordinate in mainSystem.SimulationSystem.PlotPixelLookup[plot.Key])
                    {
                        var pixelIdx = pixelCoordinate.y * mainSystem.Map.width + pixelCoordinate.x;
                        pixels[pixelIdx] = plotColor;
                    }
                }
            }
            Profiler.EndSample();

            Profiler.BeginSample("Apply Pixel Update");
            var tex = _renderer.material.mainTexture as Texture2D;
            tex.SetPixels32(pixels);
            tex.Apply();
            Profiler.EndSample();
        }

        public void Initialize(int2 dimensions, Texture2D texture)
        {
            _filter = GetComponent<MeshFilter>();
            _renderer = GetComponent<MeshRenderer>();

            if (dimensions.x <= 0 || dimensions.y <= 0) throw new System.Exception($"Must move positive grid dimension");

            if (!_height.IsCreated)
                _height = new NativeHashMap<int2, float>(dimensions.x * dimensions.y, Allocator.Persistent);
            else
                _height.Clear();

            if(!_terrain.IsCreated)
                _terrain = new NativeHashMap<int2, int>(dimensions.x * dimensions.y, Allocator.Persistent);
            else
                _terrain.Clear();

            for (int x = 0; x < dimensions.x; x++)
            {
                for (int y = 0; y < dimensions.y; y++)
                {
                    var index = new int2(x, y);
                    _height.Add(index, 0f);
                    _terrain.Add(index, 0);
                }
            }

            // TODO - make sure everything is disposed properly down here on reset
            _mesh = PixelMesh.GenerateParallel(dimensions, _height, _terrain);
            _filter.mesh = _mesh;

            var material = Material.Instantiate(_renderer.material);
            _modifiedTexture = Texture2D.Instantiate(texture);
            var txPixels = _modifiedTexture.GetPixels32();
            // data-process map, unnecessary later just wanna color the "dead plot"
            for(int i = 0; i < txPixels.Length; i++)
            {
                if (txPixels[i].Equals(SimulationSystem.DeadPlotID))
                {
                    txPixels[i] = new Color32(0,0,200, 255);
                }
                else
                {
                    txPixels[i] = new Color32(0, (byte)(200 * UnityEngine.Random.Range(0.75f, 1f)), 0, 255);
                }
            }
            _modifiedTexture.SetPixels32(txPixels);
            _modifiedTexture.Apply();

            material.mainTexture = _modifiedTexture;
            _renderer.material = material;
        }

        private void OnDestroy()
        {
            if (_height.IsCreated)
                _height.Dispose();
            if (_terrain.IsCreated)
                _terrain.Dispose();
        }
    }
}