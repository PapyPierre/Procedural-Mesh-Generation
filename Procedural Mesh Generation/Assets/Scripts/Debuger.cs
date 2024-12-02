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

    [FormerlySerializedAs("m_showIslandSpine")] [SerializeField] private bool m_showSpine;


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
                if (m_showSpine) DrawSpine(floor, meshData.Floors.Count, GetNextFloorAnchor(floor, meshData));
                if (m_showFloorsRadius) DrawFloorRadius(floor, meshData.Floors.Count);

                if (m_showFloorLabels)
                {
                    string floorTextLabel = $"Floor {floor.Index}\nPos: {floor.AnchorPos}\nRadius: {floor.Radius}";
                    Handles.Label(floor.AnchorPos, floorTextLabel);
                }
            }

            if (m_showVertices) DrawVertices(meshData);
            if (m_showVerticesLinks) DrawVerticesLinks(meshData);

            if (m_showVertices)
            {
                DrawCenterVertex(meshData.CenterVertex);

                if (meshData is IslandMeshData islandMeshData)
                {
                    DrawTipVertex(islandMeshData.TipVertex);
                }
            }

            if (m_showVerticesLabels)
            {
                Handles.Label(meshData.CenterVertex, "Center \nPos : " + meshData.CenterVertex);

                if (meshData is IslandMeshData islandMeshData)
                {
                    Handles.Label(islandMeshData.TipVertex, "Tip \nPos : " + islandMeshData.TipVertex);
                }
            }
        }
    }

    private Vector3 GetNextFloorAnchor(Floor floor, MeshData meshData)
    {
        if (meshData is IslandMeshData islandMeshData)
        {
            return floor.Index == meshData.Floors.Count - 1
                ? islandMeshData.TipVertex
                : meshData.Floors[floor.Index + 1].AnchorPos;
        }

        return meshData.Floors[floor.Index + 1].AnchorPos;
    }

    private void DrawFloorRadius(Floor floor, int floorCount)
    {
        Handles.color = GetFloorColor(floor, floorCount);
        Handles.DrawWireDisc(floor.AnchorPos, Vector3.up, floor.Radius, 3);
    }

    private Color GetFloorColor(Floor floor, int floorCount)
    {
        float t = (float)floor.Index / floorCount;
        return m_floorColorGradient.Evaluate(t);
    }

    private void DrawVertices(MeshData meshData)
    {
        for (var i = 0; i < meshData.Vertices.Count; i++)
        {
            Vector3 vertex = meshData.Vertices[i];
            Gizmos.color = m_floorColorGradient.Evaluate(-vertex.y / meshData.Vertices.Count);
            Gizmos.DrawSphere(vertex, m_verticesGizmosSize);
            if (m_showVerticesLabels) Handles.Label(vertex, "Vextex " + i + " \nPos : " + vertex);
        }
    }

    private void DrawSpine(Floor floor, int floorCount, Vector3 nextFloorPos)
    {
        Handles.color = GetFloorColor(floor, floorCount);
        Handles.DrawLine(floor.AnchorPos, nextFloorPos, 3);
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
        for (int i = 0; i < meshData.Vertices.Count; i++)
        {
            Vector3 vertex = meshData.Vertices[i];

            Handles.color = m_floorColorGradient.Evaluate(-vertex.y / meshData.Vertices.Count);

            // If first floor, draw to island center
            if (i < meshData.GenerationData.MeshComplexity)
            {
                Handles.DrawLine(vertex, meshData.CenterVertex, m_verticesLinksThickness);
            }

            // Draw to next vertex in floor
            if ((i + 1) % meshData.GenerationData.MeshComplexity == 0)
            {
                Handles.DrawLine(vertex, meshData.Vertices[i - (meshData.GenerationData.MeshComplexity - 1)],
                    m_verticesLinksThickness);
            }
            else
            {
                if (i < meshData.Vertices.Count - 2)
                {
                    Handles.DrawLine(vertex, meshData.Vertices[i + 1], m_verticesLinksThickness);
                }
            }

            // Draw to same vertex in next floor, or if last floor to tip vertex
            if (i < meshData.GenerationData.MeshComplexity * (meshData.Floors.Count - 1))
            {
                Handles.DrawLine(vertex, meshData.Vertices[i + meshData.GenerationData.MeshComplexity],
                    m_verticesLinksThickness);
            }
            else
            {
                if (vertex != meshData.CenterVertex)
                {
                    if (meshData is IslandMeshData islandMeshData)
                    {
                        Handles.DrawLine(vertex, islandMeshData.TipVertex, m_verticesLinksThickness);
                    }
                }
            }
        }
    }
#endif
}