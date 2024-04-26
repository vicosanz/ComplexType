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
    /// <param name="AdditionalConverters"> Array of additional converters </param>
    public record Metadata(string Namespace, IReadOnlyList<string> Usings,
        bool AllowNulls, string Name, string NameTyped, string FullName, string Modifiers,
        string InnerType, IReadOnlyList<int> AdditionalConverters)
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
        public IReadOnlyList<int> AdditionalConverters { get; internal set; } = AdditionalConverters;

        internal bool IsPrimitive() => InnerType switch
        {
            "string" => true,
            "bool" => true,
            "byte" => true,
            "DateTime" => true,
            "DateTimeOffset" => true,
            "decimal" => true,
            "double" => true,
            "Guid" => true,
            "short" => true,
            "Ulid" => true,
            "int" => true,
            "long" => true,
            "sbyte" => true,
            "float" => true,
            "TimeSpan" => true,
            "ushort" => true,
            "uint" => true,
            "ulong" => true,
            _ => false
        };
    }

}
