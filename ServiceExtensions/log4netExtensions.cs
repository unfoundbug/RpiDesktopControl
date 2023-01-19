using log4net;
using System.Runtime.CompilerServices;

namespace ServiceExtensions
{
    public static class log4netExtensions
    {

        public static void InfoDetail(this ILog log , string toLog,
        [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            log.Info($"{memberName}^{sourceLineNumber}^{toLog}");
        }

        public static void DebugDetail(this ILog log, string toLog,
        [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            log.Debug($"{memberName}^{sourceLineNumber}^{toLog}");
        }

        public static void WarnDetail(this ILog log, string toLog,
        [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            log.Warn($"{memberName}^{sourceLineNumber}^{toLog}");
        }

        public static void ErrorDetail(this ILog log, string toLog, 
            Exception? ex = null,
        [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            log.Error($"{memberName}^{sourceLineNumber}^{toLog}");
        }
    }
}