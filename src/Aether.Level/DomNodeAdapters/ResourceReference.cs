//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Does not call Globals.ResourceService / native asset load. Target is a
// URI-only IResource wrapper. Slot / IHierarchical replace-in-parent omitted.

using System;
using System.Collections.Generic;
using System.IO;

using Sce.Atf;
using Sce.Atf.Adaptation;
using Sce.Atf.Dom;

using Aether.Level;
using LevelEditorCore;

namespace LevelEditor.DomNodeAdapters
{
    /// <summary>
    /// Reference to a IResource</summary>
    public class ResourceReference : DomNodeAdapter, IReference<IResource>
    {
        public static IReference<IResource> Create(IResource resource)
        {
            return Create(null, resource);
        }

        public static IReference<IResource> Create(DomNodeType domtype,IResource resource)
        {
            if (resource == null)
                throw new ArgumentNullException("resource");

            if(domtype == null)
                domtype = Schema.resourceReferenceType.Type;

            if (!Schema.resourceReferenceType.Type.IsAssignableFrom(domtype))
                return null;
            
            ResourceReference resRef = null;
            if (CanReference(domtype, resource))
            {
                resRef = new DomNode(domtype).As<ResourceReference>();
                resRef.m_target = resource;
                resRef.SetAttribute(Schema.resourceReferenceType.uriAttribute, resource.Uri);                
            }
            return resRef;
        }

        public static bool CanReference(DomNodeType domtype, IResource resource)
        {
            if (domtype == null || resource == null || !Schema.resourceReferenceType.Type.IsAssignableFrom(domtype))
                return false;
            // valid resource file extensions
            var exts = (HashSet<string>)domtype.GetTag(Annotations.ReferenceConstraint.ValidResourceFileExts);
            if (resource.Uri == null)
                return exts == null;
            string reExt = Path.GetExtension(resource.Uri.LocalPath).ToLower();
            bool canReference = exts == null || exts.Contains(".*") || exts.Contains(reExt);
            return canReference;            
        }
      
        protected override void OnNodeSet()
        {
            base.OnNodeSet();
            Uri resUri = GetAttribute<Uri>(Schema.resourceReferenceType.uriAttribute);
            if (resUri != null)
                m_target = new UriResource(resUri);            
        }

        #region IReference<IResource> Members

        /// <summary>
        /// Always returns true, as any IResource can be referenced and null is acceptable</summary>
        /// <param name="item">Resource to be referenced, can be null</param>
        /// <returns>Always true</returns>
        public bool CanReference(IResource item)
        {
            return false;
        }

        /// <summary>
        /// Gets or sets the referenced IResource</summary>
        public IResource Target
        {
            get { return m_target; }
            set { throw new InvalidOperationException(); }
        }

        #endregion
        
        private IResource m_target;

       
    }
}
