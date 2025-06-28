using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Extensions;

public static class ArrayExtensions
{
    public static T[] Xor<T>(this T[] first, T[] second) where T : IBinaryNumber<T>
    {
        var maxLength = Math.Max(first.Length, second.Length);
        var result = new T[maxLength];
        Array.Copy(first.Length > second.Length ? first : second, result, maxLength);

        for (var i = 0; i < Math.Min(first.Length, second.Length); i++)
        {
            result[i] = first[i] ^ second[i];
        }

        return result;
    }
    
    public static unsafe byte[] Xor(this byte[] first, long second)
    {
        if (first.Length < sizeof(long))
        {
            throw new ArgumentException($@"Array must be at least {sizeof(long)} bytes", nameof(first));
        }
        
        var result= new byte[first.Length];
        Array.Copy(first, result, first.Length);

        fixed (byte* f = result)
        {
            *(long*)f ^= second;
        }

        return result;
    }
}