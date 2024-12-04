using UnityEngine;

namespace Procedural_Mesh_Generation.Tree_Generation
{
    public class TreeMeshGenerator : MeshGenerator
    {
        public static GeneratedTree GenerateTree(Vector3 position, TreeGenerationData data, uint seed = 0)
        {
            TreeMeshData generatedTreeMeshData = new TreeMeshData
            {
                GenData = data
            };
            
            InitSeed(seed);
         
            generatedTreeMeshData.CenterVertex = generatedTreeMeshData.PivotVertex = position;

            SetUpFloors(generatedTreeMeshData);
            CreateVertices(generatedTreeMeshData);

            return GenerateTreeMesh(generatedTreeMeshData);
        }

        private static GeneratedTree GenerateTreeMesh(TreeMeshData treeMeshData)
        {
            foreach (var floor in treeMeshData.Floors)
            {
                AddTrianglesToMeshData(treeMeshData, floor);
            }
            
            return GenerateObj<GeneratedTree>(CreateMesh(treeMeshData), treeMeshData);
        }
    }
}