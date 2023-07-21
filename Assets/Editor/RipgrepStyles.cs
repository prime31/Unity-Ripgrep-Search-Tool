using UnityEditor;
using UnityEngine;

// https://github.com/halak/unity-editor-icons
namespace Editor.Tools.Ripgrep
{
    static class RipgrepStyles
    {
        public static readonly Texture2D scriptTex = EditorGUIUtility.FindTexture("cs Script Icon");
        public static readonly Texture2D jsScriptTex = (Texture2D)EditorGUIUtility.IconContent("Js Script Icon").image;
        public static readonly Texture2D booScriptTex = (Texture2D)EditorGUIUtility.IconContent("boo Script Icon").image;
        public static readonly Texture2D textAssetTex = (Texture2D)EditorGUIUtility.IconContent("d_TextAsset Icon").image;
        public static readonly Texture2D animationAssetTex = (Texture2D)EditorGUIUtility.IconContent("Animation.FilterBySelection").image;
        public static readonly Texture2D shaderTex = (Texture2D)EditorGUIUtility.IconContent("d_Shader Icon").image;
        public static readonly Texture2D prefabTex = EditorGUIUtility.FindTexture("Prefab Icon");
        public static readonly Texture2D scriptableObjectTex = (Texture2D)EditorGUIUtility.IconContent("ScriptableObject Icon").image;
        public static readonly Texture2D textureTex = (Texture2D)EditorGUIUtility.IconContent("d_Texture Icon").image;
        public static readonly Texture2D spriteAtlasTex = (Texture2D)EditorGUIUtility.IconContent("SpriteAtlasAsset Icon").image;
        public static readonly Texture2D androidTex = (Texture2D)EditorGUIUtility.IconContent("BuildSettings.Android On").image;
        public static readonly Texture2D xmlTex = (Texture2D)EditorGUIUtility.IconContent("UxmlScript Icon").image;
        public static readonly Texture2D metaFileTex = (Texture2D)EditorGUIUtility.IconContent("MetaFile Icon").image;
        public static readonly Texture2D sceneFileTex = (Texture2D)EditorGUIUtility.IconContent("d_Scene").image;
        public static readonly Texture2D materialFileTex = (Texture2D)EditorGUIUtility.IconContent("d_Material Icon").image;
        public static readonly Texture2D presetFileTex = (Texture2D)EditorGUIUtility.IconContent("d_Preset.Context@2x").image;

        private static readonly RectOffset marginNone = new(0, 0, 0, 0);
        private static readonly RectOffset paddingNone = new(0, 0, 0, 0);

        public static readonly GUIStyle optionsPanel = new(EditorStyles.helpBox)
        {
            name = "options",
            fontSize = 20,
            fixedHeight = 0,
            fixedWidth = 0,
            wordWrap = true,
            richText = true,
            alignment = TextAnchor.UpperLeft,
            margin = marginNone,
            padding = new RectOffset(10, 10, 5, 5),
            normal = new GUIStyleState
            {
                background = GenerateSolidColorTexture(new Color(0.2f, 0.2f, 0.2f, 1f))
            }
        };

        public static readonly GUIContent optionsIconContent = new(string.Empty, EditorGUIUtility.FindTexture("d_UnityEditor.AnimationWindow@2x"), "Show Saved Searches");

        public static readonly GUIStyle noResult = new(EditorStyles.centeredGreyMiniLabel)
        {
            name = "no-result",
            fontSize = 20,
            fixedHeight = 0,
            fixedWidth = 0,
            wordWrap = true,
            richText = true,
            alignment = TextAnchor.MiddleCenter,
            margin = marginNone,
            padding = paddingNone
        };

        public static readonly GUIStyle optionsHeaderIcon = new("IconButton")
        {
            margin = new RectOffset(4, 4, 4, 4),
            padding = new RectOffset(2, 2, 4, 4),
            fixedWidth = 30,
            fixedHeight = 30,
            imagePosition = ImagePosition.ImageOnly,
            alignment = TextAnchor.MiddleCenter
        };

        public static readonly GUIStyle panelHeader = new(EditorStyles.whiteLabel)
        {
            margin = new RectOffset(2, 2, 2, 2),
            padding = new RectOffset(1, 1, 4, 4),
            fontSize = 14,
            fixedHeight = 24,
            wordWrap = false,
            alignment = TextAnchor.MiddleLeft,
            stretchWidth = false,
            clipping = TextClipping.Clip
        };

        public static readonly GUIStyle tipIcon = new("Label")
        {
            margin = new RectOffset(4, 4, 2, 2),
            stretchWidth = false
        };
        
        public static readonly GUIStyle tipTextTitle = new("Label")
        {
            fontSize = 20,
            richText = true,
            wordWrap = false,
            alignment = TextAnchor.MiddleCenter
        };
        
        public static readonly GUIStyle tipText = new("Label")
        {
            wordWrap = false,
            richText = true,
        };

        public static readonly GUIStyle searchTextField = new("TextField")
        {
            fontSize = 24,
            wordWrap = false
        };

        public const float tipSizeOffset = 100f;
        public const float tipMaxSize = 350f;

        public static readonly GUIContent[] searchTipIcons =
        {
            new(string.Empty, EditorGUIUtility.FindTexture("_Popup")),
            new(string.Empty, EditorGUIUtility.IconContent("Toolbar Plus More").image),
            new(string.Empty, EditorGUIUtility.FindTexture("SaveAs")),
            new(string.Empty, EditorGUIUtility.FindTexture("d_Profiler.UIDetails")),
            new(string.Empty, EditorGUIUtility.FindTexture("UnityEditor.InspectorWindow")),
            new(string.Empty, EditorGUIUtility.FindTexture("Refresh")),
            new(string.Empty, EditorGUIUtility.FindTexture("Linked")),
            new(string.Empty, EditorGUIUtility.FindTexture("winbtn_win_close"))
        };

        public static readonly GUIContent[] searchTipLabels =
        {
            new("Press <b>enter</b> to start a search"),
            new("Search history remembers your previous searches"),
            new("<b>Right click</b> search results for more options"),
            new("<b>Double click</b> search results to open in a text editor"),
            new("Check the <b>options panel</b> for search settings"),
            new("Separate multiple search terms with <b>spaces</b>"),
            new("Cancel a search by pressing <b>escape</b> when it is running"),
            new("<b>Escape</b> closes the window if there is no search running")
        };
        
        public static readonly GUIContent clearSearchIconContent = new(string.Empty, EditorGUIUtility.FindTexture("d_winbtn_win_close@2x"), "Clear Search and Results");
        public static readonly GUIContent openOptionsIconContent = new(string.Empty, EditorGUIUtility.FindTexture("Audio Mixer@2x"), "Open Options Panel");

        public static readonly GUIStyle openOptionsPanelButton = new("IconButton")
        {
            margin = new RectOffset(4, 4, 4, 4),
            padding = new RectOffset(2, 2, 2, 2),
            fixedWidth = 30f,
            fixedHeight = 30f,
            imagePosition = ImagePosition.ImageOnly,
            alignment = TextAnchor.MiddleCenter
        };

        public static readonly GUIStyle historyButton = new("Button")
        {
            margin = new RectOffset(4, 4, 6, 6),
            padding = new RectOffset(2, 2, 2, 2),
            fixedWidth = 24f,
            fixedHeight = 24f
        };

        private static Texture2D GenerateSolidColorTexture(Color fillColor)
        {
            var texture = new Texture2D(1, 1);
            var fillColorArray = texture.GetPixels();

            for (var i = 0; i < fillColorArray.Length; ++i)
                fillColorArray[i] = fillColor;

            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.SetPixels(fillColorArray);
            texture.Apply();

            return texture;
        }
    }
}
