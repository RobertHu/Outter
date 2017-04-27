using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Xml;
using iExchange.Common;

namespace Core.TransactionServer.Agent.Framework
{
    internal static class BusinessItemFactory
    {
        internal static BusinessItem<T> CreateVolatileItem<T>(string name, Func<T> producer, BusinessRecord parent)
        {
            return new VolatileBusinessItem<T>(name, producer, parent);
        }

        internal static BusinessItem<T> Create<T>(string name, T value, PermissionFeature feature, BusinessRecord parent)
        {
            if (feature == PermissionFeature.ReadOnly)
            {
                return new ReadOnlyBusinessItem<T>(name, value, parent, false);
            }
            else if (feature == PermissionFeature.Sound)
            {
                return new TransactionalBusinessItem<T>(name, value, parent);
            }
            else if (feature == PermissionFeature.Key)
            {
                return new ReadOnlyBusinessItem<T>(name, value, parent, true);
            }
            else if (feature == PermissionFeature.Dumb)
            {
                return new DumpBusinessItem<T>(name, value, parent);
            }
            else if (feature == PermissionFeature.AlwaysSound)
            {
                return new AlwaysSoundItem<T>(name, value, parent);
            }
            else
            {
                throw new NotSupportedException(string.Format("{0} is not a supported ChangeItemFeature", feature));
            }
        }
    }
}