using Library.Infra.Config;
using Library.Infra.Exceptions;
using Microsoft.Data.SqlClient;

namespace Library.Infra.Helpers
{
    /// <summary>
    /// Provides utility functions for data loading operations.
    /// </summary>
    public static class DataBaseUtilities
    {
        /// <summary>
        /// Asynchronously truncates the specified table using the provided configuration settings.
        /// </summary>
        /// <param name="config">The configuration containing the connection string and the name of the table to be truncated.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ConfigException">Thrown when the provided configuration is null, or when required properties such as the connection string or table name are missing or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when there is an error executing the truncate table command, including connection issues or SQL command execution errors.</exception>
        public static async Task TruncateTableAsync(IDataBaseConfig config)
        {
            // Checks if the configuration and necessary properties have been provided.
            if (config == null) throw new ConfigException("Config should not be null.", nameof(config), null);
            if (string.IsNullOrWhiteSpace(config.ConnectionString)) throw new ConfigException($"{nameof(config.ConnectionString)} is required.", nameof(config), nameof(config.ConnectionString));
            if (string.IsNullOrWhiteSpace(config.TableName)) throw new ConfigException($"{nameof(config.TableName)} is required.", nameof(config), nameof(config.TableName));

            var query = $"TRUNCATE TABLE {config.TableName}";

            try
            {
                using var connection = new SqlConnection(config.ConnectionString);
                await connection.OpenAsync();
                using var command = new SqlCommand(query, connection);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error truncating table {config.TableName}.", ex);
            }
        }

        /// <summary>
        /// Truncates the specified table using the provided configuration settings.
        /// </summary>
        /// <param name="config">The configuration containing the connection string and the name of the table to be truncated.</param>
        /// <exception cref="ConfigException">Thrown when the provided configuration is null, or when required properties such as the connection string or table name are missing or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when there is an error executing the truncate table command, including connection issues or SQL command execution errors.</exception>
        public static void TruncateTable(IDataBaseConfig config)
        {
            // Checks if the configuration and necessary properties have been provided.
            if (config == null) throw new ConfigException("Config should not be null.", nameof(config), null);
            if (string.IsNullOrWhiteSpace(config.ConnectionString)) throw new ConfigException($"{nameof(config.ConnectionString)} is required.", nameof(config), nameof(config.ConnectionString));
            if (string.IsNullOrWhiteSpace(config.TableName)) throw new ConfigException($"{nameof(config.TableName)} is required.", nameof(config), nameof(config.TableName));

            var query = $"TRUNCATE TABLE {config.TableName}";

            try
            {
                using var connection = new SqlConnection(config.ConnectionString);
                connection.Open();
                using var command = new SqlCommand(query, connection);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error truncating table {config.TableName}.", ex);
            }
        }

    }
}
