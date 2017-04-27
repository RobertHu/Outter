using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Diagnostics;
using iExchange.Common;

namespace Core.TransactionServer.Agent.Framework
{
    public interface IPriceFactory
    {
        Price Create(string price, IPriceParameterProvider provider);
    }


    public abstract class BusinessItemBuilder
    {
        private BusinessRecord _parent;

        protected BusinessItemBuilder(BusinessRecord parent)
        {
            _parent = parent;
        }

        protected BusinessItem<T> CreateKey<T>(string name, T value)
        {
            return this.CreateItem(name, value, PermissionFeature.Key);
        }

        protected BusinessItem<T> CreateSoundItem<T>(string name, T value)
        {
            return this.CreateItem(name, value, PermissionFeature.Sound);
        }

        protected BusinessItem<T> CreateAlwaysSoundItem<T>(string name, T value)
        {
            return this.CreateItem(name, value, PermissionFeature.AlwaysSound);
        }

        protected BusinessItem<T> CreateReadonlyItem<T>(string name, T value)
        {
            return this.CreateItem(name, value, PermissionFeature.ReadOnly);
        }
        protected BusinessItem<T> CreateDumpItem<T>(string name, T value)
        {
            return this.CreateItem(name, value, PermissionFeature.Dumb);
        }

        protected BusinessItem<T> CreateItem<T>(string name, T value, PermissionFeature permission)
        {
            return BusinessItemFactory.Create(name, value, permission, _parent);
        }
    }
}
