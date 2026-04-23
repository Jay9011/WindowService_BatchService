using CoreDAL.ORM;
using CoreDAL.ORM.Extensions;

namespace BatchService.Models;

/// <summary>
/// Input parameters for the <c>BATCH_GetBatchList</c> stored procedure.
/// </summary>
public class BatchListQueryEntity : SQLParam
{
    /// <summary>
    /// When <c>true</c>, rows with <c>IsEnabled = 0</c> are included in the result set.
    /// Maps to the SP's <c>@IncludeDisabled BIT</c> argument (default <c>0</c> on the server side).
    /// </summary>
    [DbParameter]
    public bool? IncludeDisabled { get; set; }
}
