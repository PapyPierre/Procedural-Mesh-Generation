using System.Collections.Generic;
using Procedural_Mesh_Generation.Island_Generation;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Procedural_Mesh_Generation
{
    public class MeshGenerator
    {
        protected static void InitSeed(uint seed)
        {
            seed = seed == 0 ? (uint)Random.Range(1, 9999999) : seed;
            SRnd.SetSeed(seed);
        }

        #region Floors

        protected static void SetUpFloors(MeshData meshData)
        {
            int floorCount = SRnd.RangeInt(meshData.GenData.MinFloorCount,
                meshData.GenData.MaxFloorCount + 1);

            int branching = 0;

            GenerateFloors(meshData, floorCount, ref branching, 0, floorCount);

            float tempRadius = SRnd.RangeFloat(meshData.GenData.MinFloorsRadius,
                meshData.GenData.MaxFloorsRadius);

            Vector2 genDirection = new Vector2(SRnd.RangeFloat(0, 1), SRnd.RangeFloat(0, 1));
            float genDirImpact = meshData.GenData.GenDirImpact;
            
            foreach (var floor in meshData.Floors)
            {
                SetFloorAnchor(meshData, floor, genDirection);
                UpdateGenerationDirection(ref genDirection, genDirImpact);
                SetFloorRadius(meshData, floor, tempRadius);
            }
        }

        private static void GenerateFloors(MeshData meshData, int floorCountToGenerate, ref int branchCount,
            int baseIndexOfNewBranch, int totalFloorCount)
        {
            for (int i = 0; i < floorCountToGenerate; i++)
            {
                Floor floor = new Floor
                {
                    Index = meshData.Floors.Count
                };

                SetFloorInfos(meshData, floor, i, baseIndexOfNewBranch);

                meshData.Floors.Add(floor);

                float branchProba = meshData.GenData.BranchProba.Evaluate((float)floor.DepthIndex / totalFloorCount);
                int maxBranchCount = meshData.GenData.MaxNumberOfBranches;
                int floorLeftToGen = floorCountToGenerate - i;
                
                if (branchCount < maxBranchCount && floorLeftToGen > 3 && SRnd.RangeFloat(0f, 1f) <= branchProba)
                {
                    branchCount++;

                    GenerateFloors(meshData, floorLeftToGen / 3, ref branchCount, floor.Index, totalFloorCount);

                    if (branchCount < maxBranchCount)
                    {
                        GenerateFloors(meshData,  floorLeftToGen / 3 * 2, ref branchCount, floor.Index,
                            totalFloorCount);
                    }

                    return;
                }
            }
        }

        private static void SetFloorInfos(MeshData meshData, Floor floor, int i, int baseIndexOfNewBranch)
        {
            if (i == 0)
            {
                if (baseIndexOfNewBranch != 0) // First floor of new branch
                {
                    floor.PreviousFloorIndex = meshData.Floors[baseIndexOfNewBranch].Index;
                    meshData.Floors[baseIndexOfNewBranch].NextFloorsIndex.Add(floor.Index);

                    floor.DepthIndex = baseIndexOfNewBranch + 1;
                }
                else // First floor
                {
                    floor.PreviousFloorIndex = -2;
                }
            }
            else // Other floors
            {
                if (baseIndexOfNewBranch != 0)
                {
                    floor.PreviousFloorIndex = meshData.Floors[baseIndexOfNewBranch + i].Index;
                    meshData.Floors[baseIndexOfNewBranch + i].NextFloorsIndex.Add(floor.Index);

                    floor.DepthIndex = baseIndexOfNewBranch + i + 1;
                }
                else
                {
                    floor.PreviousFloorIndex = meshData.Floors[i - 1].Index;
                    meshData.Floors[i - 1].NextFloorsIndex.Add(floor.Index);

                    floor.DepthIndex = i;
                }
            }
        }

        private static void SetFloorAnchor(MeshData meshData, Floor floor, Vector2 genDir)
        {
            Vector3 previousFloorAnchorPos = meshData.IsFirstFloor(floor)
                ? meshData.CenterVertex
                : meshData.GetPreviousFloor(floor).AnchorPos;

            Vector3 randomPos = GetFloorPos(meshData, floor, previousFloorAnchorPos, genDir);

            float y = meshData.CenterVertex.y;

            if (!meshData.IsFirstFloor(floor) || meshData.GenData.HeightBetweenCenterAndFirstFloor)
            {
                y = previousFloorAnchorPos.y + randomPos.y;
            }

            floor.AnchorPos = new Vector3(randomPos.x, y, randomPos.z);
        }

        private static void UpdateGenerationDirection(ref Vector2 genDir, float impact)
        {
            genDir += new Vector2(SRnd.RangeFloat(0, impact), SRnd.RangeFloat(0, impact)) * 2;
            genDir /= 3;
        }

        private static void SetFloorRadius(MeshData meshData, Floor floor, float tempRadius)
        {
            float t = (float)floor.DepthIndex / meshData.Floors.Count;
            float swellingValue = meshData.GenData.SwellingAlongVerticalAxis.Evaluate(t);
            swellingValue *= meshData.GenData.SwellingImpact;

            float randomSwelling = 0;

            if (SRnd.RangeInt(0, 100) <= meshData.GenData.RandomSwellingChance)
            {
                randomSwelling = SRnd.RangeFloat(meshData.GenData.MinRandomSwelling,
                    meshData.GenData.MaxRandomSwelling);
            }

            float radius = tempRadius * swellingValue + randomSwelling;
            floor.Radius = radius;
        }

        private static Vector3 GetFloorPos(MeshData meshData, Floor currentFloor, Vector3 prevFloorPos, Vector2 genDir)
        {
            float height = SRnd.RangeFloat(meshData.GenData.MinFloorsHeight, meshData.GenData.MaxFloorsHeight);

            float t = (float)currentFloor.DepthIndex / meshData.Floors.Count;
            float offsetValue = meshData.GenData.FloorsOffsetAlongVerticalAxis.Evaluate(t);
            offsetValue *= meshData.GenData.FloorsOffesetImpact;

            float xOffset = SRnd.RangeFloat(-offsetValue, offsetValue) + prevFloorPos.x + genDir.x;
            float zOffset = SRnd.RangeFloat(-offsetValue, offsetValue) + prevFloorPos.z + genDir.y;

            return new Vector3(xOffset, height, zOffset);
        }

        #endregion

        #region Verticies

        protected static void CreateVertices(MeshData meshData)
        {
            foreach (var floor in meshData.Floors)
            {
                for (int i = 0; i < meshData.GenData.MeshComplexity; i++)
                {
                    floor.Vertices.Add(GetVertexPos(meshData, floor, i));
                }
            }
        }

        private static Vector3 GetVertexPos(MeshData meshData, Floor floor, int index)
        {
            float angle = index * 2 * Mathf.PI / meshData.GenData.MeshComplexity;

            float x = floor.AnchorPos.x + floor.Radius * Mathf.Cos(angle) +
                      SRnd.RangeFloat(0, meshData.GenData.VerticesMaxOffset);

            float y = meshData.IsFirstFloor(floor)
                ? meshData.Floors[0].AnchorPos.y
                : floor.AnchorPos.y + SRnd.RangeFloat(0, meshData.GenData.VerticesMaxOffset / floor.Index);

            float z = floor.AnchorPos.z + floor.Radius * Mathf.Sin(angle) +
                      SRnd.RangeFloat(0, meshData.GenData.VerticesMaxOffset);

            return new Vector3(x, y, z);
        }

        #endregion

        protected static void AddTrianglesToMeshData(MeshData meshData, Floor floor)
        {
            int meshComplexity = meshData.GenData.MeshComplexity;

            for (int i = 0; i < meshComplexity; i++)
            {
                int currentVertex = i + floor.Index * meshComplexity;

                // If last vertex in floor (need to apply changes for it to loop)
                if (i == meshComplexity - 1)
                {
                    int firstVertexInFloor = floor.Index * meshComplexity;

                    if (meshData.IsLastFloor(floor)) continue;

                    foreach (var nextFloor in meshData.GetNextFloors(floor))
                    {
                        int firstVertexInNextFloor = nextFloor.Index * meshComplexity;
                        int currentVertexInNextFloor = i + nextFloor.Index * meshComplexity;

                        meshData.Triangles.Add(currentVertex);
                        meshData.Triangles.Add(currentVertexInNextFloor);
                        meshData.Triangles.Add(firstVertexInFloor);

                        meshData.Triangles.Add(firstVertexInFloor);
                        meshData.Triangles.Add(currentVertexInNextFloor);
                        meshData.Triangles.Add(firstVertexInNextFloor);
                    }
                }
                else
                {
                    int nextVertexInFloor = i + floor.Index * meshComplexity + 1;

                    if (meshData.IsLastFloor(floor)) continue;

                    foreach (var nextFloor in meshData.GetNextFloors(floor))
                    {
                        int currentVertexInNextFloor = i + nextFloor.Index * meshComplexity;
                        int nextVertexInNextFloor = i + nextFloor.Index * meshComplexity + 1;

                        meshData.Triangles.Add(currentVertex);
                        meshData.Triangles.Add(currentVertexInNextFloor);
                        meshData.Triangles.Add(nextVertexInFloor);

                        meshData.Triangles.Add(nextVertexInFloor);
                        meshData.Triangles.Add(currentVertexInNextFloor);
                        meshData.Triangles.Add(nextVertexInNextFloor);
                    }
                }
            }
        }


        protected static Mesh CreateMesh(MeshData data)
        {
            Mesh mesh = new Mesh();

            List<Vector3> allVertices = new List<Vector3>();

            foreach (var floor in data.Floors)
            {
                foreach (var vertex in floor.Vertices)
                {
                    allVertices.Add(vertex);
                }
            }

            allVertices.Add(data.CenterVertex);
            if (data is IslandMeshData islandMeshData) allVertices.Add(islandMeshData.PivotVertex);

            mesh.vertices = allVertices.ToArray();
            mesh.triangles = data.Triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.name = data.GenData.MeshName;

            return mesh;
        }

        protected static T GenerateObj<T>(Mesh mesh, MeshData data) where T : GeneratedObj
        {
            T generatedObj = new GameObject().AddComponent<T>();

            generatedObj.name = data.GenData.ObjName;
            generatedObj.MeshData = data;

            MeshRenderer renderer = generatedObj.gameObject.AddComponent<MeshRenderer>();
            renderer.material = data.GenData.Material;

            MeshFilter islandMeshFilter = generatedObj.gameObject.AddComponent<MeshFilter>();
            islandMeshFilter.mesh = mesh;

            return generatedObj;
        }
    }
}