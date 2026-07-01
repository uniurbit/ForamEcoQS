// MIT License
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ForamEcoQS
{
    /// <summary>
    /// Client for the WoRMS (World Register of Marine Species) REST webservice
    /// (https://www.marinespecies.org/rest/), used to verify species names that are
    /// not found in the local ecological databank and to surface the currently
    /// accepted taxonomic name when the entered name is an outdated synonym.
    /// </summary>
    public static class WormsService
    {
        private const string BaseUrl = "https://www.marinespecies.org/rest/";

        // WoRMS accepts at most 50 names per AphiaRecordsByMatchNames call.
        private const int MaxNamesPerRequest = 50;

        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Looks up multiple scientific names at once using the AphiaRecordsByMatchNames
        /// fuzzy-matching endpoint (handles typos and near-matches). Requests are batched
        /// automatically because WoRMS limits each call to 50 names.
        /// </summary>
        /// <returns>A case-insensitive map from the requested name to the best matching AphiaRecord.
        /// Names with no match found are absent from the result.</returns>
        public static async Task<Dictionary<string, WormsRecord>> MatchNamesAsync(IEnumerable<string?> names)
        {
            var distinctNames = names
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var results = new Dictionary<string, WormsRecord>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < distinctNames.Count; i += MaxNamesPerRequest)
            {
                var batch = distinctNames.Skip(i).Take(MaxNamesPerRequest).ToList();
                foreach (var kvp in await MatchNamesBatchAsync(batch))
                {
                    results[kvp.Key] = kvp.Value;
                }
            }

            return results;
        }

        private static async Task<Dictionary<string, WormsRecord>> MatchNamesBatchAsync(List<string> batch)
        {
            var query = new StringBuilder("AphiaRecordsByMatchNames?");
            foreach (var name in batch)
            {
                query.Append("scientificnames%5B%5D=").Append(Uri.EscapeDataString(name)).Append('&');
            }
            query.Append("marine_only=false");

            string response = await _httpClient.GetStringAsync(BaseUrl + query);

            var results = new Dictionary<string, WormsRecord>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(response))
                return results;

            // The endpoint returns one array of candidate matches per requested name, in the same order.
            var matchGroups = JsonSerializer.Deserialize<List<List<WormsRecord>>>(response, _jsonOptions);
            if (matchGroups == null)
                return results;

            for (int i = 0; i < batch.Count && i < matchGroups.Count; i++)
            {
                var candidates = matchGroups[i];
                if (candidates == null || candidates.Count == 0)
                    continue;

                var best = candidates.FirstOrDefault(c => c.MatchType == "exact") ?? candidates[0];
                results[batch[i]] = best;
            }

            return results;
        }
    }

    /// <summary>
    /// A taxonomic record ("AphiaRecord") as returned by the WoRMS webservice.
    /// </summary>
    public class WormsRecord
    {
        [JsonPropertyName("AphiaID")]
        public long AphiaID { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("scientificname")]
        public string? Scientificname { get; set; }

        [JsonPropertyName("authority")]
        public string? Authority { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("unacceptreason")]
        public string? Unacceptreason { get; set; }

        [JsonPropertyName("rank")]
        public string? Rank { get; set; }

        [JsonPropertyName("valid_AphiaID")]
        public long? Valid_AphiaID { get; set; }

        [JsonPropertyName("valid_name")]
        public string? Valid_name { get; set; }

        [JsonPropertyName("valid_authority")]
        public string? Valid_authority { get; set; }

        [JsonPropertyName("kingdom")]
        public string? Kingdom { get; set; }

        [JsonPropertyName("phylum")]
        public string? Phylum { get; set; }

        // "class" is a reserved word in C#.
        [JsonPropertyName("class")]
        public string? TaxonomicClass { get; set; }

        [JsonPropertyName("order")]
        public string? Order { get; set; }

        [JsonPropertyName("family")]
        public string? Family { get; set; }

        [JsonPropertyName("genus")]
        public string? Genus { get; set; }

        [JsonPropertyName("lsid")]
        public string? Lsid { get; set; }

        [JsonPropertyName("isMarine")]
        public int? IsMarine { get; set; }

        [JsonPropertyName("isBrackish")]
        public int? IsBrackish { get; set; }

        [JsonPropertyName("isFreshwater")]
        public int? IsFreshwater { get; set; }

        [JsonPropertyName("isTerrestrial")]
        public int? IsTerrestrial { get; set; }

        [JsonPropertyName("isExtinct")]
        public int? IsExtinct { get; set; }

        [JsonPropertyName("match_type")]
        public string? MatchType { get; set; }

        [JsonIgnore]
        public bool IsAccepted => string.Equals(Status, "accepted", StringComparison.OrdinalIgnoreCase);
    }
}
