using System;
using System.Runtime.Remoting.Messaging;
using System.Text;
using log4net;
using log4net.Config;
using Log4NetErrorRepro.Contracts;

namespace Log4NetErrorRepro.Server
{
    public class ErrorService : MarshalByRefObject, IErrorService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ErrorService));

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public string Ping()
        {
            return "pong from " + typeof(ErrorService).Assembly.GetName().Name
                + ", log4net " + typeof(ILog).Assembly.GetName().Version;
        }

        public RemoteResponse Execute(Scenario scenario)
        {
            try
            {
                EnsureLog4Net();
                Log4NetCallContext.Clear();

                BusinessWorker.DoWork();
                return new RemoteResponse { Success = true, Message = "unexpected success" };
            }
            catch (MyException ex)
            {
                StringBuilder diagnostics = new StringBuilder();
                string before = ExceptionProbe.DumpFields("before log, scenario=" + scenario, ex);
                diagnostics.Append(before);
                Console.Write(before);

                RemoteResponse response = HandleCaught(scenario, ex, diagnostics, out var rethrow);

                string after = ExceptionProbe.DumpFields("after log / before remote return", ex);
                diagnostics.Append(after);
                Console.Write(after);

                string serialized = ExceptionProbe.TryLocalSerialize(ex);
                diagnostics.Append(serialized);
                Console.Write(serialized);

                if (rethrow)
                {
                    throw;
                }

                response.ServerDiagnostics = diagnostics.ToString();
                return response;
            }
        }

        private static RemoteResponse HandleCaught(Scenario scenario, MyException ex, StringBuilder diagnostics, out bool rethrow)
        {
            rethrow = false;

            switch (scenario)
            {
                case Scenario.NoLog_Rethrow:
                    diagnostics.AppendLine("action: throw without logging");
                    rethrow = true;
                    return null;

                case Scenario.LogMessageOnly_Rethrow:
                    Log.Error("Текст");
                    diagnostics.AppendLine("action: log.Error(\"Текст\") then throw");
                    rethrow = true;
                    return null;

                case Scenario.LogErrorWithException_Rethrow:
                    Log.Error("Текст", ex);
                    diagnostics.AppendLine("action: log.Error(\"Текст\", ex) then throw");
                    rethrow = true;
                    return null;

                case Scenario.LogErrorWithException_ReturnDto:
                    Log.Error("Текст", ex);
                    diagnostics.AppendLine("action: log.Error(\"Текст\", ex) then return DTO without exception object");
                    return new RemoteResponse
                    {
                        Success = false,
                        Message = ex.Message
                    };

                case Scenario.LogErrorWithException_ReturnExceptionObject:
                    Log.Error("Текст", ex);
                    diagnostics.AppendLine("action: log.Error(\"Текст\", ex) then return DTO with MyException");
                    return new RemoteResponse
                    {
                        Success = false,
                        Message = ex.Message,
                        Error = ex
                    };

                case Scenario.LogError_LogicalThreadContext_ReturnDto:
                    log4net.LogicalThreadContext.Properties["requestId"] = "remote-demo";
                    log4net.LogicalThreadContext.Properties["user"] = "tester";
                    Log.Error("Текст", ex);
                    diagnostics.AppendLine("action: LogicalThreadContext.Properties + log.Error(\"Текст\", ex) then return DTO");
                    return new RemoteResponse
                    {
                        Success = false,
                        Message = ex.Message
                    };

                case Scenario.LogError_NullLogicalThreadContext_CrossAppDomain:
                    log4net.LogicalThreadContext.Properties["requestId"] = "remote-demo";
                    log4net.LogicalThreadContext.Properties["user"] = null;
                    Log.Error("Текст", ex);
                    diagnostics.AppendLine("action: LogicalThreadContext[user]=null + log.Error(\"Текст\", ex) then CrossAppDomain + remoting CallContext");
                    DumpLtcThenCrossAppDomain(diagnostics);
                    return new RemoteResponse
                    {
                        Success = false,
                        Message = ex.Message
                    };

                case Scenario.LogError_NullLogicalThreadContextProperties_CrossAppDomain:
                    log4net.LogicalThreadContext.Properties["requestId"] = "remote-demo";
                    log4net.LogicalThreadContext.Properties["user"] = null;
                    CallContext.LogicalSetData(Log4NetCallContext.PropertiesSlotName, null);
                    Log.Error("Текст", ex);
                    diagnostics.AppendLine("action: LogicalThreadContext[user]=null, Properties (CallContext)=null + log.Error then CrossAppDomain");
                    DumpLtcThenCrossAppDomain(diagnostics);
                    return new RemoteResponse
                    {
                        Success = false,
                        Message = ex.Message
                    };

                case Scenario.TouchTargetSite_ReturnExceptionObject:
                    System.Reflection.MethodBase site = ex.TargetSite;
                    Console.WriteLine(
                        "TargetSite touched: " +
                        (site == null
                            ? "null"
                            : site.DeclaringType.FullName + "." + site.Name + " in " + site.Module.Assembly.GetName().Name));
                    diagnostics.AppendLine("action: read ex.TargetSite then return DTO with MyException (no log4net)");
                    return new RemoteResponse
                    {
                        Success = false,
                        Message = ex.Message,
                        Error = ex
                    };

                default:
                    throw new InvalidOperationException("Unknown scenario: " + scenario);
            }
        }

        private static void DumpLtcThenCrossAppDomain(StringBuilder diagnostics)
        {
            string dump = ExceptionProbe.DumpLogicalThreadContextProperties();
            diagnostics.Append(dump);
            Console.Write(dump);
            CrossAppDomainCall.Invoke();
        }

        private static void EnsureLog4Net()
        {
            if (!LogManager.GetRepository().Configured)
            {
                XmlConfigurator.Configure();
            }
        }
    }
}
