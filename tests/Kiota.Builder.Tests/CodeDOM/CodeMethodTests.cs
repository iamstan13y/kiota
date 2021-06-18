using System;
using System.Linq;
using Xunit;

namespace Kiota.Builder.Tests {
    public class CodeMethodTests {
        [Fact]
        public void Defensive() {
            var root = CodeNamespace.InitRootNamespace();
            var method = new CodeMethod(root) {
                Name = "class",
            };
            Assert.False(method.IsOfKind((CodeMethodKind[])null));
            Assert.False(method.IsOfKind(new CodeMethodKind[] { }));
        }
        [Fact]
        public void IsOfKind() {
            var root = CodeNamespace.InitRootNamespace();
            var method = new CodeMethod(root) {
                Name = "class",
            };
            Assert.False(method.IsOfKind(CodeMethodKind.Constructor));
            method.MethodKind = CodeMethodKind.Deserializer;
            Assert.True(method.IsOfKind(CodeMethodKind.Deserializer));
            Assert.True(method.IsOfKind(CodeMethodKind.Deserializer, CodeMethodKind.Getter));
            Assert.False(method.IsOfKind(CodeMethodKind.Getter));
        }
        [Fact]
        public void AddsParameter() {
            var root = CodeNamespace.InitRootNamespace();
            var method = new CodeMethod(root) {
                Name = "method1"
            };
            Assert.Throws<ArgumentNullException>(() => {
                method.AddParameter((CodeParameter)null);
            });
            Assert.Throws<ArgumentNullException>(() => {
                method.AddParameter(null);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                method.AddParameter(new CodeParameter[] { });
            });
        }
        [Fact]
        public void ClonesParameters() {
            var root = CodeNamespace.InitRootNamespace();
            var method = new CodeMethod(root) {
                Name = "method1"
            };
            method.AddParameter(new CodeParameter(method) {
                Name = "param1"
            });
            var clone = method.Clone() as CodeMethod;
            Assert.Equal(method.Name, clone.Name);
            Assert.Single(method.Parameters);
            Assert.Equal(method.Parameters.First().Name, clone.Parameters.First().Name);
        }
        [Fact]
        public void ParametersExtensionsReturnsValue() {
            var root = CodeNamespace.InitRootNamespace();
            var method = new CodeMethod(root) {
                Name = "method1"
            };
            method.AddParameter(new CodeParameter(method) {
                Name = "param1",
                ParameterKind = CodeParameterKind.Custom,
            });
            Assert.NotNull(method.Parameters.OfKind(CodeParameterKind.Custom));
            Assert.Null(method.Parameters.OfKind(CodeParameterKind.RequestBody));
        }
    }
}
