using System;
using System.Runtime.CompilerServices;

namespace MabinogiMobileServer
{
    class Exception : System.Exception
    {
        public string Log { get; } = "";
        public string FilePath { get; }
        public int LineNumber { get; }

        public Exception(
            string msg,
            [CallerFilePath] string FilePath = "",
            [CallerLineNumber] int LineNumber = 0
            )
        {
            Log = msg;
            this.FilePath = FilePath;
            this.LineNumber = LineNumber;
        }

        public void OutputExceptionLog()
        {
            Console.WriteLine($"message : {Log}");
            Console.WriteLine(
                $"""
                    Code Location
                    file path : {FilePath},
                    line : {LineNumber}
                """
            );
        }
    }
}
