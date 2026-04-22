using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Utility.Common;
using Utility.DataAccess;
using Utility.Security;

namespace Utility.Settings
{
    /// <summary>
    /// Reads/writes the Batch Service configuration file (appsettings.json).
    /// Preserves sections not owned by the UI (ApplicationInfo, custom keys, etc.).
    /// Encrypts the DB password on write using DPAPI (LocalMachine) and decrypts on read.
    /// </summary>
    public class SettingsFileStore
    {
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        private readonly string _filePath;
        public string FilePath => _filePath;

        public bool FileExists => File.Exists(_filePath);

        /// <summary>
        /// Creates a store pointing to the default configuration file path from <see cref="ConfigPaths"/>.
        /// </summary>
        public SettingsFileStore() : this(ConfigPaths.GetMainConfigFilePath())
        { }

        /// <summary>
        /// Creates a store for a custom file path (useful for tests).
        /// </summary>
        public SettingsFileStore(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path must not be empty.", nameof(filePath));
            }

            _filePath = filePath;
        }

        /// <summary>
        /// Creates the configuration directory and writes a default template if the file does not exist.
        /// </summary>
        public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
        {
            ConfigPaths.EnsureDirectoryExists();

            if (!File.Exists(_filePath))
            {
                await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    if (!File.Exists(_filePath))
                    {
                        var template = BuildDefaultTemplate();
                        await WriteAtomicAsync(template.ToString(Formatting.Indented), cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _lock.Release();
                }
            }
        }

        /// <summary>
        /// Loads the Batch section from the file. Missing sections default to an empty options object.
        /// The returned <see cref="BatchServiceOptions.Database"/>.Password is automatically decrypted.
        /// </summary>
        public async Task<BatchServiceOptions> LoadBatchOptionsAsync(CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var root = await ReadRootAsync(cancellationToken).ConfigureAwait(false);
                var batchToken = root[Keys.Key_Batch];

                BatchServiceOptions options;
                if (batchToken == null || batchToken.Type == JTokenType.Null)
                {
                    options = new BatchServiceOptions();
                }
                else
                {
                    options = batchToken.ToObject<BatchServiceOptions>() ?? new BatchServiceOptions();
                }

                options.Database = options.Database ?? new DbSettingsDTO();
                options.BatchJob = options.BatchJob ?? new BatchJobOptions();

                if (!string.IsNullOrEmpty(options.Database.Password))
                {
                    options.Database.Password = DpapiProtector.Decrypt(options.Database.Password);
                }

                return options;
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException(Resources.Strings.ErrorConfigFileRead, ex);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Replaces only the "Batch" section; all other top-level sections are preserved.
        /// The password is DPAPI-protected before being written.
        /// </summary>
        public async Task SaveBatchOptionsAsync(BatchServiceOptions options, CancellationToken cancellationToken = default)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var root = await ReadRootOrNewAsync(cancellationToken).ConfigureAwait(false);

                var toPersist = CloneForPersistence(options);
                root[Keys.Key_Batch] = JObject.FromObject(toPersist);

                await WriteAtomicAsync(root.ToString(Formatting.Indented), cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException(Resources.Strings.ErrorConfigFileWrite, ex);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Loads the "Logging:LogLevel" dictionary. Missing sections return an empty map.
        /// </summary>
        public async Task<Dictionary<string, string>> LoadLogLevelsAsync(CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var root = await ReadRootAsync(cancellationToken).ConfigureAwait(false);
                var levels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var logLevelObj = root[Keys.Key_Logging]?[Keys.Key_LogLevel] as JObject;

                if (logLevelObj != null)
                {
                    foreach (var kv in logLevelObj)
                    {
                        levels[kv.Key] = kv.Value?.ToString() ?? string.Empty;
                    }
                }

                return levels;
            }
            catch (Exception ex)
            {
                throw new IOException(Resources.Strings.ErrorConfigFileRead, ex);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Replaces the "Logging:LogLevel" dictionary; other Logging keys are preserved.
        /// </summary>
        public async Task SaveLogLevelsAsync(IDictionary<string, string> levels, CancellationToken ct = default)
        {
            if (levels == null) throw new ArgumentNullException(nameof(levels));

            await _lock.WaitAsync(ct).ConfigureAwait(false);

            try
            {
                var root = await ReadRootOrNewAsync(ct).ConfigureAwait(false);

                var logging = root[Keys.Key_Logging] as JObject ?? new JObject();
                var logLevelObj = new JObject();

                foreach (var kv in levels)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key))
                    {
                        continue;
                    }

                    logLevelObj[kv.Key] = kv.Value ?? string.Empty;
                }

                logging[Keys.Key_LogLevel] = logLevelObj;
                root[Keys.Key_Logging] = logging;

                await WriteAtomicAsync(root.ToString(Formatting.Indented), ct).ConfigureAwait(false);
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException(Resources.Strings.ErrorConfigFileWrite, ex);
            }
            finally
            {
                _lock.Release();
            }
        }

        #region Private Methods

        /// <summary>
        /// Clones the source options object for persistence.
        /// The password is DPAPI-protected before being written.
        /// </summary>
        /// <param name="source">The source options object to clone.</param>
        /// <returns>The cloned options object.</returns>
        private BatchServiceOptions CloneForPersistence(BatchServiceOptions source)
        {
            var clone = new BatchServiceOptions
            {
                Database = new DbSettingsDTO
                {
                    Server = source.Database?.Server ?? string.Empty,
                    Port = source.Database?.Port,
                    Database = source.Database?.Database ?? string.Empty,
                    UserId = source.Database?.UserId ?? string.Empty,
                    Password = DpapiProtector.Encrypt(source.Database?.Password ?? string.Empty),
                    IntegratedSecurity = source.Database?.IntegratedSecurity ?? false,
                },
                BatchJob = new BatchJobOptions
                {
                    ProcedureName = source.BatchJob?.ProcedureName ?? string.Empty,
                    PollingInterval = source.BatchJob?.PollingInterval ?? TimeSpan.FromSeconds(10),
                },
            };

            return clone;
        }

        /// <summary>
        /// Reads the root object from the file.
        /// If the file does not exist, a default template is returned.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The root object.</returns>
        private async Task<JObject> ReadRootAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(_filePath))
            {
                return BuildDefaultTemplate();
            }

            string raw;
            using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs, Encoding.UTF8))
            {
                raw = await sr.ReadToEndAsync().ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(raw))
            {
                return BuildDefaultTemplate();
            }

            return JObject.Parse(raw);
        }

        /// <summary>
        /// Reads the root object from the file.
        /// If the file does not exist, a default template is returned.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The root object.</returns>
        private async Task<JObject> ReadRootOrNewAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await ReadRootAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException)
            {
                return BuildDefaultTemplate();
            }
        }

        /// <summary>
        /// Writes the content to the file atomically.
        /// </summary>
        /// <param name="content">The content to write.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task WriteAtomicAsync(string content, CancellationToken cancellationToken)
        {
            ConfigPaths.EnsureDirectoryExists();

            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = _filePath + ".tmp";
            var backupPath = _filePath + ".bak";

            var bytes = new UTF8Encoding(false).GetBytes(content);

            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await fs.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
                await fs.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            if (File.Exists(_filePath))
            {
                File.Replace(tempPath, _filePath, backupPath, ignoreMetadataErrors: true);
                try
                {
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                }
                catch
                {
                }
            }
            else
            {
                File.Move(tempPath, _filePath);
            }
        }

        /// <summary>
        /// Builds the default template for the configuration file.
        /// </summary>
        /// <returns>The default template.</returns>
        private static JObject BuildDefaultTemplate()
        {
            return new JObject
            {
                [Keys.Key_ApplicationInfo] = new JObject
                {
                    [Keys.Key_CompanyName] = Keys.Key_Company,
                    [Keys.Key_Tel] = Keys.Value_Tel,
                },
                [Keys.Key_Logging] = new JObject
                {
                    [Keys.Key_LogLevel] = new JObject
                    {
                        [Keys.Key_LogLevel_Default] = Keys.Value_LogLevel_Information,
                        [Keys.Key_LogLevel_HostingLifetime] = Keys.Value_LogLevel_Information,
                    },
                },
                [Keys.Key_Batch] = new JObject
                {
                    [Keys.Key_Database] = new JObject
                    {
                        [Keys.Key_Server] = string.Empty,
                        [Keys.Key_Port] = Keys.Value_Port,
                        [Keys.Key_Database] = string.Empty,
                        [Keys.Key_UserId] = string.Empty,
                        [Keys.Key_Password] = string.Empty,
                        [Keys.Key_IntegratedSecurity] = false,
                    },
                    [Keys.Key_BatchJob] = new JObject
                    {
                        [Keys.Key_ProcedureName] = string.Empty,
                        [Keys.Key_PollingInterval] = Keys.Value_PollingInterval,
                    },
                },
            };
        }

        #endregion
    }
}
