using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Factorial{
    static Dictionary<uint,uint> factorials = new Dictionary<uint, uint>();

    public static uint CalculateFactorial ( uint number ) {
        if (number <= 0) return 1;
        if (number == 1) return 1;

        if (factorials.ContainsKey(number))
            return factorials[number];

        uint value = number * CalculateFactorial(number - 1);
        factorials[number] = value;
        return value;
    } 

}
