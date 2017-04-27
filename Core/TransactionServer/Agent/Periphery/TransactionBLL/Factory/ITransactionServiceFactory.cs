using Core.TransactionServer.Agent.BinaryOption;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Periphery.TransactionBLL.Services;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.TransactionBusiness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Factory
{
    internal interface ITransactionServiceFactory
    {
        TransactionExecuteService CreateExecuteService(Transaction tran, TransactionSettings settings);
        Transaction CreateTransaction(Account account, TransactionConstructParams constructParams);
        FillServiceBase CreateFillService(Transaction tran, TransactionSettings settings);
        TransactionPreCheckService CreatePreCheckService(Transaction tran);
        PreCheckVerifierBase CreatePreCheckVerifier(Transaction tran);
        TransactionExecuteNecessaryCheckServiceBase CreateExecuteNecessaryCheckService();
        AutoFillServiceBase CreateAutoFillService();
    }

    internal sealed class TransactionServiceFactory : ITransactionServiceFactory
    {
        public static readonly TransactionServiceFactory Default = new TransactionServiceFactory();

        static TransactionServiceFactory() { }
        private TransactionServiceFactory() { }

        public TransactionExecuteService CreateExecuteService(Transaction tran, TransactionSettings settings)
        {
            return TransactionExecuteService.Default;
        }

        public Transaction CreateTransaction(Account account, TransactionConstructParams constructParams)
        {
            return new Transaction(account, constructParams, this);
        }


        public TransactionPreCheckService CreatePreCheckService(Transaction tran)
        {
            return new TransactionPreCheckService(tran);
        }

        public PreCheckVerifierBase CreatePreCheckVerifier(Transaction tran)
        {
            return new PreCheckVerifier(tran);
        }


        public FillServiceBase CreateFillService(Transaction tran, TransactionSettings settings)
        {
            return new FillService(tran, settings);
        }


        public TransactionExecuteNecessaryCheckServiceBase CreateExecuteNecessaryCheckService()
        {
            return TransactionExecuteNecessaryCheckService.Default;
        }


        public AutoFillServiceBase CreateAutoFillService()
        {
            return AutoFillService.Default;
        }
    }

    internal sealed class PhysicalTransactionServiceFactory : ITransactionServiceFactory
    {
        internal static readonly PhysicalTransactionServiceFactory Default = new PhysicalTransactionServiceFactory();

        static PhysicalTransactionServiceFactory() { }
        private PhysicalTransactionServiceFactory() { }

        public TransactionExecuteService CreateExecuteService(Transaction tran, TransactionSettings settings)
        {
            return TransactionExecuteService.Default;
        }

        public Transaction CreateTransaction(Account account, TransactionConstructParams constructParams)
        {
            return new PhysicalTransaction(account, constructParams, this);
        }

        public TransactionPreCheckService CreatePreCheckService(Transaction tran)
        {
            return new TransactionPreCheckService(tran);
        }

        public PreCheckVerifierBase CreatePreCheckVerifier(Transaction tran)
        {
            return new PhysicalPreCheckVerifier((PhysicalTransaction)tran);
        }


        public FillServiceBase CreateFillService(Transaction tran, TransactionSettings settings)
        {
            return new PhysicalFillService((PhysicalTransaction)tran, settings);
        }


        public TransactionExecuteNecessaryCheckServiceBase CreateExecuteNecessaryCheckService()
        {
            return PhysicalTransactionExecuteNecessaryCheckService.Default;
        }


        public AutoFillServiceBase CreateAutoFillService()
        {
            return AutoFillService.Default;
        }
    }

    internal sealed class BOTransactionServiceFactory : ITransactionServiceFactory
    {
        internal static readonly BOTransactionServiceFactory Default = new BOTransactionServiceFactory();

        static BOTransactionServiceFactory() { }
        private BOTransactionServiceFactory() { }

        public TransactionExecuteService CreateExecuteService(Transaction tran, TransactionSettings settings)
        {
            return TransactionExecuteService.Default;
        }

        public TransactionExecuteService CreateBookService(Transaction tran, TransactionSettings settings)
        {
            throw new NotImplementedException();
        }

        public Transaction CreateTransaction(Account account, TransactionConstructParams constructParams)
        {
            return new BOTransaction(account, constructParams, this);
        }


        public TransactionPreCheckService CreatePreCheckService(Transaction tran)
        {
            return new TransactionPreCheckService(tran);
        }

        public PreCheckVerifierBase CreatePreCheckVerifier(Transaction tran)
        {
            return new BOPreCheckVerifier((BOTransaction)tran);
        }


        public FillServiceBase CreateFillService(Transaction tran, TransactionSettings settings)
        {
            return new BOTransactionFillService((BOTransaction)tran, settings);
        }


        public TransactionExecuteNecessaryCheckServiceBase CreateExecuteNecessaryCheckService()
        {
            return BOTransactionExecuteNecessaryCheckService.Default;
        }


        public AutoFillServiceBase CreateAutoFillService()
        {
            return BOAutoFillService.Default;
        }
    }

}
