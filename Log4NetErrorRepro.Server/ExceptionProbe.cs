using System;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using log4net;

namespace Log4NetErrorRepro.Server
{
    internal static class ExceptionProbe
    {
        public static string DumpFields(string label, Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[" + label + "] type=" + ex.GetType().FullName);
            sb.AppendLine("  Message=" + ex.Message);
            DumpField(sb, ex, "_exceptionMethod");
            DumpField(sb, ex, "_exceptionMethodString");
            DumpField(sb, ex, "_stackTrace");
            DumpField(sb, ex, "_stackTraceString");
            DumpField(sb, ex, "_remoteStackTraceString");

            object ltc = CallContext.LogicalGetData("log4net.Util.LogicalThreadContextProperties");
            sb.AppendLine("  CallContext[log4net.Util.LogicalThreadContextProperties]=" + Describe(ltc));
            sb.Append(DumpLogicalThreadContextProperties());
            return sb.ToString();
        }

        /// <summary>
        /// Какие ключи LogicalThreadContext уйдут в remoting.
        /// В log4net 2.0.9 падает GetObjectData на entry.Value == null (GetType).
        /// </summary>
        public static string DumpLogicalThreadContextProperties()
        {
            StringBuilder sb = new StringBuilder();
            object raw = CallContext.LogicalGetData("log4net.Util.LogicalThreadContextProperties");
            if (raw == null)
            {
                sb.AppendLine("  LTC dump: слот пуст, в CallContext нет PropertiesDictionary");
                return sb.ToString();
            }

            System.Collections.IDictionary dict = raw as System.Collections.IDictionary;
            if (dict == null)
            {
                sb.AppendLine("  LTC dump: неожиданный тип " + raw.GetType().FullName);
                return sb.ToString();
            }

            if (dict.Count == 0)
            {
                sb.AppendLine("  LTC dump: словарь пустой");
                return sb.ToString();
            }

            sb.AppendLine("  LTC dump (" + dict.Count + " entries):");
            foreach (System.Collections.DictionaryEntry entry in dict)
            {
                object key = entry.Key;
                object value = entry.Value;
                if (value == null)
                {
                    sb.AppendLine("    GUILTY null: key=" + key);
                    continue;
                }

                Type type = value.GetType();
                sb.AppendLine(
                    "    ok: key=" + key +
                    " type=" + type.FullName +
                    " serializable=" + type.IsSerializable +
                    " value=" + Truncate(value.ToString()));
            }

            return sb.ToString();
        }

        public static string TryLocalSerialize(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter().Serialize(stream, ex);
                    sb.AppendLine("  Local BinaryFormatter(MyException) OK, bytes=" + stream.Length);
                }
            }
            catch (Exception serializeEx)
            {
                sb.AppendLine("  Local BinaryFormatter(MyException) FAIL: " + serializeEx.GetType().Name + ": " + serializeEx.Message);
            }

            return sb.ToString();
        }

        public static void Write(ILog log, string dump)
        {
            Console.Write(dump);
            if (log == null)
            {
                return;
            }
            log.Info(dump.TrimEnd());
        }

        private static void DumpField(StringBuilder sb, Exception ex, string fieldName)
        {
            FieldInfo field = typeof(Exception).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                sb.AppendLine("  " + fieldName + " = <field not found>");
                return;
            }

            object value = field.GetValue(ex);
            sb.AppendLine("  " + fieldName + " = " + Describe(value));
        }

        private static string Describe(object value)
        {
            if (value == null)
            {
                return "null";
            }

            string text = value.ToString();
            return value.GetType().FullName + " | " + Truncate(text);
        }

        private static string Truncate(string text)
        {
            if (text == null)
            {
                return "null";
            }

            if (text.Length > 180)
            {
                return text.Substring(0, 180) + "...";
            }

            return text;
        }
    }
}
