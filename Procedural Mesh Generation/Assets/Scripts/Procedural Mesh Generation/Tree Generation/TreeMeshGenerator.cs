using UnityEngine;

namespace Procedural_Mesh_Generation.Tree_Generation
{
    public class TreeMeshGenerator : MeshGenerator
    {
        public static GeneratedTree GenerateTree(Vector3 position, TreeGenerationData data, uint seed = 0)
        {
            TreeMeshData generatedTreeMeshData = new TreeMeshData
            {
                GenerationData = data
            };
            
            InitSeed(seed);
         
            generatedTreeMeshData.CenterVertex = position;

            SetUpFloors(generatedTreeMeshData);

            return GenerateTreeMesh(generatedTreeMeshData);
        }

        private static GeneratedTree GenerateTreeMesh(TreeMeshData treeMeshData)
        {
            return GenerateObj<GeneratedTree>(CreateMesh(treeMeshData), treeMeshData);
        }
    }
}