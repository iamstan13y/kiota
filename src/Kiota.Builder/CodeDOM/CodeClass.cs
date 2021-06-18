﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder
{
    public enum CodeClassKind {
        Custom,
        RequestBuilder,
        Model,
        QueryParameters,
    }
    /// <summary>
    /// CodeClass represents an instance of a Class to be generated in source code
    /// </summary>
    public class CodeClass : CodeBlock, IDocumentedElement, ITypeDefinition
    {
        private string name;

        public CodeClass(CodeElement parent):base(parent)
        {
            StartBlock = new Declaration(this);
            EndBlock = new End(this);
        }
        public CodeClassKind ClassKind { get; set; } = CodeClassKind.Custom;

        public string Description {get; set;}
        /// <summary>
        /// Name of Class
        /// </summary>
        public override string Name
        {
            get => name;
            set
            {
                name = value;
                StartBlock = new Declaration(this) { Name = name };
            }
        }

        public void SetIndexer(CodeIndexer indexer)
        {
            if(indexer == null)
                throw new ArgumentNullException(nameof(indexer));
            if(InnerChildElements.Values.OfType<CodeIndexer>().Any())
                throw new InvalidOperationException("this class already has an indexer, remove it first");
            AddRange(indexer);
        }

        public IEnumerable<CodeProperty> AddProperty(params CodeProperty[] properties)
        {
            if(properties == null || properties.Any(x => x == null))
                throw new ArgumentNullException(nameof(properties));
            if(!properties.Any())
                throw new ArgumentOutOfRangeException(nameof(properties));
            return AddRange(properties);
        }

        public bool ContainsMember(string name)
        {
            return this.InnerChildElements.ContainsKey(name);
        }

        public IEnumerable<CodeMethod> AddMethod(params CodeMethod[] methods)
        {
            if(methods == null || methods.Any(x => x == null))
                throw new ArgumentNullException(nameof(methods));
            if(!methods.Any())
                throw new ArgumentOutOfRangeException(nameof(methods));
            return AddRange(methods);
        }

        public IEnumerable<CodeClass> AddInnerClass(params CodeClass[] codeClasses)
        {
            if(codeClasses == null || codeClasses.Any(x => x == null))
                throw new ArgumentNullException(nameof(codeClasses));
            if(!codeClasses.Any())
                throw new ArgumentOutOfRangeException(nameof(codeClasses));
            return AddRange(codeClasses);
        }
        public CodeClass GetParentClass() {
            if(StartBlock is Declaration declaration)
                return declaration.Inherits?.TypeDefinition as CodeClass;
            else return null;
        }
        
        public CodeClass GetGreatestGrandparent(CodeClass startClassToSkip = null) {
            var parentClass = GetParentClass();
            if(parentClass == null)
                return startClassToSkip != null && startClassToSkip == this ? null : this;
            // we don't want to return the current class if this is the start node in the inheritance tree and doesn't have parent
            else
                return parentClass.GetGreatestGrandparent(startClassToSkip);
        }

        public bool IsOfKind(params CodeClassKind[] kinds) {
            return kinds?.Contains(ClassKind) ?? false;
        }

    public class Declaration : BlockDeclaration
        {
            public Declaration(CodeElement parent):base(parent)
            {
                
            }
            public CodeType Inherits { get; set; }
            public List<CodeType> Implements { get; set; } = new List<CodeType>();
        }

        public class End : BlockEnd
        {
            public End(CodeElement parent):base(parent)
            {
                
            }
        }
    }
}
