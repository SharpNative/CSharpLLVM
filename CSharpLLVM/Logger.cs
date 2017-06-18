using System;

namespace CSharpLLVM
{
    class Logger
    {
        private static bool mVerbose;

        /// <summary>
        /// Initializes the logger.
        /// </summary>
        /// <param name="verbose">If verbose or not.</param>
        public static void Init(bool verbose)
        {
            mVerbose = verbose;
        }

        /// <summary>
        /// Logs a new line.
        /// </summary>
        /// <param name="color">The color of the line.</param>
        /// <param name="str">The line text.</param>
        /// <param name="p">Parameters.</param>
        public static void Log(ConsoleColor color, string str, params object[] p)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(str, p);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Logs a new line (if verbose mode is on).
        /// </summary>
        /// <param name="color">The color of the line.</param>
        /// <param name="str">The line text.</param>
        /// <param name="p">Parameters.</param>
        public static void LogVerbose(ConsoleColor color, string str, params object[] p)
        {
            if (mVerbose)
                Log(color, str, p);
        }

        /// <summary>
        /// Logs a new error.
        /// </summary>
        /// <param name="str">The line text.</param>
        /// <param name="p">Parameters.</param>
        public static void LogError(string str, params object[] p)
        {
            Log(ConsoleColor.Red, str, p);
        }

        /// <summary>
        /// Logs a new info.
        /// </summary>
        /// <param name="str">The line text.</param>
        /// <param name="p">Parameters.</param>
        public static void LogInfo(string str, params object[] p)
        {
            Log(ConsoleColor.Gray, str, p);
        }

        /// <summary>
        /// Logs a new detail.
        /// </summary>
        /// <param name="str">The line text.</param>
        /// <param name="p">Parameters.</param>
        public static void LogDetail(string str, params object[] p)
        {
            Log(ConsoleColor.DarkGray, str, p);
        }

        /// <summary>
        /// Logs a new detail (if verbose mode is on).
        /// </summary>
        /// <param name="str">The line text.</param>
        /// <param name="p">Parameters.</param>
        public static void LogDetailVerbose(string str, params object[] p)
        {
            if (mVerbose)
                Log(ConsoleColor.DarkGray, str, p);
        }
    }
}
