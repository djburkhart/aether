//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Image is object instead of System.Drawing.Image.

using System.Collections.Generic;
using System.Drawing;

namespace Sce.Atf.Controls.Adaptable.Graphs
{
    /// <summary>
    /// Simple implementation of ICircuitElementType</summary>
    public class ElementType : ICircuitElementType
    {
        /// <summary>
        /// Default constructor, so we can add these to sub-circuit extension data</summary>
        public ElementType()
        {
        }

        /// <summary>
        /// Constructor specifying circuit element type's attributes</summary>
        public ElementType(
            string name,
            bool isConnector,
            Size size,
            object image,
            ICircuitPin[] inputPins,
            ICircuitPin[] outputPins)
        {
            Set(name, isConnector, size, image, inputPins, outputPins);
        }

        /// <summary>
        /// Sets circuit element type's attributes</summary>
        public void Set(
            string name,
            bool isConnector,
            Size size,
            object image,
            ICircuitPin[] inputPins,
            ICircuitPin[] outputPins)
        {
            m_name = name;
            m_isConnector = isConnector;
            m_image = image;
            m_inputPins = inputPins;
            m_outputPins = outputPins;
        }

        /// <summary>
        /// Gets whether the element type is a connector. Connectors are used
        /// during grouping to define the group's interface.</summary>
        public bool IsConnector
        {
            get { return m_isConnector; }
        }

        /// <summary>
        /// Gets the element type's name</summary>
        public string Name
        {
            get { return m_name; }
        }

        /// <summary>
        /// Gets desired size of element type's interior, in pixels</summary>
        public Size InteriorSize
        {
            get { return (m_image != null) ? new Size(32, 32) : new Size(); }
        }

        /// <summary>
        /// Gets Image for this element type, to be displayed in interior</summary>
        public object Image
        {
            get { return m_image; }
            set { m_image = value; }
        }

        /// <summary>
        /// Gets the element type's input pins</summary>
        public IList<ICircuitPin> Inputs
        {
            get { return m_inputPins; }
        }

        /// <summary>
        /// Gets the element type's output pins</summary>
        public IList<ICircuitPin> Outputs
        {
            get { return m_outputPins; }
        }

        /// <summary>
        /// Pin for an ElementType</summary>
        public class Pin : ICircuitPin
        {
            /// <summary>
            /// Constructor specifying Pin type's attributes</summary>
            public Pin(string name, string typeName, int index)
            {
                m_name = name;
                m_typeName = typeName;
                m_index = index;
            }

            /// <summary>
            /// Gets pin's name</summary>
            public string Name
            {
                get { return m_name; }
            }

            /// <summary>
            /// Gets pin's type's name</summary>
            public string TypeName
            {
                get { return m_typeName; }
            }

            /// <summary>
            /// Gets index of pin on module</summary>
            public int Index
            {
                get { return m_index; }
            }

            /// <summary>
            /// Gets whether connection fan in to pin is allowed</summary>
            public bool AllowFanIn
            {
                get { return false; }
            }

            /// <summary>
            /// Gets whether connection fan out from pin is allowed</summary>
            public bool AllowFanOut
            {
                get { return true; }
            }

            private string m_name;
            private string m_typeName;
            private int m_index;
        }

        private string m_name;
        private ICircuitPin[] m_inputPins;
        private ICircuitPin[] m_outputPins;
        private object m_image;
        private bool m_isConnector;
    }
}
