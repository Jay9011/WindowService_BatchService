namespace BatchService.Models;

/// <summary>
/// Last-known result of a batch. Mirrors <c>BatchList.LastResult</c>
/// </summary>
public enum EBatchResult
{
    Unknown = 0,
    Success = 1,
    Failure = 2,
}