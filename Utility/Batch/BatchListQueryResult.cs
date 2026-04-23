using System;
using System.Collections.Generic;

namespace Utility.Batch
{
    /// <summary>
    /// Result of executing the configured BatchList stored procedure.
    /// </summary>
    public class BatchListQueryResult
    {
        private static readonly IReadOnlyList<BatchListDTO> EmptyRows = Array.Empty<BatchListDTO>();

        public bool IsSuccess { get; }

        /// <summary>
        /// Rows projected from the first result set of the SP. Empty when the call failed or returned no data.
        /// </summary>
        public IReadOnlyList<BatchListDTO> Rows { get; }

        public int Count => Rows.Count;

        public string Message { get; }

        private BatchListQueryResult(bool isSuccess, IReadOnlyList<BatchListDTO> rows, string message)
        {
            IsSuccess = isSuccess;
            Rows = rows ?? EmptyRows;
            Message = message ?? string.Empty;
        }

        public static BatchListQueryResult Success(IReadOnlyList<BatchListDTO> rows, string message = "") => new BatchListQueryResult(true, rows ?? EmptyRows, message);
        public static BatchListQueryResult Failure(string message) => new BatchListQueryResult(false, EmptyRows, message);
    }
}
