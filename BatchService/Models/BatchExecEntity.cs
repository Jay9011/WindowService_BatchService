using CoreDAL.ORM;
using CoreDAL.ORM.Extensions;

namespace BatchService.Models;

/// <summary>
/// used to EXEC a batch <c>ProcedureName</c> with up to user-defined arguments <c>@Custom1 .. @Custom5</c>.
/// </summary>
public class BatchExecEntity : SQLParam
{
    [DbParameter]
    public string? Custom1 { get; set; }
    [DbParameter]
    public string? Custom2 { get; set; }
    [DbParameter]
    public string? Custom3 { get; set; }
    [DbParameter]
    public string? Custom4 { get; set; }
    [DbParameter]
    public string? Custom5 { get; set; }
}
