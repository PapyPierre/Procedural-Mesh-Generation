using UnityEngine;
using Random = UnityEngine.Random;

public static class IslandMeshGenerator
{
    public static uint GenerateIsland(Vector3 position, IslandGenerationData data, uint seed = 0)
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
        GenerateIslandMesh(generatedIsland);
        
        return finalSeed;
    }

    private static void SetUpFloors(Island island)
    {
        int floorCount = SRnd.RangeInt(island.Data.minFloorCount, island.Data.maxFloorCount + 1);

        for (int i = 0; i < floorCount; i++)
        {
            island.Floors.Add(new Floor());
        }

        float upwardSwelling = SRnd.RangeFloat(island.Data.minUpwardSwelling, island.Data.maxUpwardSwelling);
        float tempRadius = SRnd.RangeFloat(island.Data.minFloorsRadius, island.Data.maxFloorsRadius);

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
        float height = SRnd.RangeFloat(data.minFloorsHeight, data.maxFloorsHeight);
        float xOffset = SRnd.RangeFloat(-data.floorsMaxOffset, data.floorsMaxOffset) + previousFloor.anchorPos.x;
        float zOffset = SRnd.RangeFloat(-data.floorsMaxOffset, data.floorsMaxOffset) + previousFloor.anchorPos.z;

        return new Vector3(xOffset, height, zOffset);
    }

    private static void CreateVertices(Island island)
    {
        foreach (var floor in island.Floors)
        {
            var floorRadius = floor.radius;

            for (int i = 0; i < island.Data.meshComplexity; i++)
            {
                float angle = i * 2 * Mathf.PI / island.Data.meshComplexity;

                float x = floor.anchorPos.x + floorRadius * Mathf.Cos(angle) +
                          SRnd.RangeFloat(0, island.Data.verticesMaxOffset);
                float y = IsFirstFloor(floor)
                    ? island.Floors[0].anchorPos.y
                    : floor.anchorPos.y + SRnd.RangeFloat(0, island.Data.verticesMaxOffset / floor.index);
                float z = floor.anchorPos.z + floorRadius * Mathf.Sin(angle) +
                          SRnd.RangeFloat(0, island.Data.verticesMaxOffset);

                Vector3 vertexPos = new Vector3(x, y, z);
                island.Vertices.Add(vertexPos);
            }
        }
    }

    private static void GenerateIslandMesh(Island island)
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
        renderer.material = island.Data.material;
        MeshFilter islandMeshFilter = islandObj.AddComponent<MeshFilter>();
        islandMeshFilter.mesh = mesh;
        islandMeshFilter.mesh.name = "Generated Island Mesh";
    }

    private static void GenerateFloorTriangles(Island island, Floor floor)
    {
        int meshComplexity = island.Data.meshComplexity;

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