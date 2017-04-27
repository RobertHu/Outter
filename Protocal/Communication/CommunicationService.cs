using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Protocal.Communication
{
    public abstract class CommunicationService<T> where T : class
    {
        protected string _serviceUrl;
        protected string _endpointName;

        protected CommunicationService(string serviceUrl, string endpointName = null)
        {
            _endpointName = endpointName;
            _serviceUrl = serviceUrl;
            this.Service = this.CreateService();
        }

        protected T Service { get; private set; }

        protected T2 Call<T2>(Func<T2> fun)
        {
            try
            {
                return fun();
            }
            catch (Exception ex)
            {
                this.RecoverConnection(ex);
                return default(T2);
            }
        }


        protected void Call(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                this.RecoverConnection(ex);
            }

        }


        protected void RecoverConnection(Exception ex)
        {
            this.Logger.Warn(ex);
            this.Service = this.CreateService();
        }


        private T CreateService()
        {
            try
            {
                this.Close();
                return this.CreateUnderlyService();
            }
            catch (Exception ex)
            {
                this.Logger.Warn(ex);
                return null;
            }
        }

        protected abstract T CreateUnderlyService();

        protected abstract ILog Logger { get; }


        private void Close()
        {
            try
            {
                if (this.Service != null)
                {
                    ICommunicationObject communicationObj = this.Service as ICommunicationObject;
                    if (communicationObj != null)
                    {
                        communicationObj.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                ICommunicationObject communicationObj = this.Service as ICommunicationObject;
                if (communicationObj != null)
                {
                    communicationObj.Abort();
                }
                this.Logger.Warn(ex);
            }
        }
    }

    public abstract class HttpCommunicationService<T> : CommunicationService<T> where T : class
    {
        protected HttpCommunicationService(string serviceUrl) :
            base(serviceUrl) { }

        protected override T CreateUnderlyService()
        {
            return ChannelFactory.CreateHttpChannel<T>(_serviceUrl);
        }
    }

    public abstract class CommunicationServiceByEndPointName<T> : CommunicationService<T> where T : class
    {
        protected CommunicationServiceByEndPointName(string serviceUrl, string endpointName) :
            base(serviceUrl, endpointName)
        {
        }

        protected override T CreateUnderlyService()
        {
            return ChannelFactory.CreateChannelByName<T>(_endpointName, new EndpointAddress(_serviceUrl));
        }
    }

}
