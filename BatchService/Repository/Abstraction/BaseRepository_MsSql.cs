using CoreDAL.Configuration;
using CoreDAL.DALs.Interface;
using CoreDAL.ORM;
using CoreDAL.ORM.Extensions;
using CoreDAL.ORM.Interfaces;
using Microsoft.Extensions.Options;
using Utility.Settings;

namespace BatchService.Repository.Abstraction;

/// <summary>
/// MSSQL base for all batch-service repositories.
/// </summary>
public abstract class BaseRepository_MsSql
{
    protected readonly ICoreDAL _coreDAL;
    protected readonly IOptionsMonitor<BatchServiceOptions> _options;
    protected readonly ILogger _logger;

    protected BaseRepository_MsSql(IOptionsMonitor<BatchServiceOptions> options, ILogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _coreDAL = DbDALFactory.CreateCoreDal(DatabaseType.MSSQL);
    }

    /// <summary>
    /// Returns the current MSSQL connection string built from the latest options snapshot.
    /// </summary>
    protected string GetConnectionString()
    {
        var database = _options.CurrentValue?.Database ?? throw new InvalidOperationException("Batch Database settings are not configured.");

        var info = database.ToConnectionInfo();
        if (!info.Validate(out var message))
        {
            throw new InvalidOperationException(message);
        }

        return info.ToConnectionString();
    }

    /// <summary>
    /// Executes a stored procedure with an <see cref="ISQLParam"/> model.
    /// Caller owns the returned <see cref="SQLResult"/> (and must dispose it).
    /// </summary>
    protected Task<SQLResult> ExecuteProcedureAsync(string procedureName, ISQLParam? parameters = null, bool isReturn = false)
    {
        if (string.IsNullOrWhiteSpace(procedureName))
        {
            throw new ArgumentException("Procedure name is required.", nameof(procedureName));
        }

        return _coreDAL.ExecuteProcedureAsync(GetConnectionString(), procedureName, parameters, isReturn);
    }

    /// <summary>
    /// Executes a stored procedure and parses the first result set to <c>List&lt;T&gt;</c>.
    /// An empty or missing result set returns an empty list.
    /// </summary>
    protected async Task<List<T>> ProcDataListAsync<T>(string procedureName, ISQLParam? parameters = null) where T : class, new()
    {
        using var result = await ExecuteProcedureAsync(procedureName, parameters, isReturn: false);

        return DataListParsing<T>(result, procedureName);
    }

    /// <summary>
    /// Parses the result of a stored procedure execution to a list of objects.
    /// </summary>
    /// <typeparam name="T">The type of the objects to map.</typeparam>
    /// <param name="result">The result of the stored procedure execution.</param>
    /// <param name="procedureName">The name of the stored procedure.</param>
    /// <returns>A list of objects of type <typeparamref name="T"/>.</returns>
    private List<T> DataListParsing<T>(SQLResult result, string procedureName) where T : class, new()
    {
        if (!result.IsSuccess)
        {
            _logger.LogWarning("{Procedure} failed: {Message}", procedureName, result.Message);
            return new List<T>();
        }

        if (result.DataSet == null || result.DataSet.Tables.Count == 0)
        {
            return new List<T>();
        }

        var table = result.DataSet.Tables[0];
        if (table.Rows.Count == 0)
        {
            return new List<T>();
        }

        return table.ToObject<T>().ToList();
    }
}
