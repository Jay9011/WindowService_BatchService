namespace Utility.Settings
{
    /// <summary>
    /// Database Settings DTO
    /// </summary>
    public class DbSettingsDTO
    {
        public string Server { get; set; } = string.Empty;
        /// <summary>
        /// Database Port (Default port is SQL Server default port)
        /// </summary>
        public int? Port { get; set; } = 1433;
        public string Database { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IntegratedSecurity { get; set; } = false;
    }
}
