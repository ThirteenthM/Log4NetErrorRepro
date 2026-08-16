using System;

namespace Log4NetErrorRepro.Contracts
{
    [Serializable]
    public class RemoteResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        /// <summary>
        /// Если заполнен, BinaryFormatter сериализует MyException в ответе remoting.
        /// </summary>
        public MyException Error { get; set; }

        public string ServerDiagnostics { get; set; }
    }
}
