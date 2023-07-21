using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Editor.Tools.Ripgrep
{
    public class RipgrepSearchResultsTreeView : TreeView
    {
        public Action<RipgrepTreeViewItem> OnContextClicked;
        public Action<RipgrepTreeViewItem> OnSingleClicked;
        public Action<RipgrepTreeViewItem> OnDoubleClicked;
        public Action<RipgrepTreeViewItem> OnSelectionChanged;
        public SearchField SearchField;
        public bool HasCompletedSearch => rgResults != null;
        public bool HasCompletedSearchWithNoResults => HasCompletedSearch && rgResults.Length == 0;
        Ripgrep.Results[] rgResults;

        public RipgrepSearchResultsTreeView(TreeViewState state) : base(state)
        {
            showBorder = true;
            showAlternatingRowBackgrounds = true;

            SetMultiColumnHeader("Ripgrep Search Results", "Result tree roots are the file path and branches are the line in the file that matched the query");
            multiColumnHeader.ResizeToFit();

            Reload();
        }

        public void SetResults(Ripgrep.Results[] results)
        {
            var headerTitle = results != null ? $"Total Search Results: {results.Length}" : "Ripgrep Search Results";
            multiColumnHeader.state.columns[0].headerContent = new GUIContent(headerTitle, multiColumnHeader.state.columns[0].headerContent.tooltip);

            SetSelection(Array.Empty<int>());
            SetExpanded(new List<int>());

            rgResults = results;
            Reload();
        }

        public void SetMultiColumnHeader(string title, string tooltip)
        {
            multiColumnHeader = new MultiColumnHeader(new MultiColumnHeaderState(new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(title, tooltip),
                    headerTextAlignment = TextAlignment.Center,
                    canSort = false,
                    autoResize = true,
                    allowToggleVisibility = false
                }
            }));
        }

        public void OpenItemInTextMate(int id) => OpenItemInTextMate((RipgrepTreeViewItem)FindItem(id, rootItem));

        public void OpenItemInTextMate(RipgrepTreeViewItem item) => TextMate.Open(item.FilePath, item.LineNumber > 0 ? item.LineNumber : 1);

        protected override TreeViewItem BuildRoot()
        {
            Texture2D TextureForFileType(string extension)
            {
                return extension switch
                {
                    ".cs" => RipgrepStyles.scriptTex,
                    ".xml" => RipgrepStyles.xmlTex,
                    ".php" => RipgrepStyles.booScriptTex,
                    ".txt" => RipgrepStyles.textAssetTex,
                    ".shader" => RipgrepStyles.shaderTex,
                    var ext when ext == ".js" || ext == ".json" => RipgrepStyles.jsScriptTex,
                    ".scene" => RipgrepStyles.sceneFileTex,
                    ".unity" => RipgrepStyles.sceneFileTex,
                    ".anim" => RipgrepStyles.animationAssetTex,
                    ".prefab" => RipgrepStyles.prefabTex,
                    ".asset" => RipgrepStyles.scriptableObjectTex,
                    ".mat" => RipgrepStyles.materialFileTex,
                    ".preset" => RipgrepStyles.presetFileTex,
                    ".spriteatlas" => RipgrepStyles.spriteAtlasTex,
                    ".gradle" => RipgrepStyles.androidTex,
                    ".meta" => RipgrepStyles.metaFileTex,
                    var ext when ext == ".png" || ext == ".jpg" => RipgrepStyles.textureTex,
                    _ => null
                };
            }

            var root = new RipgrepTreeViewItem { id = int.MaxValue, depth = -1, displayName = "Root", children = new List<TreeViewItem>()};

            if (rgResults != null)
            {
                var uniqueId = 0;
                for (var i = 0; i < rgResults.Length; i++)
                {
                    void BuildChildren(RipgrepTreeViewItem parent, Ripgrep.Results result)
                    {
                        for (var j = 0; j < result.Children.Count; j++)
                        {
                            var displayName = result.Children[j].LineNumber >= 0 ? $"{result.Children[j].LineNumber}: {result.Children[j].Text}" : result.Children[j].Text;
                            var child = new RipgrepTreeViewItem(uniqueId++, displayName, parent.depth + 1);
                            child.LineNumber = result.Children[j].LineNumber;
                            
                            // LineNumber < 0 means this is not a line in a file but an actual asset
                            if (child.LineNumber < 0)
                                child.icon = TextureForFileType(System.IO.Path.GetExtension(displayName));
                            child.parent = parent;
                            parent.AddChild(child);
                            
                            BuildChildren(child, result.Children[j]);
                        }
                    }
                    
                    var treeViewItem = new RipgrepTreeViewItem { id = uniqueId++, depth = 0, displayName = rgResults[i].Text, parent = root, children = new List<TreeViewItem>() };
                    treeViewItem.icon = TextureForFileType(System.IO.Path.GetExtension(treeViewItem.FilePath));
                    root.AddChild(treeViewItem);
                    
                    BuildChildren(treeViewItem, rgResults[i]);
                }
            }

            return root;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds == null) return;
            OnSelectionChanged?.Invoke((RipgrepTreeViewItem)FindItem(selectedIds[0], rootItem));
        }

        protected override void ContextClickedItem(int id) => OnContextClicked?.Invoke((RipgrepTreeViewItem)FindItem(id, rootItem));
        protected override void SingleClickedItem(int id) => OnSingleClicked?.Invoke((RipgrepTreeViewItem)FindItem(id, rootItem));
        protected override void DoubleClickedItem(int id) => OnDoubleClicked?.Invoke((RipgrepTreeViewItem)FindItem(id, rootItem));
    }

    public class RipgrepTreeViewItem : TreeViewItem
    {
        public int LineNumber = -1;
        public string FilePath
        {
            get
            {
                var path = depth == 1 && LineNumber > 0 ? parent.displayName : displayName;
                if (path.StartsWith("Packages"))
                    return System.IO.Path.GetFullPath(path);
                return path;
            }
        }

        public RipgrepTreeViewItem()
        { }

        public RipgrepTreeViewItem(int id, string displayName, int depth = 0) : base(id, depth, displayName)
        {}
        
        public bool IsMetaFile() => FilePath.EndsWith(".meta");
        public bool IsInAssetFolder() => FilePath.StartsWith("Assets");
    }
}
