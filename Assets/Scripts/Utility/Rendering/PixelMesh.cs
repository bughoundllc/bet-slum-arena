using System;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace bet_slum.Utility.Rendering
{
    public class PixelMesh
    {
        public static Mesh GenerateParallel(int2 dimensions, NativeHashMap<int2, float> heightMap, NativeHashMap<int2, int> terrainMap, GeneratePixelQuadsJob.UVArrangementStyle uvStyle = GeneratePixelQuadsJob.UVArrangementStyle.Texture, float maxHeight = 0f)
        {
            var mesh = new Mesh
            {
                name = "PixelMesh",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };

            var pixelCount = dimensions.x * dimensions.y;
            using var positions = new NativeArray<float3>(pixelCount * 4, Allocator.TempJob);
            using var normals = new NativeArray<float3>(pixelCount * 4, Allocator.TempJob);
            using var uvs = new NativeArray<float2>(pixelCount * 4, Allocator.TempJob);
            using var triangles = new NativeArray<int>(pixelCount * 6, Allocator.TempJob);

            new GeneratePixelQuadsJob
            {
                MaxHeight = maxHeight,
                MeshDimensions = dimensions,
                HeightMap = heightMap,
                UVArrangement = uvStyle,

                TilingTextureCount = 2, // TODO - Extract me
                TilingTextureSize = 256, // TODO - Extract me
                TilingTextureIndices = terrainMap,

                VertexPositions = positions,
                VertexNormals = normals,
                VertexUVs = uvs,
                Triangles = triangles
            }.Schedule(pixelCount, 256).Complete();

            mesh.vertices = positions.Select(f => new Vector3(f.x, f.y, f.z)).ToArray();
            mesh.normals = normals.Select(f => new Vector3(f.x, f.y, f.z)).ToArray();
            mesh.uv = uvs.Select(f => new Vector2(f.x, f.y)).ToArray();
            mesh.triangles = triangles.ToArray();

            return mesh;
        }

        [BurstCompile]
        public partial struct GeneratePixelQuadsJob : IJobParallelFor
        {
            [ReadOnly] public int2 MeshDimensions;
            [ReadOnly] public float MaxHeight;
            [ReadOnly] public NativeHashMap<int2, int> TilingTextureIndices;
            [ReadOnly] public int TilingTextureCount;
            [ReadOnly] public int TilingTextureSize;
            [ReadOnly] public NativeHashMap<int2, float> HeightMap;
            public enum UVArrangementStyle
            {
                Tiling,
                Texture
            }
            [ReadOnly] public UVArrangementStyle UVArrangement;

            [NativeDisableParallelForRestriction] public NativeArray<float3> VertexPositions;
            [NativeDisableParallelForRestriction] public NativeArray<float3> VertexNormals;
            [NativeDisableParallelForRestriction] public NativeArray<float2> VertexUVs;
            [NativeDisableParallelForRestriction] public NativeArray<int> Triangles;


            public void Execute(int index)
            {
                var coordinates = new int2(index % MeshDimensions.x, index / MeshDimensions.x);
                float centerHeight = 0f,
                    heightL = 0f,
                    heightR = 0f,
                    heightU = 0f,
                    heightD = 0f,
                    heightUL = 0f,
                    heightUR = 0f,
                    heightDL = 0f,
                    heightDR = 0f;

                if (HeightMap.IsCreated)
                {
                    centerHeight = math.lerp(0, MaxHeight, HeightMap[coordinates]);

                    heightL = centerHeight;
                    if (coordinates.x > 0)
                    {
                        heightL = math.lerp(0, MaxHeight, HeightMap[coordinates + new int2(-1, 0)]);
                    }
                    heightR = centerHeight;
                    if (coordinates.x < MeshDimensions.x - 1)
                    {
                        heightR = math.lerp(0, MaxHeight, HeightMap[coordinates + new int2(1, 0)]);
                    }
                    heightD = centerHeight;
                    if (coordinates.y > 0)
                    {
                        heightD = math.lerp(0, MaxHeight, HeightMap[coordinates + new int2(0, -1)]);
                    }
                    heightU = centerHeight;
                    if (coordinates.y < MeshDimensions.y - 1)
                    {
                        heightU = math.lerp(0, MaxHeight, HeightMap[coordinates + new int2(0, 1)]);
                    }

                    heightUL = centerHeight;
                    if (coordinates.x > 0 && coordinates.y < MeshDimensions.y - 1)
                    {
                        heightUL = math.lerp(0, MaxHeight, HeightMap[coordinates + new int2(-1, 1)]);
                    }
                    heightUR = centerHeight;
                    if (coordinates.x < MeshDimensions.x - 1 && coordinates.y < MeshDimensions.y - 1)
                    {
                        heightUR = math.lerp(0, MaxHeight, HeightMap[coordinates + new int2(1, 1)]);
                    }
                    heightDL = centerHeight;
                    if (coordinates.x > 0 && coordinates.y > 0)
                    {
                        heightDL = math.lerp(0, MaxHeight, HeightMap[coordinates + new int2(-1, -1)]);
                    }
                    heightDR = centerHeight;
                    if (coordinates.x < MeshDimensions.x - 1 && coordinates.y > 0)
                    {
                        heightDR = math.lerp(0, MaxHeight, HeightMap[coordinates + new int2(1, -1)]);
                    }

                }
                // BL -> BR -> TL -> TR

                VertexPositions[index * 4 + 0] = new(coordinates.x - 0.5f, (centerHeight + heightDL + heightD + heightL) / 4f, coordinates.y - 0.5f);
                VertexPositions[index * 4 + 1] = new(coordinates.x + 0.5f, (centerHeight + heightDR + heightD + heightR) / 4f, coordinates.y - 0.5f);
                VertexPositions[index * 4 + 2] = new(coordinates.x - 0.5f, (centerHeight + heightUL + heightU + heightL) / 4f, coordinates.y + 0.5f);
                VertexPositions[index * 4 + 3] = new(coordinates.x + 0.5f, (centerHeight + heightUR + heightU + heightR) / 4f, coordinates.y + 0.5f);

                VertexNormals[index * 4 + 0] = new(0, 1, 0);
                VertexNormals[index * 4 + 1] = new(0, 1, 0);
                VertexNormals[index * 4 + 2] = new(0, 1, 0);
                VertexNormals[index * 4 + 3] = new(0, 1, 0);

                switch (UVArrangement)
                {
                    case UVArrangementStyle.Tiling:
                        if (!TilingTextureIndices.IsCreated) throw new Exception("Trying to use Tiling textures without indices defined");

                        var textureIndex = TilingTextureIndices[coordinates];

                        var uvWidth = 1f / TilingTextureCount;
                        var startValue = textureIndex * uvWidth;
                        var bufferValue = uvWidth / TilingTextureSize;
                        var endValue = (startValue + uvWidth) - bufferValue;

                        VertexUVs[index * 4 + 0] = new(startValue, 0f);
                        VertexUVs[index * 4 + 1] = new(endValue, 0f);
                        VertexUVs[index * 4 + 2] = new(startValue, 1f);
                        VertexUVs[index * 4 + 3] = new(endValue, 1f);
                        break;
                    case UVArrangementStyle.Texture:
                        // This will map to a same-size texture
                        VertexUVs[index * 4 + 0] = new(((float)coordinates.x - 0) / (float)MeshDimensions.x, ((float)coordinates.y - 0f) / (float)MeshDimensions.y);
                        VertexUVs[index * 4 + 1] = new(((float)coordinates.x + 1f) / (float)MeshDimensions.x, ((float)coordinates.y - 0f) / (float)MeshDimensions.y);
                        VertexUVs[index * 4 + 2] = new(((float)coordinates.x - 0f) / (float)MeshDimensions.x, ((float)coordinates.y + 1f) / (float)MeshDimensions.y);
                        VertexUVs[index * 4 + 3] = new(((float)coordinates.x + 1f) / (float)MeshDimensions.x, ((float)coordinates.y + 1f) / (float)MeshDimensions.y);
                        break;
                }


                Triangles[index * 6 + 0] = index * 4;
                Triangles[index * 6 + 1] = index * 4 + 2;
                Triangles[index * 6 + 2] = index * 4 + 1;
                Triangles[index * 6 + 3] = index * 4 + 1;
                Triangles[index * 6 + 4] = index * 4 + 2;
                Triangles[index * 6 + 5] = index * 4 + 3;
            }
        }
    }
}