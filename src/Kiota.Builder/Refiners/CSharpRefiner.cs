﻿using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners {
    public class CSharpRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        public CSharpRefiner(GenerationConfiguration configuration) : base(configuration) {}
        public override void Refine(CodeNamespace generatedCode)
        {
            AddDefaultImports(generatedCode);
            MoveClassesWithNamespaceNamesUnderNamespace(generatedCode);
            ConvertUnionTypesToWrapper(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, false, false, false);
            AddAsyncSuffix(generatedCode);
            AddInnerClasses(generatedCode);
            AddParsableInheritanceForModelClasses(generatedCode);
            CapitalizeNamespacesFirstLetters(generatedCode);
            ReplaceBinaryByNativeType(generatedCode, "Stream", "System.IO");
            MakeEnumPropertiesNullable(generatedCode);
            ReplaceReservedNames(generatedCode, new CSharpReservedNamesProvider(), x => $"@{x.ToFirstCharacterUpperCase()}");
            DisambiguatePropertiesWithClassNames(generatedCode);
            AddConstructorsForDefaultValues(generatedCode, false);
        }
        private static void DisambiguatePropertiesWithClassNames(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass) {
                var sameNameProperty = currentClass
                                                .GetChildElements(true)
                                                .OfType<CodeProperty>()
                                                .FirstOrDefault(x => x.Name.Equals(currentClass.Name));
                if(sameNameProperty != null) {
                    currentClass.RemoveChildElement(sameNameProperty);
                    sameNameProperty.SerializationName = sameNameProperty.SerializationName ?? sameNameProperty.Name;
                    sameNameProperty.Name = $"{sameNameProperty.Name}_prop";
                    currentClass.AddProperty(sameNameProperty);
                }
            }
            CrawlTree(currentElement, DisambiguatePropertiesWithClassNames);
        }
        private static void MakeEnumPropertiesNullable(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model))
                currentClass.GetChildElements(true)
                            .OfType<CodeProperty>()
                            .Where(x => x.Type is CodeType propType && propType.TypeDefinition is CodeEnum)
                            .ToList()
                            .ForEach(x => x.Type.IsNullable = true);
            CrawlTree(currentElement, MakeEnumPropertiesNullable);
        }
        private static void AddParsableInheritanceForModelClasses(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model)) {
                var declaration = currentClass.StartBlock as CodeClass.Declaration;
                declaration.Implements.Add(new CodeType(currentClass) {
                    IsExternal = true,
                    Name = $"IParsable",
                });
                declaration.Usings.Add(new CodeUsing(currentClass) {
                    Name = "Microsoft.Kiota.Abstractions.Serialization"
                });
            }
            CrawlTree(currentElement, AddParsableInheritanceForModelClasses);
        }
        private static readonly string[] defaultNamespacesForClasses = new string[] {"System", "System.Collections.Generic", "System.Linq"};
        private static readonly string[] defaultNamespacesForRequestBuilders = new string[] { "System.Threading.Tasks", "System.IO", "Microsoft.Kiota.Abstractions", "Microsoft.Kiota.Abstractions.Serialization"};
        private static void AddDefaultImports(CodeElement current) {
            if(current is CodeClass currentClass) {
                currentClass.AddUsing(defaultNamespacesForClasses.Select(x => new CodeUsing(currentClass) { Name = x }).ToArray());
                if(currentClass.IsOfKind(CodeClassKind.RequestBuilder))
                    currentClass.AddUsing(defaultNamespacesForRequestBuilders.Select(x => new CodeUsing(currentClass) { Name = x }).ToArray());
            }
            CrawlTree(current, AddDefaultImports);
        }
        private static void CapitalizeNamespacesFirstLetters(CodeElement current) {
            if(current is CodeNamespace currentNamespace)
                currentNamespace.Name = currentNamespace.Name?.Split('.')?.Select(x => x.ToFirstCharacterUpperCase())?.Aggregate((x, y) => $"{x}.{y}");
            CrawlTree(current, CapitalizeNamespacesFirstLetters);
        }
        private static void AddAsyncSuffix(CodeElement currentElement) {
            if(currentElement is CodeMethod currentMethod && currentMethod.IsAsync)
                currentMethod.Name += "Async";
            CrawlTree(currentElement, AddAsyncSuffix);
        }
    }
}
