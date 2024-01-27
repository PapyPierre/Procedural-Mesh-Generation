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

    private List<Floor> _floors = new List<Floor>();
    private Vector3 centerVertex;
    private Vector3 tipVertex;
    [SerializeField] private List<Vector3> islandVertices = new List<Vector3>();
    private List<int> islandTriangles = new List<int>();
    private Mesh islandMesh;
    private MeshFilter islandMeshFilter;

    #endregion

    [Button()]
    private void GenerateIsland()
    {
        islandMeshFilter = GetComponent<MeshFilter>();
        islandMeshFilter.mesh = null;

        if (randomSpeed) islandSeed = (uint) Random.Range(0, 9999999);
        else islandSeed = customIslandSeed;
        SRnd.SetSeed(islandSeed);

        DestroyPreviousIsland();

        centerVertex = generateAt;

        SetUpFloors();
        SetUpTip();
        CreateVertices();
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

            float t = (float) i / _floors.Count;

            floor.index = i;
            floor.color = floorColorGradient.Evaluate(t);

            SetUpFloorAnchor(floor);
            SetUpFloorRadius(floor, upwardSwelling, tempRadius);
        }
    }

    private void SetUpFloorAnchor(Floor newFloor)
    {
        if (IsFirstFloor(newFloor))
        {
            newFloor.anchorPos = centerVertex;
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

    private void SetUpTip()
    {
        Vector3 randomPos = GetFloorPos(_floors[^1]);
        Vector3 lastAnchorPos = _floors[^1].anchorPos;
        float y = lastAnchorPos.y - randomPos.y;
        tipVertex = new Vector3(randomPos.x, y, randomPos.z);
    }

    private Vector3 GetFloorPos(Floor previousFloor)
    {
        float height = SRnd.RangeFloat(minFloorsHeight, maxFloorsHeight);
        float xOffset = SRnd.RangeFloat(-floorsMaxOffset, floorsMaxOffset) + previousFloor.anchorPos.x;
        float zOffset = SRnd.RangeFloat(-floorsMaxOffset, floorsMaxOffset) + previousFloor.anchorPos.z;

        return new Vector3(xOffset, height, zOffset);
    }

    private void CreateVertices()
    {
        foreach (var floor in _floors)
        {
            var floorRadius = floor.radius;

            for (int i = 0; i < meshComplexity; i++)
            {
                float angle = i * 2 * Mathf.PI / meshComplexity;

                float x = floor.anchorPos.x + floorRadius * Mathf.Cos(angle) + SRnd.RangeFloat(0, verticesMaxOffset);
                float y = IsFirstFloor(floor)
                    ? _floors[0].anchorPos.y
                    : floor.anchorPos.y + SRnd.RangeFloat(0, verticesMaxOffset / floor.index);
                float z = floor.anchorPos.z + floorRadius * Mathf.Sin(angle) + SRnd.RangeFloat(0, verticesMaxOffset);

                Vector3 vertexPos = new Vector3(x, y, z);
                islandVertices.Add(vertexPos);
            }
        }
    }

    private void GenerateIslandMesh()
    {
        islandMesh = new Mesh();

        islandVertices.Add(centerVertex);
        islandVertices.Add(tipVertex);

        foreach (var floor in _floors)
        {
            for (int i = 0; i < meshComplexity; i++)
            {
                int currentVertex = i + floor.index * meshComplexity;

                if (i == meshComplexity - 1)
                {
                    // Ground on island
                    islandTriangles.Add(i);
                    islandTriangles.Add(0);
                    islandTriangles.Add(islandVertices.Count - 2); // Center vertex

                    int firstVertexInFloor = floor.index * meshComplexity;

                    if (IsLastFloor(floor))
                    {
                        islandTriangles.Add(firstVertexInFloor);
                        islandTriangles.Add(currentVertex);
                        islandTriangles.Add(i + floor.index * meshComplexity + 2);
                    }
                    else
                    {
                        int firstVertexInNextFloor = _floors[floor.index + 1].index * meshComplexity;
                        int currentVertexInNextFloor = i + _floors[floor.index + 1].index * meshComplexity;

                        islandTriangles.Add(firstVertexInFloor);
                        islandTriangles.Add(currentVertex);
                        islandTriangles.Add(currentVertexInNextFloor);

                        islandTriangles.Add(firstVertexInNextFloor);
                        islandTriangles.Add(firstVertexInFloor);
                        islandTriangles.Add(currentVertexInNextFloor);
                    }
                }
                else
                {
                    // Ground on island
                    islandTriangles.Add(i);
                    islandTriangles.Add(i + 1);
                    islandTriangles.Add(islandVertices.Count - 2); // Center vertex

                    int nextVertexInFloor = i + floor.index * meshComplexity + 1;

                    if (IsLastFloor(floor))
                    {
                        islandTriangles.Add(nextVertexInFloor);
                        islandTriangles.Add(currentVertex);
                        islandTriangles.Add(islandVertices.Count - 1);
                    }
                    else
                    {
                        int currentVertexInNextFloor = i + _floors[floor.index + 1].index * meshComplexity;
                        int nextVertexInNextFloor = i + _floors[floor.index + 1].index * meshComplexity + 1;

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

        islandMesh.vertices = islandVertices.ToArray();
        islandMesh.triangles = islandTriangles.ToArray();

        islandMesh.RecalculateNormals();

        islandMeshFilter.mesh = islandMesh;
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
        return currentFloor.index == _floors.Count - 1 ? tipVertex : _floors[currentFloor.index + 1].anchorPos;
    }

    private static bool IsFirstFloor(Floor floor)
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

            if (showFloorLabels)
            {
                string floorTextLabel = "Floor " + floor.index + "\nPOS : " + floor.anchorPos;
                Handles.Label(floor.anchorPos, floorTextLabel);
            }
        }

        if (showVertices) DrawVertices();
        if (showVerticesLinks) DrawVerticesLinks();

        if (showVertices)
        {
            DrawCenterVertex();
            DrawTipVertex();
        }

        if (showVerticesLabels)
        {
            Handles.Label(centerVertex, "Center \nPos : " + centerVertex);
            Handles.Label(tipVertex, "Tip \nPos : " + tipVertex);
        }
    }

    private void DrawFloorRadius(Floor floor)
    {
        Handles.color = floor.color;
        Handles.DrawWireDisc(floor.anchorPos, Vector3.up, floor.radius, 3);
    }

    private void DrawVertices()
    {
        for (var i = 0; i < islandVertices.Count; i++)
        {
            Vector3 vertex = islandVertices[i];
            Gizmos.color = floorColorGradient.Evaluate((float) i / islandVertices.Count);
            Gizmos.DrawSphere(vertex, verticesGizmosSize);
            if (showVerticesLabels) Handles.Label(vertex, "Vextex " + i + " \nPos : " + vertex);
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
        Gizmos.DrawSphere(centerVertex, verticesGizmosSize);
    }

    private void DrawTipVertex()
    {
        Gizmos.color = tipGizmosColor;
        Gizmos.DrawSphere(tipVertex, verticesGizmosSize);
    }

    private void DrawVerticesLinks()
    {
        for (int i = 0; i < islandVertices.Count; i++)
        {
            Vector3 vertex = islandVertices[i];

            Handles.color = floorColorGradient.Evaluate((float) i / islandVertices.Count);

            // If first floor, draw to island center
            if (i < meshComplexity)
            {
                Handles.DrawLine(vertex, centerVertex, verticesLinksThickness);
            }


            // Draw to next vertex in floor
            if ((i + 1) % meshComplexity == 0)
            {
                Handles.DrawLine(vertex, islandVertices[i - (meshComplexity - 1)], verticesLinksThickness);
            }
            else
            {
                if (i < islandVertices.Count - 2)
                {
                    Handles.DrawLine(vertex, islandVertices[i + 1], verticesLinksThickness);
                }
            }

            // Draw to same vertex in next floor, or if last floor to tip vertex
            if (i < meshComplexity * (_floors.Count - 1))
            {
                Handles.DrawLine(vertex, islandVertices[i + meshComplexity], verticesLinksThickness);
            }
            else
            {
                if (vertex != centerVertex) Handles.DrawLine(vertex, tipVertex, verticesLinksThickness);
            }
        }
    }
#endif
}