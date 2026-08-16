using System;
using System.Runtime.Remoting.Channels;
using System.Text;
using log4net;
using log4net.Config;
using Log4NetErrorRepro.Contracts;

namespace Log4NetErrorRepro.Client
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            XmlConfigurator.Configure();
            Console.WriteLine("Client log4net " + typeof(ILog).Assembly.GetName().Version);

            try
            {
                RegisterClientChannel();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Не удалось зарегистрировать remoting-канал: " + ex.Message);
                return 1;
            }

            IErrorService service = (IErrorService)Activator.GetObject(
                typeof(IErrorService),
                RemotingEndpoints.Url);

            if (!TryPing(service))
            {
                Console.WriteLine("Сервер не отвечает: " + RemotingEndpoints.Url);
                Console.WriteLine("Сначала запустите: dotnet run --project Log4NetErrorRepro.Server");
                return 2;
            }

            if (args != null && args.Length > 0)
            {
                if (int.TryParse(args[0], out var number) && Enum.IsDefined(typeof(Scenario), number))
                {
                    RunScenario(service, (Scenario)number);
                    return 0;
                }

                if (string.Equals(args[0], "all", StringComparison.OrdinalIgnoreCase))
                {
                    RunAll(service);
                    return 0;
                }
            }

            InteractiveLoop(service);
            return 0;
        }

        private static void InteractiveLoop(IErrorService service)
        {
            while (true)
            {
                PrintMenu();
                Console.Write("> ");
                string line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                line = line.Trim();
                if (line.Equals("q", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (line.Equals("all", StringComparison.OrdinalIgnoreCase) || line == "a")
                {
                    RunAll(service);
                    continue;
                }

                if (int.TryParse(line, out var number) && Enum.IsDefined(typeof(Scenario), number))
                {
                    RunScenario(service, (Scenario)number);
                    continue;
                }

                Console.WriteLine("Неизвестная команда.");
            }
        }

        private static void PrintMenu()
        {
            Console.WriteLine();
            Console.WriteLine("=== Стенд: net472 + log4net 2.0.9 + .NET Remoting ===");
            Console.WriteLine("Сервер ловит MyException в catch и формирует remote-ответ.");
            Console.WriteLine("Клиент и сервер используют одну версию log4net 2.0.9 (как на ПРОМе).");
            Console.WriteLine();
            Console.WriteLine("  1  catch + throw, без логирования");
            Console.WriteLine("  2  catch + log.Error(\"Текст\") + throw");
            Console.WriteLine("  3  catch + log.Error(\"Текст\", ex) + throw          [основной]");
            Console.WriteLine("  4  catch + log.Error(\"Текст\", ex) + DTO без exception");
            Console.WriteLine("  5  catch + log.Error(\"Текст\", ex) + DTO с MyException");
            Console.WriteLine("  6  LogicalThreadContext (строки) + log.Error(\"Текст\", ex) + DTO");
            Console.WriteLine("  7  прочитать ex.TargetSite + DTO с MyException (без log)");
            Console.WriteLine("  8  LTC[user]=null + log.Error + CrossAppDomain (и remote-ответ)");
            Console.WriteLine("  9  LTC[user]=null, Properties=null + log.Error + CrossAppDomain");
            Console.WriteLine("  a  прогнать все сценарии");
            Console.WriteLine("  q  выход");
            Console.WriteLine();
        }

        private static void RunAll(IErrorService service)
        {
            foreach (Scenario scenario in Enum.GetValues(typeof(Scenario)))
            {
                RunScenario(service, scenario);
            }
        }

        private static void RunScenario(IErrorService service, Scenario scenario)
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine("---------- " + (int)scenario + " " + scenario + " ----------");
                Log4NetCallContext.Clear();

                RemoteResponse response = service.Execute(scenario);

                Console.WriteLine("Remote-ответ получен (DTO).");
                Console.WriteLine("  Success=" + response.Success);
                Console.WriteLine("  Message=" + response.Message);
                Console.WriteLine("  Error=" + (response.Error == null ? "null" : response.Error.GetType().Name + ": " + response.Error.Message + " code=" + response.Error.ErrorCode + " OrderId=" + (response.Error.OrderId.HasValue ? response.Error.OrderId.Value.ToString() : "null")));
                if (!string.IsNullOrEmpty(response.ServerDiagnostics))
                {
                    Console.WriteLine("  --- диагностика сервера ---");
                    Console.WriteLine(response.ServerDiagnostics);
                }
            }
            catch (MyException ex)
            {
                Console.WriteLine("Сервер пробросил MyException через remoting (для сценариев throw это ожидаемо).");
                Console.WriteLine("  " + ex.Message + " code=" + ex.ErrorCode + " OrderId=" + (ex.OrderId.HasValue ? ex.OrderId.Value.ToString() : "null"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("СБОЙ СЕРИАЛИЗАЦИИ remote-ответа на клиенте:");
                PrintExceptionChain(ex);
            }
            finally
            {
                Log4NetCallContext.Clear();
            }
        }

        private static void PrintExceptionChain(Exception ex)
        {
            int depth = 0;
            for (Exception current = ex; current != null; current = current.InnerException)
            {
                Console.WriteLine("  [" + depth + "] " + current.GetType().FullName + ": " + current.Message);
                depth++;
            }

            Console.WriteLine(ex.ToString());
        }

        private static bool TryPing(IErrorService service)
        {
            try
            {
                string pong = service.Ping();
                Console.WriteLine("Ping: " + pong);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ping failed: " + ex.Message);
                return false;
            }
        }

        private static void RegisterClientChannel()
        {
            ChannelServices.RegisterChannel(
                RemotingEndpoints.CreateChannel(0, "ErrorReproClient"),
                false);
        }
    }
}
