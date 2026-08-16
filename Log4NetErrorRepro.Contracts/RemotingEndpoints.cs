using System.Collections;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;

namespace Log4NetErrorRepro.Contracts
{
    public static class RemotingEndpoints
    {
        public const int Port = 9099;
        public const string ObjectUri = "ErrorService";
        public const string Url = "tcp://127.0.0.1:9099/ErrorService";

        public static TcpChannel CreateChannel(int port, string name)
        {
            BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
            serverProvider.TypeFilterLevel = TypeFilterLevel.Full;

            BinaryClientFormatterSinkProvider clientProvider = new BinaryClientFormatterSinkProvider();

            IDictionary properties = new Hashtable();
            properties["name"] = name;
            properties["port"] = port;
            properties["typeFilterLevel"] = "Full";

            return new TcpChannel(properties, clientProvider, serverProvider);
        }
    }
}
