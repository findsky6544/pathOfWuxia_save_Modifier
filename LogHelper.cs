using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static 侠之道存档修改器.Parameter;

namespace 侠之道存档修改器
{
    class LogHelper
    {
        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Debug(object messsage)
        {
            if ((int)Parameter.LogLevel <= (int)LogLevelEnum.Debug)
            {
                log.Debug(messsage);
            }
        }

        public static void DebugFormatted(string format, params object[] args)
        {
            if ((int)Parameter.LogLevel <= (int)LogLevelEnum.Debug)
            {
                log.DebugFormat(format, args);
            }
        }

        public static void Info(object message)
        {
            if ((int)Parameter.LogLevel <= (int)LogLevelEnum.Info)
            {
                log.Info(message);
            }
        }

        public static void InfoFormatted(string format, params object[] args)
        {
            if ((int)Parameter.LogLevel <= (int)LogLevelEnum.Info)
            {
                log.InfoFormat(format, args);
            }
        }

        public static void Warn(object message)
        {
            if ((int)Parameter.LogLevel <= (int)LogLevelEnum.Warn)
            {
                log.Warn(message);
            }
        }

        public static void WarnFormatted(string format, params object[] args)
        {
            if ((int)Parameter.LogLevel <= (int)LogLevelEnum.Warn)
            {
                log.WarnFormat(format, args);
            }
        }

        public static void Error(object message)
        {
            if ((int)Parameter.LogLevel <= (int)LogLevelEnum.Error)
            {
                log.Error(message);
            }
        }

        public static void Error(object message, Exception exception)
        {
            if ((int)Parameter.LogLevel <= (int)LogLevelEnum.Error)
            {
                log.Error(message, exception);
            }
        }

        public static void ErrorFormatted(string format, params object[] args)
        {
            if ((int)Parameter.LogLevel <= (int)LogLevelEnum.Error)
            {
                log.ErrorFormat(format, args);
            }
        }

        public static void Fatal(object message)
        {
            if ((int)Parameter.LogLevel <= (int)LogLevelEnum.Fatal)
            {
                log.Fatal(message);
            }
        }

        public static void Fatal(object message, Exception exception)
        {
            if ((int)Parameter.LogLevel <= (int)LogLevelEnum.Fatal)
            {
                log.Fatal(message, exception);
            }
        }

        public static void FatalFormatted(string format, params object[] args)
        {
            if ((int)Parameter.LogLevel <= (int)LogLevelEnum.Fatal)
            {
                log.FatalFormat(format, args);
            }
        }
    }
}
