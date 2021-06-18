using System.Linq;
using Xunit;

namespace Kiota.Builder.Refiners.Tests {
    public class TypeScriptLanguageRefinerTests {
        private readonly CodeNamespace root;
        private readonly CodeNamespace graphNS;
        private readonly CodeClass parentClass;
        public TypeScriptLanguageRefinerTests() {
            root = CodeNamespace.InitRootNamespace();
            graphNS = root.AddNamespace("graph");
            parentClass = new (graphNS) {
                Name = "parentClass"
            };
            graphNS.AddClass(parentClass);
        }
#region common
        [Fact]
        public void ReplacesImportsSubNamespace() {
            var rootNS = parentClass.Parent as CodeNamespace;
            rootNS.RemoveChildElement(parentClass);
            graphNS.AddClass(parentClass);
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            var subNS = graphNS.AddNamespace($"{graphNS.Name}.messages");
            var messageClassDef = new CodeClass(subNS) {
                Name = "Message",
            };
            declaration.Usings.Add(new (parentClass) {
                Name = "graph",
                Declaration = new(parentClass) {
                    Name = "Message",
                    TypeDefinition = messageClassDef,
                }
            });
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
            Assert.Equal("./messages/message", declaration.Usings.First().Declaration.Name);
        }
        [Fact]
        public void ReplacesImportsParentNamespace() {
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            var subNS = root.AddNamespace("messages");
            var messageClassDef = new CodeClass(subNS) {
                Name = "Message",
            };
            subNS.AddClass(messageClassDef);
            declaration.Usings.Add(new (parentClass) {
                Name = "messages",
                Declaration = new(parentClass) {
                    Name = "Message",
                    TypeDefinition = messageClassDef,
                }
            });
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
            Assert.Equal("../messages/message", declaration.Usings.First().Declaration.Name);
        }
        [Fact]
        public void ReplacesImportsSameNamespace() {
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            var messageClassDef = new CodeClass(graphNS) {
                Name = "Message",
            };
            declaration.Usings.Add(new (parentClass) {
                Name = "graph",
                Declaration = new(parentClass) {
                    Name = "Message",
                    TypeDefinition = messageClassDef,
                }
            });
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
            Assert.Equal("./message", declaration.Usings.First().Declaration.Name);
        }
#endregion
#region typescript
        private const string httpCoreDefaultName = "IHttpCore";
        private const string factoryDefaultName = "ISerializationWriterFactory";
        private const string deserializeDefaultName = "IDictionary<string, Action<Model, IParseNode>>";
        private const string dateTimeOffsetDefaultName = "DateTimeOffset";
        private const string addiationalDataDefaultName = "new Dictionary<string, object>()";
        private const string handlerDefaultName = "IResponseHandler";
        [Fact]
        public void EscapesReservedKeywords() {
            var model = root.AddClass(new CodeClass (root) {
                Name = "break",
                ClassKind = CodeClassKind.Model
            }).First();
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
            Assert.NotEqual("break", model.Name);
            Assert.Contains("escaped", model.Name);
        }
        [Fact]
        public void CorrectsCoreType() {

            var model = root.AddClass(new CodeClass (root) {
                Name = "model",
                ClassKind = CodeClassKind.Model
            }).First();
            model.AddProperty(new (model) {
                Name = "core",
                PropertyKind = CodePropertyKind.HttpCore,
                Type = new CodeType(model) {
                    Name = httpCoreDefaultName
                }
            }, new (model) {
                Name = "serializerFactory",
                PropertyKind = CodePropertyKind.SerializerFactory,
                Type = new CodeType(model) {
                    Name = factoryDefaultName,
                }
            }, new (model) {
                Name = "someDate",
                PropertyKind = CodePropertyKind.Custom,
                Type = new CodeType(model) {
                    Name = dateTimeOffsetDefaultName,
                }
            }, new (model) {
                Name = "additionalData",
                PropertyKind = CodePropertyKind.AdditionalData,
                Type = new CodeType(model) {
                    Name = addiationalDataDefaultName
                }
            });
            var executorMethod = model.AddMethod(new CodeMethod(model) {
                Name = "executor",
                MethodKind = CodeMethodKind.RequestExecutor,
                ReturnType = new CodeType(model) {
                    Name = "string"
                }
            }).First();
            executorMethod.AddParameter(new CodeParameter(executorMethod) {
                Name = "handler",
                ParameterKind = CodeParameterKind.ResponseHandler,
                Type = new CodeType(executorMethod) {
                    Name = handlerDefaultName,
                }
            });
            const string serializerDefaultName = "ISerializationWriter";
            var serializationMethod = model.AddMethod(new CodeMethod(model) {
                Name = "seriailization",
                MethodKind = CodeMethodKind.Serializer,
                ReturnType = new CodeType(model) {
                    Name = "string"
                }
            }).First();
            serializationMethod.AddParameter(new CodeParameter(serializationMethod) {
                Name = "handler",
                ParameterKind = CodeParameterKind.ResponseHandler,
                Type = new CodeType(executorMethod) {
                    Name = serializerDefaultName,
                }
            });
            var responseHandlerMethod = model.AddMethod(new CodeMethod(model) {
                Name = "defaultResponseHandler",
                ReturnType = new CodeType(model) {
                    Name = "string"
                }
            }, new (model) {
                Name = "deserializeFields",
                ReturnType = new CodeType(model) {
                    Name = deserializeDefaultName,
                },
                MethodKind = CodeMethodKind.Deserializer
            }).First();
            const string streamDefaultName = "Stream";
            responseHandlerMethod.AddParameter(new CodeParameter(responseHandlerMethod) {
                Name = "param1",
                Type = new CodeType(responseHandlerMethod) {
                    Name = streamDefaultName
                }
            });
            ILanguageRefiner.Refine(new GenerationConfiguration{ Language = GenerationLanguage.TypeScript }, root);
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => httpCoreDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => factoryDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => dateTimeOffsetDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => addiationalDataDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().Where(x => deserializeDefaultName.Equals(x.ReturnType.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().SelectMany(x => x.Parameters).Where(x => handlerDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().SelectMany(x => x.Parameters).Where(x => serializerDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().SelectMany(x => x.Parameters).Where(x => streamDefaultName.Equals(x.Type.Name)));
        }
    }
#endregion
}
