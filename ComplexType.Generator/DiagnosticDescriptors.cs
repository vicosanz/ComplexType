using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace ComplexType.Generator
{
    public static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor StructNotPartial =
            new(
                "CTI001",
                "Struct must be declared as 'readonly partial record struct'",
                "Struct {0} must be declared as 'readonly partial record struct'",
                DiagnosticCategories.ComplexType,
                DiagnosticSeverity.Warning,
                true
            );
        public static readonly DiagnosticDescriptor ValidateMethodNotStatic =
            new(
                "CTI002",
                "Validate method must be declared as 'public static {0} Validate'",
                "Validate method must be declared as 'public static {0} Validate'",
                DiagnosticCategories.ComplexType,
                DiagnosticSeverity.Warning,
                true
            );
    }
}
