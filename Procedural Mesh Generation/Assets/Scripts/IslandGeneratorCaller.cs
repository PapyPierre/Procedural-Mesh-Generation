using UnityEngine;

// Example Class
public class IslandGeneratorCaller : MonoBehaviour
{
    [SerializeField] private IslandGenerationData generationData;

    private void Start()
    {
       IslandMeshGenerator.GenerateIsland(Vector3.zero, generationData);
    }
}