using System;
using System.Collections.Generic;
using System.Text;

namespace tusk
{
    public class LogDebug:Singleton<LogDebug>
    {
        public static void Log(string logs)
        {
            Console.WriteLine($"{logs}");
        }
    }
}
