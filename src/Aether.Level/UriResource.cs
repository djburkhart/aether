//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// URI-only IResource so resource-reference adapters can store a path without
// Globals.ResourceService or the native engine.

using System;

using Sce.Atf;

namespace Aether.Level
{
    /// <summary>
    /// Minimal <see cref="IResource"/> that holds a URI and a type name.</summary>
    public sealed class UriResource : IResource
    {
        /// <summary>
        /// Constructor</summary>
        /// <param name="uri">Resource URI</param>
        /// <param name="type">Type string shown to the user (default "Resource")</param>
        public UriResource(Uri uri, string type = "Resource")
        {
            if (uri == null)
                throw new ArgumentNullException("uri");
            m_uri = uri;
            Type = type ?? "Resource";
        }

        /// <inheritdoc/>
        public string Type { get; }

        /// <inheritdoc/>
        public Uri Uri
        {
            get { return m_uri; }
            set
            {
                if (m_uri == value)
                    return;
                Uri old = m_uri;
                m_uri = value;
                UriChanged?.Invoke(this, new UriChangedEventArgs(old));
            }
        }

        /// <inheritdoc/>
        public event EventHandler<UriChangedEventArgs> UriChanged;

        private Uri m_uri;
    }
}
