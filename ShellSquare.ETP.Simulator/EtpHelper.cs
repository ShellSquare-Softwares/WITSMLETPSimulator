using Energistics.Etp.v11;
using Energistics.Etp.v11.Datatypes;
using System.Collections.Generic;
using System.Threading;

namespace ShellSquare.ETP.Simulator
{
    public class EtpHelper
    {
        private static int MessageId = 1;

        public static int NextMessageId
        {
            get
            {
                return Interlocked.Increment(ref MessageId);
            }
        }

        public static SupportedProtocol ToSupportedProtocol(Protocols protocol, string role)
        {
            SupportedProtocol supportedProtocol = new SupportedProtocol()
            {
                ProtocolVersion = new Energistics.Etp.v11.Datatypes.Version()
                {
                    Major = 1,
                    Minor = 1,
                    Revision = 0,
                    Patch = 0
                },
                Protocol = (int)protocol,
                Role = role,
                ProtocolCapabilities = new Dictionary<string, DataValue>()
            };

            return supportedProtocol;
        }

 
    }
}
