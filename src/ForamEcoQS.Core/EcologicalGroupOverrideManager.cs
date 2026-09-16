using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace ForamEcoQS
{
    public class EcologicalGroupOverrideManager
    {
        private Dictionary<string, int> overrides;
        private string filePath;

        public EcologicalGroupOverrideManager()
        {
            overrides = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ForamEcoQS");
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            
            filePath = Path.Combine(appDataPath, "eco_overrides.json");
            Load();
        }

        public void AddOverride(string species, int group)
        {
            if (group < 1 || group > 5) return;
            overrides[species.Trim()] = group;
            Save();
        }

        public void RemoveOverride(string species)
        {
            if (overrides.Remove(species.Trim()))
            {
                Save();
            }
        }

        public void ClearAll()
        {
            overrides.Clear();
            Save();
        }

        public bool HasOverride(string species)
        {
            return overrides.ContainsKey(species.Trim());
        }

        public int? GetOverride(string species)
        {
            if (overrides.TryGetValue(species.Trim(), out int group))
            {
                return group;
            }
            return null;
        }

        public Dictionary<string, int> GetAllOverrides()
        {
            return new Dictionary<string, int>(overrides, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Exports all overrides to a standalone JSON file (species name -> ecological group),
        /// so they can be shared with collaborators or committed to version control alongside a dataset.
        /// </summary>
        public void ExportToFile(string exportPath)
        {
            string json = JsonSerializer.Serialize(overrides, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(exportPath, json);
        }

        /// <summary>
        /// Imports overrides from a previously exported JSON file.
        /// </summary>
        /// <param name="importPath">Path to the JSON file to import.</param>
        /// <param name="merge">If true, imported entries are added to the existing overrides
        /// (overwriting entries with the same species name). If false, existing overrides are replaced entirely.</param>
        /// <returns>The number of override entries imported.</returns>
        public int ImportFromFile(string importPath, bool merge)
        {
            string json = File.ReadAllText(importPath);
            var imported = JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                           ?? new Dictionary<string, int>();

            if (!merge)
            {
                overrides = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            }

            foreach (var kvp in imported)
            {
                if (kvp.Value >= 1 && kvp.Value <= 5 && !string.IsNullOrWhiteSpace(kvp.Key))
                {
                    overrides[kvp.Key.Trim()] = kvp.Value;
                }
            }

            Save();
            return imported.Count;
        }

        private void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(overrides, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
            catch (Exception)
            {
                // Ignore save errors
            }
        }

        private void Load()
        {
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    overrides = JsonSerializer.Deserialize<Dictionary<string, int>>(json) 
                                ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    
                    // Ensure case-insensitive comparer is used even after deserialization
                    overrides = new Dictionary<string, int>(overrides, StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception)
                {
                    overrides = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                }
            }
        }
    }
}
