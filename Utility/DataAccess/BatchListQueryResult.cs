namespace Utility.DataAccess
{
    /// <summary>
    /// Result of executing the configured BatchList stored procedure.
    /// Keeps the UI layer free of CoreDAL specific types.
    /// </summary>
    public class BatchListQueryResult
    {
        public bool IsSuccess { get; }

        /// <summary>
        /// Number of rows returned by the SP (first result set). 0 when not applicable.
        /// </summary>
        public int Count { get; }

        public string Message { get; }

        private BatchListQueryResult(bool isSuccess, int count, string message)
        {
            IsSuccess = isSuccess;
            Count = count;
            Message = message ?? string.Empty;
        }

        public static BatchListQueryResult Success(int count, string message = "") => new BatchListQueryResult(true, count, message);

        public static BatchListQueryResult Failure(string message) => new BatchListQueryResult(false, 0, message);
    }
}
