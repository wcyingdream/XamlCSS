﻿using System;
using System.Collections.Generic;
using System.Linq;
using XamlCSS.Utils;

namespace XamlCSS.Dom
{
    public abstract class DomElementBase<TDependencyObject, TDependencyProperty> : IDomElement<TDependencyObject>
        where TDependencyObject : class
        where TDependencyProperty : class
    {
        private const string Undefined = "UNDEFINED";

        private static readonly CachedSelectorProvider cachedSelectorProvider = new CachedSelectorProvider();

        public IList<StyleSheet> XamlCssStyleSheets { get; protected set; } = new List<StyleSheet>();

        protected ITreeNodeProvider<TDependencyObject> treeNodeProvider;
        protected readonly static char[] classSplitter = { ' ' };
        protected readonly TDependencyObject dependencyObject;
        protected string id;
        protected string assemblyQualifiedNamespaceName = Undefined;
        protected IList<IDomElement<TDependencyObject>> childNodes = null;
        protected IList<IDomElement<TDependencyObject>> logicalChildNodes = null;
        protected IDictionary<string, TDependencyProperty> attributes = null;
        protected IList<string> classList = null;

        public DomElementBase(
            TDependencyObject dependencyObject,
            IDomElement<TDependencyObject> parent,
            IDomElement<TDependencyObject> logicalParent,
            ITreeNodeProvider<TDependencyObject> treeNodeProvider
            )
        {
            if (dependencyObject == null)
                throw new ArgumentNullException(nameof(dependencyObject));

            this.dependencyObject = dependencyObject;
            this.parent = parent;
            this.logicalParent = logicalParent;
            this.treeNodeProvider = treeNodeProvider;

            this.id = GetId(dependencyObject);
            this.TagName = dependencyObject.GetType().Name;
            this.classList = GetClassList(dependencyObject);
            this.assemblyQualifiedNamespaceName = GetAssemblyQualifiedNamespaceName(dependencyObject.GetType());
        }

        protected void ElementAdded(object sender, EventArgs e)
        {
            if (sender == Element)
            {
                // force reevaluation
                parent = treeNodeProvider.GetDomElement(treeNodeProvider.GetParent(Element, SelectorType.VisualTree));
                logicalParent = treeNodeProvider.GetDomElement(treeNodeProvider.GetParent(Element, SelectorType.LogicalTree));
            }

            if (treeNodeProvider.GetParent(sender as TDependencyObject, SelectorType.VisualTree) == dependencyObject)
            {
                if (childNodes != null)
                {
                    var node = treeNodeProvider.GetDomElement(sender as TDependencyObject);
                    if (childNodes.Any(x => x.Element == sender))
                    {

                    }
                    else
                    {
                        childNodes.Add(node);
                    }
                }
            }

            if (treeNodeProvider.GetParent(sender as TDependencyObject, SelectorType.LogicalTree) == dependencyObject)
            {
                if (logicalChildNodes != null)
                {
                    var node = treeNodeProvider.GetDomElement(sender as TDependencyObject);
                    if (logicalChildNodes.Any(x => x.Element == sender))
                    {

                    }
                    else
                    {
                        logicalChildNodes.Add(node);
                    }
                }
            }
        }

        protected void ElementRemoved(object sender, EventArgs e)
        {
            if (ChildNodes.Any(x => x.Element == sender))
            {
                if (childNodes != null)
                {
                    var node = childNodes.First(x => x.Element == sender as TDependencyObject);
                    childNodes.Remove(node);
                }
            }

            if (LogicalChildNodes.Any(x => x.Element == sender))
            {
                if (logicalChildNodes != null)
                {
                    var node = logicalChildNodes.First(x => x.Element == sender as TDependencyObject);
                    logicalChildNodes.Remove(node);
                }
            }

            if (sender == Element)
            {
                parent = null;
                logicalParent = null;
            }
        }

        //protected void ResetChildren()
        //{
        //    this.childNodes = null;
        //}

        public void ResetClassList()
        {
            classList = null;
        }

        public static string GetAssemblyQualifiedNamespaceName(Type type)
        {
            return type.AssemblyQualifiedName.Replace($".{type.Name},", ",");
        }

        public TDependencyObject Element { get { return dependencyObject; } }

        abstract protected IDictionary<string, TDependencyProperty> CreateNamedNodeMap(TDependencyObject dependencyObject);

        // abstract protected INodeList CreateNodeList(IEnumerable<INode> nodes);

        protected virtual IList<IDomElement<TDependencyObject>> GetChildNodes(SelectorType type)
        {
            return "GetChildNodes".Measure(() => treeNodeProvider
                .GetChildren(dependencyObject, type)
                .Select(x => treeNodeProvider.GetDomElement(x))
                .ToList());
        }

        abstract protected IList<string> GetClassList(TDependencyObject dependencyObject);

        abstract protected string GetId(TDependencyObject dependencyObject);

        public IDictionary<string, TDependencyProperty> Attributes => attributes ?? (attributes = CreateNamedNodeMap(dependencyObject));

        public IList<IDomElement<TDependencyObject>> ChildNodes => childNodes ?? (childNodes = GetChildNodes(SelectorType.VisualTree));
        public IList<IDomElement<TDependencyObject>> LogicalChildNodes => logicalChildNodes ?? (logicalChildNodes = GetChildNodes(SelectorType.LogicalTree));

        public IList<string> ClassList => classList ?? (classList = GetClassList(dependencyObject));

        public bool HasChildNodes { get { return ChildNodes.Any(); } }
        public bool HasLogicalChildNodes { get { return LogicalChildNodes.Any(); } }

        public string Id { get { return id; } set { id = value; } }

        public bool IsFocused { get { return false; } }

        public string TagName { get; protected set; }

        public IDomElement<TDependencyObject> Owner
        {
            get
            {
                IDomElement<TDependencyObject> currentNode = this;
                while (true)
                {
                    if (currentNode.Parent == null)
                    {
                        break;
                    }

                    currentNode = currentNode.Parent;
                }

                return currentNode;
            }
        }

        protected IDomElement<TDependencyObject> parent = null;
        protected IDomElement<TDependencyObject> logicalParent;

        public virtual IDomElement<TDependencyObject> Parent
        {
            get
            {
                if (parent != null)
                {
                    return parent;
                }
                return null;
            }
        }

        public virtual IDomElement<TDependencyObject> LogicalParent
        {
            get
            {
                if (logicalParent != null)
                {
                    return logicalParent;
                }
                return null;
            }
        }

        public string AssemblyQualifiedNamespaceName
        {
            get
            {
                return assemblyQualifiedNamespaceName;
            }
        }

        public StyleUpdateInfo StyleInfo { get; set; }

        public bool Contains(IDomElement<TDependencyObject> otherNode, SelectorType type)
        {
            return type == SelectorType.VisualTree ? ChildNodes.Contains(otherNode) : LogicalChildNodes.Contains(otherNode);
        }

        public bool Equals(IDomElement<TDependencyObject> otherNode)
        {
            if (otherNode == null)
            {
                return false;
            }

            if (object.ReferenceEquals(this, otherNode))
            {
                return true;
            }

            return dependencyObject == otherNode.Element;
        }

        public bool HasAttribute(string name)
        {
            return Attributes.ContainsKey(name);
        }

        //public string LookupNamespaceUri(string prefix)
        //{
        //    return namespaceProvider.LookupNamespaceUri(this, prefix);
        //}

        //public string LookupPrefix(string namespaceUri)
        //{
        //    return namespaceProvider.LookupPrefix(this, namespaceUri);
        //}

        public MatchResult Matches(StyleSheet styleSheet, ISelector selector)
        {
            return selector.Match(styleSheet, this);
        }

        public IDomElement<TDependencyObject> QuerySelector(StyleSheet styleSheet, ISelector selector, SelectorType type)
        {
            // var selector = cachedSelectorProvider.GetOrAdd(selectors);

            return (type == SelectorType.LogicalTree ? LogicalChildNodes : ChildNodes).QuerySelector(styleSheet, selector, type);
        }

        public IDomElement<TDependencyObject> QuerySelectorWithSelf(StyleSheet styleSheet, ISelector selector, SelectorType type)
        {
            var match = Matches(styleSheet, selector);
            if (match.IsSuccess)
            {
                return this;
            }
            else if (match.HasGeneralParentFailed)
            {
                // return null;
            }

            //var selector = cachedSelectorProvider.GetOrAdd(selectors);

            return (type == SelectorType.LogicalTree ? LogicalChildNodes : ChildNodes).QuerySelector(styleSheet, selector, type);
        }

        public IList<IDomElement<TDependencyObject>> QuerySelectorAll(StyleSheet styleSheet, ISelector selector, SelectorType type)
        {
            // var selector = cachedSelectorProvider.GetOrAdd(selectors);

            return LogicalChildNodes.QuerySelectorAll(styleSheet, selector, type);
        }

        public IList<IDomElement<TDependencyObject>> QuerySelectorAllWithSelf(StyleSheet styleSheet, ISelector selector, SelectorType type)
        {
            // var selector = cachedSelectorProvider.GetOrAdd(selectors);
            var check = "StyleInfo check".Measure(() =>
            {
                if (!ReferenceEquals(StyleInfo.CurrentStyleSheet, styleSheet) ||
                    StyleInfo.DoMatchCheck == SelectorType.None)
                {
                    return new List<IDomElement<TDependencyObject>>();
                }
                return null;
            });
            if (check != null)
                return check;

            var res = "ROOT QuerySelectorAll".Measure(() => (type == SelectorType.LogicalTree ? LogicalChildNodes : ChildNodes).QuerySelectorAll(styleSheet, selector, type));

            "match this".Measure(() =>
            {
                var match = Matches(styleSheet, selector);
                if (match.IsSuccess)
                {
                    res.Add(this);
                }
            });

            return res;
        }

        public bool HasFocus()
        {
            return false;
        }


        public override int GetHashCode()
        {
            return dependencyObject.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as DomElementBase<TDependencyObject, TDependencyProperty>;
            if (other == null)
                return false;
            return this.dependencyObject == other.dependencyObject;
        }

        public static bool operator ==(DomElementBase<TDependencyObject, TDependencyProperty> a, DomElementBase<TDependencyObject, TDependencyProperty> b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }
            return a?.Equals(b) == true;
        }
        public static bool operator !=(DomElementBase<TDependencyObject, TDependencyProperty> a, DomElementBase<TDependencyObject, TDependencyProperty> b)
        {
            return !(a == b);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DomElementBase() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}
