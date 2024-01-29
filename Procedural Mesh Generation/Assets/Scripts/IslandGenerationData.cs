using UnityEngine;

[CreateAssetMenu(fileName = "IslandGenerationPreset", menuName = "ScriptableObjects/IslandGenerationPreset", order = 1)]
public class IslandGenerationData : ScriptableObject
{
    public Material material;
    
    [Header("Floors parameters"), SerializeField, Tooltip("minimal number of floors")]
    public int minFloorCount;

    [SerializeField, Tooltip("minimal number of floors")]
    public int maxFloorCount;

    [SerializeField, Space, Tooltip("minimal space between floors")]
    public float minFloorsHeight;

    [SerializeField, Tooltip("maximal space between floors")]
    public float maxFloorsHeight;

    [SerializeField, Space] public float minFloorsRadius;
    [SerializeField] public float maxFloorsRadius;

    [SerializeField, Space, Tooltip("maximal floors offset on x and z axis")]
    public float floorsMaxOffset;

    [SerializeField, Space, Tooltip("maximal vertices offset on all axis")]
    public float verticesMaxOffset;

    [SerializeField, Tooltip("The higher the floors are, the more they grow depending on this value.")]
    public float minUpwardSwelling;

    [SerializeField, Tooltip("The higher the floors are, the more they grow depending on this value.")]
    public float maxUpwardSwelling;

    [SerializeField, Tooltip("Number of vertices per floor")]
    public int meshComplexity;
}