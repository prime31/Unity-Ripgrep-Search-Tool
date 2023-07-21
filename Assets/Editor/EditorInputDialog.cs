using System;
using UnityEditor;
using UnityEngine;

namespace Editor.Tools.Ripgrep
{
    public class EditorInputDialog : EditorWindow
    {
        string description, inputText;
        string okButton, cancelButton;
        bool initializedPosition;
        Action onOKButton;
        bool shouldClose;

        void OnGUI()
        {
            // Check if Esc/Return have been pressed
            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                switch (e.keyCode)
                {
                    // Escape pressed
                    case KeyCode.Escape:
                        shouldClose = true;
                        e.Use();
                        break;

                    // Enter pressed
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        onOKButton?.Invoke();
                        shouldClose = true;
                        e.Use();
                        break;
                }
            }

            if (shouldClose)
                Close();

            // Draw our control
            var rect = EditorGUILayout.BeginVertical();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField(description);

            EditorGUILayout.Space(8);
            GUI.SetNextControlName("inText");
            inputText = EditorGUILayout.TextField("", inputText);
            GUI.FocusControl("inText");
            EditorGUILayout.Space(12);

            // OK / Cancel buttons
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button(okButton))
                {
                    onOKButton();
                    shouldClose = true;
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button(cancelButton))
                {
                    inputText = null;
                    shouldClose = true;
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.EndVertical();

            // Force change size of the window
            if (rect.width != 0 && minSize != rect.size)
                minSize = maxSize = rect.size;

            // Set dialog position next to mouse position
            if (!initializedPosition && e.type == EventType.Layout)
            {
                initializedPosition = true;
                Focus();
            }
        }

        public static string Show(string title, string description, string inputText, string okButton = "OK", string cancelButton = "Cancel")
        {
            string ret = null;

            var window = CreateInstance<EditorInputDialog>();
            window.titleContent = new GUIContent(title);
            window.description = description;
            window.inputText = inputText ?? string.Empty;
            window.okButton = okButton;
            window.cancelButton = cancelButton;
            window.onOKButton += () => ret = window.inputText;
            window.ShowModalUtility();
            window.Repaint();

            return ret;
        }
    }
}
