using UnityEngine;

namespace Procedural_Mesh_Generation.Island_Generation
{
    public abstract class IslandMeshGenerator : MeshGenerator
    {
        public static GeneratedIsland GenerateIsland(Vector3 position, IslandGenerationData data, uint seed = 0)
        {
            IslandMeshData generatedIslandMeshData = new IslandMeshData
            {
                GenerationData = data
            };

            InitSeed(seed);
                
            generatedIslandMeshData.CenterVertex = position;

            SetUpFloors(generatedIslandMeshData);
            CreateVertices(generatedIslandMeshData);
            SetUpTip(generatedIslandMeshData);

            return GenerateIslandMesh(generatedIslandMeshData);
        }

        private static void SetUpTip(IslandMeshData islandMeshData)
        {
            Vector3 randomPos = GetFloorPos(islandMeshData, islandMeshData.Floors[^1]);
            Vector3 lastAnchorPos = islandMeshData.Floors[^1].AnchorPos;
            float y = lastAnchorPos.y - randomPos.y;
            islandMeshData.TipVertex = new Vector3(randomPos.x, y, randomPos.z);
        }

        private static GeneratedIsland GenerateIslandMesh(IslandMeshData islandMeshData)
        {
            foreach (var floor in islandMeshData.Floors)
            {
                AddTrianglesToMeshData(islandMeshData, floor);
                AddSpecificTriangles(islandMeshData, floor);
            }
            
            return GenerateObj<GeneratedIsland>(CreateMesh(islandMeshData), islandMeshData);
        }

        private static void AddSpecificTriangles(IslandMeshData meshData, Floor floor)
        {
            int meshComplexity = meshData.GenerationData.MeshComplexity;

            for (int i = 0; i < meshComplexity; i++)
            {
                int currentVertex = i + floor.Index * meshComplexity;

                // If last vertex in floor (need to apply changes for it to loop)
                if (i == meshComplexity - 1)
                {
                    int firstVertexInFloor = floor.Index * meshComplexity;

                    if (meshData.IsLastFloor(floor)) // Connect last floor vertices to tip vertex
                    {
                        meshData.Triangles.Add(currentVertex);
                        meshData.Triangles.Add(firstVertexInFloor);
                        meshData.Triangles.Add(currentVertex + 2); // Tip vertex
                    }

                    if (meshData.IsFirstFloor(floor)) // Draw ground on the island
                    {
                        meshData.Triangles.Add(0);
                        meshData.Triangles.Add(i);
                        meshData.Triangles.Add(meshData.Floors.Count * meshComplexity); // Center vertex
                    }
                }
                else
                {              
                    int nextVertexInFloor = i + floor.Index * meshComplexity + 1;

                    if (meshData.IsLastFloor(floor))  // Connect last floor vertices to tip vertex
                    {
                        meshData.Triangles.Add(currentVertex);
                        meshData.Triangles.Add(nextVertexInFloor);
                        meshData.Triangles.Add((1 + floor.Index) * meshComplexity + 1); // Tip vertex
                    }

                    // Ground on island
                    if (meshData.IsFirstFloor(floor)) // Draw ground on the island
                    {
                        meshData.Triangles.Add(nextVertexInFloor);
                        meshData.Triangles.Add(currentVertex);
                        meshData.Triangles.Add(meshData.Floors.Count * meshComplexity); // Center vertex
                    }
                }
            }
        }
    }
}