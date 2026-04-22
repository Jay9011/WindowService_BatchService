namespace Utility.DataAccess
{
    /// <summary>
    /// Result of a database connection test.
    /// Keeps the UI layer free of CoreDAL specific types.
    /// </summary>
    public class DbConnectionTestResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }

        private DbConnectionTestResult(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message ?? string.Empty;
        }

        public static DbConnectionTestResult Success(string message) => new DbConnectionTestResult(true, message);

        public static DbConnectionTestResult Failure(string message) => new DbConnectionTestResult(false, message);
    }
}
