using System;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using log4net;
using log4net.Config;
using Log4NetErrorRepro.Contracts;

namespace Log4NetErrorRepro.Server
{
    internal static class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        private static int Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            XmlConfigurator.Configure();
            Log.Info("log4net 2.0.9 configured, runtime=" + Environment.Version);

            TcpChannelRegistration();
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(ErrorService),
                RemotingEndpoints.ObjectUri,
                WellKnownObjectMode.Singleton);

            Console.WriteLine();
            Console.WriteLine("=== Log4NetErrorRepro.Server ===");
            Console.WriteLine("net472 + log4net 2.0.9");
            Console.WriteLine("Remoting: " + RemotingEndpoints.Url);
            Console.WriteLine("Жду вызовы. Остановка: Ctrl+C");
            Console.WriteLine();

            ManualResetEvent exit = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                exit.Set();
            };
            exit.WaitOne();
            return 0;
        }

        private static void TcpChannelRegistration()
        {
            System.Runtime.Remoting.Channels.Tcp.TcpChannel channel =
                RemotingEndpoints.CreateChannel(RemotingEndpoints.Port, "ErrorReproServer");
            ChannelServices.RegisterChannel(channel, false);
        }
    }
}
