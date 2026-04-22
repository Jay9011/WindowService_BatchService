namespace Utility.Common
{
    public static class Keys
    {
        public const string Key_ApplicationInfo = "ApplicationInfo";

        /// <summary>
        /// ProgramData subfolder for SECUiDEA
        /// </summary>
        public const string Key_Company = "SECUiDEA";

        /// <summary>
        /// Batch Service Options Key
        /// </summary>
        public const string Key_Batch = "Batch";

        /// <summary>
        /// ProgramData subfolder for Batch Service
        /// </summary>
        public const string Key_ProductFolder = "BatchService";

        /// <summary>
        /// Subfolder under the product directory where log files are written.
        /// Final path: %ProgramData%\{Key_Company}\{Key_ProductFolder}\{Key_LogsFolder}\
        /// </summary>
        public const string Key_LogsFolder = "Logs";

        /// <summary>
        /// Service Name Key
        /// </summary>
        public const string Key_ServiceName = "SECUiDEA.BatchService";

        #region AppSettings Keys

        public const string MainConfigFileName = "appsettings.json";

        public const string DevelopmentConfigFileName = "appsettings.Development.json";

        #endregion

        #region Database Keys

        public const string Key_Database = "Database";
        public const string Key_Server = "Server";
        public const string Key_Port = "Port";
        public const int Value_Port = 1433;
        public const string Key_UserId = "UserId";
        public const string Key_Password = "Password";
        public const string Key_IntegratedSecurity = "IntegratedSecurity";

        #endregion

        #region Batch Keys

        public const string Key_BatchJob = "BatchJob";
        public const string Key_ProcedureName = "ProcedureName";
        public const string Key_PollingInterval = "PollingInterval";
        public const string Value_PollingInterval = "00:00:10";

        #endregion

        #region Logging Keys

        public const string Key_LogLevel_Default = "Default";
        public const string Key_Logging = "Logging";
        public const string Key_LogLevel = "LogLevel";
        public const string Value_LogLevel_Information = "Information";
        public const string Value_LogLevel_Trace = "Trace";
        public const string Value_LogLevel_Debug = "Debug";
        public const string Value_LogLevel_Warning = "Warning";
        public const string Value_LogLevel_Error = "Error";
        public const string Value_LogLevel_Critical = "Critical";
        public const string Value_LogLevel_None = "None";

        #endregion

        public const string Key_LogLevel_HostingLifetime = "Microsoft.Hosting.Lifetime";

        #region Company Keys

        public const string Key_CompanyName = "CompanyName";
        public const string Key_Tel = "Tel";
        public const string Value_Tel = "02-6267-1622";

        #endregion
    }
}
