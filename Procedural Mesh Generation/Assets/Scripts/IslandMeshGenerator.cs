using System.Collections.Generic;
using NaughtyAttributes;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[ExecuteAlways]
public class IslandMeshGenerator : MonoBehaviour
{
    [Header("Seed")] [SerializeField] private bool randomSpeed;

    [SerializeField, HideIf("randomSpeed")]
    private uint customIslandSeed;

    [SerializeField, ReadOnly] private uint islandSeed;

    [Header("Floors parameters")] [SerializeField]
    private int floorCount;

    [SerializeField, Space] private float minFloorsHeight;
    [SerializeField] private float maxFloorsHeight;

    [SerializeField, Space] private float minFloorsRadius;
    [SerializeField] private float maxFloorsRadius;

    [SerializeField, Space] private float floorsMaxOffset;

    [Header("Upward Swelling")] [SerializeField]
    private float upwardSwelling;

    [SerializeField] private float upwardSwellingPow;

    [Header("Vertices")] [SerializeField, Tooltip("Number of vertices in the mesh")]
    private int meshComplexity;

    [Header("Global Gizmos")] [SerializeField]
    private bool hideAllGizmos;

    [SerializeField, HideIf("hideAllGizmos")]
    private bool showCenterLine;

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
    private Color tipGizmosColor;

    [Header("Vertices Links"), HideIf("hideAllGizmos"), SerializeField]
    private bool showVerticesLinks;

    [SerializeField, HideIf("hideAllGizmos")]
    private float verticesLinksThickness;

    #region Hidden Fields

    [SerializeField] private List<Floor> _floors = new List<Floor>();
    private Vertex tipVertex;

    #endregion

    [Button()]
    private void GenerateIsland()
    {
        if (randomSpeed) islandSeed = (uint)Random.Range(0, 9999999);
        else islandSeed = customIslandSeed;
        SRnd.SetSeed(islandSeed);

        DestroyPreviousIsland();
        SetUpFloors();
        SetUpTip();
        CreateFloorsVertices();
        MoveFloorsVertices();
        SortAndIndexFloorVertices();
    }

    private void DestroyPreviousIsland()
    {
        _floors.Clear();
    }

    private void SetUpFloors()
    {
        for (int i = 0; i < floorCount; i++)
        {
            _floors.Add(new Floor());
        }

        for (var i = 0; i < _floors.Count; i++)
        {
            Floor floor = _floors[i];

            float t = (float)i / _floors.Count;

            floor.index = i;
            floor.color = floorColorGradient.Evaluate(t);
            floor.verticesCount = meshComplexity;

            SetUpFloorAnchor(floor);
            SetUpFloorRadius(floor);
        }
    }

    private void SetUpFloorAnchor(Floor newFloor)
    {
        Vector3 randomPos = GetRandomFloorPos();

        float y;

        if (newFloor.index > 0) // Is not first floor
        {
            Vector3 previousFloorAnchorPos = _floors[newFloor.index - 1].anchorPos;
            y = previousFloorAnchorPos.y - randomPos.y;
        }
        else y = 0;

        newFloor.anchorPos = new Vector3(randomPos.x, y, randomPos.z);
    }

    private void SetUpFloorRadius(Floor newFloor)
    {
        float upwardSwellingValue = Mathf.Pow(_floors.Count - newFloor.index, upwardSwellingPow) * upwardSwelling;
        float tempRadius = SRnd.RangeFloat(minFloorsRadius, maxFloorsRadius);
        float radius = tempRadius * upwardSwellingValue;
        newFloor.radius = radius;
    }

    private void SetUpTip()
    {
        Vector3 randomPos = GetRandomFloorPos();
        Vector3 lastAnchorPos = _floors[^1].anchorPos;
        float y = lastAnchorPos.y - randomPos.y;
        tipVertex = new Vertex();
        tipVertex.position = new Vector3(randomPos.x / _floors.Count, y, randomPos.z / _floors.Count);
    }

    private Vector3 GetRandomFloorPos()
    {
        float height = SRnd.RangeFloat(minFloorsHeight, maxFloorsHeight);
        float xOffset = SRnd.RangeFloat(-floorsMaxOffset, floorsMaxOffset);
        float zOffset = SRnd.RangeFloat(-floorsMaxOffset, floorsMaxOffset);

        return new Vector3(xOffset, height, zOffset);
    }

    private void CreateFloorsVertices()
    {
        foreach (var floor in _floors)
        {
            var floorRadius = floor.radius;

            for (int i = 0; i < floor.verticesCount; i++)
            {
                Vector2 randomPosInCircle =
                    SRnd.GenerateRandomPointInsideUnitCircle() * floorRadius * SRnd.RangeFloat(0, 1);

                float x = randomPosInCircle.x + floor.anchorPos.x;
                float y = floor.anchorPos.y;
                float z = randomPosInCircle.y + floor.anchorPos.z;

                Vertex newVertex = new Vertex
                {
                    position = new Vector3(x, y, z)
                };

                floor.vertices.Add(newVertex);
            }
        }
    }

    private void MoveFloorsVertices()
    {
        for (var i = 0; i < _floors.Count; i++)
        {
            Floor floor = _floors[i];

            Vector3 nextFloorPos = GetNextFloorAnchor(floor);

            Vector3 dirToNextFloor = (nextFloorPos - floor.anchorPos).normalized;
            float distToNextFloor = Vector3.Distance(floor.anchorPos, nextFloorPos);

            foreach (var vertex in floor.vertices)
            {
                vertex.position += dirToNextFloor * SRnd.RangeFloat(0, distToNextFloor);
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

    List<Vertex> SortVerticesClockwise(List<Vertex> vertices, Vector3 referencePoint)
    {
        vertices.Sort((p1, p2) => ClockwiseCompare(p1.position, p2.position, referencePoint));
        return vertices;
    }

    int ClockwiseCompare(Vector3 p1, Vector3 p2, Vector3 referencePoint)
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

    Floor GetNextFloor(Floor currentFloor)
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

    Vector3 GetNextFloorAnchor(Floor currentFloor)
    {
        return currentFloor.index == _floors.Count - 1 ? tipVertex.position : _floors[currentFloor.index + 1].anchorPos;
    }

    bool IsLastFloor(Floor floor)
    {
        return floor.index == _floors.Count -1;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (hideAllGizmos) return;
        if (_floors.Count < 1) return;

        if (showCenterLine) Handles.DrawLine(Vector3.zero, new Vector3(0, -200, 0));

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

        if (showVertices) DrawTipVertex();
        if (showVerticesLabels) Handles.Label(tipVertex.position, "Tip \nPos : " + tipVertex.position);
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
                    floor.vertices[index].position,
                    nextFloor.vertices[index].position,
                    verticesLinksThickness);
            }
        }
    }
#endif
}