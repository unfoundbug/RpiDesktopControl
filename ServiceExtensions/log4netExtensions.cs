using log4net;
using System.Runtime.CompilerServices;

namespace ServiceExtensions
{
    public static class log4netExtensions
    {

        public static void InfoDetail(this ILog log , string toLog,
        [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "",
        [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            var callerTypeName = Path.GetFileNameWithoutExtension(Path.GetFileName(callerFilePath));
            log.Info($"{callerTypeName}::{memberName}^{sourceLineNumber}^{toLog}");
        }

        public static void DebugDetail(this ILog log, string toLog,
        [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "",
        [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            var callerTypeName = Path.GetFileNameWithoutExtension(Path.GetFileName(callerFilePath));
            log.Debug($"{callerTypeName}::{memberName}^{sourceLineNumber}^{toLog}");
        }

        public static void WarnDetail(this ILog log, string toLog,
        [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "",
        [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            var callerTypeName = Path.GetFileNameWithoutExtension(Path.GetFileName(callerFilePath));
            log.Warn($"{callerTypeName}::{memberName}^{sourceLineNumber}^{toLog}");
        }

        public static void ErrorDetail(this ILog log, string toLog, 
            Exception? ex = null,
        [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "",
        [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            var callerTypeName = Path.GetFileNameWithoutExtension(Path.GetFileName(callerFilePath));
            log.Error($"{callerTypeName}::{memberName}^{sourceLineNumber}^{toLog}");
        }
    }
}