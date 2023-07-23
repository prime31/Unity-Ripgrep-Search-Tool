using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Editor.Tools.Ripgrep
{
    public class RipgrepSearchWindow : EditorWindow
    {
        [SerializeField] string searchStr = string.Empty;
        [SerializeField] bool isOptionsOpen;
        [SerializeField] TreeViewState treeViewState = new();

        // options, persisted in saved searches
        RipgrepSettings.SearchOptions searchOptions = RipgrepSettings.SearchOptions.ExcludeMetaFiles;

        // history
        [SerializeField] List<string> searchHistory = new();
        [SerializeField] int searchHistoryCursor = -1;
        Ripgrep.Future rgResultFuture;
        RipgrepSearchResultsTreeView treeView;
        bool focusTextField;

        [MenuItem("Tools/Editors/Open Ripgrep Search Window %g")]
        static public RipgrepSearchWindow ShowWindow()
        {
            var window = GetWindow<RipgrepSearchWindow>();
            window.titleContent = new GUIContent("Ripgrep Search");
            window.minSize = new Vector2(750, 350);
            window.ShowUtility();

            return window;
        }

        [MenuItem("Assets/Search for this asset's GUID %&g")]
        public static void SearchAssetGuid()
        {
            UnityEngine.Object selectedObject = Selection.activeObject;
            string assetPath = AssetDatabase.GetAssetPath(selectedObject);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            var window = ShowWindow();
            window.EnsureTreeView();

            if (selectedObject == null || string.IsNullOrEmpty(guid)) {
                window.ShowNotification(new GUIContent("No asset selected"));
            } else {
                window.searchStr = guid;
                window.StartSearch();
            }
        }

        void OnEnable() => EditorApplication.delayCall += () => focusTextField = true;

        void OnInspectorUpdate()
        {
            // force a repaint when our background task is running
            if (rgResultFuture != null)
                Repaint();
        }

        void EnsureTreeView()
        {
            if (treeView != null) return;

            treeView = new RipgrepSearchResultsTreeView(treeViewState);
            treeView.OnDoubleClicked += item => treeView.OpenItemInTextMate(item.id);
            treeView.OnSingleClicked += item =>
            {
                if (RipgrepSettings.instance.PingObjectsOnSelectionChange)
                {
                    var foundObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.FilePath);
                    Selection.activeObject = foundObject;
                    EditorGUIUtility.PingObject(foundObject);
                }
            };

            treeView.OnContextClicked += item =>
            {
                var menu = new GenericMenu();
                if (item.LineNumber < 0)
                {
                    if (!item.IsMetaFile() && item.IsInAssetFolder())
                    {
                        menu.AddItem(new GUIContent("Search for this asset's GUID"), false, () =>
                        {
                            searchStr = AssetDatabase.AssetPathToGUID(item.FilePath);
                            StartSearch();
                        });

                        menu.AddItem(new GUIContent("Get Dependency Tree"), false, () =>
                        {
                            treeView.SetResults(null);

                            var results = new List<Ripgrep.Results>();
                            var root = new Ripgrep.Results { Text = item.FilePath };
                            results.Add(root);

                            var paths = new HashSet<string> { item.FilePath };

                            void RecurseChildDependencies(Ripgrep.Results parent, string assetPath)
                            {
                                foreach (var dep in AssetDatabase.GetDependencies(item.displayName))
                                {
                                    if (paths.Add(dep))
                                    {
                                        var child = new Ripgrep.Results { Text = dep };
                                        parent.Children.Add(child);
                                        // TODO: make the UI reasonable for a dependency tree, for now its just one level
                                        // RecurseChildDependencies(child, dep);
                                    }
                                    else if (dep != assetPath)
                                    {
                                        parent.Children.Add(new Ripgrep.Results { Text = $"Dupe - {System.IO.Path.GetFileName(dep)}" });
                                    }
                                }
                            }

                            RecurseChildDependencies(root, item.FilePath);

                            treeView.SetResults(results.ToArray());
                        });
                    }

                    menu.AddItem(new GUIContent("Open in TextMate"), false, () => treeView.OpenItemInTextMate(item.id));
                }
                else
                {
                    menu.AddItem(new GUIContent("Open in TextMate at this line"), false, () => treeView.OpenItemInTextMate(item.id));
                }

                menu.ShowAsContext();
            };

            treeView.SearchField = new SearchField();
        }

        void DrawTips()
        {
            using (new GUILayout.VerticalScope(RipgrepStyles.noResult))
            {
                GUILayout.FlexibleSpace();
                using (new GUILayout.HorizontalScope(RipgrepStyles.noResult, GUILayout.ExpandHeight(true)))
                {
                    GUILayout.FlexibleSpace();
                    using (new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(240)))
                    {
                        GUILayout.Label("<b>Search Tips</b>", RipgrepStyles.tipTextTitle);
                        GUILayout.Space(15);
                        for (var i = 0; i < RipgrepStyles.searchTipIcons.Length; ++i)
                        {
                            using (new GUILayout.HorizontalScope(RipgrepStyles.noResult))
                            {
                                GUILayout.Label(RipgrepStyles.searchTipIcons[i], RipgrepStyles.tipIcon);
                                GUILayout.Label(RipgrepStyles.searchTipLabels[i], RipgrepStyles.tipText, GUILayout.Width(Mathf.Min(RipgrepStyles.tipMaxSize, position.width - RipgrepStyles.tipSizeOffset)));
                            }
                        }

                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.FlexibleSpace();
                }

                GUILayout.FlexibleSpace();
            }
        }

        void DrawNoResults()
        {
            using (new GUILayout.VerticalScope(RipgrepStyles.noResult))
            {
                GUILayout.FlexibleSpace();
                using (new GUILayout.HorizontalScope(RipgrepStyles.noResult, GUILayout.ExpandHeight(true)))
                {
                    GUILayout.FlexibleSpace();
                    using (new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(25)))
                    {
                        GUILayout.Label(L10n.Tr("<b>No results</b>"), RipgrepStyles.tipText);
                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.FlexibleSpace();
                }

                GUILayout.FlexibleSpace();
            }
        }

        void DrawOptions(float optionsWidth)
        {
            using (new GUILayout.VerticalScope(RipgrepStyles.optionsPanel, GUILayout.Width(optionsWidth), GUILayout.ExpandHeight(true)))
            {
                GUILayout.Label("Search Options", RipgrepStyles.panelHeader);

                // options
                SearchOptionsExt.OnGui(ref searchOptions);

                GUILayout.Space(10);
                GUILayout.Label("Persisted Settings", RipgrepStyles.panelHeader);
                RipgrepSettings.instance.ShowResultNotifications = GUILayout.Toggle(RipgrepSettings.instance.ShowResultNotifications, "Show live result count notifications");
                RipgrepSettings.instance.PingObjectsOnSelectionChange = GUILayout.Toggle(RipgrepSettings.instance.PingObjectsOnSelectionChange, "Ping objects on selection change");

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label($"Threads ({RipgrepSettings.instance.ThreadCount})", GUILayout.Width(75));
                    RipgrepSettings.instance.ThreadCount = (int)GUILayout.HorizontalSlider(RipgrepSettings.instance.ThreadCount, 1, 16);
                }

                // built-in searches
                GUILayout.Space(20);
                GUILayout.Label("Built-in Searches", RipgrepStyles.panelHeader);

                if (GUILayout.Button("All Assets with WeakReferences"))
                    StartSearch(@"guid:\s[a-zA-Z0-9]{32}\n.*?subAssetName", RipgrepSettings.SearchOptions.Multiline | RipgrepSettings.SearchOptions.RestrictToUnityAssetFiles | RipgrepSettings.SearchOptions.ExcludeMetaFiles);

                if (GUILayout.Button("All WeakReferences with Missing GUIDs"))
                    StartSearch(@"\s+-\sguid:\s*$\n.*?subAssetName", RipgrepSettings.SearchOptions.Multiline | RipgrepSettings.SearchOptions.RestrictToUnityAssetFiles | RipgrepSettings.SearchOptions.ExcludeMetaFiles);

                GUILayout.FlexibleSpace();
            }
        }

        void OnGUI()
        {
            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Escape)
            {
                if (rgResultFuture != null)
                {
                    rgResultFuture.Cancel();
                    rgResultFuture = null;
                    ShowNotification(new GUIContent("Search cancelled, only partial results returned"));
                    return;
                }

                Event.current.Use();
                Close();
                return;
            }

            EnsureTreeView();

            GUI.enabled = rgResultFuture == null;

            using (new GUILayout.HorizontalScope())
            {
                isOptionsOpen = GUILayout.Toggle(isOptionsOpen, RipgrepStyles.openOptionsIconContent, RipgrepStyles.openOptionsPanelButton);

                EditorGUI.BeginDisabledGroup(searchHistoryCursor <= 0);
                if (GUILayout.Button("<", RipgrepStyles.historyButton))
                {
                    searchStr = searchHistory[--searchHistoryCursor];
                    StartSearch(true);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(searchHistoryCursor == searchHistory.Count - 1);
                if (GUILayout.Button(">", RipgrepStyles.historyButton, GUILayout.MaxWidth(20)))
                {
                    searchStr = searchHistory[++searchHistoryCursor];
                    StartSearch(true);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUIUtility.labelWidth = 1;
                var nextControlId = GUIUtility.GetControlID(FocusType.Passive) + 1;
                GUI.SetNextControlName("SearchTF");
                searchStr = EditorGUILayout.TextField((string)null, searchStr, RipgrepStyles.searchTextField, GUILayout.Height(34));
                EditorGUIUtility.labelWidth = -1;

                if (GUIUtility.keyboardControl == nextControlId && Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return)
                    StartSearch();

                if (GUILayout.Button(RipgrepStyles.clearSearchIconContent, RipgrepStyles.openOptionsPanelButton))
                {
                    searchStr = string.Empty;
                    treeView.SetResults(null);
                }

                if (EditorGUILayout.DropdownButton(RipgrepStyles.optionsIconContent, FocusType.Keyboard, RipgrepStyles.optionsHeaderIcon))
                    OnClickShowSavedSearches();
            }

            if (focusTextField)
            {
                GUI.FocusControl("SearchTF");
                focusTextField = false;
            }

            if (rgResultFuture != null)
                EditorGUILayout.HelpBox($"Ripgrepping. Elapsed time: {rgResultFuture.GetElapsedTime():F1}", MessageType.Info);

            var lastRect = GUILayoutUtility.GetLastRect();
            lastRect.y += 10;
            using (new GUILayout.HorizontalScope())
            {
                var optionsWidth = 5;
                if (isOptionsOpen)
                {
                    optionsWidth += 250;
                    DrawOptions(optionsWidth);
                    optionsWidth += 60; // TODO: why do we have so much extra width here?
                }

                if (treeView.HasCompletedSearchWithNoResults)
                {
                    DrawNoResults();
                }
                else if (treeView.HasCompletedSearch)
                {
                    const float SEARCH_FIELD_HEIGHT = 20;
                    const float SEARCH_FIELD_MARGIN = 20;
                    const float TREE_MARGIN = 5;
                    var width = position.width - optionsWidth - TREE_MARGIN;
                    var height = position.height - lastRect.yMax - SEARCH_FIELD_HEIGHT - TREE_MARGIN;

                    treeView.searchString = treeView.SearchField.OnToolbarGUI(new Rect(optionsWidth + SEARCH_FIELD_MARGIN, lastRect.yMax, width - SEARCH_FIELD_MARGIN * 2, SEARCH_FIELD_HEIGHT), treeView.searchString);
                    treeView.OnGUI(new Rect(optionsWidth, lastRect.yMax + SEARCH_FIELD_HEIGHT, width, height));
                }
                else
                {
                    DrawTips();
                }
            }
        }

        void OnClickShowSavedSearches()
        {
            var menu = new GenericMenu();
            menu.AddSeparator("Saved Searches");
            menu.AddSeparator("");

            foreach (var savedSearch in RipgrepSettings.instance.SavedSearches)
            {
                menu.AddItem(new GUIContent(savedSearch.SearchTerm + "/Execute Search"), false, () =>
                {
                    searchStr = savedSearch.SearchTerm;
                    searchOptions = savedSearch.SearchOptions;
                    EditorApplication.delayCall += () => StartSearch();
                });

                menu.AddItem(new GUIContent(savedSearch.SearchTerm + "/Delete"), false, () => RipgrepSettings.instance.RemoveSavedSearch(savedSearch));
                menu.AddSeparator(savedSearch.SearchTerm + "/");

                var searchName = string.IsNullOrEmpty(savedSearch.Name) ? "Set Name" : savedSearch.Name;
                menu.AddItem(new GUIContent(savedSearch.SearchTerm + "/" + searchName), false, () =>
                {
                    var newName = EditorInputDialog.Show("Rename Saved Search", "Set the name for this saved search", savedSearch.Name, "Save");
                    if (newName != null)
                        savedSearch.Name = newName;
                    EditorUtility.SetDirty(RipgrepSettings.instance);
                });

                if (!string.IsNullOrEmpty(savedSearch.Name))
                    menu.AddSeparator(savedSearch.SearchTerm + "/" + savedSearch.SearchTerm);
            }

            menu.AddSeparator("");
            if (searchStr.Length >= 3)
                menu.AddItem(new GUIContent("Save Current Search"), false, () => RipgrepSettings.instance.AddSavedSearch(searchStr, searchOptions));
            else
                menu.AddDisabledItem(new GUIContent("Save Current Search"));
            menu.ShowAsContext();
        }

        void OnResultParsed(int totalResults) => ShowNotification(new($"Total Results {totalResults}"), 0.1);

        void StartSearch(string query, RipgrepSettings.SearchOptions options)
        {
            searchStr = query;
            searchOptions = options;
            StartSearch();
        }

        void StartSearch(bool ignoreHistory = false)
        {
            if (searchStr.Length < 3)
            {
                ShowNotification(new GUIContent("At least 3 characters are required for a search"));
                return;
            }

            treeView.SetResults(null);

            if (searchHistory.LastOrDefault() != searchStr && !ignoreHistory)
            {
                searchHistory.Add(searchStr);
                searchHistoryCursor = searchHistory.Count - 1;
            }

            var opts = new Ripgrep.Options
            {
                SearchTerm = searchStr,
                WorkingDir = Environment.CurrentDirectory,
                IgnoreCase = searchOptions.ContainsFlag(RipgrepSettings.SearchOptions.IgnoreCase),
                Multiline = searchOptions.ContainsFlag(RipgrepSettings.SearchOptions.Multiline),
                LiteralStringSearch = searchOptions.ContainsFlag(RipgrepSettings.SearchOptions.LiteralStringSearch),
                ThreadCount = RipgrepSettings.instance.ThreadCount,
                OnResultParsed = RipgrepSettings.instance.ShowResultNotifications ? OnResultParsed : null
            };

            if (searchOptions.ContainsFlag(RipgrepSettings.SearchOptions.RestrictToUnityAssetFiles))
                opts.RestrictToUnityAssetFiles();

            if (searchOptions.ContainsFlag(RipgrepSettings.SearchOptions.RestrictToCodeFiles))
                opts.AddIncludedFileType("cs");

            if (searchOptions.ContainsFlag(RipgrepSettings.SearchOptions.ExcludeMetaFiles))
                opts.AddExcludedFileType("meta");

            // always include Assets folder
            opts.AddSearchFolder("Assets");

            if (searchOptions.ContainsFlag(RipgrepSettings.SearchOptions.IncludePackagesFolder))
                opts.AddSearchFolder("Packages");

            rgResultFuture = Ripgrep.RunAsync(opts);
            rgResultFuture.WhenComplete(rgResults =>
            {
                rgResultFuture = null;
                treeView.SetResults(rgResults);
                EditorApplication.delayCall += Repaint;
            });
        }
    }
}
