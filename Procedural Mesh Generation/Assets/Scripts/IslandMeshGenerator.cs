using System.Collections.Generic;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[ExecuteAlways]
public class IslandMeshGenerator : MonoBehaviour
{
    [SerializeField] private bool showGizmos;

    [Header("Seed")] [SerializeField, ReadOnly]
    private uint islandSeed;

    [SerializeField, HideIf("randomSpeed")]
    private uint customIslandSeed;

    [SerializeField] private bool randomSpeed;

    private readonly List<Floor> _floors = new List<Floor>();
    private Vector3 tipPos;

    [SerializeField, Header("Floors parameters")]
    private int floorCount;

    [SerializeField] private Gradient floorColorGradient;
    [SerializeField] private float minFloorsHeight;
    [SerializeField] private float maxFloorsHeight;
    [SerializeField] private float minFloorsRadius;
    [SerializeField] private float maxFloorsRadius;
    [SerializeField] private float floorsMaxOffset;

    [SerializeField, Space] private float upwardSwelling;
    [SerializeField] private float upwardSwellingPow;

    [SerializeField, Tooltip("Number of vertices per floor")]
    private int verticesPerFloor;

    private readonly List<Vector3> _verticesPos = new List<Vector3>();
    [SerializeField] private int minVerticesSpacing;
    [SerializeField] private int maxVerticesSpacing;

    [SerializeField] private float verticesGizmosSize;
    [SerializeField] private Color verticesGizmosColor;

    [Button()]
    private void GenerateIsland()
    {
        if (randomSpeed) islandSeed = (uint)Random.Range(0, 9999999);
        else islandSeed = customIslandSeed;
        SRnd.SetSeed(islandSeed);

        DestroyPreviousIsland();
        SetUpFloors();
        FindTipPos();
        FindIslandVertices();
    }

    private void DestroyPreviousIsland()
    {
        _floors.Clear();
        _verticesPos.Clear();
    }

    private void SetUpFloors()
    {
        for (int i = 0; i < floorCount; i++)
        {
            _floors.Add( new Floor());
        }

        for (var index = 0; index < _floors.Count; index++)
        {
            Floor floor = _floors[index];
            float x = (float)index / _floors.Count;
            floor.color = floorColorGradient.Evaluate(x);
            SetUpFloorAnchor(floor, index);
            SetUpFloorRadius(floor, index);
        }
    }

    private void SetUpFloorAnchor(Floor newFloor, int i)
    {
        Vector3 randomPos = GetRandomFloorPos();
        
        float y;

        if (i > 0)
        {
            Vector3 previousFloorAnchorPos = _floors[i - 1].anchorPos;
            y = previousFloorAnchorPos.y - randomPos.y;
        }
        else y = -randomPos.y;
        
        newFloor.anchorPos = new Vector3(randomPos.x, y, randomPos.z);
    }

    private void SetUpFloorRadius(Floor newFloor, int i)
    {
        float upwardSwellingValue = Mathf.Pow(_floors.Count - i, upwardSwellingPow) * upwardSwelling;
        float tempRadius = SRnd.RangeFloat(minFloorsRadius, maxFloorsRadius);
        float radius = tempRadius * upwardSwellingValue;
        newFloor.radius = radius;
    }

    private void FindTipPos()
    {
        Vector3 randomPos = GetRandomFloorPos();
        Vector3 lastAnchorPos = _floors[^1].anchorPos;
        float y = lastAnchorPos.y - randomPos.y;
        tipPos = new Vector3(randomPos.x /_floors.Count, y, randomPos.z /_floors.Count);
    }

    private Vector3 GetRandomFloorPos()
    {
        float height = SRnd.RangeFloat(minFloorsHeight, maxFloorsHeight);
        float xOffset = SRnd.RangeFloat(-floorsMaxOffset, floorsMaxOffset);
        float zOffset = SRnd.RangeFloat(-floorsMaxOffset, floorsMaxOffset);

        return new Vector3(xOffset, height, zOffset);
    }

    private void FindIslandVertices()
    {
        foreach (var floor in _floors)
        {
            var floorRadius = floor.radius;
            
            for (int i = 0; i < verticesPerFloor; i++)
            {
                Vector2 randomPosInCircle = SRnd.GenerateRandomPointInsideUnitCircle() * floorRadius * SRnd.RangeFloat(0, 1);

                float x = randomPosInCircle.x + floor.anchorPos.x;
                float y = floor.anchorPos.y;
                float z = randomPosInCircle.y + floor.anchorPos.z;
                
                _verticesPos.Add(new Vector3(x, y, z));
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        if (_floors.Count < 1) return;
        
        // White center line
        Handles.DrawLine(Vector3.zero, new Vector3(0,-200,0));
        
        DrawFloorsRadius();
        DrawTip();
        DrawVertices();
    }

    private void DrawFloorsRadius()
    {
        Handles.DrawWireDisc(_floors[0].anchorPos, Vector3.up, _floors[0].radius, 3);

        for (var i = 0; i < _floors.Count; i++)
        {
            Floor floor = _floors[i];
            Handles.color = floor.color;
            Handles.DrawWireDisc(floor.anchorPos, Vector3.up, floor.radius, 3);
        }
    }

    private void DrawTip()
    {
        Gizmos.color = verticesGizmosColor;
        Gizmos.DrawSphere(tipPos, verticesGizmosSize);
    }

    private void DrawVertices()
    {
        foreach (var pos in _verticesPos)
        {
            Gizmos.color = verticesGizmosColor;
            Gizmos.DrawSphere(pos, verticesGizmosSize);
        }
    }
#endif
}