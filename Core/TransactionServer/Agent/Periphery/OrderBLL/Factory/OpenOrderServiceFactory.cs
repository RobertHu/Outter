using Core.TransactionServer.Agent.BinaryOption;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Factory
{
    public abstract class OpenOrderServiceFactoryBase
    {
        internal abstract OrderFloating CreateFloatPLCalcaulator(Order order);

        internal virtual OrderSplitCalculator CreateSplitOrderCalculator(Order order, OrderSettings settings)
        {
            return new OrderSplitCalculator(order, settings);
        }
    }

    public sealed class GeneralOpenOrderServiceFactory : OpenOrderServiceFactoryBase
    {
        public static readonly GeneralOpenOrderServiceFactory Default = new GeneralOpenOrderServiceFactory();
        private GeneralOpenOrderServiceFactory() { }
        internal override OrderFloating CreateFloatPLCalcaulator(Order order)
        {
            return new OrderFloating(order, new CalculateParams(order));
        }
    }

    public sealed class PhysicalOpenOrderServiceFactory : OpenOrderServiceFactoryBase
    {
        public static readonly PhysicalOpenOrderServiceFactory Default = new PhysicalOpenOrderServiceFactory();
        private PhysicalOpenOrderServiceFactory() { }

        internal override OrderFloating CreateFloatPLCalcaulator(Order order)
        {
            return new PhysicalFloating((PhysicalOrder)order, new PhysicalCalculateParams((PhysicalOrder)order));
        }
    }

    public sealed class BOOpenOrderServiceFactory : OpenOrderServiceFactoryBase
    {
        public static readonly BOOpenOrderServiceFactory Default = new BOOpenOrderServiceFactory();
        private BOOpenOrderServiceFactory() { }

        internal override BLL.OrderBusiness.Calculator.OrderFloating CreateFloatPLCalcaulator(Agent.Order order)
        {
            return BOFloatPLCalculator.Default;
        }

        internal override OrderBusiness.Calculator.OrderSplitCalculator CreateSplitOrderCalculator(Agent.Order order, BLL.OrderBusiness.OrderSettings settings)
        {
            return null;
        }
    }

}
