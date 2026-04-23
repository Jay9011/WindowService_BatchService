using BatchService.Models;
using BatchService.Repository.Abstraction;
using BatchService.Repository.Abstraction.Batch;
using CoreDAL.ORM;
using Microsoft.Extensions.Options;
using Utility.Settings;

namespace BatchService.Repository.Batch;

/// <summary>
/// Batch repository that talks to the <c>BatchList</c> table via stored procedures.
/// </summary>
public class BatchRepository : BaseRepository_MsSql, IBatchRepository
{
    #region Constants
    private const string SP_GetBatchList = "BATCH_GetBatchList";
    private const string SP_WriteLog = "BATCH_WriteLog";
    #endregion

    public BatchRepository(IOptionsMonitor<BatchServiceOptions> options, ILogger<BatchRepository> logger) : base(options, logger)
    { }

    public Task<List<BatchListDTO>> GetBatchListAsync(bool includeDisabled = false)
    {
        var param = new BatchListQueryEntity { IncludeDisabled = includeDisabled };

        return ProcDataListAsync<BatchListDTO>(SP_GetBatchList, param);
    }

    public Task<SQLResult> RunBatchAsync(BatchListDTO dto, CancellationToken cancellationToken = default)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.ProcedureName))
        {
            throw new InvalidOperationException($"BatchDefinition(ID={dto.ID}) has no ProcedureName.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var param = BatchListDTO.ToExecEntity(dto);
        return ExecuteProcedureAsync(dto.ProcedureName, param, isReturn: false);
    }

    public Task<SQLResult> WriteLogAsync(BatchLogEntity entry, CancellationToken cancellationToken = default)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));

        cancellationToken.ThrowIfCancellationRequested();

        return ExecuteProcedureAsync(SP_WriteLog, entry, isReturn: false);
    }
}
