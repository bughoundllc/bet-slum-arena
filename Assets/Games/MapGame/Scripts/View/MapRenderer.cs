using bet_slum.Utility.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

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

        // TODO - split this logic up
        public void UpdateControlledMapView(MapGameMatchRunner mainSystem)
        {
            var pixels = new Color32[mainSystem.Map.width * mainSystem.Map.height];
            //var mapPixels = GameRunner.Instance.
            //(_renderer.material.mainTexture as Texture2D).SetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                var coordinate = new int2(i % mainSystem.Map.width, i / mainSystem.Map.width);
                var plotID = mainSystem.SimulationSystem.PixelPlotLookup[coordinate];
                if (plotID.Equals(new Color32(0, 0, 0, 255)))
                {
                    continue;
                }
                var commander = mainSystem.SimulationSystem.Plots[plotID].Controller;

                pixels[i] = commander.TestData.DisplayColor;
            }

            var tex = _renderer.material.mainTexture as Texture2D;
            tex.SetPixels32(pixels);
            tex.Apply();
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
            material.mainTexture = Texture2D.Instantiate(texture);
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