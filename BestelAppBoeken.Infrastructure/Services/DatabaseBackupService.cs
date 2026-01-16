using Microsoft.Extensions.Logging;

namespace BestelAppBoeken.Infrastructure.Services
{
    public interface IDatabaseBackupService
    {
        Task<string> CreateBackupAsync();
        Task<bool> RestoreBackupAsync(string backupFilePath);
        List<string> GetAvailableBackups();
    }

    public class DatabaseBackupService : IDatabaseBackupService
    {
        private readonly string _databasePath;
        private readonly string _backupDirectory;
        private readonly ILogger<DatabaseBackupService> _logger;

        public DatabaseBackupService(ILogger<DatabaseBackupService> logger)
        {
            _logger = logger;
            _databasePath = Path.Combine(Directory.GetCurrentDirectory(), "bookstore.db");
            _backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
            
            // Maak backup directory aan als deze niet bestaat
            if (!Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
                _logger.LogInformation($"?? Backup directory aangemaakt: {_backupDirectory}");
            }
        }

        public async Task<string> CreateBackupAsync()
        {
            try
            {
                if (!File.Exists(_databasePath))
                {
                    throw new FileNotFoundException("Database bestand niet gevonden", _databasePath);
                }

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"bookstore_backup_{timestamp}.db";
                var backupFilePath = Path.Combine(_backupDirectory, backupFileName);

                // Kopieer database bestand asynchroon
                await Task.Run(() => File.Copy(_databasePath, backupFilePath, overwrite: false));

                _logger.LogInformation($"? Database backup succesvol aangemaakt: {backupFileName}");
                _logger.LogInformation($"?? Alle backups worden bewaard (geen automatische cleanup)");

                return backupFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Fout bij aanmaken database backup");
                throw;
            }
        }

        public async Task<bool> RestoreBackupAsync(string backupFilePath)
        {
            try
            {
                if (!File.Exists(backupFilePath))
                {
                    throw new FileNotFoundException("Backup bestand niet gevonden", backupFilePath);
                }

                // Maak eerst een veiligheids backup van huidige database
                var safetyBackup = await CreateBackupAsync();
                _logger.LogInformation($"?? Veiligheids backup aangemaakt: {Path.GetFileName(safetyBackup)}");

                // Herstel backup
                await Task.Run(() => File.Copy(backupFilePath, _databasePath, overwrite: true));

                _logger.LogInformation($"? Database succesvol hersteld van: {Path.GetFileName(backupFilePath)}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Fout bij herstellen database backup");
                return false;
            }
        }

        public List<string> GetAvailableBackups()
        {
            if (!Directory.Exists(_backupDirectory))
                return new List<string>();

            return Directory.GetFiles(_backupDirectory, "bookstore_backup_*.db")
                .OrderByDescending(f => f)
                .Select(f => Path.GetFileName(f))
                .ToList();
        }
    }
}
