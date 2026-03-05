// MIT License
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ForamEcoQS
{
    public partial class UpdateDownloadForm : Form
    {
        private readonly string _downloadUrl;
        private readonly string _destinationPath;
        private readonly string _fileName;
        private string? _extractedInstallerPath;
        private string? _extractionDirectory;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private bool _downloadCompleted;

        public UpdateDownloadForm(string downloadUrl)
        {
            InitializeComponent();
            _downloadUrl = downloadUrl;
            _destinationPath = Path.Combine(Path.GetTempPath(), GetFileName(downloadUrl));
            _fileName = Path.GetFileName(_destinationPath);
            fileNameLabel.Text = $"File: {_fileName}";
            AddLog($"URL download: {_downloadUrl}");
            AddLog($"Percorso destinazione: {_destinationPath}");
        }

        private static string GetFileName(string url)
        {
            try
            {
                var uri = new Uri(url);
                var fileName = Path.GetFileName(uri.LocalPath);
                if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains("?"))
                {
                    return "ForamEcoQSInstaller.zip";
                }

                if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".zip";
                }

                return fileName;
            }
            catch
            {
                return "ForamEcoQSInstaller.zip";
            }
        }

        private async void UpdateDownloadForm_Shown(object sender, EventArgs e)
        {
            await StartDownloadAsync();
        }

        private async Task StartDownloadAsync()
        {
            var launchInstaller = false;

            try
            {
                statusLabel.Text = $"Preparazione download {_fileName}...";
                AddLog("Inizio download...");
                using var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 10
                };
                using var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(30) };
                client.DefaultRequestHeaders.Add("User-Agent", "ForamEcoQS-Updater/1.0 (Windows; .NET)");
                client.DefaultRequestHeaders.Add("Accept", "*/*");
                using var response = await client.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead, _cancellationTokenSource.Token);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength;
                if (totalBytes == null)
                {
                    downloadProgressBar.Style = ProgressBarStyle.Marquee;
                }

                await using (var contentStream = await response.Content.ReadAsStreamAsync(_cancellationTokenSource.Token))
                await using (var fileStream = new FileStream(_destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var buffer = new byte[81920];
                    long totalRead = 0;
                    var lastReported = -1;

                    while (true)
                    {
                        var read = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), _cancellationTokenSource.Token);
                        if (read == 0)
                        {
                            break;
                        }

                        await fileStream.WriteAsync(buffer.AsMemory(0, read), _cancellationTokenSource.Token);
                        totalRead += read;

                        if (totalBytes.HasValue && totalBytes.Value > 0)
                        {
                            var percent = (int)((totalRead * 100) / totalBytes.Value);
                            percent = Math.Min(100, Math.Max(0, percent));
                            if (percent != lastReported)
                            {
                                lastReported = percent;
                                downloadProgressBar.Style = ProgressBarStyle.Continuous;
                                downloadProgressBar.Value = percent;
                                statusLabel.Text = $"Scaricamento {_fileName}... {percent}%";
                                AddLog($"Avanzamento: {percent}% (scaricati {totalRead / 1024 / 1024} MB)");
                            }
                        }
                        else
                        {
                            statusLabel.Text = $"Scaricati {totalRead / (1024 * 1024)} MB di {_fileName}...";
                        }
                    }
                }

                downloadProgressBar.Value = downloadProgressBar.Maximum;
                statusLabel.Text = "Download completato. Estrazione in corso...";
                AddLog("Download completato, avvio estrazione pacchetto");
                _downloadCompleted = true;
                cancelButton.Enabled = false;

                _extractedInstallerPath = await ExtractInstallerAsync();
                launchInstaller = _extractedInstallerPath != null;
            }
            catch (OperationCanceledException)
            {
                statusLabel.Text = "Download annullato.";
                AddLog("Download annullato dall'utente, rimozione file parziale");
                TryDeleteInstaller();
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Errore durante il download.";
                AddLog($"Errore: {ex.Message}");
                MessageBox.Show($"Impossibile scaricare l'aggiornamento:\n{ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                TryDeleteInstaller();
            }
            finally
            {
                cancelButton.Enabled = false;
                closeButton.Enabled = true;
                if (!_downloadCompleted)
                {
                    closeButton.Text = "Chiudi";
                }
            }

            if (launchInstaller && _extractedInstallerPath != null)
            {
                LaunchInstallerAndExit(_extractedInstallerPath);
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            cancelButton.Enabled = false;
            statusLabel.Text = "Annullamento in corso...";
            _cancellationTokenSource.Cancel();
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            if (_downloadCompleted && _extractedInstallerPath != null)
            {
                LaunchInstallerAndExit(_extractedInstallerPath);
            }
            else
            {
                Close();
            }
        }

        private async Task<string?> ExtractInstallerAsync()
        {
            try
            {
                _extractionDirectory = Path.Combine(Path.GetTempPath(), $"ForamEcoQSUpdate_{Guid.NewGuid():N}");
                Directory.CreateDirectory(_extractionDirectory);

                AddLog($"Estrazione in {_extractionDirectory}");
                statusLabel.Text = "Estrazione file...";

                using var archive = ZipFile.OpenRead(_destinationPath);
                var exeEntry = archive.Entries
                    .Where(e => !string.IsNullOrWhiteSpace(e.Name))
                    .FirstOrDefault(e => e.FullName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

                if (exeEntry == null)
                {
                    throw new InvalidOperationException("Nessun file .exe trovato nell'archivio di aggiornamento.");
                }

                var targetPath = Path.Combine(_extractionDirectory, exeEntry.Name);
                await using (var entryStream = exeEntry.Open())
                await using (var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await entryStream.CopyToAsync(fileStream, _cancellationTokenSource.Token);
                }

                AddLog($"Estratto installer: {targetPath}");
                statusLabel.Text = "Estrazione completata. Avvio dell'installer...";
                return targetPath;
            }
            catch (OperationCanceledException)
            {
                statusLabel.Text = "Estrazione annullata.";
                AddLog("Estrazione annullata dall'utente");
                TryDeleteInstaller();
                return null;
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Errore durante l'estrazione.";
                AddLog($"Errore estrazione: {ex.Message}");
                MessageBox.Show($"Impossibile estrarre l'aggiornamento:\n{ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                TryDeleteInstaller();
                return null;
            }
        }

        private void LaunchInstallerAndExit(string installerPath)
        {
            try
            {
                AddLog("Avvio dell'installer...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = installerPath,
                    UseShellExecute = true
                });
                Application.Exit();
            }
            catch (Exception ex)
            {
                AddLog($"Impossibile avviare l'installer: {ex.Message}");
                MessageBox.Show($"Impossibile avviare l'installer:\n{ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Close();
            }
        }

        private void AddLog(string message)
        {
            if (logListBox.InvokeRequired)
            {
                _ = logListBox.BeginInvoke(new Action<string>(AddLog), message);
                return;
            }

            logListBox.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            logListBox.TopIndex = Math.Max(0, logListBox.Items.Count - 1);
        }

        private void TryDeleteInstaller()
        {
            try
            {
                if (File.Exists(_destinationPath))
                {
                    File.Delete(_destinationPath);
                }

                if (!string.IsNullOrWhiteSpace(_extractionDirectory) && Directory.Exists(_extractionDirectory))
                {
                    Directory.Delete(_extractionDirectory, true);
                }
            }
            catch
            {
                // Ignored
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_extractedInstallerPath == null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            base.OnFormClosing(e);
        }
    }
}
