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
    internal class ConsumerHandler
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
                p = EtpHelper.ToSupportedProtocol(Protocols.ChannelStreaming, "producer");
                protocols.Add(p);

                p = EtpHelper.ToSupportedProtocol(Protocols.Discovery, "store");
                protocols.Add(p);


                var auth = Authorization.Basic(username, password);
                m_client = EtpFactory.CreateClient(WebSocketType.WebSocket4Net, url, m_ApplicationName, m_ApplicationVersion, SUBPROTOCOL, auth);


                m_client.Register<IChannelStreamingConsumer, ChannelStreamingConsumerHandler>();
                m_client.Handler<IChannelStreamingConsumer>().OnChannelMetadata += HandleChannelMetadata;
                m_client.Handler<IChannelStreamingConsumer>().OnProtocolException += HandleProtocolException;
                m_client.Handler<IChannelStreamingConsumer>().OnChannelData += HandleChannelData;

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

        protected void HandleChannelMetadata(object sender, ProtocolEventArgs<ChannelMetadata> e)
        {
            var receivedTime = DateTime.UtcNow;
            if (e.Header.Protocol == 1 && e.Header.MessageType == 2)
            {
                var timediff = receivedTime - m_Time;

                string message = "Channels received: [";
                ChannelMetadata metadata = new ChannelMetadata();
                metadata.Channels = new List<ChannelMetadataRecord>();
                foreach (var channel in e.Message.Channels)
                {

                    metadata.Channels.Add(channel);
                    ChannelStreamingInfo channelStreamingInfo = new ChannelStreamingInfo()
                    {
                        ChannelId = channel.ChannelId,
                        StartIndex = new StreamingStartIndex()
                        {
                            Item = null
                        },
                        ReceiveChangeNotification = true
                    };

                }


                ChannelInfoReceived?.Invoke(metadata);


                message = $"\nResponse : [Protocol {e.Header.Protocol} MessageType {e.Header.MessageType}]";
                Message?.Invoke(message, timediff.TotalMilliseconds, TraceLevel.Info);

            }
        }

        protected void HandleChannelData(object sender, ProtocolEventArgs<ChannelData> e)
        {
            
        }

        protected void HandleOpenSession(object sender, ProtocolEventArgs<OpenSession> e)
        {
            var receivedTime = DateTime.UtcNow;           
            string message = e.Message.ToMessage();
            message = $"\nResponse : [Protocol {e.Header.Protocol} MessageType {e.Header.MessageType}]\n{message}";
            var timediff = receivedTime - m_Time;
            Message?.Invoke(message, timediff.TotalMilliseconds, TraceLevel.Info);
        }

        protected void HandleProtocolException(object sender, ProtocolEventArgs<IProtocolException> e)
        {
            var receivedTime = DateTime.UtcNow;
            if (e.Header.MessageType == 1000)
            {
                var timediff = receivedTime - m_Time;
                var bodyrecord = Activator.CreateInstance<ProtocolException>();
                string message = $"Error Received ({e.Message.ErrorCode}): {e.Message.ErrorMessage}";

                message = $"\nResponse : [Protocol {e.Header.Protocol} MessageType {e.Header.MessageType}]\n{message}";

                Message?.Invoke(message, timediff.TotalMilliseconds, TraceLevel.Error);
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


        public void Describe(List<string> uris)
        {
            MessageHeader header = new MessageHeader();
            header.Protocol = (int)Protocols.ChannelStreaming;
            header.MessageType = 1;
            header.MessageId = EtpHelper.NextMessageId;
            header.MessageFlags = 3;
            header.CorrelationId = 10;

            var channelDescribe = new ChannelDescribe()
            {
                Uris = uris
            };

            string message = $"\nRequest: [Protocol {header.Protocol} MessageType {header.MessageType}]";

            Message?.Invoke(message, 0, TraceLevel.Info);

            m_client.Handler<IChannelStreamingConsumer>().ChannelDescribe(uris);
        }


    }
}
