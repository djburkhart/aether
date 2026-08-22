//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Image is object instead of System.Drawing.Image (same pattern as IStatusImage).
// GetInputPin / GetOutputPin do not walk Group — groups are out of this slice.

using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Sce.Atf.Controls.Adaptable.Graphs
{
    /// <summary>
    /// Interface for circuit element types, which define the appearance, inputs,
    /// and outputs of the element.</summary>
    public interface ICircuitElementType
    {
        /// <summary>
        /// Gets the element type name</summary>
        string Name
        {
            get;
        }

        /// <summary>
        /// Gets desired interior size, in pixels, of this element type</summary>
        Size InteriorSize
        {
            get;
        }

        /// <summary>
        /// Gets image to draw for this element type. Hosts may store any bitmap-like object;
        /// ATF used System.Drawing.Image.</summary>
        object Image
        {
            get;
        }

        /// <summary>
        /// Gets a read-only list of input pins for this element type.</summary>
        IList<ICircuitPin> Inputs
        {
            get;
        }

        /// <summary>
        /// Gets a read-only list of output pins for this element type.</summary>
        IList<ICircuitPin> Outputs
        {
            get;
        }
    }

    /// <summary>
    /// Extension methods for ICircuitElementType</summary>
    public static class CircuitElementTypes
    {
        /// <summary>
        /// Gets all the input pins for this element.</summary>
        public static IEnumerable<ICircuitPin> GetAllInputPins(this ICircuitElementType type)
        {
            return type.Inputs;
        }

        /// <summary>
        /// Gets all the output pins for this element.</summary>
        public static IEnumerable<ICircuitPin> GetAllOutputPins(this ICircuitElementType type)
        {
            return type.Outputs;
        }

        /// <summary>
        /// Gets the input pin whose zero-based index is 'index'.</summary>
        public static ICircuitPin GetInputPin(this ICircuitElementType type, int index)
        {
            return type.Inputs.FirstOrDefault(p => p.Index == index);
        }

        /// <summary>
        /// Gets the output pin whose zero-based index is 'index'.</summary>
        public static ICircuitPin GetOutputPin(this ICircuitElementType type, int index)
        {
            return type.Outputs.FirstOrDefault(p => p.Index == index);
        }
    }
}
