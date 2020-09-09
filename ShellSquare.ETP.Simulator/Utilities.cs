using Energistics.Etp.v11.Protocol.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellSquare.ETP.Simulator
{
    internal static class Utilities
    {
        public static string ToMessage(this OpenSession os)
        {
            string message = $"Connected to the application : {os.ApplicationName}, \n version : {os.ApplicationVersion},\n SessionId : {os.SessionId}";

            foreach (var s in os.SupportedProtocols)
            {
                message = message + $"\n   Protocol : {s.Protocol}";

                foreach (var c in s.ProtocolCapabilities)
                {
                    message = message + $"\n      {c.Key} : {c.Value.Item}";
                }

                message = message + $"\n   Role : {s.Role}\n";

            }

            return message;

        }
    }
}
