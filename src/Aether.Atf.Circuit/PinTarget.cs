//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.

using Sce.Atf.Dom;

namespace Sce.Atf.Controls.Adaptable.Graphs
{
    /// <summary>
    /// Encapsulates the circuit element and pin index for a given Pin</summary>
    public class PinTarget
    {
        /// <summary>
        /// Constructor</summary>
        /// <param name="targetDomNode">The circuit element on which this pin is originally defined</param>
        /// <param name="targetPinIndex">The pin index on the defining element</param>
        /// <param name="referencingDomNode">DomNode that instances the template that the pin is associated with</param>
        public PinTarget(DomNode targetDomNode, int targetPinIndex, DomNode referencingDomNode)
        {
            m_leafDomNode = targetDomNode;
            m_leafPinIndex = targetPinIndex;
            m_instancingNode = referencingDomNode;
        }

        /// <summary>
        /// Gets the ultimate DomNode(representing a circuit element) that the pin binds</summary>
        public DomNode LeafDomNode
        {
            get { return m_leafDomNode; }
        }

        /// <summary>
        /// Gets the ultimate pin index(in the binding circuit element) that this group pin binds</summary>
        public int LeafPinIndex
        {
            get { return m_leafPinIndex; }
        }

        /// <summary>
        /// Gets or sets the DomNode that instances the template that the pin is associated with</summary>
        public DomNode InstancingNode
        {
            get { return m_instancingNode; }
            set { m_instancingNode = value; }
        }

        /// <summary>
        /// Returns the hash code for this instance</summary>
        public override int GetHashCode()
        {
            int hash1 = LeafDomNode.GetHashCode();
            int hash2 = LeafPinIndex.GetHashCode();
            return hash1 ^ hash2;
        }

        /// <summary>
        /// Tests equality with an object</summary>
        public override bool Equals(object other)
        {
            bool result = false;
            if (other is PinTarget)
            {
                result = Equals((PinTarget)other);
            }

            return result;
        }

        /// <summary>
        /// Tests equality of two targets</summary>
        public static bool operator ==(PinTarget lhs, PinTarget rhs)
        {
            if (object.Equals(lhs, null))
                return object.Equals(rhs, null);
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Tests inequality of two targets</summary>
        public static bool operator !=(PinTarget lhs, PinTarget rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Tests equality with a target</summary>
        public bool Equals(PinTarget other)
        {
            if (other == null)
                return false;
            return (LeafDomNode == other.LeafDomNode && LeafPinIndex == other.LeafPinIndex);
        }

        /// <summary>
        /// Determines whether the specified pin target is equal to the current pin target, including the instancing node</summary>
        public bool FullyEquals(PinTarget other)
        {
            if (other == null)
                return false;
            return (LeafDomNode == other.LeafDomNode && LeafPinIndex == other.LeafPinIndex &&
                InstancingNode == other.InstancingNode);
        }

        private readonly DomNode m_leafDomNode;
        private readonly int m_leafPinIndex;
        private DomNode m_instancingNode;
    }
}
