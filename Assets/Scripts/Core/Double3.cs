
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace SW.Core
{
    [Serializable]
    public struct Double3
    {
        public double x;
        public double y;
        public double z;


        public Double3(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3((float)this.x, (float)this.y, (float)this.z);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double3 operator +(Double3 a, Double3 b) => new Double3(a.x + b.x, a.y + b.y, a.z + b.z);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double3 operator -(Double3 a, Double3 b) => new Double3(a.x - b.x, a.y - b.y, a.z - b.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double3 operator -(Double3 a) => new Double3(-a.x, -a.y, -a.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double3 operator *(Double3 a, double d) => new Double3(a.x * d, a.y * d, a.z * d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double3 operator *(double d, Double3 a) => new Double3(a.x * d, a.y * d, a.z * d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double3 operator /(Double3 a, double d) => new Double3(a.x / d, a.y / d, a.z / d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Double3 lhs, Double3 rhs)
        {
            double num1 = lhs.x - rhs.x;
            double num2 = lhs.y - rhs.y;
            double num3 = lhs.z - rhs.z;
            return num1 * num1 + num2 * num2 + num3 * num3 < 9.999999439624929E-11;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Double3 lhs, Double3 rhs) => !(lhs == rhs);
    }
}