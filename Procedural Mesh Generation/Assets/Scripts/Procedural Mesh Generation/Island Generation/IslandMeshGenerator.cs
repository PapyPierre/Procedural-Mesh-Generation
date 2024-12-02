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
            SetUpTip(generatedIslandMeshData);
            CreateVertices(generatedIslandMeshData);

            return GenerateIslandMesh(generatedIslandMeshData);
        }

        private static void SetUpTip(IslandMeshData islandMeshData)
        {
            Vector3 randomPos = GetFloorPos(islandMeshData.GenerationData, islandMeshData.Floors[^1]);
            Vector3 lastAnchorPos = islandMeshData.Floors[^1].AnchorPos;
            float y = lastAnchorPos.y - randomPos.y;
            islandMeshData.TipVertex = new Vector3(randomPos.x, y, randomPos.z);
        }

        private static void CreateVertices(IslandMeshData islandMeshData)
        {
            foreach (var floor in islandMeshData.Floors)
            {
                var floorRadius = floor.Radius;

                for (int i = 0; i < islandMeshData.GenerationData.MeshComplexity; i++)
                {
                    Vector3 vertexPos = GetVertexPos(islandMeshData, floor, floorRadius, i);
                    islandMeshData.Vertices.Add(vertexPos);
                }
            }
            
            islandMeshData.Vertices.Add(islandMeshData.CenterVertex);
            islandMeshData.Vertices.Add(islandMeshData.TipVertex);
        }

        private static Vector3 GetVertexPos(IslandMeshData islandMeshData, Floor floor, float floorRadius, int index)
        {
            float angle = index * 2 * Mathf.PI / islandMeshData.GenerationData.MeshComplexity;

            float x = floor.AnchorPos.x + floorRadius * Mathf.Cos(angle) +
                      SRnd.RangeFloat(0, islandMeshData.GenerationData.VerticesMaxOffset);

            float y = IsFirstFloor(floor)
                ? islandMeshData.Floors[0].AnchorPos.y
                : floor.AnchorPos.y + SRnd.RangeFloat(0, islandMeshData.GenerationData.VerticesMaxOffset / floor.Index);

            float z = floor.AnchorPos.z + floorRadius * Mathf.Sin(angle) + 
                      SRnd.RangeFloat(0, islandMeshData.GenerationData.VerticesMaxOffset);

            return new Vector3(x, y, z);
        }

        private static GeneratedIsland GenerateIslandMesh(IslandMeshData islandMeshData)
        {
            foreach (var floor in islandMeshData.Floors)
            {
                AddTrianglesToMeshData(islandMeshData, floor);
            }
            
            return GenerateObj<GeneratedIsland>(CreateMesh(islandMeshData), islandMeshData);
        }

        private static void AddTrianglesToMeshData(IslandMeshData islandMeshData, Floor floor)
        {
            int meshComplexity = islandMeshData.GenerationData.MeshComplexity;

            for (int i = 0; i < meshComplexity; i++)
            {
                int currentVertex = i + floor.Index * meshComplexity;

                // If last vertex in floor (need to apply changes for it to loop)
                if (i == meshComplexity - 1)
                {
                    // Ground on island
                    islandMeshData.Triangles.Add(0);
                    islandMeshData.Triangles.Add(i);
                    islandMeshData.Triangles.Add(islandMeshData.Vertices.Count - 2); // Center vertex

                    int firstVertexInFloor = floor.Index * meshComplexity;

                    if (IsLastFloor(islandMeshData, floor))
                    {
                        islandMeshData.Triangles.Add(currentVertex);
                        islandMeshData.Triangles.Add(firstVertexInFloor);
                        islandMeshData.Triangles.Add(i + floor.Index * meshComplexity + 2);
                    }
                    else
                    {
                        int firstVertexInNextFloor = islandMeshData.Floors[floor.Index + 1].Index * meshComplexity;
                        int currentVertexInNextFloor = i + islandMeshData.Floors[floor.Index + 1].Index * meshComplexity;

                        islandMeshData.Triangles.Add(currentVertex);
                        islandMeshData.Triangles.Add(firstVertexInFloor);
                        islandMeshData.Triangles.Add(currentVertexInNextFloor);

                        islandMeshData.Triangles.Add(firstVertexInFloor);
                        islandMeshData.Triangles.Add(firstVertexInNextFloor);
                        islandMeshData.Triangles.Add(currentVertexInNextFloor);
                    }
                }
                else
                {
                    // Ground on island
                    islandMeshData.Triangles.Add(i + 1);
                    islandMeshData.Triangles.Add(i);
                    islandMeshData.Triangles.Add(islandMeshData.Vertices.Count - 2); // Center vertex

                    int nextVertexInFloor = i + floor.Index * meshComplexity + 1;

                    if (IsLastFloor(islandMeshData, floor))
                    {
                        islandMeshData.Triangles.Add(currentVertex);
                        islandMeshData.Triangles.Add(nextVertexInFloor);
                        islandMeshData.Triangles.Add(islandMeshData.Vertices.Count - 1);
                    }
                    else
                    {
                        int currentVertexInNextFloor = i + islandMeshData.Floors[floor.Index + 1].Index * meshComplexity;
                        int nextVertexInNextFloor = i + islandMeshData.Floors[floor.Index + 1].Index * meshComplexity + 1;

                        islandMeshData.Triangles.Add(currentVertex);
                        islandMeshData.Triangles.Add(nextVertexInFloor);
                        islandMeshData.Triangles.Add(currentVertexInNextFloor);

                        islandMeshData.Triangles.Add(nextVertexInFloor);
                        islandMeshData.Triangles.Add(nextVertexInNextFloor);
                        islandMeshData.Triangles.Add(currentVertexInNextFloor);
                    }
                }
            }
        }
    }
}