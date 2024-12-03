using System.Collections;
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

        [SerializeField] private bool testIsland;
        [SerializeField] private bool testTree;
        
        private IEnumerator Start()
        {
            if (testIsland)
            {
                GeneratedIsland island = IslandMeshGenerator.GenerateIsland(m_generateAt, m_islandGenerationData);
                Debuger.Instance.GeneratedObjToDebug.Add(island);
            }

            if (testTree)
            {
                GeneratedTree tree = TreeMeshGenerator.GenerateTree(m_generateAt, m_treeGenerationData);
                Debuger.Instance.GeneratedObjToDebug.Add(tree);
            }
            
            yield return null;
            
            /*
            for (int i = 0; i < 100; i++)
            {
                GameObject lastIsland = IslandMeshGenerator.GenerateIsland(m_generateAt, m_generationData);
                yield return new WaitForSeconds(5);
                Destroy(lastIsland);
            }
            */
        }
    }
}