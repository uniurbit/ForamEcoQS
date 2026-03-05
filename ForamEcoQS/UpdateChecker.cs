// MIT License
using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace ForamEcoQS
{
    /// <summary>
    /// Handles checking for application updates from a remote server.
    /// Uses GitHub Gist as the update manifest host (free and reliable).
    /// </summary>
    /// 
    public class UpdateChecker
    {
        // IMPORTANT: Replace this URL with actual GitHub Gist raw URL
        // To create: 1. Go to https://gist.github.com
        //            2. Create a new public gist with filename "foramecqs-version.json"
        //            3. Copy the "Raw" URL and paste it here
        // Example: "https://gist.githubusercontent.com/USERNAME/GIST_ID/raw/foramecqs-version.json"
        private const string VERSION_CHECK_URL = "https://gist.githubusercontent.com/mattemangia/693267b3aa9a954356e2123d6c7dc64d/raw/foramecqs-version.json";

        private static readonly HttpClient _httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        /// <summary>
        /// Gets the current application version.
        /// </summary>
        public static Version CurrentVersion
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version ?? new Version(1, 0, 0, 0);
            }
        }

        /// <summary>
        /// Gets a display-friendly version string (e.g., "1.0.0").
        /// </summary>
        public static string CurrentVersionString
        {
            get
            {
                var v = CurrentVersion;
                return $"{v.Major}.{v.Minor}.{v.Build}";
            }
        }

        /// <summary>
        /// Checks for updates asynchronously.
        /// </summary>
        /// <returns>UpdateInfo if successful, null if check failed.</returns>
        public static async Task<UpdateCheckResult> CheckForUpdatesAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(VERSION_CHECK_URL);
                var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(response, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (updateInfo == null)
                {
                    return new UpdateCheckResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to parse update information."
                    };
                }

                var latestVersion = Version.Parse(updateInfo.LatestVersion);
                var currentVersion = CurrentVersion;

                // Compare only Major.Minor.Build (ignore revision)
                var latestComparable = new Version(latestVersion.Major, latestVersion.Minor,
                    latestVersion.Build >= 0 ? latestVersion.Build : 0);
                var currentComparable = new Version(currentVersion.Major, currentVersion.Minor,
                    currentVersion.Build >= 0 ? currentVersion.Build : 0);

                return new UpdateCheckResult
                {
                    Success = true,
                    UpdateInfo = updateInfo,
                    IsUpdateAvailable = latestComparable > currentComparable,
                    CurrentVersion = CurrentVersionString,
                    LatestVersion = updateInfo.LatestVersion
                };
            }
            catch (HttpRequestException ex)
            {
                return new UpdateCheckResult
                {
                    Success = false,
                    ErrorMessage = $"Network error: {ex.Message}\n\nPlease check your internet connection."
                };
            }
            catch (TaskCanceledException)
            {
                return new UpdateCheckResult
                {
                    Success = false,
                    ErrorMessage = "Connection timed out. Please check your internet connection."
                };
            }
            catch (JsonException ex)
            {
                return new UpdateCheckResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to parse update information: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new UpdateCheckResult
                {
                    Success = false,
                    ErrorMessage = $"An error occurred: {ex.Message}"
                };
            }
        }
    }

    /// <summary>
    /// Contains information about an available update.
    /// This class maps to the JSON structure in the version manifest file.
    /// </summary>
    public class UpdateInfo
    {
        /// <summary>
        /// The latest available version (e.g., "1.1.0").
        /// </summary>
        public string LatestVersion { get; set; } = "";

        /// <summary>
        /// URL to download the installer.
        /// </summary>
        public string DownloadUrl { get; set; } = "";

        /// <summary>
        /// Release notes or changelog for this version.
        /// </summary>
        public string ReleaseNotes { get; set; } = "";

        /// <summary>
        /// Release date in ISO format (e.g., "2024-12-01").
        /// </summary>
        public string ReleaseDate { get; set; } = "";

        /// <summary>
        /// Minimum required version to update from (optional).
        /// If current version is below this, user needs to download full installer.
        /// </summary>
        public string? MinimumVersion { get; set; }

        /// <summary>
        /// Whether this update is mandatory/critical.
        /// </summary>
        public bool IsMandatory { get; set; } = false;
    }

    /// <summary>
    /// Result of an update check operation.
    /// </summary>
    public class UpdateCheckResult
    {
        /// <summary>
        /// Whether the update check completed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if Success is false.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// The update information from the server.
        /// </summary>
        public UpdateInfo? UpdateInfo { get; set; }

        /// <summary>
        /// Whether an update is available.
        /// </summary>
        public bool IsUpdateAvailable { get; set; }

        /// <summary>
        /// Current application version string.
        /// </summary>
        public string CurrentVersion { get; set; } = "";

        /// <summary>
        /// Latest available version string.
        /// </summary>
        public string LatestVersion { get; set; } = "";
    }
}
