using System.Collections.Generic;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[ExecuteAlways, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class IslandMeshGenerator : MonoBehaviour
{
    [Header("Seed")] [SerializeField] private bool randomSpeed;

    [SerializeField, HideIf("randomSpeed")]
    private uint customIslandSeed;

    [SerializeField, ReadOnly] private uint islandSeed;

    [SerializeField, Space] private bool generateMesh;

    [SerializeField, Space] private Vector3 generateAt;

    [Header("Floors parameters"), SerializeField, Tooltip("minimal number of floors")]
    private int minFloorCount;

    [SerializeField, Tooltip("minimal number of floors")]
    private int maxFloorCount;

    [SerializeField, Space, Tooltip("minimal space between floors")]
    private float minFloorsHeight;

    [SerializeField, Tooltip("maximal space between floors")]
    private float maxFloorsHeight;

    [SerializeField, Space] private float minFloorsRadius;
    [SerializeField] private float maxFloorsRadius;

    [SerializeField, Space, Tooltip("maximal floors offset on x and z axis")]
    private float floorsMaxOffset;

    [SerializeField, Space, Tooltip("maximal vertices offset on all axis")]
    private float verticesMaxOffset;

    [SerializeField, Tooltip("The higher the floors are, the more they grow depending on this value.")]
    private float minUpwardSwelling;

    [SerializeField, Tooltip("The higher the floors are, the more they grow depending on this value.")]
    private float maxUpwardSwelling;

    [SerializeField, Tooltip("Number of vertices per floor")]
    private int meshComplexity;

    [Header("Global Gizmos")] [SerializeField]
    private bool hideAllGizmos;

    [SerializeField, HideIf("hideAllGizmos")]
    private bool showIslandSpine;

    [Header("Floor Gizmos")] [SerializeField, HideIf("hideAllGizmos")]
    private bool showFloorsRadius;

    [SerializeField, HideIf("hideAllGizmos")]
    private Gradient floorColorGradient;

    [SerializeField, HideIf("hideAllGizmos")]
    private bool showFloorLabels;

    [Header("Vertices Gizmos")] [SerializeField, HideIf("hideAllGizmos")]
    private bool showVertices;

    [SerializeField, HideIf("hideAllGizmos")]
    private float verticesGizmosSize;

    [SerializeField, HideIf("hideAllGizmos")]
    private bool showVerticesLabels;

    [SerializeField, HideIf("hideAllGizmos")]
    private Color centerGizmosColor;

    [SerializeField] private Color tipGizmosColor;

    [Header("Vertices Links"), HideIf("hideAllGizmos"), SerializeField]
    private bool showVerticesLinks;

    [SerializeField, HideIf("hideAllGizmos")]
    private float verticesLinksThickness;

    #region Hidden Fields

    [SerializeField] private List<Floor> _floors = new List<Floor>();
    private Vertex centerVertex;
    private Vertex tipVertex;
    private List<Vector3> islandVertices;
    private List<int> islandTriangles;
    private Mesh islandMesh;
    private MeshFilter islandMeshFilter;

    #endregion

    [Button()]
    private void GenerateIsland()
    {
        islandMeshFilter = GetComponent<MeshFilter>();
        islandMeshFilter.mesh = null;

        if (randomSpeed) islandSeed = (uint)Random.Range(0, 9999999);
        else islandSeed = customIslandSeed;
        SRnd.SetSeed(islandSeed);

        DestroyPreviousIsland();
        SetUpCenterVertex(generateAt); // Center of the island and pivot point
        SetUpFloors();
        SetUpTip();
        CreateFloorsVertices();
        SortAndIndexFloorVertices();
        if (generateMesh) GenerateIslandMesh();
    }

    private void DestroyPreviousIsland()
    {
        _floors.Clear();
        islandVertices.Clear();
        islandTriangles.Clear();
    }

    private void SetUpFloors()
    {
        int floorCount = SRnd.RangeInt(minFloorCount, maxFloorCount + 1);

        for (int i = 0; i < floorCount; i++)
        {
            _floors.Add(new Floor());
        }

        float upwardSwelling = SRnd.RangeFloat(minUpwardSwelling, maxUpwardSwelling);
        float tempRadius = SRnd.RangeFloat(minFloorsRadius, maxFloorsRadius);

        for (var i = 0; i < _floors.Count; i++)
        {
            Floor floor = _floors[i];

            float t = (float)i / _floors.Count;

            floor.index = i;
            floor.color = floorColorGradient.Evaluate(t);
            floor.verticesCount = meshComplexity;

            SetUpFloorAnchor(floor);
            SetUpFloorRadius(floor, upwardSwelling, tempRadius);
        }
    }

    private void SetUpFloorAnchor(Floor newFloor)
    {
        if (IsFirstFloor(newFloor))
        {
            newFloor.anchorPos = centerVertex.position;
        }
        else
        {
            Floor previousFloor = GetPreviousFloor(newFloor);
            Vector3 randomPos = GetFloorPos(previousFloor);
            Vector3 previousFloorAnchorPos = previousFloor.anchorPos;
            float y = previousFloorAnchorPos.y - randomPos.y;
            newFloor.anchorPos = new Vector3(randomPos.x, y, randomPos.z);
        }
    }

    private void SetUpFloorRadius(Floor newFloor, float upwardSwelling, float tempRadius)
    {
        float upwardSwellingValue = Mathf.Pow(_floors.Count - newFloor.index, upwardSwelling);
        float radius = tempRadius * upwardSwellingValue;
        newFloor.radius = radius;
    }

    private void SetUpCenterVertex(Vector3 islandPos)
    {
        centerVertex = new Vertex
        {
            position = islandPos
        };
    }

    private void SetUpTip()
    {
        Vector3 randomPos = GetFloorPos(_floors[^1]);
        Vector3 lastAnchorPos = _floors[^1].anchorPos;
        float y = lastAnchorPos.y - randomPos.y;
        tipVertex = new Vertex
        {
            position = new Vector3(randomPos.x, y, randomPos.z)
        };
    }

    private Vector3 GetFloorPos(Floor previousFloor)
    {
        float height = SRnd.RangeFloat(minFloorsHeight, maxFloorsHeight);
        float xOffset = SRnd.RangeFloat(-floorsMaxOffset, floorsMaxOffset) + previousFloor.anchorPos.x;
        float zOffset = SRnd.RangeFloat(-floorsMaxOffset, floorsMaxOffset) + previousFloor.anchorPos.z;

        return new Vector3(xOffset, height, zOffset);
    }

    private void CreateFloorsVertices()
    {
        foreach (var floor in _floors)
        {
            var floorRadius = floor.radius;

            for (int i = 0; i < floor.verticesCount; i++)
            {
                float angle = i * 2 * Mathf.PI / floor.verticesCount;

                float x = floor.anchorPos.x + floorRadius * Mathf.Cos(angle) + SRnd.RangeFloat(0, verticesMaxOffset);
                float y = IsFirstFloor(floor)
                    ? _floors[0].anchorPos.y
                    : floor.anchorPos.y + SRnd.RangeFloat(0, verticesMaxOffset / floor.index);
                float z = floor.anchorPos.z + floorRadius * Mathf.Sin(angle) + SRnd.RangeFloat(0, verticesMaxOffset);

                Vector3 vertexPos = new Vector3(x, y, z);

                Vertex newVertex = new Vertex
                {
                    position = vertexPos
                };

                floor.vertices.Add(newVertex);
            }
        }
    }

    private void SortAndIndexFloorVertices()
    {
        foreach (var floor in _floors)
        {
            List<Vertex> sortedVertices = SortVerticesClockwise(floor.vertices, floor.anchorPos);

            for (var i = 0; i < sortedVertices.Count; i++)
            {
                var vertex = sortedVertices[i];
                vertex.index = (sortedVertices.Count - i) - 1; // -1 to start at 0
            }

            sortedVertices.Reverse();
            floor.vertices = sortedVertices;
        }
    }

    private void GenerateIslandMesh()
    {
        islandMesh = new Mesh();

        foreach (var floor in _floors)
        {
            // Index all floors vertices to global list of vertices
            foreach (var t in floor.vertices)
            {
                islandVertices.Add(t.position);
            }
        }

        islandVertices.Add(centerVertex.position);
        islandVertices.Add(tipVertex.position);

        foreach (var floor in _floors)
        {
            if (IsLastFloor(floor))
            {
                for (int i = 0; i < meshComplexity; i++)
                {
                    if (i == meshComplexity - 1)
                    {
                        islandTriangles.Add(floor.index * meshComplexity);
                        islandTriangles.Add(i + floor.index * meshComplexity);
                        islandTriangles.Add(i + floor.index * meshComplexity + 2);
                    }
                    else
                    {
                        islandTriangles.Add(i + floor.index * meshComplexity + 1);
                        islandTriangles.Add(i + floor.index * meshComplexity);
                        islandTriangles.Add(islandVertices.Count - 1);
                    }
                }
            }
            else
            {
                Floor nextFloor = GetNextFloor(floor);

                for (int i = 0; i < meshComplexity; i++)
                {
                    int firstVertexInFloor = floor.index * meshComplexity;
                    int firstVertexInNextFloor = nextFloor.index * meshComplexity;
                    int currentVertex = i + floor.index * meshComplexity;
                    int nextVertexInFloor = i + floor.index * meshComplexity + 1;
                    int currentVertexInNextFloor = i + nextFloor.index * meshComplexity;
                    int nextVertexInNextFloor = i + nextFloor.index * meshComplexity + 1;

                    if (i == meshComplexity - 1)
                    {
                        islandTriangles.Add(firstVertexInFloor);
                        islandTriangles.Add(currentVertex);
                        islandTriangles.Add(currentVertexInNextFloor);

                        islandTriangles.Add(firstVertexInNextFloor);
                        islandTriangles.Add(firstVertexInFloor);
                        islandTriangles.Add(currentVertexInNextFloor);
                    }
                    else
                    {
                        islandTriangles.Add(nextVertexInFloor);
                        islandTriangles.Add(currentVertex);
                        islandTriangles.Add(currentVertexInNextFloor);

                        islandTriangles.Add(nextVertexInNextFloor);
                        islandTriangles.Add(nextVertexInFloor);
                        islandTriangles.Add(currentVertexInNextFloor);
                    }
                }
            }
        }

        // generate ground on island
        for (int i = 0; i < meshComplexity; i++)
        {
            if (i == meshComplexity - 1)
            {
                islandTriangles.Add(i);
                islandTriangles.Add(0);
                islandTriangles.Add(islandVertices.Count - 2); // Center vertex
            }
            else
            {
                islandTriangles.Add(i);
                islandTriangles.Add(i + 1);
                islandTriangles.Add(islandVertices.Count - 2); // Center vertex
            }
        }

        islandMesh.vertices = islandVertices.ToArray();
        islandMesh.triangles = islandTriangles.ToArray();

        islandMesh.RecalculateNormals();

        islandMeshFilter.mesh = islandMesh;
    }

    private List<Vertex> SortVerticesClockwise(List<Vertex> vertices, Vector3 referencePoint)
    {
        vertices.Sort((p1, p2) => ClockwiseCompare(p1.position, p2.position, referencePoint));
        return vertices;
    }

    private int ClockwiseCompare(Vector3 p1, Vector3 p2, Vector3 referencePoint)
    {
        // Convert to 2d
        p1 = new Vector3(p1.x, 0, p1.z);
        p2 = new Vector3(p2.x, 0, p2.z);
        referencePoint = new Vector3(referencePoint.x, 0, referencePoint.z);

        float angle1 = Mathf.Atan2(p1.z - referencePoint.z, p1.x - referencePoint.x);
        float angle2 = Mathf.Atan2(p2.z - referencePoint.z, p2.x - referencePoint.x);

        if (angle1 < angle2)
        {
            return -1;
        }
        else if (angle1 > angle2)
        {
            return 1;
        }
        else
        {
            float distance1 = Vector3.Distance(p1, referencePoint);
            float distance2 = Vector3.Distance(p2, referencePoint);
            return distance1.CompareTo(distance2);
        }
    }

    private Floor GetNextFloor(Floor currentFloor)
    {
        if (currentFloor.index == _floors.Count - 1) // last Floor
        {
            Debug.LogError("Is last floor");
            return null;
        }
        else
        {
            return _floors[currentFloor.index + 1];
        }
    }

    private Floor GetPreviousFloor(Floor currentFloor)
    {
        if (currentFloor.index == 0) // last Floor
        {
            Debug.LogError("Is first floor");
            return null;
        }
        else
        {
            return _floors[currentFloor.index - 1];
        }
    }

    private Vector3 GetNextFloorAnchor(Floor currentFloor)
    {
        return currentFloor.index == _floors.Count - 1 ? tipVertex.position : _floors[currentFloor.index + 1].anchorPos;
    }

    private bool IsFirstFloor(Floor floor)
    {
        return floor.index == 0;
    }

    private bool IsLastFloor(Floor floor)
    {
        return floor.index == _floors.Count - 1;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (hideAllGizmos) return;
        if (_floors.Count < 1) return;

        foreach (var floor in _floors)
        {
            if (showIslandSpine) DrawSpine(floor, GetNextFloorAnchor(floor));
            if (showFloorsRadius) DrawFloorRadius(floor);
            if (showVertices) DrawFloorVertices(floor);
            if (showVerticesLinks) DrawVerticesLinksInFloor(floor);
            if (showVerticesLinks) DrawVerticesLinksBetweenFloor(floor);

            if (showFloorLabels)
            {
                string floorTextLabel = "Floor " + floor.index + "\nVertices Count : " + floor.verticesCount;
                Handles.Label(floor.anchorPos, floorTextLabel);
            }
        }

        if (showVertices)
        {
            DrawCenterVertex();
            DrawTipVertex();
        }

        if (showVerticesLabels)
        {
            Handles.Label(centerVertex.position, "Center \nPos : " + centerVertex.position);
            Handles.Label(tipVertex.position, "Tip \nPos : " + tipVertex.position);
        }
    }

    private void DrawFloorRadius(Floor floor)
    {
        Handles.color = floor.color;
        Handles.DrawWireDisc(floor.anchorPos, Vector3.up, floor.radius, 3);
    }

    private void DrawFloorVertices(Floor floor)
    {
        Gizmos.color = floor.color;

        foreach (var vertex in floor.vertices)
        {
            Gizmos.DrawSphere(vertex.position, verticesGizmosSize);
            if (showVerticesLabels)
                Handles.Label(vertex.position, "Vextex " + vertex.index + " \nPos : " + vertex.position);
        }
    }

    private void DrawSpine(Floor floor, Vector3 nextFloorPos)
    {
        Handles.color = floor.color;
        Handles.DrawLine(floor.anchorPos, nextFloorPos, 3);
    }

    private void DrawCenterVertex()
    {
        Gizmos.color = centerGizmosColor;
        Gizmos.DrawSphere(centerVertex.position, verticesGizmosSize);
    }

    private void DrawTipVertex()
    {
        Gizmos.color = tipGizmosColor;
        Gizmos.DrawSphere(tipVertex.position, verticesGizmosSize);
    }

    private void DrawVerticesLinksInFloor(Floor floor)
    {
        for (var index = 0; index < floor.vertices.Count; index++)
        {
            var vertex = floor.vertices[index];

            Handles.color = floor.color;

            if (index == floor.vertices.Count - 1)
            {
                Handles.DrawLine(vertex.position, floor.vertices[0].position, verticesLinksThickness);
            }
            else Handles.DrawLine(vertex.position, floor.vertices[index + 1].position, verticesLinksThickness);
        }
    }

    private void DrawVerticesLinksBetweenFloor(Floor floor)
    {
        if (IsLastFloor(floor))
        {
            foreach (var vertex in floor.vertices)
            {
                Handles.color = floor.color;
                Handles.DrawLine(vertex.position, tipVertex.position, verticesLinksThickness);
            }
        }
        else
        {
            Floor nextFloor = GetNextFloor(floor);

            for (var index = 0; index < floor.vertices.Count; index++)
            {
                Handles.color = floor.color;

                Handles.DrawLine(
                    floor.vertices[index].position, nextFloor.vertices[index].position, verticesLinksThickness);

                if (IsFirstFloor(floor))
                {
                    Handles.DrawLine(floor.vertices[index].position, centerVertex.position, verticesLinksThickness);
                }
            }
        }
    }
#endif
}