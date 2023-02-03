using System.Runtime.CompilerServices;

namespace CheckerLib.Common.Logger
{
    public class CallerInfo
    {
        public CallerInfo([CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            MemberName = memberName;
            FilePath = filePath;
            LineNumber = lineNumber;
            FileName = Path.GetFileName(filePath);
        }

        public string MemberName { get; }
        public string FileName { get; }
        public string FilePath { get; }
        public int LineNumber { get; }

        public override string? ToString()
        {
            return $"{MemberName}, {FileName}@{LineNumber}";
        }
    }
}
