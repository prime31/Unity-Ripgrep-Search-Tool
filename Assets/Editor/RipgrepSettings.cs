using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Editor.Tools.Ripgrep
{
    [FilePath("UserSettings/ripgrep.settings", FilePathAttribute.Location.ProjectFolder)]
    public class RipgrepSettings : ScriptableSingleton<RipgrepSettings>
    {
        [Flags]
        public enum SearchOptions
        {
            None = 0,
            IgnoreCase = 1 << 0,
            LiteralStringSearch = 1 << 1,
            Multiline = 1 << 2,
            IncludePackagesFolder = 1 << 3,
            RestrictToCodeFiles = 1 << 4,
            RestrictToUnityAssetFiles = 1 << 5,
            ExcludeMetaFiles = 1 << 6,
        }

        [Serializable]
        public class SavedSearch
        {
            public string SearchTerm;
            public string Name;
            public SearchOptions SearchOptions;
        }
        
        public List<SavedSearch> SavedSearches = new();
        public bool ShowResultNotifications = true;
        public int ThreadCount = 4;
        public bool PingObjectsOnSelectionChange = true;

        public void AddSavedSearch(string search, SearchOptions options)
        {
            SavedSearches.Add(new SavedSearch { SearchTerm = search, SearchOptions = options});
            Save(true);
        }
        
        public void RemoveSavedSearch(SavedSearch search)
        {
            SavedSearches.Remove(search);
            Save(true);
        }
    }

    public static class SearchOptionsExt
    {
        public static void OnGui(ref RipgrepSettings.SearchOptions opts)
        {
            void Draw(ref RipgrepSettings.SearchOptions opts, RipgrepSettings.SearchOptions flag, string text)
            {
                var isChecked = GUILayout.Toggle(opts.ContainsFlag(flag), text);
                SetFlag(ref opts, flag, isChecked);
            }

            Draw(ref opts, RipgrepSettings.SearchOptions.IgnoreCase, "Ignore case (a lowercase search does the same)");
            Draw(ref opts, RipgrepSettings.SearchOptions.LiteralStringSearch, "Literal string search (disable regex)");
            Draw(ref opts, RipgrepSettings.SearchOptions.Multiline, "Multiline regex search");
            Draw(ref opts, RipgrepSettings.SearchOptions.IncludePackagesFolder, "Include Packages folder in searches");
            Draw(ref opts, RipgrepSettings.SearchOptions.RestrictToCodeFiles, "Restrict to code files (cs)");
            Draw(ref opts, RipgrepSettings.SearchOptions.RestrictToUnityAssetFiles, "Restrict to asset files (asset/prefab/unity)");
            Draw(ref opts, RipgrepSettings.SearchOptions.ExcludeMetaFiles, "Exclude meta files");
        }

        public static bool ContainsFlag(this RipgrepSettings.SearchOptions self, RipgrepSettings.SearchOptions flag) => (self & flag) == flag;

        public static void SetFlag(ref RipgrepSettings.SearchOptions self, RipgrepSettings.SearchOptions flag, bool isSet)
        {
            if (isSet) self |= flag;
            else self &= ~flag;
        }
    }
}
