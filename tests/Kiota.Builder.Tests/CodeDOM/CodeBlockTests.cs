using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Kiota.Builder.Tests {
    public class CodeBlockTests {
        [Fact]
        public void Defensive() {
            var root = CodeNamespace.InitRootNamespace();
            var child = new NeverBlock(root);
            child.AddRange();
            Assert.Empty(child.GetChildElements(true));
        }
        class NeverBlock : CodeBlock
        {
            public void AddRange() {
                base.AddRange((CodeClass[]) null);
            }
            public NeverBlock(CodeElement parent) : base(parent){
            }

            public override string Name
            {
                get => base.Name;
                set => base.Name = value;
            }

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override IEnumerable<CodeElement> GetChildElements(bool innerOnly = false)
            {
                return base.GetChildElements(innerOnly);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override string ToString()
            {
                return base.ToString();
            }
        }
        [Fact]
        public void FindInChildElements() {
            var grandChildName = "child1.grandchild1";
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace("child1");
            var grandChild = child.AddNamespace(grandChildName);
            Assert.NotNull(root.FindChildByName<CodeNamespace>(grandChildName));
            Assert.Null(root.FindChildByName<CodeNamespace>("child2"));
        }
        [Fact]
        public void RemovesElements() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            var elements = child.AddClass(new CodeClass(child) { Name = "class1"},
                            new CodeClass(child) { Name = "class2"});
            child.RemoveChildElement(elements.First());
            Assert.Single(child.GetChildElements(true));

            child.RemoveChildElement<CodeClass>(null); // doesn't fail when passing null collection
        }
        [Fact]
        public void AddsUsing() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            child.AddUsing(new CodeUsing(child) {
                Name = "someNS"
            });
            Assert.Single(child.StartBlock.Usings);
        }
        [Fact]
        public void ThrowsWhenInsertingDuplicatedElements() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            Assert.Throws<InvalidOperationException>(() => {
                child.AddClass(new CodeClass(child) {
                    Name = "class1"
                });
                child.AddEnum(new CodeEnum(child) {
                    Name = "class1"
                });
            });
        }
        [Fact]
        public void DoesntThrowWhenAddingOverloads() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            var codeClass = child.AddClass(new CodeClass(child) {
                Name = "class1"
            }).First();
            var method = new CodeMethod(codeClass) {
                Name = "method",
                MethodKind = CodeMethodKind.RequestExecutor,
            };
            var overload = method.Clone() as CodeMethod;
            overload.Parameters.Add(new CodeParameter(overload) {
                Name = "param1"
            });
            codeClass.AddMethod(method, overload);
        }
        [Fact]
        public void DoesntThrowWhenAddingIndexersWithPropName() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            var codeClass = child.AddClass(new CodeClass(child) {
                Name = "class1"
            }).First();
            var property = new CodeProperty(codeClass) {
                Name = "method",
                PropertyKind = CodePropertyKind.RequestBuilder,
            };
            var indexer = new CodeMethod(child) {
                Name = "method",
                MethodKind = CodeMethodKind.IndexerBackwardCompatibility
            };
            codeClass.AddProperty(property);
            codeClass.AddMethod(indexer);
        }
        [Fact]
        public void FindChildByNameThrowsOnEmptyNames() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            Assert.Throws<ArgumentNullException>(() => {
                child.FindChildByName<CodeClass>(string.Empty);
            });
            Assert.Throws<ArgumentNullException>(() => {
                child.FindChildrenByName<CodeClass>(string.Empty);
            });
        }
        [Fact]
        public void FindsChildByNameInSubnamespace() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            var className = "class1";
            var class1 = child.AddClass(new CodeClass(child) {
                Name = className
            }).First();
            Assert.Equal(class1, child.FindChildByName<CodeClass>(className));
            Assert.Null(child.FindChildByName<CodeClass>("class2"));
            Assert.Null(child.FindChildByName<CodeEnum>(className));
        }
        [Fact]
        public void FindsChildrenByName() {
            var root = CodeNamespace.InitRootNamespace();
            var child = root.AddNamespace(CodeNamespaceTests.childName);
            var className = "class1";
            child.AddClass(new CodeClass(child) {
                Name = className
            });
            var subchild = child.AddNamespace($"{child.Name}.four");
            subchild.AddClass(new CodeClass(subchild) {
                Name = className
            });
            Assert.Equal(2, root.FindChildrenByName<CodeClass>(className).Count());
        }
    }
}
