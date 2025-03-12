using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace ComplexType.Generator
{
    /// <param name="Namespace"> The namespace found in the base struct </param>
    /// <param name="Usings"> Usings of the base struct </param>
    /// <param name="AllowNulls"> Struct allow nulls </param>
    /// <param name="Name"> The short name of the base struct </param>
    /// <param name="NameTyped"> The shot name with typed parameters e.g. <T0, T1>. </param>
    /// <param name="FullName"> The full name of the base struct </param>
    /// <param name="Modifiers"> All modifiers of the base struct e.g. public readonly </param>
    /// <param name="InnerType"> Inner type </param>
    /// <param name="BaseInnerType"> Base Inner type </param>
    /// <param name="AdditionalConverters"> Array of additional converters </param>
    public record Metadata(string Namespace, IReadOnlyList<string> Usings,
        bool AllowNulls, string Name, string NameTyped, string FullName, string Modifiers,
        string InnerType, string? BaseInnerType, IReadOnlyList<int> AdditionalConverters, bool ValidateExist)
    {
        /// <summary>
        /// The namespace found in the base struct
        /// </summary>
        public string Namespace { get; internal set; } = Namespace;

        /// <summary>
        /// Usings of the base struct
        /// </summary>
        public IReadOnlyList<string> Usings { get; internal set; } = Usings;

        /// <summary>
        /// Struct allow nulls
        /// </summary>
        public bool AllowNulls { get; internal set; } = AllowNulls;

        /// <summary>
        /// The short name of the base struct
        /// </summary>
        public string Name { get; internal set; } = Name;

        /// <summary>
        /// The shot name with typed parameters e.g. <T0, T1>.
        /// </summary>
        public string NameTyped { get; internal set; } = NameTyped;

        /// <summary>
        /// The full name of the base struct
        /// </summary>
        public string FullName { get; internal set; } = FullName;

        /// <summary>
        /// All modifiers of the base struct e.g. public readonly
        /// </summary>
        public string Modifiers { get; internal set; } = Modifiers;

        /// <summary>
        /// All types of the ComplexTypes configured
        /// </summary>
        public string InnerType { get; internal set; } = InnerType;

        /// <summary>
        /// All base types of the ComplexTypes configured
        /// </summary>
        public string? BaseInnerType { get; internal set; } = BaseInnerType;

        public IReadOnlyList<int> AdditionalConverters { get; internal set; } = AdditionalConverters;

        internal string GetBaseInnerType() => BaseInnerType ?? InnerType;
        internal bool IsInnerTypePrimitiveOrId() => InnerType switch
        {
            "string" or "bool" or "byte" or "DateTime" or "DateTimeOffset" or
            "decimal" or "double" or "Guid" or "short" or "Ulid" or "int" or "sbyte" or "float" or
            "TimeSpan" or "ushort" or "uint" or "char" or "long" or "ulong" => true,
            _ => false
        };
        internal bool IsBaseInnerTypePrimitiveOrId() => GetBaseInnerType() switch
        {
            "string" or "bool" or "byte" or "DateTime" or "DateTimeOffset" or
            "decimal" or "double" or "Guid" or "short" or "Ulid" or "int" or "sbyte" or "float" or
            "TimeSpan" or "ushort" or "uint" or "char" or "long" or "ulong" => true,
            _ => false
        };
        public bool ValidateExist { get; internal set; } = ValidateExist;
    }

}
