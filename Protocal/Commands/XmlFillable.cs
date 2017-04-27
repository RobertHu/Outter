using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Protocal.Commands
{
    public abstract class Parser
    {
        public static readonly Dictionary<Type, Parser> Parsers;

        public abstract object Parse(string s);

        static Parser()
        {
            Parsers = new Dictionary<Type, Parser>
            {
                {typeof(string), new Parser<string>{Get = s => s}},
                {typeof(decimal), new Parser<decimal>{Get = s => decimal.Parse(s)}},
                {typeof(decimal?), new Parser<decimal?>{Get = s=> decimal.Parse(s)}},
                {typeof(int), new Parser<int>{Get = s => int.Parse(s)}},
                {typeof(int?), new Parser<int?>{Get = s =>int.Parse(s)}},
                {typeof(bool), new Parser<bool>{Get = s => bool.Parse(s)}},
                {typeof(bool?), new Parser<bool?>{Get = s => bool.Parse(s)}},
                {typeof(Guid), new Parser<Guid>{Get = s => Guid.Parse(s)}},
                {typeof(Guid?), new Parser<Guid?>{Get = s => Guid.Parse(s)}},
                {typeof(DateTime), new Parser<DateTime>{Get = s => DateTime.Parse(s)}},
                {typeof(DateTime?), new Parser<DateTime?>{Get = s => DateTime.Parse(s)}},
                {typeof(OrderPhase), new Parser<OrderPhase>{Get = s => (OrderPhase)int.Parse(s)}},
                {typeof(OrderPhase?), new Parser<OrderPhase?>{Get = s => (OrderPhase)int.Parse(s)}},
                {typeof(TradeOption), new Parser<TradeOption>{Get = s => (TradeOption)int.Parse(s)}},
                {typeof(PhysicalTradeSide), new Parser<PhysicalTradeSide>{Get = s => (PhysicalTradeSide)int.Parse(s)}},
                {typeof(CancelReason),new Parser<CancelReason>{Get = s=> (CancelReason)int.Parse(s)}},
                {typeof(CancelReason?),new Parser<CancelReason>{Get = s=> (CancelReason)int.Parse(s)}},
                {typeof(InstalmentType), new Parser<InstalmentType>{Get = s => (InstalmentType)int.Parse(s)}},
                {typeof(DownPaymentBasis), new Parser<DownPaymentBasis>{Get = s => (DownPaymentBasis)int.Parse(s)}},
                {typeof(RecalculateRateType), new Parser<RecalculateRateType>{Get = s => (RecalculateRateType)int.Parse(s)}},
                {typeof(InstalmentFrequence), new Parser<InstalmentFrequence>{Get = s => (InstalmentFrequence)int.Parse(s)}},
                {typeof(TransactionType), new Parser<TransactionType>{Get = s => (TransactionType)int.Parse(s)}},
                {typeof(TransactionSubType), new Parser<TransactionSubType>{Get = s => (TransactionSubType)int.Parse(s)}},
                {typeof(OrderType), new Parser<OrderType>{Get = s => (OrderType)int.Parse(s)}},
                {typeof(TransactionPhase), new Parser<TransactionPhase>{Get = s => (TransactionPhase)int.Parse(s)}},
                {typeof(ExpireType), new Parser<ExpireType>{Get = s => (ExpireType)int.Parse(s)}},
                {typeof(InstrumentCategory), new  Parser<InstrumentCategory>{Get = s => (InstrumentCategory)int.Parse(s)}},
                {typeof(AlertLevel), new Parser<AlertLevel>{Get = s => (AlertLevel)int.Parse(s)}},
                {typeof(AccountType), new Parser<AccountType>{Get = s => (AccountType)int.Parse(s)}},
                {typeof(DeliveryRequestStatus), new Parser<DeliveryRequestStatus>{Get = s => (DeliveryRequestStatus)int.Parse(s)}},
                {typeof(Physical.PhysicalType?), new Parser<Physical.PhysicalType?>{Get = s => string.IsNullOrEmpty(s)? new System.Nullable<Physical.PhysicalType>() : (Physical.PhysicalType)int.Parse(s)}}
            };
        }

    }

    public sealed class Parser<T> : Parser
    {
        public Func<string, T> Get;

        public override object Parse(string s)
        {
            if (string.IsNullOrEmpty(s)) return default(T);
            return this.Get(s);
        }
    }


    public abstract class XmlFillable<T>
    {
        protected XElement _currentFillElement = null;



        static XmlFillable()
        {
        }

        protected void InitializeProperties(XElement element)
        {
            this._currentFillElement = element;
            this.InnerInitializeProperties(element);
        }

        protected abstract void InnerInitializeProperties(XElement element);

        protected void FillProperty(Expression<Func<T, object>> expression, string attributeName = null)
        {
            var memberExpression = GetMemberExpression(expression.Body);
            PropertyInfo propertyInfo = (PropertyInfo)memberExpression.Member;
            if (attributeName == null) attributeName = propertyInfo.Name;
            XAttribute attribute = this._currentFillElement.Attribute(attributeName);
            if (attribute == null) return;
            object value = this.GetValue(propertyInfo.PropertyType, attribute.Value);
            propertyInfo.SetValue(this, value, null);
        }

        private object GetValue(Type type, string attrValue)
        {
            Parser parser;
            if (!Parser.Parsers.TryGetValue(type, out parser))
            {
                throw new NotImplementedException(string.Format("unrecogize type {0}", type));
            }
            return parser.Parse(attrValue);
        }


        private static MemberExpression GetMemberExpression(Expression expression)
        {
            return GetMemberExpression(expression, true);
        }

        private static MemberExpression GetMemberExpression(Expression expression, bool enforceCheck)
        {
            MemberExpression memberExpression = null;
            if (expression.NodeType == ExpressionType.Convert)
            {
                var body = (UnaryExpression)expression;
                memberExpression = body.Operand as MemberExpression;
            }
            else if (expression.NodeType == ExpressionType.MemberAccess)
            {
                memberExpression = expression as MemberExpression;
            }

            if (enforceCheck && memberExpression == null)
            {
                throw new ArgumentException("Not a member access", "expression");
            }

            return memberExpression;
        }

    }

}
