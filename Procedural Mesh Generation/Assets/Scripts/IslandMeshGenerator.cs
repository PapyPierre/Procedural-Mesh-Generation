using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteAlways]
public class IslandMeshGenerator : MonoBehaviour
{
    [SerializeField] private bool showGizmos;

    private readonly List<Vector3> _floorsAnchorPos = new List<Vector3>();
    private Vector3 tipPos;

    [SerializeField, Header("Floors parameters")] private int floorCount;
    [SerializeField] private float floorsSpacing;
    [SerializeField] private float floorsRadius;
    [SerializeField] private float floorsMarging;
    
    [SerializeField, Range(0.001f, 1f), Space] private float upwardSwelling;

    [SerializeField, Tooltip("Number of vertices per floor")] private int verticesPerFloor;
    private readonly List<Vector3> _verticesPos = new List<Vector3>();
    [SerializeField] private int minVerticesSpacing;
    [SerializeField] private int maxVerticesSpacing;

    [Button()]
    private void GenerateIsland()
    {
        DestroyPreviousIsland();
        FindFloorsAnchorPos();
        FindTipPos();
        FindIslandVertices();
    }

    private void DestroyPreviousIsland()
    {
        _floorsAnchorPos.Clear();
        _verticesPos.Clear();
    }
    private void FindFloorsAnchorPos()
    {
        for (int i = 0; i < floorCount; i++)
        {
            _floorsAnchorPos.Add(new Vector3(0,-i * floorsSpacing, 0));
        }
    }
    private void FindTipPos()
    {
        tipPos = new Vector3(0,-floorCount * floorsSpacing, 0);
    }
    private void FindIslandVertices()
    {
        //BUG Does not work
        
        float distanceWithinMarge = Random.Range(floorsRadius - floorsMarging, floorsRadius + floorsMarging) * upwardSwelling;

        for (int i = 0; i < verticesPerFloor; i++)
        {
            float angle = Random.Range(0f, 2 * Mathf.PI);
            
            float x = distanceWithinMarge * Mathf.Cos(angle);
            float y = distanceWithinMarge * Mathf.Sin(angle);
            float z = distanceWithinMarge * Mathf.Sin(angle);
            
            
            _verticesPos.Add(new Vector3(x,y, z));
        }
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        if (_floorsAnchorPos.Count < 1) return;

        DrawFloorsRadius();
        DrawTip();
        DrawVertices();
    }

    private void DrawFloorsRadius()
    {
        for (var i = 0; i < _floorsAnchorPos.Count; i++)
        {
            Vector3 pos = _floorsAnchorPos[i];

            float upwardSwellingValue = (_floorsAnchorPos.Count - i) * upwardSwelling;
            float r = floorsRadius * upwardSwellingValue;
            float margedRadius = floorsMarging  * upwardSwellingValue;
            
            Handles.color = Color.yellow;
            
            // Inner disc
            Handles.DrawWireDisc(pos, Vector3.up, r - margedRadius, 1);
            
            // Outer disc
            Handles.DrawWireDisc(pos, Vector3.up, r + margedRadius, 1);
            
            // Upper Disc
            Handles.DrawWireDisc(new Vector3(pos.x, pos.y + margedRadius, pos.z), Vector3.up, r, 1);
            
            // Lower Disc
            Handles.DrawWireDisc(new Vector3(pos.x, pos.y - margedRadius, pos.z), Vector3.up, r, 1);

            Handles.color = Color.red;
            Handles.DrawWireDisc(pos, Vector3.up, r, 3);
        }
    }
    private void DrawTip()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(tipPos, 0.2f);
    }
    private void DrawVertices()
    {
        foreach (var pos in _verticesPos)
        {
            Gizmos.DrawSphere(pos, 0.2f);
        }
    }
#endif
}