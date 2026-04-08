#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SocialManager.UI;

namespace SocialManager.Editor
{
    public static class SaveGameTestUIGenerator
    {
        [MenuItem("SocialManager/Generate Test UI/Save Game System Tester")]
        public static void GenerateSaveGameTestUI()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            GameObject panel = new GameObject("SaveGameTestUI_Panel");
            panel.transform.SetParent(canvas.transform, false);
            
            RectTransform panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.3f, 0.2f);
            panelRt.anchorMax = new Vector2(0.7f, 0.8f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            
            panel.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.2f, 0.95f);

            VerticalLayoutGroup vLayout = panel.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(20, 20, 20, 20);
            vLayout.spacing = 15;
            vLayout.childControlHeight = false;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandHeight = false;

            // Title
            CreateTextUI(panel.transform, "GENERIC SAVE GAME TESTER", 20, TextAlignmentOptions.Center, Color.cyan);

            // Inputs
            TMP_InputField inputLevel = CreateInputFieldWithLabel(panel.transform, "Set Level:", "1");
            TMP_InputField inputGold = CreateInputFieldWithLabel(panel.transform, "Set Gold:", "1000");

            // Buttons
            Button btnSave = CreateButton(panel.transform, "💾 SAVE CLOUD (GENERIC)");
            Button btnLoad = CreateButton(panel.transform, "📥 LOAD CLOUD (GENERIC)");
            Button btnDelete = CreateButton(panel.transform, "🗑 DELETE DATA", Color.red);
            
            // Logs
            GameObject logHolder = new GameObject("LogHolder");
            logHolder.transform.SetParent(panel.transform, false);
            logHolder.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            RectTransform logRt = logHolder.GetComponent<RectTransform>();
            logRt.sizeDelta = new Vector2(0, 200);

            TextMeshProUGUI txtLog = CreateTextUI(logHolder.transform, "Logs starting...", 12, TextAlignmentOptions.TopLeft, Color.white);
            txtLog.enableWordWrapping = true;
            RectTransform txtCompRt = txtLog.GetComponent<RectTransform>();
            txtCompRt.anchorMin = Vector2.zero;
            txtCompRt.anchorMax = Vector2.one;
            txtCompRt.offsetMin = new Vector2(10, 10);
            txtCompRt.offsetMax = new Vector2(-10, -10);

            // Link Component
            SaveGameTestUI saveTester = panel.AddComponent<SaveGameTestUI>();
            var sObj = new SerializedObject(saveTester);
            sObj.FindProperty("inputLevel").objectReferenceValue = inputLevel;
            sObj.FindProperty("inputGold").objectReferenceValue = inputGold;
            sObj.FindProperty("textLog").objectReferenceValue = txtLog;
            sObj.FindProperty("btnSave").objectReferenceValue = btnSave;
            sObj.FindProperty("btnLoad").objectReferenceValue = btnLoad;
            sObj.FindProperty("btnDelete").objectReferenceValue = btnDelete;
            sObj.ApplyModifiedProperties();

            Debug.Log("🎉 [Generator] Giao diện Save Game (Generic) đã hoàn thành!");
        }

        private static TMP_InputField CreateInputFieldWithLabel(Transform parent, string labelTxt, string defaultVal)
        {
            GameObject container = new GameObject("Group");
            container.transform.SetParent(parent, false);
            HorizontalLayoutGroup hLayout = container.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 10;
            hLayout.childControlHeight = true;
            hLayout.childControlWidth = true;

            CreateTextUI(container.transform, labelTxt, 16, TextAlignmentOptions.MidlineLeft, Color.white);
            
            GameObject inputObj = new GameObject("InputField");
            inputObj.transform.SetParent(container.transform, false);
            inputObj.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
            TMP_InputField input = inputObj.AddComponent<TMP_InputField>();
            
            TextMeshProUGUI textComp = CreateTextUI(inputObj.transform, defaultVal, 16, TextAlignmentOptions.MidlineLeft, Color.white);
            textComp.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            textComp.GetComponent<RectTransform>().anchorMax = Vector2.one;
            textComp.GetComponent<RectTransform>().offsetMin = new Vector2(10, 0);
            textComp.GetComponent<RectTransform>().offsetMax = new Vector2(-10, 0);
            input.textComponent = textComp;
            input.text = defaultVal;

            return input;
        }

        private static TextMeshProUGUI CreateTextUI(Transform parent, string text, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            TextMeshProUGUI textComp = obj.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.fontSize = fontSize;
            textComp.alignment = alignment;
            textComp.color = color;
            return textComp;
        }

        private static Button CreateButton(Transform parent, string text, Color? btnColor = null)
        {
            GameObject obj = new GameObject("Button");
            obj.transform.SetParent(parent, false);
            Image img = obj.AddComponent<Image>();
            img.color = btnColor ?? new Color(0.2f, 0.4f, 0.6f);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 45);
            Button btn = obj.AddComponent<Button>();
            CreateTextUI(obj.transform, text, 14, TextAlignmentOptions.Center, Color.white);
            return btn;
        }
    }
}
#endif
