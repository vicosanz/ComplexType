using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ComplexType.Generator
{
    [Generator]
    public class ComplexTypeGenerator : IIncrementalGenerator
    {
        private static readonly string complexTypeAttribute = "ComplexType.ComplexTypeAttribute";
        private static readonly string iComplexType = "IComplexType";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
//#if DEBUG
//            if (!Debugger.IsAttached) Debugger.Launch();
//#endif
            IncrementalValuesProvider<TypeDeclarationSyntax> typeDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => s.IsSyntaxTargetForGeneration(),
                    transform: static (ctx, _) => ctx.GetSemanticTargetForGeneration(complexTypeAttribute))
                .Where(static m => m is not null)!;

            IncrementalValueProvider<(Compilation, ImmutableArray<TypeDeclarationSyntax>)> compilationAndEnums
                = context.CompilationProvider.Combine(typeDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndEnums,
                static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private static void Execute(Compilation compilation, ImmutableArray<TypeDeclarationSyntax> type, SourceProductionContext context)
        {
            if (type.IsDefaultOrEmpty) return;

            var complexTypes = GetComplexTypes(compilation, type.Distinct(), context);

            if (complexTypes.Any())
            {
                foreach (var complexTyped in complexTypes)
                {
                    var generator = new ComplexTypeWriter(complexTyped);
                    context.AddSource(complexTyped.GetFileNameGenerated(),
                                      SourceText.From(generator.GetCode(), Encoding.UTF8));
                }
            }
        }

        protected static List<Metadata> GetComplexTypes(Compilation compilation,
            IEnumerable<TypeDeclarationSyntax> types, SourceProductionContext context)
        {
            var complexTypes = new List<Metadata>();
            foreach (var type in types)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                SemanticModel semanticModel = compilation.GetSemanticModel(type.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(type) is not INamedTypeSymbol typeSymbol)
                {
                    // report diagnostic, something went wrong
                    continue;
                }

                var innerType = "string"; 
                string? baseInnerType = null;

                bool allowNulls = false;
                string modifiers = type.GetModifiers();
                var additionalConverters = new List<int>();

                if (!modifiers.Contains("partial") || !modifiers.Contains("readonly") || !type.IsKind(SyntaxKind.RecordStructDeclaration))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(DiagnosticDescriptors.StructNotPartial, null, typeSymbol.ToString())
                    );
                    continue;
                }

                if (type.BaseList != null)
                {
                    foreach (var baseType in type.BaseList!.Types)
                    {
                        if (baseType.ToFullString().Contains(iComplexType))
                        {
                            var argumentsType = (GenericNameSyntax)baseType.Type;
                            innerType = argumentsType.TypeArgumentList.Arguments.First().ToFullString();
                            if (argumentsType.TypeArgumentList.Arguments.Count > 1)
                            {
                                baseInnerType = argumentsType.TypeArgumentList.Arguments[1].ToFullString();
                            }
                        }
                    }
                }

                if (baseInnerType == null && innerType != "string")
                {
                    baseInnerType = "string";
                }

                bool ValidateExist = false;
                foreach (var member in type.Members)
                {
                    if (member.IsKind(SyntaxKind.MethodDeclaration))
                    {
                        var method = (MethodDeclarationSyntax)member;
                        if (method.Identifier.Value?.ToString() == "Validate")
                        {
                            ValidateExist = true;
                            if (!method.Modifiers.ToString().Contains("static"))
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(DiagnosticDescriptors.ValidateMethodNotStatic, null, innerType)
                                );
                                continue;
                            }
                        }
                    }   
                }

                foreach (var attribute in typeSymbol.GetAttributes())
                {
                    if (attribute.AttributeClass!.ToDisplayString().Equals(complexTypeAttribute, StringComparison.OrdinalIgnoreCase))
                    {
                        if (attribute.ConstructorArguments.Any())
                        {
                            var argument = attribute.ConstructorArguments.First();
                            if (!argument.IsNull)
                            {
                                additionalConverters.AddRange(argument.Values.Select(x => int.Parse(x.Value!.ToString())));
                            }
                            
                        }
                    }
                }
                complexTypes.Add(
                    new Metadata(type.GetNamespace(),
                                        type.GetUsings(),
                                        allowNulls,
                                        typeSymbol.Name,
                                        typeSymbol.GetNameTyped(),
                                        typeSymbol.ToString(),
                                        modifiers,
                                        innerType,
                                        baseInnerType,
                                        additionalConverters,
                                        ValidateExist));
            }
            return complexTypes;
        }

    }
}
