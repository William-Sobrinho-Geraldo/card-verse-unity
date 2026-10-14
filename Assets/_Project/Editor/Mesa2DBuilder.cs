using System.IO;
using SuecaOnline.Presentation.Cards;
using SuecaOnline.Presentation.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuecaOnline.EditorTools
{
    /// <summary>
    /// Gera os assets de apresentacao da Fase 2 (prefab da carta e a cena Mesa2D)
    /// de forma idempotente, tanto pelo menu quanto por linha de comando (-executeMethod).
    /// Tambem importa os TMP Essential Resources, exigidos pelo TextMeshPro.
    /// </summary>
    public static class Mesa2DBuilder
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const string PrefabPath = "Assets/_Project/Prefabs/CardPrefab.prefab";
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string ScenePath = "Assets/_Project/Scenes/Mesa2D.unity";
        private const string TmpFolder = "Assets/TextMesh Pro";

        private const float CardWidth = 120f;
        private const float CardHeight = 168f;

        private static readonly Color FeltColor = new Color(0.06f, 0.36f, 0.18f, 1f);
        private static readonly Color CardBackColor = new Color(0.13f, 0.22f, 0.55f, 1f);

        [MenuItem("Sueca/Import TMP Essential Resources")]
        public static void ImportTmpEssentials()
        {
            if (AssetDatabase.IsValidFolder(TmpFolder))
            {
                Debug.Log("[Mesa2DBuilder] TMP Essential Resources ja importados.");
                return;
            }

            string projectRoot = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
            string packageCache = Path.Combine(projectRoot, "Library", "PackageCache");

            if (!Directory.Exists(packageCache))
            {
                Debug.LogWarning("[Mesa2DBuilder] PackageCache nao encontrado.");
                return;
            }

            string packageRoot = FindUguiPackageRoot(packageCache);
            if (packageRoot == null)
            {
                Debug.LogWarning("[Mesa2DBuilder] Pacote com.unity.ugui nao encontrado no PackageCache.");
                return;
            }

            string unityPackage = Path.Combine(packageRoot, "Package Resources", "TMP Essential Resources.unitypackage");
            if (!File.Exists(unityPackage))
            {
                Debug.LogWarning("[Mesa2DBuilder] Arquivo TMP Essential Resources.unitypackage nao encontrado.");
                return;
            }

            Debug.Log("[Mesa2DBuilder] Importando TMP Essential Resources...");
            AssetDatabase.ImportPackage(unityPackage, false);
            AssetDatabase.Refresh();
        }

        [MenuItem("Sueca/Build Prefab e Cena Mesa2D")]
        public static void BuildAll()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(SceneFolder);

            CardView cardViewPrefab = BuildCardPrefab();
            BuildMesa2DScene(cardViewPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Mesa2DBuilder] Prefab e cena gerados com sucesso.");
        }

        public static CardView BuildCardPrefab()
        {
            GameObject root = new GameObject("CardPrefab", typeof(RectTransform));
            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(CardWidth, CardHeight);

            RectTransform faceRect = CreateRect("Face", root.transform);
            Stretch(faceRect);
            Image face = faceRect.gameObject.AddComponent<Image>();
            face.color = Color.white;

            RectTransform backRect = CreateRect("Back", root.transform);
            Stretch(backRect);
            Image back = backRect.gameObject.AddComponent<Image>();
            back.color = CardBackColor;
            back.enabled = false;

            RectTransform rankRect = CreateRect("Rank", root.transform);
            rankRect.anchorMin = new Vector2(0f, 1f);
            rankRect.anchorMax = new Vector2(0f, 1f);
            rankRect.pivot = new Vector2(0f, 1f);
            rankRect.anchoredPosition = new Vector2(10f, -8f);
            rankRect.sizeDelta = new Vector2(70f, 52f);
            TextMeshProUGUI rankText = rankRect.gameObject.AddComponent<TextMeshProUGUI>();
            rankText.text = "A";
            rankText.fontSize = 42f;
            rankText.alignment = TextAlignmentOptions.TopLeft;
            rankText.raycastTarget = false;

            RectTransform suitRect = CreateRect("Suit", root.transform);
            Stretch(suitRect);
            TextMeshProUGUI suitText = suitRect.gameObject.AddComponent<TextMeshProUGUI>();
            suitText.text = "\u2660";
            suitText.fontSize = 76f;
            suitText.alignment = TextAlignmentOptions.Center;
            suitText.raycastTarget = false;

            CardView view = root.AddComponent<CardView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("_faceImage").objectReferenceValue = face;
            serializedView.FindProperty("_backImage").objectReferenceValue = back;
            serializedView.FindProperty("_rankText").objectReferenceValue = rankText;
            serializedView.FindProperty("_suitText").objectReferenceValue = suitText;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder(PrefabFolder);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();

            return AssetDatabase.LoadAssetAtPath<CardView>(PrefabPath);
        }

        public static void BuildMesa2DScene(CardView cardViewPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = FeltColor;
            cameraObject.AddComponent<AudioListener>();

            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform felt = CreateRect("Felt", canvasObject.transform);
            Stretch(felt);
            Image feltImage = felt.gameObject.AddComponent<Image>();
            feltImage.color = FeltColor;
            feltImage.raycastTarget = false;

            TableView tableView = BuildTableArea(canvasObject.transform, cardViewPrefab);

            RectTransform southHand = CreateHandRoot("SouthHand", canvasObject.transform, new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(1500f, 220f));
            RectTransform northHand = CreateHandRoot("NorthHand", canvasObject.transform, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1200f, 120f));
            RectTransform westHand = CreateHandRoot("WestHand", canvasObject.transform, new Vector2(0f, 0.5f), new Vector2(90f, 0f), new Vector2(140f, 700f));
            RectTransform eastHand = CreateHandRoot("EastHand", canvasObject.transform, new Vector2(1f, 0.5f), new Vector2(-90f, 0f), new Vector2(140f, 700f));

            TextMeshProUGUI trumpText = CreateText("TrumpText", canvasObject.transform, new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(420f, 60f), TextAlignmentOptions.TopLeft, 36f, Color.white);
            trumpText.text = "Trunfo: -";

            TextMeshProUGUI scoreText = CreateText("ScoreText", canvasObject.transform, new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(760f, 60f), TextAlignmentOptions.TopRight, 32f, Color.white);
            scoreText.text = "Nos (N/S): 0 pts - 0 jogos  |  Eles (L/O): 0 pts - 0 jogos";

            TextMeshProUGUI statusText = CreateText("StatusText", canvasObject.transform, new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(900f, 60f), TextAlignmentOptions.Center, 34f, new Color(1f, 0.96f, 0.7f, 1f));
            statusText.text = "Bem-vindo a Sueca";

            TextMeshProUGUI winnerText = CreateText("WinnerText", canvasObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1200f, 120f), TextAlignmentOptions.Center, 68f, new Color(1f, 0.83f, 0.2f, 1f));
            winnerText.text = string.Empty;
            winnerText.gameObject.SetActive(false);

            Button nextHandButton = CreateButton("NextHandButton", canvasObject.transform, new Vector2(1f, 0f), new Vector2(-150f, 90f), new Vector2(240f, 64f), "Proxima mao");
            Button newMatchButton = CreateButton("NewMatchButton", canvasObject.transform, new Vector2(1f, 0f), new Vector2(-150f, 170f), new Vector2(240f, 64f), "Nova partida");

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();

            GameObject managerObject = new GameObject("GameManager");
            LocalGameManager manager = managerObject.AddComponent<LocalGameManager>();
            SerializedObject serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("_tableView").objectReferenceValue = tableView;
            serializedManager.FindProperty("_southHandRoot").objectReferenceValue = southHand;
            serializedManager.FindProperty("_northHandRoot").objectReferenceValue = northHand;
            serializedManager.FindProperty("_eastHandRoot").objectReferenceValue = eastHand;
            serializedManager.FindProperty("_westHandRoot").objectReferenceValue = westHand;
            serializedManager.FindProperty("_cardViewPrefab").objectReferenceValue = cardViewPrefab;
            serializedManager.FindProperty("_trumpText").objectReferenceValue = trumpText;
            serializedManager.FindProperty("_scoreText").objectReferenceValue = scoreText;
            serializedManager.FindProperty("_statusText").objectReferenceValue = statusText;
            serializedManager.FindProperty("_winnerText").objectReferenceValue = winnerText;
            serializedManager.FindProperty("_nextHandButton").objectReferenceValue = nextHandButton;
            serializedManager.FindProperty("_newMatchButton").objectReferenceValue = newMatchButton;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder(SceneFolder);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings();
        }

        private static TableView BuildTableArea(Transform parent, CardView cardViewPrefab)
        {
            RectTransform tableRect = CreateRect("Table", parent);
            tableRect.anchorMin = new Vector2(0.5f, 0.5f);
            tableRect.anchorMax = new Vector2(0.5f, 0.5f);
            tableRect.pivot = new Vector2(0.5f, 0.5f);
            tableRect.anchoredPosition = new Vector2(0f, 0f);
            tableRect.sizeDelta = new Vector2(760f, 760f);

            TableView tableView = tableRect.gameObject.AddComponent<TableView>();

            RectTransform northSlot = CreateSlot("NorthSlot", tableRect, new Vector2(0f, 210f));
            RectTransform eastSlot = CreateSlot("EastSlot", tableRect, new Vector2(330f, 0f));
            RectTransform southSlot = CreateSlot("SouthSlot", tableRect, new Vector2(0f, -210f));
            RectTransform westSlot = CreateSlot("WestSlot", tableRect, new Vector2(-330f, 0f));

            tableView.Configure(cardViewPrefab, northSlot, eastSlot, southSlot, westSlot);
            return tableView;
        }

        private static RectTransform CreateHandRoot(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform CreateSlot(string name, Transform parent, Vector2 position)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(CardWidth, CardHeight);
            return rect;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            TextAlignmentOptions alignment,
            float fontSize,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.alignment = alignment;
            text.fontSize = fontSize;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            string label)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.92f, 0.92f, 0.95f, 1f);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            RectTransform labelRect = CreateRect("Label", rect);
            Stretch(labelRect);
            TextMeshProUGUI labelText = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontSize = 26f;
            labelText.color = new Color(0.08f, 0.08f, 0.1f, 1f);
            labelText.raycastTarget = false;

            return button;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static string FindUguiPackageRoot(string packageCache)
        {
            string[] candidates = Directory.GetDirectories(packageCache, "com.unity.ugui@*");
            if (candidates.Length == 0)
            {
                return null;
            }

            return candidates[0];
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void RegisterSceneInBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path == ScenePath)
                {
                    continue;
                }

                scenes.Add(existing);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
