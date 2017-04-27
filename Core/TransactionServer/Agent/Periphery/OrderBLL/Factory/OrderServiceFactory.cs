using Core.TransactionServer.Agent.BinaryOption;
using Core.TransactionServer.Agent.BinaryOption.Calculator;
using Core.TransactionServer.Agent.BinaryOption.Factory;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Factory;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Factory;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Factory
{
    internal abstract class OrderServiceFactoryBase
    {
        internal abstract OrderFeeCalculatorBase CreateOrderFeeCalculator(Order order, OrderSettings settings);
        internal abstract OpenOrderCalculatorBase CreateOpenOrderCalculator(Order order, OrderSettings settings);
        internal abstract CloseOrderCalculator CreateCloseOrderCalculator(Order order, OrderSettings settings);
        internal abstract Services.OrderExecuteServiceBase CreateOrderExecuteService(Order order, OrderSettings settings);
        internal abstract ValuedPLCalculatorBase CreateValuedPLCalculator(Order order);
        internal abstract Services.OrderPreCheckServiceBase CreatePreCheckService(Order order);
        internal abstract Order CreateOrder(Transaction tran, OrderConstructParams constructParams);
        internal abstract OrderSettings CreateOrderSettings(Order order, OrderConstructParams constructParams);
    }

    internal abstract class GeneralOrderServiceFactoryBase : OrderServiceFactoryBase
    {
        internal override CloseOrderCalculator CreateCloseOrderCalculator(Order order, OrderSettings settings)
        {
            return new CloseOrderCalculator(order, settings);
        }

        internal override Order CreateOrder(Transaction tran, OrderConstructParams constructParams)
        {
            return new Order(tran, constructParams, this);
        }

        internal override OrderSettings CreateOrderSettings(Order order, OrderConstructParams constructParams)
        {
            return new OrderSettings(order, constructParams);
        }
    }


    internal sealed class OrderBookServiceFactory : GeneralOrderServiceFactoryBase
    {
        internal static readonly OrderBookServiceFactory Default = new OrderBookServiceFactory();

        static OrderBookServiceFactory() { }
        private OrderBookServiceFactory() { }

        internal override OrderFeeCalculatorBase CreateOrderFeeCalculator(Order order, OrderSettings settings)
        {
            return new CloseOrderFeeBookCalculator(order, settings);
        }

        internal override OpenOrderCalculatorBase CreateOpenOrderCalculator(Order order, OrderSettings settings)
        {
            throw new NotImplementedException();
        }

        internal override Services.OrderExecuteServiceBase CreateOrderExecuteService(Order order, OrderSettings settings)
        {
            throw new NotImplementedException();
        }

        internal override ValuedPLCalculatorBase CreateValuedPLCalculator(Order order)
        {
            throw new NotImplementedException();
        }

        internal override Services.OrderPreCheckServiceBase CreatePreCheckService(Order order)
        {
            throw new NotImplementedException();
        }
    }



    internal sealed class GeneralOrderServiceFactory : GeneralOrderServiceFactoryBase
    {
        internal static readonly GeneralOrderServiceFactory Default = new GeneralOrderServiceFactory();
        private GeneralOrderServiceFactory() { }

        internal override OrderFeeCalculatorBase CreateOrderFeeCalculator(Order order, OrderSettings settings)
        {
            if (order.IsOpen)
            {
                return new OpenOrderFeeCalculator(order, settings);
            }
            else
            {
                return new CloseOrderFeeCalculator(order, settings);
            }
        }

        internal override OpenOrderCalculatorBase CreateOpenOrderCalculator(Order order, OrderSettings settings)
        {
            return new OpenOrderCalculator(order, settings, GeneralOpenOrderServiceFactory.Default);
        }

        internal override Services.OrderExecuteServiceBase CreateOrderExecuteService(Order order, OrderSettings settings)
        {
            return new Services.OrderExecuteService(order, settings);
        }

        internal override ValuedPLCalculatorBase CreateValuedPLCalculator(Order order)
        {
            return new ValuedPLCalculator(order);
        }


        internal override Services.OrderPreCheckServiceBase CreatePreCheckService(Order order)
        {
            return new Services.OrderPreCheckService(order);
        }
    }

    internal sealed class BOOrderServiceFactory : OrderServiceFactoryBase
    {
        internal static readonly BOOrderServiceFactory Default = new BOOrderServiceFactory();

        private BOOrderServiceFactory() { }

        internal override OrderFeeCalculatorBase CreateOrderFeeCalculator(Agent.Order order, OrderSettings settings)
        {
            return new BOOrderFeeCalculator((BinaryOption.Order)order, (BOOrderSettings)settings);
        }

        internal override BLL.OrderBusiness.Calculator.OpenOrderCalculatorBase CreateOpenOrderCalculator(Agent.Order order, OrderSettings settings)
        {
            return new BOOpenOrderCalculator((BinaryOption.Order)order, (BOOrderSettings)settings, BOOpenOrderServiceFactory.Default);
        }

        internal override BLL.OrderBusiness.Calculator.CloseOrderCalculator CreateCloseOrderCalculator(Agent.Order order, OrderSettings settings)
        {
            return new BOCloseOrderCalculator((BinaryOption.Order)order, (BOOrderSettings)settings);
        }

        internal override Services.OrderExecuteServiceBase CreateOrderExecuteService(Agent.Order order, OrderSettings settings)
        {
            return new Services.BOOrderExecuteService((BinaryOption.Order)order, (BOOrderSettings)settings);
        }


        internal override ValuedPLCalculatorBase CreateValuedPLCalculator(Agent.Order order)
        {
            return new BOValuedPLCalculator((BinaryOption.Order)order);
        }

        internal override Agent.Order CreateOrder(Transaction tran, OrderConstructParams constructParams)
        {
            return new BinaryOption.Order(tran, (BOOrderConstructParams)constructParams, this);
        }

        internal override OrderSettings CreateOrderSettings(Agent.Order order, OrderConstructParams constructParams)
        {
            return new BOOrderSettings((BinaryOption.Order)order, (BOOrderConstructParams)constructParams);
        }


        internal override Services.OrderPreCheckServiceBase CreatePreCheckService(Order order)
        {
            return new Services.BOOrderPreCheckService((BinaryOption.Order)order);
        }
    }

    internal abstract class PhysicalOrderServiceFactoryBase : OrderServiceFactoryBase
    {
        internal override CloseOrderCalculator CreateCloseOrderCalculator(Order order, OrderSettings settings)
        {
            return new PhysicalCloseOrderCalculator((PhysicalOrder)order, (PhysicalOrderSettings)settings);
        }

        internal override Order CreateOrder(Transaction tran, OrderConstructParams constructParams)
        {
            return new PhysicalOrder(tran, (PhysicalOrderConstructParams)constructParams, this);
        }

        internal override OrderSettings CreateOrderSettings(Order order, OrderConstructParams constructParams)
        {
            return new PhysicalOrderSettings((PhysicalOrder)order, (PhysicalOrderConstructParams)constructParams);
        }
    }


    internal sealed class PhysicalOrderBookServiceFactory : PhysicalOrderServiceFactoryBase
    {
        internal static readonly PhysicalOrderBookServiceFactory Default = new PhysicalOrderBookServiceFactory();

        static PhysicalOrderBookServiceFactory() { }
        private PhysicalOrderBookServiceFactory() { }

        internal override OrderFeeCalculatorBase CreateOrderFeeCalculator(Order order, OrderSettings settings)
        {
            return new PhysicalCloseOrderFeeBookCalculator((PhysicalOrder)order, (PhysicalOrderSettings)settings);
        }

        internal override OpenOrderCalculatorBase CreateOpenOrderCalculator(Order order, OrderSettings settings)
        {
            throw new NotImplementedException();
        }

        internal override Services.OrderExecuteServiceBase CreateOrderExecuteService(Order order, OrderSettings settings)
        {
            throw new NotImplementedException();
        }


        internal override ValuedPLCalculatorBase CreateValuedPLCalculator(Order order)
        {
            return new PhysicalValuedPLBookCalculator((PhysicalOrder)order);
        }

        internal override Services.OrderPreCheckServiceBase CreatePreCheckService(Order order)
        {
            throw new NotImplementedException();
        }
    }


    internal sealed class PhysicalOrderServiceFactory : PhysicalOrderServiceFactoryBase
    {
        internal static readonly PhysicalOrderServiceFactory Default = new PhysicalOrderServiceFactory();
        private PhysicalOrderServiceFactory() { }

        internal override OrderFeeCalculatorBase CreateOrderFeeCalculator(Order order, OrderSettings settings)
        {
            if (order.IsOpen)
            {
                return new PhysicalOpenOrderFeeCalculator((PhysicalOrder)order, settings);
            }
            else
            {
                return new PhysicalCloseOrderFeeCalculator((PhysicalOrder)order, settings);
            }
        }

        internal override OpenOrderCalculatorBase CreateOpenOrderCalculator(Order order, OrderSettings settings)
        {
            return new PhysicalOpenOrderCalculator((PhysicalOrder)order, (PhysicalOrderSettings)settings, PhysicalOpenOrderServiceFactory.Default);
        }

        internal override Services.OrderExecuteServiceBase CreateOrderExecuteService(Order order, OrderSettings settings)
        {
            return new Services.PhysicalOrderExecuteService((PhysicalOrder)order, settings);
        }

        internal override ValuedPLCalculatorBase CreateValuedPLCalculator(Order order)
        {
            return new PhysicalValuedPLCalculator((PhysicalOrder)order);
        }


        internal override Services.OrderPreCheckServiceBase CreatePreCheckService(Order order)
        {
            return new Services.PhysicalOrderPreCheckService((PhysicalOrder)order);
        }
    }
}
