namespace Utility.Settings
{
    /// <summary>
    /// Batch Service Options
    /// </summary>
    public class BatchServiceOptions
    {
        public DbSettingsDTO Database { get; set; } = new DbSettingsDTO();
        public BatchJobOptions BatchJob { get; set; } = new BatchJobOptions();
    }
}
