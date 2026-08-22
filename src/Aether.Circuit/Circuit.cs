//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// AnnotationChildInfo is null — annotations stay as unadapted DomNodes for this slice.

using System.Collections.Generic;
using System.Linq;

using Sce.Atf.Controls.Adaptable.Graphs;
using Sce.Atf.Dom;

namespace CircuitEditorSample
{
    /// <summary>
    /// Adapts DomNode to a circuit and observable context with change notification events</summary>
    public class Circuit : Sce.Atf.Controls.Adaptable.Graphs.Circuit, IGraph<Module, Connection, ICircuitPin>
    {
        /// <summary>
        /// Performs initialization when the adapter is connected to the circuit's DomNode</summary>
        protected override void OnNodeSet()
        {
            m_modules = new DomNodeListAdapter<Module>(DomNode, Schema.circuitType.moduleChild);
            m_connections = new DomNodeListAdapter<Connection>(DomNode, Schema.circuitType.connectionChild);
            base.OnNodeSet();
        }

        /// <summary>
        /// Gets ChildInfo for Elements (circuit elements with pins) in circuit</summary>
        protected override ChildInfo ElementChildInfo
        {
            get { return Schema.circuitType.moduleChild; }
        }

        /// <summary>
        /// Gets ChildInfo for Wires (connections) in circuit</summary>
        protected override ChildInfo WireChildInfo
        {
            get { return Schema.circuitType.connectionChild; }
        }

        /// <summary>
        /// Gets ChildInfo for annotations (comments) in circuit.
        /// Return null if annotations are not supported.</summary>
        protected override ChildInfo AnnotationChildInfo
        {
            get { return null; }
        }

        #region IGraph Members

        /// <summary>
        /// Gets all visible nodes in the circuit</summary>
        IEnumerable<Module> IGraph<Module, Connection, ICircuitPin>.Nodes
        {
            get
            {
                return m_modules.Where(x => x.Visible);
            }
        }

        /// <summary>
        /// Gets enumeration of all connections between visible nodes in the circuit</summary>
        IEnumerable<Connection> IGraph<Module, Connection, ICircuitPin>.Edges
        {
            get
            {
                return m_connections.Where(x => x.InputElement != null && x.OutputElement != null &&
                    x.InputElement.Visible && x.OutputElement.Visible);
            }
        }

        #endregion

        private DomNodeListAdapter<Module> m_modules;
        private DomNodeListAdapter<Connection> m_connections;
    }
}
