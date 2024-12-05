using UnityEngine;

public class GameManager
{
    public static void InitSeed(uint seed)
    {
        seed = seed == 0 ? (uint)Random.Range(1, 9999999) : seed;
        SRnd.SetSeed(seed);
    }
}