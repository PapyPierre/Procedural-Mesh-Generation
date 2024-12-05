using System.Collections.Generic;
using Procedural_Mesh_Generation;
using Procedural_Mesh_Generation.Island_Generation;
using UnityEngine;
using UnityEditor;
using UnityEngine.Serialization;

public class Debuger : MonoBehaviour
{
    public static Debuger Instance;

    public List<GeneratedObj> GeneratedObjToDebug = new List<GeneratedObj>();

    [Header("Global Gizmos")] [SerializeField]
    private bool m_hideAllGizmos;

    [FormerlySerializedAs("m_showIslandSpine")] [SerializeField]
    private bool m_showSpine;


    [Header("Floor Gizmos")] [SerializeField]
    private bool m_showFloorsRadius = true;

    [SerializeField] private Gradient m_floorColorGradient;

    [SerializeField] private bool m_showFloorLabels;


    [Header("Vertices Gizmos")] [SerializeField]
    private bool m_showVertices = true;

    [SerializeField] private float m_verticesGizmosSize = 0.5f;

    [SerializeField] private bool m_showVerticesLabels;

    [SerializeField] private Color m_centerGizmosColor;

    [SerializeField] private Color m_tipGizmosColor;


    [Header("Vertices Links"), SerializeField]
    private bool m_showVerticesLinks = true;

    [SerializeField] private float m_verticesLinksThickness = 1.5f;

    private void Awake()
    {
        Instance = this;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (m_hideAllGizmos) return;

        foreach (var island in GeneratedObjToDebug)
        {
            MeshData meshData = island.MeshData;

            foreach (var floor in meshData.Floors)
            {
                if (m_showSpine && !meshData.IsFirstFloor(floor)) DrawSpine(floor, meshData);
                if (m_showFloorsRadius) DrawFloorRadius(floor, meshData.Floors.Count);

                if (m_showFloorLabels)
                {
                    DrawFloorLabels(floor);
                }
            }

            if (m_showVertices)
            {
                DrawVertices(meshData);

                DrawCenterVertex(meshData.GenerationPoint);

                if (meshData is IslandMeshData islandMeshData)
                {
                    DrawTipVertex(islandMeshData.PivotVertex);
                }
            }

            if (m_showVerticesLinks) DrawVerticesLinks(meshData);
        }
    }

    private Vector3 GetNextFloorAnchor(Floor floor, MeshData meshData)
    {
        if (meshData is IslandMeshData islandMeshData)
        {
            return floor.Index == meshData.Floors.Count - 1
                ? islandMeshData.PivotVertex
                : meshData.Floors[floor.Index + 1].Pos;
        }

        return meshData.Floors[floor.Index + 1].Pos;
    }

    private void DrawFloorRadius(Floor floor, int floorCount)
    {
        Handles.color = GetFloorColor(floor, floorCount);
        Gizmos.DrawRay(floor.Pos, floor.Normal);
        Handles.DrawWireDisc(floor.Pos, floor.Normal, floor.Radius, 3);
    }

    private void DrawFloorLabels(Floor floor)
    {
        string floorTextLabel = $"Floor {floor.Index}" +
                                $"\nDepth: {floor.DepthIndex}" +
                                $"\nPos: {floor.Pos}" +
                                $"\nRadius: {floor.Radius}" +
                                $"\nPrevious Floor Index: {floor.PreviousFloorIndex}" +
                                $"\nNext Floors Index:";

        foreach (int index in floor.NextFloorsIndex)
        {
            floorTextLabel += $"\n {index}";
        }
        
        Handles.Label(floor.Pos, floorTextLabel);
    }

    private Color GetFloorColor(Floor floor, int floorCount)
    {
        float t = (float)floor.Index / floorCount;
        return m_floorColorGradient.Evaluate(t);
    }

    private void DrawVertices(MeshData meshData)
    {
        foreach (var floor in meshData.Floors)
        {
            for (var i = 0; i < floor.Vertices.Count; i++)
            {
                Vector3 vertex = floor.Vertices[i];
                Gizmos.color = GetFloorColor(floor, meshData.Floors.Count);
                Gizmos.DrawSphere(vertex, m_verticesGizmosSize);
                if (m_showVerticesLabels) Handles.Label(vertex, $"Vextex {i} \nPos : {vertex}");
            }
        }

        if (m_showVerticesLabels)
        {
            Handles.Label(meshData.GenerationPoint, "Center \nPos : " + meshData.GenerationPoint);

            if (meshData is IslandMeshData islandMeshData)
            {
                Handles.Label(islandMeshData.PivotVertex, "Tip \nPos : " + islandMeshData.PivotVertex);
            }
        }
    }

    private void DrawSpine(Floor floor, MeshData meshData)
    {
        Handles.color = GetFloorColor(floor, meshData.Floors.Count);
        Handles.DrawLine(floor.Pos,  meshData.GetPreviousFloor(floor).Pos, 3);
    }

    private void DrawCenterVertex(Vector3 centerVertexPos)
    {
        Gizmos.color = m_centerGizmosColor;
        Gizmos.DrawSphere(centerVertexPos, m_verticesGizmosSize);
    }

    private void DrawTipVertex(Vector3 tipVertexPos)
    {
        Gizmos.color = m_tipGizmosColor;
        Gizmos.DrawSphere(tipVertexPos, m_verticesGizmosSize);
    }

    private void DrawVerticesLinks(MeshData meshData)
    {
        foreach (var floor in meshData.Floors)
        {
            for (int i = 0; i < floor.Vertices.Count; i++)
            {
                Vector3 vertex = floor.Vertices[i];

                Handles.color = GetFloorColor(floor, meshData.Floors.Count);

                Handles.DrawLine(vertex, i == floor.Vertices.Count - 1 ? floor.Vertices[0] : floor.Vertices[i + 1],
                    m_verticesLinksThickness);

                // If first floor, draw to center vertex
                if (meshData.IsFirstFloor(floor))
                {
                    Handles.DrawLine(vertex, meshData.GenerationPoint, m_verticesLinksThickness);
                }
                else
                {
                    if (meshData.IsLastFloor(floor) && meshData is IslandMeshData islandMeshData)
                    {
                        Handles.DrawLine(vertex, islandMeshData.PivotVertex, m_verticesLinksThickness);
                    }

                    Floor previousFloor = meshData.GetPreviousFloor(floor);

                    if (i < previousFloor.Vertices.Count)
                    {
                        Handles.DrawLine(vertex, previousFloor.Vertices[i], m_verticesLinksThickness);
                    }
                }
            }
        }
    }
#endif
}