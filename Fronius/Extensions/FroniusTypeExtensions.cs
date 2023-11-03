using System.Reflection.Emit;

namespace De.Hochstaetter.Fronius.Extensions;

public static class FroniusTypeExtensions
{
    public static int GetSize(this Type type)
    {
        var method = new DynamicMethod("GetSizeImpl", typeof(uint), Type.EmptyTypes, typeof(TypeExtensions), false);

        ILGenerator gen = method.GetILGenerator();

        gen.Emit(OpCodes.Sizeof, type);
        gen.Emit(OpCodes.Ret);

        var func = (Func<uint>)method.CreateDelegate(typeof(Func<uint>));
        return checked((int)func());
    } 
    
}