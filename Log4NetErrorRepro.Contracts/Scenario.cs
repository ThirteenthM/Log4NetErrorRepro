using System;

namespace Log4NetErrorRepro.Contracts
{
    /// <summary>
    /// Сценарии стенда: сервер ловит MyException и по-разному
    /// логирует / формирует ответ на remote-вызов.
    /// </summary>
    [Serializable]
    public enum Scenario
    {
        /// <summary>Контроль: catch + throw без log4net.</summary>
        NoLog_Rethrow = 1,

        /// <summary>catch + log.Error("Текст") без exception + throw.</summary>
        LogMessageOnly_Rethrow = 2,

        /// <summary>Основной: catch + log.Error("Текст", ex) + throw.</summary>
        LogErrorWithException_Rethrow = 3,

        /// <summary>catch + log.Error("Текст", ex) + DTO без объекта exception.</summary>
        LogErrorWithException_ReturnDto = 4,

        /// <summary>catch + log.Error("Текст", ex) + DTO с объектом MyException.</summary>
        LogErrorWithException_ReturnExceptionObject = 5,

        /// <summary>LogicalThreadContext в CallContext + log.Error("Текст", ex) + DTO.</summary>
        LogError_LogicalThreadContext_ReturnDto = 6,

        /// <summary>Принудительно читаем TargetSite, затем DTO с exception (без log4net).</summary>
        TouchTargetSite_ReturnExceptionObject = 7,

        /// <summary>
        /// LogicalThreadContext с null + log.Error, затем CrossAppDomain
        /// (сериализация CallContext, стек GetObjectData / GetType) и remote-ответ.
        /// </summary>
        LogError_NullLogicalThreadContext_CrossAppDomain = 8,

        /// <summary>
        /// Как 8, плюс обнуление всего LogicalThreadContext.Properties
        /// (аналог Properties = null через CallContext), затем CrossAppDomain.
        /// </summary>
        LogError_NullLogicalThreadContextProperties_CrossAppDomain = 9
    }
}
