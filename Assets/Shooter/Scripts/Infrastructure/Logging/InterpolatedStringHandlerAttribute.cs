namespace System.Runtime.CompilerServices
{
    // C# 10 finds interpolated string handlers by these attributes, and .NET Standard 2.1 does not have them
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    internal sealed class InterpolatedStringHandlerAttribute : Attribute
    {
    }
}
