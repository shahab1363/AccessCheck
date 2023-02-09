using System.Runtime.CompilerServices;

namespace CheckerLib.Common.Logger
{
    public class Log
    {
        public static ConsoleColor DefaultConsoleColor = Console.ForegroundColor;

        public static void Debug(CallerInfo callerInfo, string message, params object[] formatParams)
            => InternalLog(callerInfo, LogLevel.Debug, message, formatParams);
        public static void Debug(string message, object[]? formatParams = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => InternalLog(new CallerInfo(memberName, filePath, lineNumber), LogLevel.Debug, message, formatParams);

        public static void Info(CallerInfo callerInfo, string message, params object[] formatParams)
        => InternalLog(callerInfo, LogLevel.Info, message, formatParams);
        public static void Info(string message, object[]? formatParams = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => InternalLog(new CallerInfo(memberName, filePath, lineNumber), LogLevel.Info, message, formatParams);

        public static void Warn(CallerInfo callerInfo, string message, params object[] formatParams)
        => InternalLog(callerInfo, LogLevel.Warn, message, formatParams);
        public static void Warn(string message, object[]? formatParams = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => InternalLog(new CallerInfo(memberName, filePath, lineNumber), LogLevel.Warn, message, formatParams);

        public static void Error(CallerInfo callerInfo, string message, params object[] formatParams)
        => InternalLog(callerInfo, LogLevel.Error, message, formatParams);
        public static void Error(string message, object[]? formatParams = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => InternalLog(new CallerInfo(memberName, filePath, lineNumber), LogLevel.Error, message, formatParams);

        public static void Fatal(CallerInfo callerInfo, string message, params object[] formatParams)
        => InternalLog(callerInfo, LogLevel.Fatal, message, formatParams);
        public static void Fatal(string message, object[]? formatParams = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => InternalLog(new CallerInfo(memberName, filePath, lineNumber), LogLevel.Fatal, message, formatParams);

        private static void InternalLog(CallerInfo callerInfo, LogLevel logLevel, string message, params object[]? formatParams)
        {
            if (formatParams?.Any() == true)
            {
                message = string.Format(message, formatParams);
            }

            Console.ForegroundColor = DefaultConsoleColor;
            Console.Write($"{DateTime.Now:u} ");
           
            switch (logLevel)
            {
                case LogLevel.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
                case LogLevel.Info:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogLevel.Warn:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogLevel.Fatal:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
            }
            Console.Write($"[{logLevel}]");

            Console.ForegroundColor = DefaultConsoleColor;
            Console.WriteLine($"\t[{callerInfo}] {message}");
        }
    }
}
