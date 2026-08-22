//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// SetPinTarget only binds leaf modules. GroupPin and template IReference
// walks are out of this slice.

using Sce.Atf.Dom;

namespace Sce.Atf.Controls.Adaptable.Graphs
{
    /// <summary>
    /// Adapts DomNode to connection in a circuit</summary>
    public abstract class Wire : DomNodeAdapter, IGraphEdge<Element, ICircuitPin>
    {
        /// <summary>
        /// Gets label attribute on connection</summary>
        protected abstract AttributeInfo LabelAttribute { get; }
        /// <summary>
        /// Gets input module attribute for connection</summary>
        protected abstract AttributeInfo InputElementAttribute { get; }
        /// <summary>
        /// Gets output module attribute for connection</summary>
        protected abstract AttributeInfo OutputElementAttribute { get; }
        /// <summary>
        /// Gets input pin attribute for connection</summary>
        protected abstract AttributeInfo InputPinAttribute { get; }
        /// <summary>
        /// Gets output pin attribute for connection</summary>
        protected abstract AttributeInfo OutputPinAttribute { get; }

        /// <summary>
        /// Gets or sets the element whose output pin this wire connects to</summary>
        public virtual Element OutputElement
        {
            get { return GetReference<Element>(OutputElementAttribute); }
            set { SetReference(OutputElementAttribute, value); }
        }

        /// <summary>
        /// Gets or sets the output pin, i.e., pin on element that receives connection as output</summary>
        public virtual ICircuitPin OutputPin
        {
            get
            {
                int pinIndex = GetAttribute<int>(OutputPinAttribute);
                return OutputElement != null ? OutputElement.OutputPin(pinIndex) : null;
            }
            set
            {
                DomNode.SetAttribute(OutputPinAttribute, value.Index);
            }
        }

        /// <summary>
        /// Gets or sets the element whose input pin this wire connects to</summary>
        public virtual Element InputElement
        {
            get { return GetReference<Element>(InputElementAttribute); }
            set { SetReference(InputElementAttribute, value); }
        }

        /// <summary>
        /// Gets or sets input pin, i.e., pin on element that receives connection as input</summary>
        public virtual ICircuitPin InputPin
        {
            get
            {
                int pinIndex = GetAttribute<int>(InputPinAttribute);
                return InputElement != null ? InputElement.InputPin(pinIndex) : null;
            }
            set
            {
                DomNode.SetAttribute(InputPinAttribute, value.Index);
            }
        }

        /// <summary>
        /// Sets output pin for an element</summary>
        public virtual void SetOutput(Element outputElement, ICircuitPin outputPin)
        {
            OutputElement = outputElement;
            OutputPin = outputPin;
        }

        /// <summary>
        /// Sets input pin for an element</summary>
        public virtual void SetInput(Element inputElement, ICircuitPin inputPin)
        {
            InputElement = inputElement;
            InputPin = inputPin;
        }

        /// <summary>
        /// Gets or sets label on connection</summary>
        public virtual string Label
        {
            get { return GetAttribute<string>(LabelAttribute); }
            set { SetAttribute(LabelAttribute, value); }
        }

        #region IGraphEdge Members

        /// <summary>
        /// Gets edge's source node</summary>
        Element IGraphEdge<Element>.FromNode
        {
            get { return OutputElement; }
        }

        /// <summary>
        /// Gets the route taken from the source node</summary>
        ICircuitPin IGraphEdge<Element, ICircuitPin>.FromRoute
        {
            get { return OutputPin; }
        }

        /// <summary>
        /// Gets edge's destination node</summary>
        Element IGraphEdge<Element>.ToNode
        {
            get { return InputElement; }
        }

        /// <summary>
        /// Gets the route taken to the destination node</summary>
        ICircuitPin IGraphEdge<Element, ICircuitPin>.ToRoute
        {
            get { return InputPin; }
        }

        /// <summary>
        /// Gets edge's label</summary>
        string IGraphEdge<Element>.Label
        {
            get { return Label; }
        }

        #endregion

        /// <summary>
        /// Sets input and output PinTarget for this connection</summary>
        public void SetPinTarget()
        {
            if (InputPin != null && InputElement != null)
                InputPinTarget = new PinTarget(InputElement.DomNode, InputPin.Index, null);

            if (OutputPin != null && OutputElement != null)
                OutputPinTarget = new PinTarget(OutputElement.DomNode, OutputPin.Index, null);
        }

        /// <summary>
        /// Gets or sets the input pin target</summary>
        public PinTarget InputPinTarget
        {
            get
            {
                if (m_inputPinTarget == null)
                    SetPinTarget();

                return m_inputPinTarget;
            }
            set { m_inputPinTarget = value; }
        }

        /// <summary>
        /// Gets or sets the output pin target</summary>
        public PinTarget OutputPinTarget
        {
            get
            {
                if (m_outputPinTarget == null)
                    SetPinTarget();
                return m_outputPinTarget;
            }
            set { m_outputPinTarget = value; }
        }

        internal bool IsValid(out int inputPinIndex, out int outputPinIndex)
        {
            outputPinIndex = OutputPin != null ? OutputPin.Index : GetAttribute<int>(OutputPinAttribute);
            inputPinIndex = InputPin != null ? InputPin.Index : GetAttribute<int>(InputPinAttribute);
            return OutputElement != null && InputElement != null &&
                OutputElement.Type.GetOutputPin(outputPinIndex) != null &&
                InputElement.Type.GetInputPin(inputPinIndex) != null;
        }

        private PinTarget m_inputPinTarget;
        private PinTarget m_outputPinTarget;
    }
}
