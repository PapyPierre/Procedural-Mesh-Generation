using System.Collections.Generic;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
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

    #region Non-serialized Fields

    private readonly List<Floor> _floors = new List<Floor>();
    private Vector3 _centerVertex;
    private Vector3 _tipVertex;
    private readonly List<Vector3> _islandVertices = new List<Vector3>();
    private readonly List<int> _islandTriangles = new List<int>();
    private Mesh _islandMesh;
    private MeshFilter _islandMeshFilter;

    #endregion

    [Button()]
    private void GenerateIsland()
    {
        _islandMeshFilter = GetComponent<MeshFilter>();
        _islandMeshFilter.mesh = null;

        if (randomSpeed) islandSeed = (uint) Random.Range(0, 9999999);
        else islandSeed = customIslandSeed;
        SRnd.SetSeed(islandSeed);

        DestroyPreviousIsland();

        _centerVertex = generateAt;

        SetUpFloors();
        SetUpTip();
        CreateVertices();
        if (generateMesh) GenerateIslandMesh();
    }

    private void DestroyPreviousIsland()
    {
        _floors.Clear();
        _islandVertices.Clear();
        _islandTriangles.Clear();
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

            SetFloorColor(floor, i);
            SetFloorAnchor(floor);
            SetFloorRadius(floor, upwardSwelling, tempRadius);
        }
    }

    private void SetFloorColor(Floor newFloor, int index)
    {
        float t = (float) index / _floors.Count;

        newFloor.index = index;
        newFloor.color = floorColorGradient.Evaluate(t);
    }

    private void SetFloorAnchor(Floor newFloor)
    {
        if (IsFirstFloor(newFloor))
        {
            newFloor.anchorPos = _centerVertex;
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

    private void SetFloorRadius(Floor newFloor, float upwardSwelling, float tempRadius)
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
        _tipVertex = new Vector3(randomPos.x, y, randomPos.z);
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
                _islandVertices.Add(vertexPos);
            }
        }
    }

    private void GenerateIslandMesh()
    {
        _islandMesh = new Mesh();

        _islandVertices.Add(_centerVertex);
        _islandVertices.Add(_tipVertex);

        foreach (var floor in _floors)
        {
            GenerateFloorTriangles(floor);
        }

        _islandMesh.vertices = _islandVertices.ToArray();
        _islandMesh.triangles = _islandTriangles.ToArray();

        _islandMesh.RecalculateNormals();

        _islandMeshFilter.mesh = _islandMesh;
    }

    private void GenerateFloorTriangles(Floor floor)
    {
        for (int i = 0; i < meshComplexity; i++)
        {
            int currentVertex = i + floor.index * meshComplexity;

            // If last vertex in floor (need to apply changes for it to loop)
            if (i == meshComplexity - 1)
            {
                // Ground on island
                _islandTriangles.Add(i);
                _islandTriangles.Add(0);
                _islandTriangles.Add(_islandVertices.Count - 2); // Center vertex

                int firstVertexInFloor = floor.index * meshComplexity;

                if (IsLastFloor(floor))
                {
                    _islandTriangles.Add(firstVertexInFloor);
                    _islandTriangles.Add(currentVertex);
                    _islandTriangles.Add(i + floor.index * meshComplexity + 2);
                }
                else
                {
                    int firstVertexInNextFloor = _floors[floor.index + 1].index * meshComplexity;
                    int currentVertexInNextFloor = i + _floors[floor.index + 1].index * meshComplexity;

                    _islandTriangles.Add(firstVertexInFloor);
                    _islandTriangles.Add(currentVertex);
                    _islandTriangles.Add(currentVertexInNextFloor);

                    _islandTriangles.Add(firstVertexInNextFloor);
                    _islandTriangles.Add(firstVertexInFloor);
                    _islandTriangles.Add(currentVertexInNextFloor);
                }
            }
            else
            {
                // Ground on island
                _islandTriangles.Add(i);
                _islandTriangles.Add(i + 1);
                _islandTriangles.Add(_islandVertices.Count - 2); // Center vertex

                int nextVertexInFloor = i + floor.index * meshComplexity + 1;

                if (IsLastFloor(floor))
                {
                    _islandTriangles.Add(nextVertexInFloor);
                    _islandTriangles.Add(currentVertex);
                    _islandTriangles.Add(_islandVertices.Count - 1);
                }
                else
                {
                    int currentVertexInNextFloor = i + _floors[floor.index + 1].index * meshComplexity;
                    int nextVertexInNextFloor = i + _floors[floor.index + 1].index * meshComplexity + 1;

                    _islandTriangles.Add(nextVertexInFloor);
                    _islandTriangles.Add(currentVertex);
                    _islandTriangles.Add(currentVertexInNextFloor);

                    _islandTriangles.Add(nextVertexInNextFloor);
                    _islandTriangles.Add(nextVertexInFloor);
                    _islandTriangles.Add(currentVertexInNextFloor);
                }
            }
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
        return currentFloor.index == _floors.Count - 1 ? _tipVertex : _floors[currentFloor.index + 1].anchorPos;
    }

    private static bool IsFirstFloor(Floor floor) => floor.index == 0;
    private bool IsLastFloor(Floor floor) => floor.index == _floors.Count - 1;

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
            Handles.Label(_centerVertex, "Center \nPos : " + _centerVertex);
            Handles.Label(_tipVertex, "Tip \nPos : " + _tipVertex);
        }
    }

    private void DrawFloorRadius(Floor floor)
    {
        Handles.color = floor.color;
        Handles.DrawWireDisc(floor.anchorPos, Vector3.up, floor.radius, 3);
    }

    private void DrawVertices()
    {
        for (var i = 0; i < _islandVertices.Count; i++)
        {
            Vector3 vertex = _islandVertices[i];
            Gizmos.color = floorColorGradient.Evaluate(-vertex.y / _islandVertices.Count);
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
        Gizmos.DrawSphere(_centerVertex, verticesGizmosSize);
    }

    private void DrawTipVertex()
    {
        Gizmos.color = tipGizmosColor;
        Gizmos.DrawSphere(_tipVertex, verticesGizmosSize);
    }

    private void DrawVerticesLinks()
    {
        for (int i = 0; i < _islandVertices.Count; i++)
        {
            Vector3 vertex = _islandVertices[i];

            Handles.color = floorColorGradient.Evaluate(-vertex.y / _islandVertices.Count);

            // If first floor, draw to island center
            if (i < meshComplexity)
            {
                Handles.DrawLine(vertex, _centerVertex, verticesLinksThickness);
            }

            // Draw to next vertex in floor
            if ((i + 1) % meshComplexity == 0)
            {
                Handles.DrawLine(vertex, _islandVertices[i - (meshComplexity - 1)], verticesLinksThickness);
            }
            else
            {
                if (i < _islandVertices.Count - 2)
                {
                    Handles.DrawLine(vertex, _islandVertices[i + 1], verticesLinksThickness);
                }
            }

            // Draw to same vertex in next floor, or if last floor to tip vertex
            if (i < meshComplexity * (_floors.Count - 1))
            {
                Handles.DrawLine(vertex, _islandVertices[i + meshComplexity], verticesLinksThickness);
            }
            else
            {
                if (vertex != _centerVertex) Handles.DrawLine(vertex, _tipVertex, verticesLinksThickness);
            }
        }
    }
#endif
}