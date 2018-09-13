using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Microsoft.Restier.Core.DataAnnotations
{
    public class AssociatedMetadataTypeTypeDescriptionProvider : TypeDescriptionProvider
    {
        private Type _associatedMetadataType;
        public AssociatedMetadataTypeTypeDescriptionProvider(Type type)
            : base(TypeDescriptor.GetProvider(type))
        {
        }

        public AssociatedMetadataTypeTypeDescriptionProvider(Type type, Type associatedMetadataType)
            : this(type)
        {
            if (associatedMetadataType == null)
            {
                throw new ArgumentNullException("associatedMetadataType");
            }

            _associatedMetadataType = associatedMetadataType;
        }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            ICustomTypeDescriptor baseDescriptor = base.GetTypeDescriptor(objectType, instance);
            return new AssociatedMetadataTypeTypeDescriptor(baseDescriptor, objectType, _associatedMetadataType);
        }
    }
}
