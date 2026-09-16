//MIT License
// UpdateDownloadDialog.cs - Downloads and unpacks an update package.
//
// The original was Windows only (it always looked for an .exe installer inside the archive).
// The port picks the payload that matches the running operating system and, where an installer
// cannot simply be launched, tells the user where the update was unpacked.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using ForamEcoQS.Compat;
using MessageBox = ForamEcoQS.Compat.MessageBox;

namespace ForamEcoQS
{
    public class UpdateDownloadDialog : Dialog
    {
        private readonly string _downloadUrl;
        private readonly string _destinationPath;
        private readonly string _fileName;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly Label _statusLabel;
        private readonly Label _fileNameLabel;
        private readonly ProgressBar _downloadProgressBar;
        private readonly Button _cancelButton;
        private readonly Button _closeButton;
        private readonly ListBox _logListBox;

        private string _extractedInstallerPath;
        private string _extractionDirectory;
        private bool _downloadCompleted;

        public UpdateDownloadDialog(string downloadUrl)
        {
            _downloadUrl = downloadUrl;
            _destinationPath = Path.Combine(Path.GetTempPath(), GetFileName(downloadUrl));
            _fileName = Path.GetFileName(_destinationPath);

            Title = "Aggiornamento in corso";
            ClientSize = new Size(520, 260);
            Resizable = false;
            this.Prepare();

            _statusLabel = new Label { Text = "Scaricamento aggiornamento..." };
            _fileNameLabel = new Label { Text = $"File: {_fileName}" };
            _downloadProgressBar = new ProgressBar { MinValue = 0, MaxValue = 100 };
            _logListBox = new ListBox { Height = 90 };

            _cancelButton = new Button { Text = "Annulla", Width = 110 };
            _cancelButton.Click += CancelButton_Click;

            _closeButton = new Button { Text = "Chiudi", Width = 110, Enabled = false };
            _closeButton.Click += CloseButton_Click;

            AbortButton = _cancelButton;

            Content = new TableLayout
            {
                Padding = new Padding(16, 15),
                Spacing = new Size(0, 8),
                Rows =
                {
                    new TableRow(_statusLabel),
                    new TableRow(_fileNameLabel),
                    new TableRow(_downloadProgressBar),
                    new TableRow(_logListBox) { ScaleHeight = true },
                    new TableRow(new TableCell(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalContentAlignment = HorizontalAlignment.Right,
                        Items = { _cancelButton, _closeButton }
                    }, true))
                }
            };

            AddLog($"URL download: {_downloadUrl}");
            AddLog($"Percorso destinazione: {_destinationPath}");

            Shown += async (s, e) => await StartDownloadAsync();
            Closing += (s, e) =>
            {
                if (_extractedInstallerPath == null && !_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }
            };
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
            catch (Exception)
            {
                return "ForamEcoQSInstaller.zip";
            }
        }

        private async Task StartDownloadAsync()
        {
            bool launchInstaller = false;

            try
            {
                _statusLabel.Text = $"Preparazione download {_fileName}...";
                AddLog("Inizio download...");

                using var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 10
                };
                using var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(30) };
                client.DefaultRequestHeaders.Add("User-Agent", "ForamEcoQS-Updater/2.0");
                client.DefaultRequestHeaders.Add("Accept", "*/*");

                using var response = await client.GetAsync(
                    _downloadUrl, HttpCompletionOption.ResponseHeadersRead, _cancellationTokenSource.Token);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength;
                if (totalBytes == null)
                {
                    _downloadProgressBar.Indeterminate = true;
                }

                await using (var contentStream = await response.Content.ReadAsStreamAsync(_cancellationTokenSource.Token))
                await using (var fileStream = new FileStream(_destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int lastReported = -1;

                    while (true)
                    {
                        int read = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), _cancellationTokenSource.Token);
                        if (read == 0)
                        {
                            break;
                        }

                        await fileStream.WriteAsync(buffer.AsMemory(0, read), _cancellationTokenSource.Token);
                        totalRead += read;

                        if (totalBytes.HasValue && totalBytes.Value > 0)
                        {
                            int percent = (int)((totalRead * 100) / totalBytes.Value);
                            percent = Math.Min(100, Math.Max(0, percent));
                            if (percent != lastReported)
                            {
                                lastReported = percent;
                                _downloadProgressBar.Indeterminate = false;
                                _downloadProgressBar.Value = percent;
                                _statusLabel.Text = $"Scaricamento {_fileName}... {percent}%";
                                AddLog($"Avanzamento: {percent}% (scaricati {totalRead / 1024 / 1024} MB)");
                            }
                        }
                        else
                        {
                            _statusLabel.Text = $"Scaricati {totalRead / (1024 * 1024)} MB di {_fileName}...";
                        }
                    }
                }

                _downloadProgressBar.Indeterminate = false;
                _downloadProgressBar.Value = _downloadProgressBar.MaxValue;
                _statusLabel.Text = "Download completato. Estrazione in corso...";
                AddLog("Download completato, avvio estrazione pacchetto");
                _downloadCompleted = true;
                _cancelButton.Enabled = false;

                _extractedInstallerPath = await ExtractInstallerAsync();
                launchInstaller = _extractedInstallerPath != null;
            }
            catch (OperationCanceledException)
            {
                _statusLabel.Text = "Download annullato.";
                AddLog("Download annullato dall'utente, rimozione file parziale");
                TryDeleteInstaller();
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Errore durante il download.";
                AddLog($"Errore: {ex.Message}");
                MessageBox.Show($"Impossibile scaricare l'aggiornamento:\n{ex.Message}", "Errore",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                TryDeleteInstaller();
            }
            finally
            {
                _cancelButton.Enabled = false;
                _closeButton.Enabled = true;
            }

            if (launchInstaller && _extractedInstallerPath != null)
            {
                LaunchInstallerAndExit(_extractedInstallerPath);
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            _cancelButton.Enabled = false;
            _statusLabel.Text = "Annullamento in corso...";
            _cancellationTokenSource.Cancel();
        }

        private void CloseButton_Click(object sender, EventArgs e)
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

        /// <summary>
        /// Extension candidates for the current operating system, most specific first.
        /// </summary>
        private static IReadOnlyList<string> InstallerExtensions()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new[] { ".exe", ".msi" };
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new[] { ".pkg", ".dmg" };
            }
            return new[] { ".appimage", ".deb", ".rpm", ".sh" };
        }

        private async Task<string> ExtractInstallerAsync()
        {
            try
            {
                _extractionDirectory = Path.Combine(Path.GetTempPath(), $"ForamEcoQSUpdate_{Guid.NewGuid():N}");
                Directory.CreateDirectory(_extractionDirectory);

                AddLog($"Estrazione in {_extractionDirectory}");
                _statusLabel.Text = "Estrazione file...";

                using var archive = ZipFile.OpenRead(_destinationPath);
                var candidates = archive.Entries.Where(e => !string.IsNullOrWhiteSpace(e.Name)).ToList();

                ZipArchiveEntry installerEntry = null;
                foreach (string extension in InstallerExtensions())
                {
                    installerEntry = candidates.FirstOrDefault(
                        e => e.FullName.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
                    if (installerEntry != null)
                    {
                        break;
                    }
                }

                if (installerEntry == null)
                {
                    // No installer for this platform: unpack everything and point the user at it.
                    archive.ExtractToDirectory(_extractionDirectory, overwriteFiles: true);
                    AddLog("Nessun installer per questa piattaforma; pacchetto estratto.");
                    _statusLabel.Text = "Aggiornamento estratto.";
                    MessageBox.Show(
                        "L'aggiornamento non contiene un installer per questo sistema operativo.\n\n" +
                        $"Il pacchetto è stato estratto in:\n{_extractionDirectory}",
                        "Aggiornamento scaricato", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return null;
                }

                var targetPath = Path.Combine(_extractionDirectory, installerEntry.Name);
                await using (var entryStream = installerEntry.Open())
                await using (var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await entryStream.CopyToAsync(fileStream, _cancellationTokenSource.Token);
                }

                MakeExecutableIfNeeded(targetPath);

                AddLog($"Estratto installer: {targetPath}");
                _statusLabel.Text = "Estrazione completata. Avvio dell'installer...";
                return targetPath;
            }
            catch (OperationCanceledException)
            {
                _statusLabel.Text = "Estrazione annullata.";
                AddLog("Estrazione annullata dall'utente");
                TryDeleteInstaller();
                return null;
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Errore durante l'estrazione.";
                AddLog($"Errore estrazione: {ex.Message}");
                MessageBox.Show($"Impossibile estrarre l'aggiornamento:\n{ex.Message}", "Errore",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                TryDeleteInstaller();
                return null;
            }
        }

        private static void MakeExecutableIfNeeded(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            try
            {
                File.SetUnixFileMode(path,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }
            catch (Exception)
            {
                // Not fatal: the user can still run it manually.
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
                Application.Instance.Quit();
            }
            catch (Exception ex)
            {
                AddLog($"Impossibile avviare l'installer: {ex.Message}");
                MessageBox.Show(
                    $"Impossibile avviare l'installer:\n{ex.Message}\n\nPuoi eseguirlo manualmente da:\n{installerPath}",
                    "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Close();
            }
        }

        private void AddLog(string message)
        {
            Application.Instance.Invoke(() =>
            {
                _logListBox.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                _logListBox.SelectedIndex = _logListBox.Items.Count - 1;
            });
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
            catch (Exception)
            {
                // Ignored: leftover temp files are harmless.
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
