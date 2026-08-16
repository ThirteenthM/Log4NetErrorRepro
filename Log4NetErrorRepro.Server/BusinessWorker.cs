using Log4NetErrorRepro.Contracts;

namespace Log4NetErrorRepro.Server
{
    /// <summary>
    /// Исключение бросается из сборки Server, чтобы TargetSite указывал на
    /// Log4NetErrorRepro.Server — этой DLL нет у клиента.
    /// </summary>
    internal static class BusinessWorker
    {
        public static void DoWork()
        {
            throw new MyException("Сбой бизнес-операции при remote-вызове")
            {
                ErrorCode = 5001,
                OrderId = null
            };
        }
    }
}
