using System;

namespace Log4NetErrorRepro.Server
{
    /// <summary>
    /// Принудительно сериализует CallContext так же, как .NET Remoting
    /// при формировании ответа после catch (BinaryFormatter + LogicalThreadContext).
    /// </summary>
    public class CrossAppDomainEcho : MarshalByRefObject
    {
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public string Echo(string value)
        {
            return value;
        }
    }

    internal static class CrossAppDomainCall
    {
        public static string Invoke()
        {
            AppDomain domain = AppDomain.CreateDomain(
                "RemotingCallContextProbe",
                null,
                new AppDomainSetup
                {
                    ApplicationBase = AppDomain.CurrentDomain.BaseDirectory
                });

            try
            {
                CrossAppDomainEcho echo = (CrossAppDomainEcho)domain.CreateInstanceAndUnwrap(
                    typeof(CrossAppDomainEcho).Assembly.FullName,
                    typeof(CrossAppDomainEcho).FullName);
                return echo.Echo("ping");
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }
    }
}
