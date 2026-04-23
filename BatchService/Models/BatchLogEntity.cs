using CoreDAL.ORM;
using CoreDAL.ORM.Extensions;

namespace BatchService.Models;

/// <summary>
/// Input parameters for the <c>BATCH_WriteLog</c> stored procedure.
/// One row per batch execution.
/// </summary>
public class BatchLogEntity : SQLParam
{
    [DbParameter]
    public int? BatchID { get; set; }

    [DbParameter]
    public DateTime? StartedAt { get; set; }

    [DbParameter]
    public DateTime? EndedAt { get; set; }

    [DbParameter]
    public bool? IsSuccess { get; set; }

    [DbParameter]
    public string? Message { get; set; }
}
