using System;
using UnityEngine;

public static class SRnd
{
    private static uint seed;
    private static uint position;

    const uint bitNoise1 = 0x68E31D14;
    const uint bitNoise2 = 0xB5287A4D;
    const uint bitNoise3 = 0x1B56C4E9;
    
    public static void SetSeed(uint newSeed)
    {
        position = 0;
        seed = newSeed;
    }

    static uint _result; //to avoid allocating memory at each use

    /// <summary>
    /// Returns a random positive int (uint) using the Squirrel3 algorithm
    /// </summary>
    /// <returns></returns>
    public static uint NextUint()
    {
        //gets next position
        _result = Squirrel3(position, seed);
        position += 1;
        return _result;
    }

    static uint _mangled; //this number is used and tortured during the random process, then output to the result

    static uint Squirrel3(uint pos, uint seedUsed)
    {
        _mangled = position;
        _mangled *= bitNoise1;
        _mangled += seedUsed;

        //this operation shuffles even more the number by bit shifting it, then xor ing each binary number
        _mangled ^= _mangled >> 8;
        _mangled += bitNoise2;
        _mangled ^= _mangled << 8; //same torturing
        _mangled *= bitNoise3;
        _mangled ^= _mangled >> 8; //same torturing

        return _mangled;
    }

    /// <summary>
    /// Returns a random int value between the two values specified
    /// </summary>
    /// <param name="min">Minimum INCLUDED</param>
    /// <param name="max">Maximum (NOT INCLUDED !)</param>
    /// <returns></returns>
    public static int RangeInt(int min, int max)
    {
        //if the user switched input numbers, re-switches it back to proper syntax
        if (min > max) (min, max) = (max, min);
        if (min == max) return min;
        return (int)(NextUint() % (uint)(max - min) + min);
    }

    /// <summary>
    /// Returns a random float value between 0 and 1
    /// </summary>
    /// <returns></returns>
    public static float NextFloat()
    {
        return (float)NextUint() / uint.MaxValue;
    }

    /// <summary>
    /// Returns a random float value between the two values specified
    /// </summary>
    /// <param name="min">Minimum INCLUDED</param>
    /// <param name="max">Maximum (NOT INCLUDED !)</param>
    /// <returns></returns>
    public static float RangeFloat(float min, float max)
    {
        return NextFloat() * (max - min) + min;
    }

    /// <summary>
    /// Returns a random bool value
    /// </summary>
    /// <returns></returns>
    public static bool NextBool()
    {
        return NextUint() % 2 == 0;
    }

    /// <summary>
    /// Returns a random bool value with the defined chances of being true
    /// </summary>
    /// <param name="chancesOfSuccess">Chance of success of the process (between 0 and 1)</param>
    /// <returns></returns>
    public static bool RandomBool(float chancesOfSuccess)
    {
        if (chancesOfSuccess > 1) throw new NotImplementedException();
        return NextFloat() <= chancesOfSuccess;
    }

    /// <summary>
    /// Returns a random point inside the defined sphere size
    /// </summary>
    /// <param name="size">The size of the sphere</param>
    /// <returns></returns>
    public static Vector3 GetRandomPointInsideSphere(float size)
    {
        double theta = NextFloat() * 2.0 * Math.PI;
        double phi = Math.Acos(2.0 * NextFloat() - 1.0);
        double r = Math.Cbrt(NextFloat());
        double sinTheta = Math.Sin(theta);
        double cosTheta = Math.Cos(theta);
        double sinPhi = Math.Sin(phi);
        double cosPhi = Math.Cos(phi);
        double x = r * sinPhi * cosTheta;
        double y = r * sinPhi * sinTheta;
        double z = r * cosPhi;
        return new Vector3((float)x, (float)y, (float)z) * size;
    }
}