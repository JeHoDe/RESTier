using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Restier.Core.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class MetadataTypeAttribute : Attribute
    {

        private Type _metadataClassType;

        public Type MetadataClassType
        {
            get
            {
                if (_metadataClassType == null)
                {
                    throw new InvalidOperationException("type cannot be null");
                }

                return _metadataClassType;
            }
        }

        public MetadataTypeAttribute(Type metadataClassType)
        {
            _metadataClassType = metadataClassType;
        }
    }
}