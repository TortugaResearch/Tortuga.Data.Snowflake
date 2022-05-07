#if !NETCOREAPP3_1_OR_GREATER

namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
sealed class AllowNullAttribute : Attribute
{
}

#endif
