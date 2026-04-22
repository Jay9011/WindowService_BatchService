using CoreDAL.Configuration.Models;

namespace Utility.Settings
{
    public static class DbSettingsMapper
    {
        public static MsSqlConnectionInfo ToConnectionInfo(this DbSettingsDTO dto)
        {
            return new MsSqlConnectionInfo
            {
                Server = dto.Server,
                Port = dto.Port,
                Database = dto.Database,
                UserId = dto.UserId,
                Password = dto.Password,
                IntegratedSecurity = dto.IntegratedSecurity,
            };
        }
    }
}
