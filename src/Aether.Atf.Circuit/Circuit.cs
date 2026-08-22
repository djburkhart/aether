//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// IAnnotatedDiagram / ICircuitContainer / Group / LayerFolder are out of this
// slice. Annotations are optional and ignored when AnnotationChildInfo is null.

using System.Collections.Generic;
using System.Linq;

using Sce.Atf.Dom;

namespace Sce.Atf.Controls.Adaptable.Graphs
{
    /// <summary>
    /// Adapts DomNode to a circuit and observable context with change notification events</summary>
    public abstract class Circuit : DomNodeAdapter, IGraph<Element, Wire, ICircuitPin>
    {
        /// <summary>
        /// Gets ChildInfo for Elements (circuit elements with pins) in circuit</summary>
        protected abstract ChildInfo ElementChildInfo { get; }
        /// <summary>
        /// Gets ChildInfo for Wires (connections) in circuit</summary>
        protected abstract ChildInfo WireChildInfo { get; }

        /// <summary>
        /// Gets ChildInfo for annotations (comments) in circuit.
        /// Return null if annotations are not supported.</summary>
        protected virtual ChildInfo AnnotationChildInfo
        {
            get { return null; }
        }

        /// <summary>
        /// Performs initialization when the adapter is connected to the circuit's DomNode</summary>
        protected override void OnNodeSet()
        {
            m_elements = new DomNodeListAdapter<Element>(DomNode, ElementChildInfo);
            m_wires = new DomNodeListAdapter<Wire>(DomNode, WireChildInfo);

            foreach (var connection in Wires)
                connection.SetPinTarget();

            DomNode.AttributeChanged += DomNode_AttributeChanged;
            DomNode.ChildInserted += DomNode_ChildInserted;
            DomNode.ChildRemoved += DomNode_ChildRemoved;

            base.OnNodeSet();
        }

        /// <summary>
        /// Gets an editable list of all modules in the circuit</summary>
        public IList<Element> Elements
        {
            get { return m_elements; }
        }

        /// <summary>
        /// Gets an editable list of all connections in the circuit</summary>
        public IList<Wire> Wires
        {
            get { return m_wires; }
        }

        /// <summary>
        /// Gets or sets whether the circuit is expanded</summary>
        public bool Expanded
        {
            get { return true; }
            set { }
        }

        /// <summary>
        /// Gets or sets whether or not the contents of the group have been changed</summary>
        public bool Dirty
        {
            get { return m_dirty; }
            set { m_dirty = value; }
        }

        /// <summary>
        /// Synchronize internal data and contents due to editing</summary>
        public void Update()
        {
            Dirty = false;
        }

        /// <summary>
        /// Finds the element and pin that matched the pin target for this circuit container</summary>
        public Pair<Element, ICircuitPin> MatchPinTarget(PinTarget pinTarget, bool inputSide)
        {
            var result = new Pair<Element, ICircuitPin>();

            foreach (var module in Elements)
            {
                result = module.MatchPinTarget(pinTarget, inputSide);
                if (result.First != null)
                    break;
            }

            return result;
        }

        /// <summary>
        /// Finds the element and pin that fully matched the pin target for this circuit container</summary>
        public Pair<Element, ICircuitPin> FullyMatchPinTarget(PinTarget pinTarget, bool inputSide)
        {
            var result = new Pair<Element, ICircuitPin>();

            foreach (var module in Elements)
            {
                result = module.FullyMatchPinTarget(pinTarget, inputSide);
                if (result.First != null)
                    break;
            }

            return result;
        }

        #region IGraph Members

        /// <summary>
        /// Gets all visible nodes in the circuit</summary>
        IEnumerable<Element> IGraph<Element, Wire, ICircuitPin>.Nodes
        {
            get
            {
                return m_elements.Where(x => x.Visible);
            }
        }

        /// <summary>
        /// Gets all connections between visible nodes in the circuit</summary>
        IEnumerable<Wire> IGraph<Element, Wire, ICircuitPin>.Edges
        {
            get
            {
                return m_wires.Where(x => x.InputElement != null && x.OutputElement != null &&
                    x.InputElement.Visible && x.OutputElement.Visible);
            }
        }

        #endregion

        private void DomNode_AttributeChanged(object sender, AttributeEventArgs e)
        {
            Dirty = true;
        }

        private void DomNode_ChildInserted(object sender, ChildEventArgs e)
        {
            Dirty = true;
        }

        private void DomNode_ChildRemoved(object sender, ChildEventArgs e)
        {
            Dirty = true;
        }

        private DomNodeListAdapter<Element> m_elements;
        private DomNodeListAdapter<Wire> m_wires;
        private bool m_dirty;
    }
}
