using UnityEngine;

namespace Island_Generation
{
    [CreateAssetMenu(fileName = "IslandGenerationData", menuName = "ScriptableObjects/IslandGenerationData", order = 1)]
    public class IslandGenerationData : ScriptableObject
    {
        [field: SerializeField] public Material Material { get; private set; }

        [field: Header("Floors parameters"), SerializeField, Tooltip("minimal number of floors")]
        public int MinFloorCount { get; private set; }

        [field: SerializeField, Tooltip("minimal number of floors")]
        public int MaxFloorCount { get; private set; }

        [field: SerializeField, Space, Tooltip("minimal space between floors")]
        public float MinFloorsHeight { get; private set; }

        [field: SerializeField, Tooltip("maximal space between floors")]
        public float MaxFloorsHeight { get; private set; }

        [field: SerializeField, Space] public float MinFloorsRadius { get; private set; }
        [field: SerializeField] public float MaxFloorsRadius { get; private set; }

        [field: SerializeField, Space, Tooltip("maximal floors offset on x and z axis")]
        public float FloorsMaxOffset { get; private set; }

        [field: SerializeField, Space, Tooltip("maximal vertices offset on all axis")]
        public float VerticesMaxOffset { get; private set; }

        [field: SerializeField, Tooltip("The higher the floors are, the more they grow depending on this value.")]
        public float MinUpwardSwelling { get; private set; }

        [field: SerializeField, Tooltip("The higher the floors are, the more they grow depending on this value.")]
        public float MaxUpwardSwelling { get; private set; }

        [field: SerializeField, Tooltip("Number of vertices per floor")]
        public int MeshComplexity { get; private set; }
    }
}