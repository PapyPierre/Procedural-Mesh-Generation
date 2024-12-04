using UnityEngine;

namespace Procedural_Mesh_Generation.Island_Generation
{
    public abstract class IslandMeshGenerator : MeshGenerator
    {
        public static GeneratedIsland GenerateIsland(Vector3 position, IslandGenerationData data, uint seed = 0)
        {
            IslandMeshData generatedIslandMeshData = new IslandMeshData
            {
                GenData = data
            };

            InitSeed(seed);
                
            generatedIslandMeshData.CenterVertex = position;

            SetUpFloors(generatedIslandMeshData);
            CreateVertices(generatedIslandMeshData);
          
            generatedIslandMeshData.PivotVertex = generatedIslandMeshData.Floors[^1].AnchorPos;

            return GenerateIslandMesh(generatedIslandMeshData);
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
            int meshComplexity = meshData.GenData.MeshComplexity;

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
                        meshData.Triangles.Add(currentVertex + 2); // Tip vertex
                        meshData.Triangles.Add(firstVertexInFloor);
                    }

                    if (meshData.IsFirstFloor(floor)) // Draw ground on the island
                    {
                        meshData.Triangles.Add(0);
                        meshData.Triangles.Add(meshData.Floors.Count * meshComplexity); // Center vertex
                        meshData.Triangles.Add(i);
                    }
                }
                else
                {              
                    int nextVertexInFloor = i + floor.Index * meshComplexity + 1;

                    if (meshData.IsLastFloor(floor))  // Connect last floor vertices to tip vertex
                    {
                        meshData.Triangles.Add(currentVertex);
                        meshData.Triangles.Add((1 + floor.Index) * meshComplexity + 1); // Tip vertex
                        meshData.Triangles.Add(nextVertexInFloor);
                    }

                    // Ground on island
                    if (meshData.IsFirstFloor(floor)) // Draw ground on the island
                    {
                        meshData.Triangles.Add(nextVertexInFloor);
                        meshData.Triangles.Add(meshData.Floors.Count * meshComplexity); // Center vertex
                        meshData.Triangles.Add(currentVertex);
                    }
                }
            }
        }
    }
}