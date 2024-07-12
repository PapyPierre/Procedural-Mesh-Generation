using UnityEngine;
using Random = UnityEngine.Random;

namespace Island_Generation
{
    public static class IslandMeshGenerator
    {
        public static GameObject GenerateIsland(Vector3 position, IslandGenerationData data, uint seed = 0)
        {
            Island generatedIsland = new Island();

            uint finalSeed;

            if (seed == 0) finalSeed = (uint) Random.Range(0, 9999999);
            else finalSeed = seed;

            generatedIsland.Data = data;
            SRnd.SetSeed(finalSeed);

            generatedIsland.CenterVertex = position;

            SetUpFloors(generatedIsland);
            SetUpTip(generatedIsland);
            CreateVertices(generatedIsland);

            return GenerateIslandMesh(generatedIsland);
        }

        private static void SetUpFloors(Island island)
        {
            int floorCount = SRnd.RangeInt(island.Data.MinFloorCount, island.Data.MaxFloorCount + 1);

            for (int i = 0; i < floorCount; i++)
            {
                island.Floors.Add(new Floor());
            }

            float upwardSwelling = SRnd.RangeFloat(island.Data.MinUpwardSwelling, island.Data.MaxUpwardSwelling);
            float tempRadius = SRnd.RangeFloat(island.Data.MinFloorsRadius, island.Data.MaxFloorsRadius);

            for (var i = 0; i < island.Floors.Count; i++)
            {
                Floor floor = island.Floors[i];
                floor.index = i;
                SetFloorAnchor(island, floor);
                SetFloorRadius(island, floor, upwardSwelling, tempRadius);
            }
        }

        private static void SetFloorAnchor(Island island, Floor newFloor)
        {
            if (IsFirstFloor(newFloor))
            {
                newFloor.anchorPos = island.CenterVertex;
            }
            else
            {
                Floor previousFloor = GetPreviousFloor(island, newFloor);
                Vector3 randomPos = GetFloorPos(island.Data, previousFloor);

                Vector3 previousFloorAnchorPos = previousFloor.anchorPos;
                float y = previousFloorAnchorPos.y - randomPos.y;
                newFloor.anchorPos = new Vector3(randomPos.x, y, randomPos.z);
            }
        }

        private static void SetFloorRadius(Island island, Floor newFloor, float upwardSwelling, float tempRadius)
        {
            float upwardSwellingValue = Mathf.Pow(island.Floors.Count - newFloor.index, upwardSwelling);
            float radius = tempRadius * upwardSwellingValue;
            newFloor.radius = radius;
        }

        private static void SetUpTip(Island island)
        {
            Vector3 randomPos = GetFloorPos(island.Data, island.Floors[^1]);
            Vector3 lastAnchorPos = island.Floors[^1].anchorPos;
            float y = lastAnchorPos.y - randomPos.y;
            island.TipVertex = new Vector3(randomPos.x, y, randomPos.z);
        }

        private static Vector3 GetFloorPos(IslandGenerationData data, Floor previousFloor)
        {
            float height = SRnd.RangeFloat(data.MinFloorsHeight, data.MaxFloorsHeight);
            float xOffset = SRnd.RangeFloat(-data.FloorsMaxOffset, data.FloorsMaxOffset) + previousFloor.anchorPos.x;
            float zOffset = SRnd.RangeFloat(-data.FloorsMaxOffset, data.FloorsMaxOffset) + previousFloor.anchorPos.z;

            return new Vector3(xOffset, height, zOffset);
        }

        private static void CreateVertices(Island island)
        {
            foreach (var floor in island.Floors)
            {
                var floorRadius = floor.radius;

                for (int i = 0; i < island.Data.MeshComplexity; i++)
                {
                    Vector3 vertexPos = GetVertexPos(island, floor, floorRadius, i);
                    island.Vertices.Add(vertexPos);
                }
            }
        }

        private static Vector3 GetVertexPos(Island island, Floor floor, float floorRadius, int index)
        {
            float angle = index * 2 * Mathf.PI / island.Data.MeshComplexity;

            float x = floor.anchorPos.x + floorRadius * Mathf.Cos(angle) +
                      SRnd.RangeFloat(0, island.Data.VerticesMaxOffset);

            float y = IsFirstFloor(floor)
                ? island.Floors[0].anchorPos.y
                : floor.anchorPos.y + SRnd.RangeFloat(0, island.Data.VerticesMaxOffset / floor.index);

            float z = floor.anchorPos.z + floorRadius * Mathf.Sin(angle) +
                      SRnd.RangeFloat(0, island.Data.VerticesMaxOffset);

            return new Vector3(x, y, z);
        }

        private static GameObject GenerateIslandMesh(Island island)
        {
            Mesh mesh = new Mesh();

            island.Vertices.Add(island.CenterVertex);
            island.Vertices.Add(island.TipVertex);

            foreach (var floor in island.Floors)
            {
                GenerateFloorTriangles(island, floor);
            }

            mesh.vertices = island.Vertices.ToArray();
            mesh.triangles = island.Triangles.ToArray();

            mesh.RecalculateNormals();

            GameObject islandObj = new GameObject
            {
                name = "Generated Island"
            };

            MeshRenderer renderer = islandObj.AddComponent<MeshRenderer>();
            renderer.material = island.Data.Material;
            MeshFilter islandMeshFilter = islandObj.AddComponent<MeshFilter>();
            islandMeshFilter.mesh = mesh;
            islandMeshFilter.mesh.name = "Generated Island Mesh";

            return islandObj;
        }

        private static void GenerateFloorTriangles(Island island, Floor floor)
        {
            int meshComplexity = island.Data.MeshComplexity;

            for (int i = 0; i < meshComplexity; i++)
            {
                int currentVertex = i + floor.index * meshComplexity;

                // If last vertex in floor (need to apply changes for it to loop)
                if (i == meshComplexity - 1)
                {
                    // Ground on island
                    island.Triangles.Add(i);
                    island.Triangles.Add(0);
                    island.Triangles.Add(island.Vertices.Count - 2); // Center vertex

                    int firstVertexInFloor = floor.index * meshComplexity;

                    if (IsLastFloor(island, floor))
                    {
                        island.Triangles.Add(firstVertexInFloor);
                        island.Triangles.Add(currentVertex);
                        island.Triangles.Add(i + floor.index * meshComplexity + 2);
                    }
                    else
                    {
                        int firstVertexInNextFloor = island.Floors[floor.index + 1].index * meshComplexity;
                        int currentVertexInNextFloor = i + island.Floors[floor.index + 1].index * meshComplexity;

                        island.Triangles.Add(firstVertexInFloor);
                        island.Triangles.Add(currentVertex);
                        island.Triangles.Add(currentVertexInNextFloor);

                        island.Triangles.Add(firstVertexInNextFloor);
                        island.Triangles.Add(firstVertexInFloor);
                        island.Triangles.Add(currentVertexInNextFloor);
                    }
                }
                else
                {
                    // Ground on island
                    island.Triangles.Add(i);
                    island.Triangles.Add(i + 1);
                    island.Triangles.Add(island.Vertices.Count - 2); // Center vertex

                    int nextVertexInFloor = i + floor.index * meshComplexity + 1;

                    if (IsLastFloor(island, floor))
                    {
                        island.Triangles.Add(nextVertexInFloor);
                        island.Triangles.Add(currentVertex);
                        island.Triangles.Add(island.Vertices.Count - 1);
                    }
                    else
                    {
                        int currentVertexInNextFloor = i + island.Floors[floor.index + 1].index * meshComplexity;
                        int nextVertexInNextFloor = i + island.Floors[floor.index + 1].index * meshComplexity + 1;

                        island.Triangles.Add(nextVertexInFloor);
                        island.Triangles.Add(currentVertex);
                        island.Triangles.Add(currentVertexInNextFloor);

                        island.Triangles.Add(nextVertexInNextFloor);
                        island.Triangles.Add(nextVertexInFloor);
                        island.Triangles.Add(currentVertexInNextFloor);
                    }
                }
            }
        }

        private static Floor GetPreviousFloor(Island island, Floor currentFloor)
        {
            if (currentFloor.index == 0) // last Floor
            {
                Debug.LogError("Is first floor");
                return null;
            }
            else
            {
                return island.Floors[currentFloor.index - 1];
            }
        }

        private static bool IsFirstFloor(Floor floor) => floor.index == 0;
        private static bool IsLastFloor(Island island, Floor floor) => floor.index == island.Floors.Count - 1;
    }
}