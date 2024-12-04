using System.Collections;
using NaughtyAttributes;
using Procedural_Mesh_Generation.Island_Generation;
using Procedural_Mesh_Generation.Tree_Generation;
using UnityEngine;

namespace Demo
{
    public class GeneratorCaller : MonoBehaviour
    {
        [SerializeField] private IslandGenerationData m_islandGenerationData;
        [SerializeField] private TreeGenerationData m_treeGenerationData;
        [SerializeField] private Vector3 m_generateAt;
        [SerializeField] private Vector3 m_generateAt2;

        [SerializeField] private bool testIsland;
        [SerializeField] private bool testTree;
        
        [Button]
        private void Generate()
        {
            foreach (var obj in Debuger.Instance.GeneratedObjToDebug)
            {
                Destroy(obj.gameObject);
            }
            
            Debuger.Instance.GeneratedObjToDebug.Clear();
            
            if (testIsland)
            {
                GeneratedIsland island = IslandMeshGenerator.GenerateIsland(m_generateAt, m_islandGenerationData);
                Debuger.Instance.GeneratedObjToDebug.Add(island);
            }

            if (testTree)
            {
                GeneratedTree tree = TreeMeshGenerator.GenerateTree(m_generateAt2, m_treeGenerationData);
                Debuger.Instance.GeneratedObjToDebug.Add(tree);
            }
        }
    }
}