using System;
using Procedural_Mesh_Generation.Tree_Generation;
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
            int floorCount = SRnd.RangeInt(meshData.GenerationData.MinFloorCount,
                meshData.GenerationData.MaxFloorCount + 1);
            
            int necessaryBranching = SRnd.RangeInt(2, meshData.GenerationData.MaxNumberOfBranches) - 1;

            GenerateFloors(meshData, floorCount, ref necessaryBranching);

            float directionalSwelling = SRnd.RangeFloat(meshData.GenerationData.MinDirectionSwelling,
                meshData.GenerationData.MaxDirectionSwelling);
            float tempRadius = SRnd.RangeFloat(meshData.GenerationData.MinFloorsRadius,
                meshData.GenerationData.MaxFloorsRadius);

            foreach (var floor in meshData.Floors)
            {
                SetFloorAnchor(meshData, floor);
                SetFloorRadius(meshData, floor, directionalSwelling, tempRadius);
            }
        }

        private static void GenerateFloors(MeshData meshData, int floorCountToGenerate,ref int necessaryBranching,
            int baseIndexOfNewBranch = 0)
        {
            float branchingProba = 100f / floorCountToGenerate;

            for (int i = 0; i < floorCountToGenerate; i++)
            {
                Floor floor = new Floor
                {
                    Index = baseIndexOfNewBranch + i
                };

                if (necessaryBranching > 0 && !IsFirstFloor(floor))
                {
                    if (SRnd.RangeFloat(0, 100) <= branchingProba)
                    {
                        floor.IsBranching = true;
                        necessaryBranching--;
                        meshData.Floors.Add(floor);
                        GenerateFloors(meshData, floorCountToGenerate - i, ref necessaryBranching, i++);
                        GenerateFloors(meshData, (floorCountToGenerate - i) /2, ref necessaryBranching, i++);
                        return;
                    }
                }

                branchingProba += 100f / floorCountToGenerate;
                meshData.Floors.Add(floor);
            }
        }

        private static void SetFloorAnchor(MeshData meshData, Floor floor)
        {
            if (IsFirstFloor(floor))
            {
                floor.AnchorPos = meshData.CenterVertex;
            }
            else
            {
                Floor previousFloor = GetPreviousFloor(meshData, floor);
                Vector3 randomPos = GetFloorPos(meshData.GenerationData, previousFloor);

                Vector3 previousFloorAnchorPos = previousFloor.AnchorPos;

                float y = meshData.GenerationData.SwellingDirection switch
                {
                    GenerationDirection.Upward => previousFloorAnchorPos.y + randomPos.y,
                    GenerationDirection.Downward => previousFloorAnchorPos.y - randomPos.y,
                    _ => throw new ArgumentOutOfRangeException()
                };

                floor.AnchorPos = new Vector3(randomPos.x, y, randomPos.z);
            }
        }

        private static void SetFloorRadius(MeshData meshData, Floor newFloor, float directionnalSwelling,
            float tempRadius)
        {
            float directionalSwellingValue = Mathf.Pow(meshData.Floors.Count - newFloor.Index, directionnalSwelling);

            float randomSwelling = 0;

            if (SRnd.RangeInt(0, 100) <= meshData.GenerationData.RandomSwellingChance)
            {
                randomSwelling = SRnd.RangeFloat(meshData.GenerationData.MinRandomSwelling,
                    meshData.GenerationData.MaxRandomSwelling);
            }

            float radius = tempRadius * directionalSwellingValue + randomSwelling;
            newFloor.Radius = radius;
        }

        private static Floor GetPreviousFloor(MeshData meshData, Floor currentFloor)
        {
            if (currentFloor.Index == 0)
            {
                Debug.LogError("Is first floor");
                return null;
            }

            return meshData.Floors[currentFloor.Index - 1];
        }

        protected static Vector3 GetFloorPos(MeshGenerationData data, Floor previousFloor)
        {
            float height = SRnd.RangeFloat(data.MinFloorsHeight, data.MaxFloorsHeight);
            float xOffset = SRnd.RangeFloat(-data.FloorsMaxOffset, data.FloorsMaxOffset) + previousFloor.AnchorPos.x;
            float zOffset = SRnd.RangeFloat(-data.FloorsMaxOffset, data.FloorsMaxOffset) + previousFloor.AnchorPos.z;

            return new Vector3(xOffset, height, zOffset);
        }

        protected static bool IsFirstFloor(Floor floor) => floor.Index == 0;
        protected static bool IsLastFloor(MeshData meshData, Floor floor) => floor.Index == meshData.Floors.Count - 1;

        #endregion

        protected static Mesh CreateMesh(MeshData data)
        {
            Mesh mesh = new Mesh
            {
                vertices = data.Vertices.ToArray(),
                triangles = data.Triangles.ToArray()
            };

            mesh.RecalculateNormals();
            mesh.name = data.GenerationData.MeshName;

            return mesh;
        }

        protected static T GenerateObj<T>(Mesh mesh, MeshData data) where T : GeneratedObj
        {
            T generatedObj = new GameObject().AddComponent<T>();

            generatedObj.name = data.GenerationData.ObjName;
            generatedObj.MeshData = data;

            MeshRenderer renderer = generatedObj.gameObject.AddComponent<MeshRenderer>();
            renderer.material = data.GenerationData.Material;

            MeshFilter islandMeshFilter = generatedObj.gameObject.AddComponent<MeshFilter>();
            islandMeshFilter.mesh = mesh;

            return generatedObj;
        }
    }
}