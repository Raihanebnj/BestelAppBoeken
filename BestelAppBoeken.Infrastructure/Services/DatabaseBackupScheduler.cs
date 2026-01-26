using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace BestelAppBoeken.Infrastructure.Services
{
    public class DatabaseBackupScheduler : BackgroundService
    {
        private readonly IDatabaseBackupService _backupService;
        private readonly ILogger<DatabaseBackupScheduler> _logger;
        private readonly IConfiguration _configuration;
        private readonly int _retentionDays;
        private readonly TimeSpan _runAtTime;

        public DatabaseBackupScheduler(IDatabaseBackupService backupService, ILogger<DatabaseBackupScheduler> logger, IConfiguration configuration)
        {
            _backupService = backupService;
            _logger = logger;
            _configuration = configuration;

            if (!int.TryParse(_configuration["Backup:RetentionDays"], out _retentionDays))
            {
                _retentionDays = 7; // default 7 days
            }

            var runAt = _configuration["Backup:RunAt"] ?? "02:00"; // default 02:00
            if (!TimeSpan.TryParse(runAt, out _runAtTime))
            {
                _runAtTime = TimeSpan.FromHours(2);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DatabaseBackupScheduler started. Will run daily at {RunAt} and keep backups for {RetentionDays} days.", _runAtTime, _retentionDays);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    var nextRun = now.Date + _runAtTime;
                    if (nextRun <= now)
                    {
                        nextRun = nextRun.AddDays(1);
                    }

                    var delay = nextRun - now;
                    _logger.LogInformation("Next database backup scheduled at {NextRun} (in {Delay}).", nextRun, delay);

                    await Task.Delay(delay, stoppingToken);
                    if (stoppingToken.IsCancellationRequested) break;

                    _logger.LogInformation("Starting scheduled database backup...");
                    string backupPath = await _backupService.CreateBackupAsync();
                    _logger.LogInformation("Scheduled backup created: {BackupPath}", backupPath);

                    PruneOldBackups();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during scheduled database backup");
                    try
                    {
                        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                    }
                    catch (OperationCanceledException) { break; }
                }
            }

            _logger.LogInformation("DatabaseBackupScheduler stopping.");
        }

        private void PruneOldBackups()
        {
            try
            {
                var backups = _backupService.GetAvailableBackups();
                if (backups == null || backups.Count == 0) return;

                var threshold = DateTime.Now.AddDays(-_retentionDays);
                var regex = new Regex(@"bookstore_backup_(\d{8})_(\d{6})\.db", RegexOptions.Compiled);

                foreach (var fileName in backups)
                {
                    try
                    {
                        var match = regex.Match(fileName);
                        if (!match.Success) continue;

                        var dateStr = match.Groups[1].Value; // yyyyMMdd
                        var timeStr = match.Groups[2].Value; // HHmmss

                        var year = int.Parse(dateStr.Substring(0, 4));
                        var month = int.Parse(dateStr.Substring(4, 2));
                        var day = int.Parse(dateStr.Substring(6, 2));
                        var hour = int.Parse(timeStr.Substring(0, 2));
                        var minute = int.Parse(timeStr.Substring(2, 2));
                        var second = int.Parse(timeStr.Substring(4, 2));

                        var timestamp = new DateTime(year, month, day, hour, minute, second);
                        if (timestamp < threshold)
                        {
                            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Backups", fileName);
                            if (File.Exists(fullPath))
                            {
                                File.Delete(fullPath);
                                _logger.LogInformation("Pruned old backup: {File}", fileName);
                            }
                        }
                    }
                    catch (Exception exInner)
                    {
                        _logger.LogWarning(exInner, "Failed to parse or delete backup file {FileName}", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed while pruning old backups");
            }
        }
    }
}
