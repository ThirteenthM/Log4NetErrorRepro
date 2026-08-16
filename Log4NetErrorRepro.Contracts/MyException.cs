using System;
using System.Runtime.Serialization;

namespace Log4NetErrorRepro.Contracts
{
    [Serializable]
    public class MyException : Exception
    {
        public int ErrorCode { get; set; }

        /// <summary>
        /// Намеренно не сериализуется в GetObjectData — как новое поле на ПРОМе.
        /// </summary>
        public long? OrderId { get; set; }

        public MyException()
        {
        }

        public MyException(string message)
            : base(message)
        {
        }

        public MyException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected MyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ErrorCode = info.GetInt32("ErrorCode");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("ErrorCode", ErrorCode);
        }
    }
}
