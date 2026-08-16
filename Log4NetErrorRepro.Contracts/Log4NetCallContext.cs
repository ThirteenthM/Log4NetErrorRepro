using System.Runtime.Remoting.Messaging;

namespace Log4NetErrorRepro.Contracts
{
    /// <summary>
    /// Слот CallContext, куда log4net 2.0.9 кладёт LogicalThreadContext.Properties.
    /// Remoting гоняет его с каждым вызовом — без сброса сценарии заражают друг друга.
    /// </summary>
    public static class Log4NetCallContext
    {
        public const string PropertiesSlotName = "log4net.Util.LogicalThreadContextProperties";

        public static void Clear()
        {
            CallContext.LogicalSetData(PropertiesSlotName, null);
        }
    }
}
