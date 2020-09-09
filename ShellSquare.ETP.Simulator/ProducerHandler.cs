using Energistics.Etp;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Protocol.Core;
using Energistics.Etp.Security;
using Energistics.Etp.v11;
using Energistics.Etp.v11.Datatypes;
using Energistics.Etp.v11.Datatypes.ChannelData;
using Energistics.Etp.v11.Protocol.ChannelStreaming;
using Energistics.Etp.v11.Protocol.Core;
using Energistics.Etp.v11.Protocol.Discovery;
using Energistics.Etp.v11.Protocol.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace ShellSquare.ETP.Simulator
{
    public class ProducerHandler
    {
        public Action<ChannelMetadata> ChannelInfoReceived;
        public Action<string, double, TraceLevel> Message;
        const string SUBPROTOCOL = "energistics-tp";
        IEtpClient m_client;
        private DateTime m_Time;
        string m_ApplicationName = "ShellSquare ETP Simulator";
        string m_ApplicationVersion = "1.4.1.1";


        public async Task Connect(string url, string username, string password, CancellationToken token)
        {
            try
            {
                m_Time = DateTime.UtcNow;

                var protocols = new List<SupportedProtocol>();
                SupportedProtocol p;
                p = EtpHelper.ToSupportedProtocol(Protocols.ChannelStreaming, "consumer");
                protocols.Add(p);

                p = EtpHelper.ToSupportedProtocol(Protocols.Discovery, "store");
                protocols.Add(p);


                var auth = Authorization.Basic(username, password);
                m_client = EtpFactory.CreateClient(WebSocketType.WebSocket4Net, url, m_ApplicationName, m_ApplicationVersion, SUBPROTOCOL, auth);

                m_client.Register<IChannelStreamingProducer, ChannelStreamingProducerHandler>();

                m_client.Register<IDiscoveryCustomer, DiscoveryCustomerHandler>();
                m_client.Register<IStoreCustomer, StoreCustomerHandler>();
                m_client.Handler<ICoreClient>().OnOpenSession += HandleOpenSession;
                CreateSession(protocols);
                await m_client.OpenAsync();

            }
            catch (Exception ex)
            {

                if (ex.InnerException != null)
                {
                    throw new Exception($"{ex.Message} {ex.InnerException.Message}");
                }
                throw;
            }
        }



        private void CreateSession(List<SupportedProtocol> protocols)
        {
            MessageHeader header = new MessageHeader();
            header.Protocol = (int)Protocols.Core;
            header.MessageType = 1;
            header.MessageId = EtpHelper.NextMessageId;
            header.MessageFlags = 0;
            header.CorrelationId = 0;

            List<ISupportedProtocol> requestedProtocols = new List<ISupportedProtocol>();
            requestedProtocols.AddRange(protocols);

            var requestSession = new RequestSession()
            {
                ApplicationName = m_ApplicationName,
                ApplicationVersion = m_ApplicationVersion,
                RequestedProtocols = requestedProtocols.Cast<SupportedProtocol>().ToList(),
                SupportedObjects = new List<string>()
            };

            string message = $"\nRequest: [Protocol {header.Protocol} MessageType {header.MessageType}]";
            Message?.Invoke(message, 0, TraceLevel.Info);
            m_client.Handler<ICoreClient>().RequestSession(m_ApplicationName, m_ApplicationVersion, requestedProtocols);
        }


        protected void HandleOpenSession(object sender, ProtocolEventArgs<OpenSession> e)
        {
            //var receivedTime = DateTime.UtcNow;
            //lock (m_ConnectionLock)
            //{
            //    m_HasConnected = true;
            //}
            //string message = ToString(e.Message);
            //message = $"\nResponse : [Protocol {e.Header.Protocol} MessageType {e.Header.MessageType}]\n{message}";
            //var timediff = receivedTime - m_Time;
            //Message?.Invoke(message, timediff.TotalMilliseconds, TraceLevel.Info);
        }


        public async Task SendChannelMetadata(ChannelMetadata metadata)
        {
            MessageHeader header = new MessageHeader();
            header.Protocol = (int)Protocols.ChannelStreaming;
            header.MessageType = 2;
            header.MessageId = EtpHelper.NextMessageId;
            header.MessageFlags = 0;
            header.CorrelationId = 0;

            var result = m_client.Handler<IChannelStreamingProducer>().ChannelMetadata(header, metadata.Channels);
        }

        int index = 0;
        public async Task SendChannelData(List<ChannelStreamingInfo> lstChannels)
        {
            while (true)
            {
                var handler = m_client.Handler<IChannelStreamingProducer>();

                index = index + 1;

                await Task.Run(async () =>
                {
                    MessageHeader header = new MessageHeader();
                    header.Protocol = (int)Protocols.ChannelStreaming;
                    header.MessageType = 3;
                    header.MessageId = EtpHelper.NextMessageId;
                    header.MessageFlags = 0;
                    header.CorrelationId = 0;

                    var recordData = Activator.CreateInstance<ChannelData>();
                    recordData.Data = new List<DataItem>();

                    Random random = new Random();

                    foreach (var item in lstChannels)
                    {
                        DataItem d = new DataItem();
                        d.ChannelId = item.ChannelId;
                        d.Indexes = new List<long>();
                        d.Indexes.Add(index);
                        d.Value = new DataValue();
                        d.Value.Item = random.Next();

                        d.ValueAttributes = new List<DataAttribute>();

                        recordData.Data.Add(d);
                    }

                    handler.ChannelData(header, recordData.Data);
                });
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

    }
}
