using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Linq;
using System.Collections;
using Sirenix.OdinInspector.Editor;

namespace Ardenfall.Mlf
{
    //For now, the fancy inspector is too buggy to use. So just default to multiline editor built into editor
    
    [DrawerPriority(DrawerPriorityLevel.AttributePriority)]
    public class PythonInspector: OdinAttributeDrawer<PythonEditorAttribute, string>
    {
        private EditorView editor;

        private GUIStyle fontDrag, buttonTabs;

        protected override void Initialize()
        {
            base.Initialize();

            string rawScript = this.ValueEntry.SmartValue;
            
            if (editor == null)
                editor = new EditorView();

            editor.OnEnable(rawScript);

        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            SetStyles();

            editor.EditorViewGUI();

            this.ValueEntry.SmartValue = editor.buffer.CodeBuffer;
        }

        private void SetStyles()
        {
            //Style of text
            fontDrag = new GUIStyle(GUI.skin.box);
            fontDrag.fontSize = 16;
            fontDrag.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            fontDrag.alignment = TextAnchor.MiddleCenter;
            fontDrag.hover.background = TextureColor(Color.yellow);
            //Style of Tabs
            buttonTabs = new GUIStyle(GUI.skin.box);
            buttonTabs.fontSize = 16;
            buttonTabs.normal.textColor = Color.white;
            buttonTabs.alignment = TextAnchor.MiddleCenter;

            Color ColorTabs = new Color(0, 0, 0, 0.9f);
            buttonTabs.normal.background = TextureColor(ColorTabs);
        }

        private static Texture2D TextureColor(Color color)
        {
            Texture2D TextureColor = new Texture2D(1, 1);
            TextureColor.SetPixels(new Color[] { color });
            TextureColor.Apply();
            TextureColor.hideFlags = HideFlags.HideAndDontSave;
            return TextureColor;
        }
        

    }

}
