using System;

namespace ComplexType;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
public class ComplexTypeAttribute : Attribute
{
    public ComplexTypeAttribute() { }

    public ComplexTypeAttribute(EnumAdditionalConverters[] converters = null) => Converters = converters ?? [];

    public EnumAdditionalConverters[] Converters { get; }
}
