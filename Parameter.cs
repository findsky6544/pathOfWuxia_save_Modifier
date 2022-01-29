using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 侠之道存档修改器
{
    class Parameter
    {/// <summary>
     /// 日志等级
     /// </summary>
        public enum LogLevelEnum
        {
            Debug = 0,
            Info = 1,
            Warn = 2,
            Error = 3,
            Fatal = 4
        }

        /// <summary>
        /// 当前保存日志等级
        /// </summary>
        public static LogLevelEnum LogLevel;

        /// <summary>
        /// 日志存放路径
        /// </summary>
        public static string LogFilePath;

        /// <summary>
        /// 日志存放天数
        /// </summary>
        public static int LogFileExistDay;
    }
}
