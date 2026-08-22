//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Group / template-instance pin walks are out of this slice. ICircuitElementType
// is required on the DomNodeType tag (registered by ModuleCatalog).

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Sce.Atf.Adaptation;
using Sce.Atf.Dom;
using Sce.Atf.Rendering;

namespace Sce.Atf.Controls.Adaptable.Graphs
{
    /// <summary>
    /// Adapts DomNode to circuit element, which is the base circuit element with pins.
    /// It maintains local name and bounds for faster
    /// circuit rendering during editing operations, such as dragging elements and wires.</summary>
    public abstract class Element : DomNodeAdapter, ICircuitElement, IVisible
    {
        /// <summary>
        /// Gets the AttributeInfo for the Id property (and nothing else)</summary>
        protected abstract AttributeInfo NameAttribute { get; }

        /// <summary>
        /// Gets the AttributeInfo for the Name property (and nothing else)</summary>
        protected abstract AttributeInfo LabelAttribute { get; }

        /// <summary>
        /// Gets the AttributeInfo for the Position property (and nothing else)</summary>
        protected abstract AttributeInfo XAttribute { get; }

        /// <summary>
        /// Gets the AttributeInfo for the Position property (and nothing else)</summary>
        protected abstract AttributeInfo YAttribute { get; }

        /// <summary>
        /// Gets the AttributeInfo for the Visible property (and nothing else)</summary>
        protected abstract AttributeInfo VisibleAttribute { get; }

        /// <summary>
        /// Gets the optional AttributeInfo for the original GUID of template
        /// if this module is a copy-instance of a template(and nothing else) </summary>
        protected virtual AttributeInfo SourceGuidAttribute
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the optional AttributeInfo for storing whether or not unconnected
        /// pins should be displayed.</summary>
        protected virtual AttributeInfo ShowUnconnectedPinsAttribute
        {
            get { return null; }
        }

        /// <summary>
        /// Gets or sets the circuit element ID</summary>
        public virtual string Id
        {
            get { return GetAttribute<string>(NameAttribute); }
            set { SetAttribute(NameAttribute, value); }
        }

        /// <summary>
        /// Gets or sets the user-visible name</summary>
        public virtual string Name
        {
            get { return GetAttribute<string>(LabelAttribute); }
            set { SetAttribute(LabelAttribute, value); }
        }

        /// <summary>
        /// Gets or sets the position of the element</summary>
        public virtual Point Position
        {
            get
            {
                return new Point(
                    GetAttribute<int>(XAttribute),
                    GetAttribute<int>(YAttribute));
            }
            set
            {
                SetAttribute(XAttribute, value.X);
                SetAttribute(YAttribute, value.Y);
            }
        }

        /// <summary>
        /// Gets the circuit element type</summary>
        public virtual ICircuitElementType Type
        {
            get
            {
                ICircuitElementType result = null;
                if (DomNode.Is<ICircuitElement>())
                {
                    var circuitElement = DomNode.Cast<ICircuitElement>();
                    if (circuitElement != this)
                        result = circuitElement.Type;
                }

                if (result == null)
                {
                    if (m_elementType == null)
                        m_elementType = DomNode.Type.GetTag<ICircuitElementType>();
                    result = m_elementType;
                }

                if (result == null)
                    throw new InvalidOperationException("ICircuitElementType is not defined on " + DomNode.Type.Name);
                return result;
            }
        }

        /// <summary>
        /// Gets the CircuitElementInfo for this circuit element, which specifies additional options</summary>
        public CircuitElementInfo ElementInfo
        {
            get { return m_elementInfo; }
        }

        /// <summary>
        /// Gets level, or depth of the element </summary>
        public int Level
        {
            get { return DomNode.Ancestry.Count(); }
        }

        /// <summary>
        /// Tests if the element has a given input pin</summary>
        public virtual bool HasInputPin(ICircuitPin pin)
        {
            return Type.Inputs.Contains(pin);
        }

        /// <summary>
        /// Tests if the element has a given output pin.</summary>
        public virtual bool HasOutputPin(ICircuitPin pin)
        {
            return Type.Outputs.Contains(pin);
        }

        /// <summary>
        /// Gets the input pin for the given pin index.</summary>
        public virtual ICircuitPin InputPin(int pinIndex)
        {
            return Type.GetInputPin(pinIndex);
        }

        /// <summary>
        /// Gets the output pin for the given pin index.</summary>
        public virtual ICircuitPin OutputPin(int pinIndex)
        {
            return Type.GetOutputPin(pinIndex);
        }

        /// <summary>
        /// Gets a read-only list of all the input pins for this element.</summary>
        public virtual IEnumerable<ICircuitPin> AllInputPins
        {
            get { return Type.Inputs; }
        }

        /// <summary>
        /// Gets a read-only list of all the output pins for this element.</summary>
        public virtual IEnumerable<ICircuitPin> AllOutputPins
        {
            get { return Type.Outputs; }
        }

        /// <summary>
        /// Finds the element and pin that matched the pin target for this circuit container</summary>
        public virtual Pair<Element, ICircuitPin> MatchPinTarget(PinTarget pinTarget, bool inputSide)
        {
            var result = new Pair<Element, ICircuitPin>();
            if (pinTarget != null && pinTarget.LeafDomNode == DomNode)
            {
                var pin = inputSide ? Type.GetInputPin(pinTarget.LeafPinIndex)
                                        : Type.GetOutputPin(pinTarget.LeafPinIndex);
                if (pin != null)
                {
                    result.First = this;
                    result.Second = pin;
                }
            }
            return result;
        }

        /// <summary>
        /// Finds the element and pin that fully matched the pin target for this circuit container</summary>
        public virtual Pair<Element, ICircuitPin> FullyMatchPinTarget(PinTarget pinTarget, bool inputSide)
        {
            return MatchPinTarget(pinTarget, inputSide);
        }

        #region IVisible Members

        /// <summary>
        /// Gets or sets whether the element is visible</summary>
        public virtual bool Visible
        {
            get
            {
                return VisibleAttribute == null || GetAttribute<bool>(VisibleAttribute);
            }
            set { SetAttribute(VisibleAttribute, value); }
        }

        #endregion

        /// <summary>
        /// Gets or sets the local bounds information, in world coordinates</summary>
        public virtual Rectangle Bounds
        {
            get
            {
                return new Rectangle(Position, m_size);
            }
            set
            {
                SetAttribute(XAttribute, value.X);
                SetAttribute(YAttribute, value.Y);
                m_size = value.Size;
            }
        }

        /// <summary>
        /// Gets or sets original GUID of template if this module is a copy-instance of a template</summary>
        public Guid SourceGuid
        {
            get
            {
                if (SourceGuidAttribute == null)
                    return Guid.Empty;
                var guidValue = DomNode.GetAttribute(SourceGuidAttribute) as string;
                if (string.IsNullOrEmpty(guidValue))
                    return Guid.Empty;
                return new Guid(guidValue);
            }
            set
            {
                if (SourceGuidAttribute != null)
                    DomNode.SetAttribute(SourceGuidAttribute, value.ToString());
            }
        }

        /// <summary>
        /// Convert pin index to display order</summary>
        public virtual int PinDisplayOrder(int pinIndex, bool inputSide)
        {
            return pinIndex;
        }

        /// <summary>
        /// Performs one-time initialization when this adapter's DomNode property is set.</summary>
        protected override void OnNodeSet()
        {
            base.OnNodeSet();

            m_elementInfo = CreateElementInfo();

            if (ShowUnconnectedPinsAttribute != null)
                m_elementInfo.ShowUnconnectedPins = GetAttribute<bool>(ShowUnconnectedPinsAttribute);

            m_elementInfo.PropertyChanged += (sender, args) =>
            {
                if (!m_syncingElementInfo)
                {
                    m_syncingElementInfo = true;
                    try
                    {
                        if (ShowUnconnectedPinsAttribute != null)
                            SetAttribute(ShowUnconnectedPinsAttribute, m_elementInfo.ShowUnconnectedPins);
                    }
                    finally
                    {
                        m_syncingElementInfo = false;
                    }
                }
            };

            DomNode.AttributeChanged += (sender, args) =>
            {
                if (!m_syncingElementInfo && args.DomNode == DomNode)
                {
                    m_syncingElementInfo = true;
                    try
                    {
                        if (ShowUnconnectedPinsAttribute != null &&
                            args.AttributeInfo.Equivalent(ShowUnconnectedPinsAttribute))
                            m_elementInfo.ShowUnconnectedPins = (bool)args.NewValue;
                    }
                    finally
                    {
                        m_syncingElementInfo = false;
                    }
                }
            };
        }

        /// <summary>
        /// Creates the circuit element information object</summary>
        protected virtual CircuitElementInfo CreateElementInfo()
        {
            return new CircuitElementInfo();
        }

        private ICircuitElementType m_elementType;
        private Size m_size;
        private CircuitElementInfo m_elementInfo;
        private bool m_syncingElementInfo;
    }
}
