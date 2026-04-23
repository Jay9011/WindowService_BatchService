using CoreDAL.ORM;
using CoreDAL.ORM.Extensions;
using Utility.Batch;

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

    public static BatchExecEntity FromBatchList(BatchListDTO dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        return new BatchExecEntity
        {
            Custom1 = dto.CustomValue1,
            Custom2 = dto.CustomValue2,
            Custom3 = dto.CustomValue3,
            Custom4 = dto.CustomValue4,
            Custom5 = dto.CustomValue5,
        };
    }
}
