using System.IO;
using PongRoyale.Core.Combat;
using PongRoyale.Core.Simulation;
using PongRoyale.Gameplay;
using PongRoyale.Gameplay.Balance;
using PongRoyale.Gameplay.Input;
using PongRoyale.Gameplay.Ball;
using PongRoyale.Gameplay.Paddle;
using PongRoyale.Gameplay.Pickups;
using PongRoyale.Gameplay.Towers;
using PongRoyale.Presentation.CameraRig;
using PongRoyale.Presentation.Hud;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PongRoyale.Editor
{
    /// <summary>
    /// Monta a cena Match inteira a partir do BalanceData: sprites de placeholder, prefabs
    /// de bola, raquete e torre, e a hierarquia da arena ja com as referencias ligadas.
    ///
    /// Reconstroi do zero a cada execucao, de proposito. Enquanto a arte nao existe, e mais
    /// util poder regenerar tudo depois de mudar um numero de balanceamento do que preservar
    /// ajustes manuais numa cena de placeholder.
    ///
    /// Menu: Pong Royale / Setup / Rebuild Match Scene
    /// </summary>
    public static class MatchSceneSetupTool
    {
        private const string ArtFolder = "Assets/_Project/Art/Arena";
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const string BalanceAssetPath = "Assets/_Project/ScriptableObjects/Balance/DefaultBalance.asset";
        private const string MatchScenePath = "Assets/_Project/Scenes/Match.unity";
        private const string BallPrefabPath = PrefabFolder + "/Ball/Ball.prefab";
        private const string PaddlePrefabPath = PrefabFolder + "/Paddle/Paddle.prefab";
        private const string TowerPrefabPath = PrefabFolder + "/Towers/Tower.prefab";

        private static readonly Color BackgroundColor = new Color(0.10f, 0.11f, 0.16f, 1f);
        private static readonly Color WallColor = new Color(0.30f, 0.33f, 0.42f, 1f);
        private static readonly Color BottomColor = new Color(0.30f, 0.80f, 1.00f, 1f);
        private static readonly Color TopColor = new Color(1.00f, 0.42f, 0.62f, 1f);
        private static readonly Color BallColor = new Color(1.00f, 0.95f, 0.70f, 1f);

        private const int BackgroundSortingOrder = -20;
        private const int WallSortingOrder = -10;
        private const int TowerSortingOrder = 0;
        private const int PaddleSortingOrder = 10;
        private const int BallSortingOrder = 20;

        private const float WallThickness = 0.12f;

        [MenuItem("Pong Royale/Setup/Rebuild Match Scene")]
        public static void RebuildMatchScene()
        {
            // ORDEM IMPORTA, E MAIS DO QUE PARECE. Toda operacao que mexe no AssetDatabase
            // — Refresh, SaveAndReimport, SaveAsPrefabAsset, ate OpenScene — pode destruir e
            // recriar os objetos ja carregados. A referencia antiga vira um objeto morto, e
            // atribui-la a um campo grava NULL em silencio: sem excecao, sem aviso, so uma
            // cena quebrada que compila e passa em todo teste de EditMode.
            //
            // Por isso a ferramenta faz TODO o I/O de asset primeiro e so entao carrega, de
            // uma vez, as referencias que vai usar para montar a cena.
            string squarePath = EnsureSpriteAsset("Placeholder_Square", size: 16, circular: false);
            string circlePath = EnsureSpriteAsset("Placeholder_Circle", size: 128, circular: true);

            EnsureFolder($"{PrefabFolder}/Ball");
            EnsureFolder($"{PrefabFolder}/Paddle");
            EnsureFolder($"{PrefabFolder}/Towers");
            AssetDatabase.Refresh();

            BuildPrefabs(squarePath, circlePath);

            Scene scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
            ClearScene(scene);

            // A partir daqui nao ha mais importacao de asset, entao as referencias carregadas
            // agora sobrevivem ate o fim da montagem.
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(squarePath);
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(circlePath);
            var balance = AssetDatabase.LoadAssetAtPath<BalanceData>(BalanceAssetPath);
            var ballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BallPrefabPath);
            var paddlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlePrefabPath);
            var towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TowerPrefabPath);

            if (square == null || circle == null || balance == null || ballPrefab == null
                || paddlePrefab == null || towerPrefab == null)
            {
                Debug.LogError("[MatchScene] Falha ao carregar os assets necessarios. " +
                               $"BalanceData existe em {BalanceAssetPath}?");
                return;
            }

            MatchConfig config = balance.ToMatchConfig();

            MatchRunner runner = CreateRunner(balance);
            Camera camera = CreateCamera(balance);
            CreateArena(square, config);
            CreatePaddles(paddlePrefab, runner);
            CreateTowers(towerPrefab, runner);
            CreateBall(ballPrefab, runner);
            CreateInput(runner, camera);
            CreatePickups(circle, runner);
            CreateHud(runner, square, config);

            // A ferramenta valida o proprio resultado. Uma referencia perdida aqui nao
            // quebra compilacao nem teste de EditMode: quebraria so a partida, na tela.
            var check = new SerializedObject(runner);
            if (check.FindProperty("balanceData").objectReferenceValue == null)
            {
                Debug.LogError("[MatchScene] O MatchRunner ficou sem BalanceData. Cena NAO salva.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[MatchScene] Cena Match reconstruida a partir do BalanceData.");
        }

        private const int HudBackgroundSortingOrder = 90;
        private const int HudFillSortingOrder = 91;
        private const int HudTextSortingOrder = 100;
        private const int DamageNumberCount = 12;
        private const int PickupSortingOrder = 25;

        /// <summary>
        /// Uma view por vaga de drop, criadas desde ja e desligadas. Nada de Instantiate no
        /// meio da partida. A cor de cada uma sai de quem vai coletar, entao o jogador ve
        /// num relance se o premio que esta caindo e dele.
        /// </summary>
        private static void CreatePickups(Sprite circle, MatchRunner runner)
        {
            var root = new GameObject("Pickups");

            for (int i = 0; i < MatchState.MaxPickups; i++)
            {
                GameObject instance = CreateSpriteObject(
                    "Pickup_" + i, circle, Color.white, PickupSortingOrder,
                    Vector3.zero, Vector3.one, root.transform);

                var view = instance.AddComponent<PickupView>();
                var serialized = new SerializedObject(view);
                serialized.FindProperty("runner").objectReferenceValue = runner;
                serialized.FindProperty("pickupIndex").intValue = i;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// Barra de ciclo na divisao dos lados, com os indicadores de carga dos DOIS
        /// jogadores. Uma barra so, porque o metronomo e global.
        /// </summary>
        private static void CreateElixirBar(Transform parent, MatchRunner runner, Sprite square)
        {
            const float BarWidth = 8f;
            const float BarHeight = 0.18f;
            const float ChargeSize = 0.34f;
            const float ChargeSpacing = 0.5f;
            const float ChargeOffsetY = 0.45f;

            var host = new GameObject("ElixirCycle");
            host.transform.SetParent(parent, worldPositionStays: false);

            CreateSpriteObject(
                "BarBackground", square, new Color(0.08f, 0.09f, 0.13f, 0.85f), HudBackgroundSortingOrder,
                Vector3.zero, new Vector3(BarWidth, BarHeight, 1f), host.transform);

            GameObject fill = CreateSpriteObject(
                "BarFill", square, new Color(0.55f, 0.85f, 1f, 1f), HudFillSortingOrder,
                Vector3.zero, new Vector3(0f, BarHeight, 1f), host.transform);

            var bottomCharges = new SpriteRenderer[3];
            var topCharges = new SpriteRenderer[3];

            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * ChargeSpacing;

                bottomCharges[i] = CreateSpriteObject(
                    "ChargeBottom_" + i, square, Color.gray, HudFillSortingOrder,
                    new Vector3(x, -ChargeOffsetY, 0f), new Vector3(ChargeSize, ChargeSize, 1f),
                    host.transform).GetComponent<SpriteRenderer>();

                topCharges[i] = CreateSpriteObject(
                    "ChargeTop_" + i, square, Color.gray, HudFillSortingOrder,
                    new Vector3(x, ChargeOffsetY, 0f), new Vector3(ChargeSize, ChargeSize, 1f),
                    host.transform).GetComponent<SpriteRenderer>();
            }

            var view = host.AddComponent<ElixirCycleView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("runner").objectReferenceValue = runner;
            serialized.FindProperty("barFill").objectReferenceValue = fill.transform;
            serialized.FindProperty("barWidth").floatValue = BarWidth;
            serialized.FindProperty("barHeight").floatValue = BarHeight;

            AssignRenderers(serialized.FindProperty("bottomCharges"), bottomCharges);
            AssignRenderers(serialized.FindProperty("topCharges"), topCharges);

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignRenderers(SerializedProperty array, SpriteRenderer[] renderers)
        {
            array.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            }
        }

        /// <summary>
        /// Monta a HUD: vida das torres, numeros de dano, relogio e painel de resultado.
        ///
        /// Usa TextMesh e nao TextMeshPro de proposito. O TMP exige um font asset importado
        /// por menu, o que uma ferramenta em batchmode nao consegue garantir; o TextMesh usa
        /// a fonte embutida do Unity e funciona sem nenhum asset no projeto. Quando a arte
        /// real entrar na FASE 5, trocar para TMP e um ajuste de apresentacao, nao de logica.
        /// </summary>
        private static void CreateHud(MatchRunner runner, Sprite square, MatchConfig config)
        {
            Font font = LoadBuiltinFont();
            if (font == null)
            {
                Debug.LogError("[MatchScene] Fonte embutida nao encontrada. A HUD nao foi criada.");
                return;
            }

            var root = new GameObject("Hud");

            foreach (PlayerSlot slot in new[] { PlayerSlot.Bottom, PlayerSlot.Top })
            {
                foreach (TowerKind kind in new[] { TowerKind.King, TowerKind.LeftGuard, TowerKind.RightGuard })
                {
                    CreateTowerHealth(root.transform, runner, square, font, slot, kind);
                }
            }

            CreateClock(root.transform, runner, font, config);
            CreateResultPanel(root.transform, runner, square, font, config);
            CreateDamageNumbers(root.transform, runner, font);
            CreateElixirBar(root.transform, runner, square);
        }

        private static void CreateTowerHealth(
            Transform parent, MatchRunner runner, Sprite square, Font font, PlayerSlot slot, TowerKind kind)
        {
            var host = new GameObject($"TowerHealth_{slot}_{kind}");
            host.transform.SetParent(parent, worldPositionStays: false);

            var background = CreateSpriteObject(
                "Background", square, new Color(0.08f, 0.09f, 0.13f, 0.9f), HudBackgroundSortingOrder,
                Vector3.zero, Vector3.one, host.transform);

            var fill = CreateSpriteObject(
                "Fill", square, Color.green, HudFillSortingOrder,
                Vector3.zero, Vector3.one, host.transform);

            TextMesh label = CreateLabel(
                "Label", host.transform, font, characterSize: 0.05f, fontSize: 60,
                sortingOrder: HudTextSortingOrder, anchor: TextAnchor.MiddleCenter);
            label.transform.localPosition = new Vector3(0f, 0.32f, 0f);

            var view = host.AddComponent<TowerHealthView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("runner").objectReferenceValue = runner;
            serialized.FindProperty("owner").enumValueIndex = (int)slot;
            serialized.FindProperty("kind").enumValueIndex = (int)kind;
            serialized.FindProperty("barFill").objectReferenceValue = fill.transform;
            serialized.FindProperty("barBackground").objectReferenceValue =
                background.GetComponent<SpriteRenderer>();
            serialized.FindProperty("barFillRenderer").objectReferenceValue =
                fill.GetComponent<SpriteRenderer>();
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateClock(Transform parent, MatchRunner runner, Font font, MatchConfig config)
        {
            var host = new GameObject("MatchClock");
            host.transform.SetParent(parent, worldPositionStays: false);

            TextMesh label = CreateLabel(
                "Label", host.transform, font, characterSize: 0.09f, fontSize: 60,
                sortingOrder: HudTextSortingOrder, anchor: TextAnchor.MiddleCenter);

            // Na margem vertical que sobra acima da arena em telas altas.
            label.transform.position = new Vector3(0f, config.Arena.HalfHeight + 0.75f, 0f);

            var view = host.AddComponent<MatchClockView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("runner").objectReferenceValue = runner;
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateResultPanel(
            Transform parent, MatchRunner runner, Sprite square, Font font, MatchConfig config)
        {
            var host = new GameObject("MatchResult");
            host.transform.SetParent(parent, worldPositionStays: false);

            var backdrop = CreateSpriteObject(
                "Backdrop", square, new Color(0.03f, 0.03f, 0.06f, 0.75f), HudBackgroundSortingOrder + 5,
                Vector3.zero, new Vector3(config.Arena.Width, config.Arena.Height, 1f), host.transform);

            TextMesh label = CreateLabel(
                "Label", host.transform, font, characterSize: 0.10f, fontSize: 60,
                sortingOrder: HudTextSortingOrder + 5, anchor: TextAnchor.MiddleCenter);

            var view = host.AddComponent<MatchResultView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("runner").objectReferenceValue = runner;
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.FindProperty("backdrop").objectReferenceValue = backdrop.GetComponent<SpriteRenderer>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateDamageNumbers(Transform parent, MatchRunner runner, Font font)
        {
            var host = new GameObject("DamageNumbers");
            host.transform.SetParent(parent, worldPositionStays: false);

            var labels = new TextMesh[DamageNumberCount];
            for (int i = 0; i < DamageNumberCount; i++)
            {
                labels[i] = CreateLabel(
                    $"Damage_{i}", host.transform, font, characterSize: 0.06f, fontSize: 60,
                    sortingOrder: HudTextSortingOrder, anchor: TextAnchor.MiddleCenter);
            }

            var view = host.AddComponent<DamageNumbersView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("runner").objectReferenceValue = runner;

            SerializedProperty array = serialized.FindProperty("labels");
            array.arraySize = DamageNumberCount;
            for (int i = 0; i < DamageNumberCount; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = labels[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TextMesh CreateLabel(
            string name, Transform parent, Font font,
            float characterSize, int fontSize, int sortingOrder, TextAnchor anchor)
        {
            var host = new GameObject(name, typeof(TextMesh));
            host.transform.SetParent(parent, worldPositionStays: false);

            var text = host.GetComponent<TextMesh>();
            text.font = font;
            text.fontSize = fontSize;
            text.characterSize = characterSize;
            text.anchor = anchor;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;

            var renderer = host.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            renderer.sortingOrder = sortingOrder;

            return text;
        }

        /// <summary>
        /// Fonte embutida do Unity. O nome mudou entre versoes, entao ha uma cadeia de
        /// tentativas em vez de um nome cravado.
        /// </summary>
        private static Font LoadBuiltinFont()
        {
            string[] candidates = { "LegacyRuntime.ttf", "Arial.ttf" };

            foreach (string candidate in candidates)
            {
                var font = Resources.GetBuiltinResource<Font>(candidate);
                if (font != null)
                {
                    return font;
                }
            }

            return null;
        }

        private static void ClearScene(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(root);
            }
        }

        private static MatchRunner CreateRunner(BalanceData balance)
        {
            var runnerObject = new GameObject("MatchRunner");
            var runner = runnerObject.AddComponent<MatchRunner>();

            var serialized = new SerializedObject(runner);
            serialized.FindProperty("balanceData").objectReferenceValue = balance;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return runner;
        }

        /// <summary>
        /// Liga as duas fontes de input: o ponteiro controla a raquete de baixo (dedo no
        /// celular, mouse no Editor) e o bot controla a de cima, o que torna a partida
        /// jogavel sem multiplayer.
        /// </summary>
        private static void CreateInput(MatchRunner runner, Camera camera)
        {
            var root = new GameObject("Input");

            var playerObject = new GameObject("Input_Player");
            playerObject.transform.SetParent(root.transform, worldPositionStays: false);

            var pointerInput = playerObject.AddComponent<PointerPaddleInput>();
            var pointerSerialized = new SerializedObject(pointerInput);
            pointerSerialized.FindProperty("worldCamera").objectReferenceValue = camera;
            pointerSerialized.ApplyModifiedPropertiesWithoutUndo();

            AttachController(playerObject, runner, PlayerSlot.Bottom, pointerInput);

            var botObject = new GameObject("Input_Bot");
            botObject.transform.SetParent(root.transform, worldPositionStays: false);

            var botInput = botObject.AddComponent<AiPaddleInput>();
            AttachController(botObject, runner, PlayerSlot.Top, botInput);
        }

        private static void AttachController(
            GameObject host, MatchRunner runner, PlayerSlot slot, PaddleInputSource source)
        {
            var controller = host.AddComponent<PaddleController>();

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("runner").objectReferenceValue = runner;
            serialized.FindProperty("slot").enumValueIndex = (int)slot;
            serialized.FindProperty("inputSource").objectReferenceValue = source;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Camera CreateCamera(BalanceData balance)
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            var fitter = cameraObject.AddComponent<ArenaCameraFitter>();
            var serialized = new SerializedObject(fitter);
            serialized.FindProperty("balanceData").objectReferenceValue = balance;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return camera;
        }

        private static void CreateArena(Sprite square, MatchConfig config)
        {
            var arenaRoot = new GameObject("Arena");

            CreateSpriteObject(
                "Background", square, BackgroundColor, BackgroundSortingOrder,
                Vector3.zero, new Vector3(config.Arena.Width, config.Arena.Height, 1f), arenaRoot.transform);

            float halfWidth = config.Arena.HalfWidth;
            float halfHeight = config.Arena.HalfHeight;

            CreateSpriteObject("Wall_Left", square, WallColor, WallSortingOrder,
                new Vector3(-halfWidth, 0f, 0f), new Vector3(WallThickness, config.Arena.Height, 1f), arenaRoot.transform);
            CreateSpriteObject("Wall_Right", square, WallColor, WallSortingOrder,
                new Vector3(halfWidth, 0f, 0f), new Vector3(WallThickness, config.Arena.Height, 1f), arenaRoot.transform);
            CreateSpriteObject("Wall_Bottom", square, WallColor, WallSortingOrder,
                new Vector3(0f, -halfHeight, 0f), new Vector3(config.Arena.Width, WallThickness, 1f), arenaRoot.transform);
            CreateSpriteObject("Wall_Top", square, WallColor, WallSortingOrder,
                new Vector3(0f, halfHeight, 0f), new Vector3(config.Arena.Width, WallThickness, 1f), arenaRoot.transform);
        }

        private static void CreatePaddles(GameObject prefab, MatchRunner runner)
        {
            var root = new GameObject("Paddles");

            CreatePaddle(prefab, runner, PlayerSlot.Bottom, BottomColor, root.transform);
            CreatePaddle(prefab, runner, PlayerSlot.Top, TopColor, root.transform);
        }

        private static void CreatePaddle(
            GameObject prefab, MatchRunner runner, PlayerSlot slot, Color color, Transform parent)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = $"Paddle_{slot}";
            instance.GetComponent<SpriteRenderer>().color = color;

            var serialized = new SerializedObject(instance.GetComponent<PaddleView>());
            serialized.FindProperty("runner").objectReferenceValue = runner;
            serialized.FindProperty("slot").enumValueIndex = (int)slot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateTowers(GameObject prefab, MatchRunner runner)
        {
            var root = new GameObject("Towers");

            foreach (PlayerSlot slot in new[] { PlayerSlot.Bottom, PlayerSlot.Top })
            {
                Color color = slot == PlayerSlot.Bottom ? BottomColor : TopColor;

                foreach (TowerKind kind in new[] { TowerKind.King, TowerKind.LeftGuard, TowerKind.RightGuard })
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                    instance.name = $"Tower_{slot}_{kind}";

                    var serialized = new SerializedObject(instance.GetComponent<TowerView>());
                    serialized.FindProperty("runner").objectReferenceValue = runner;
                    serialized.FindProperty("owner").enumValueIndex = (int)slot;
                    serialized.FindProperty("kind").enumValueIndex = (int)kind;
                    serialized.FindProperty("baseColor").colorValue = color;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void CreateBall(GameObject prefab, MatchRunner runner)
        {
            var root = new GameObject("Balls");

            for (int i = 0; i < MatchState.MaxBalls; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                instance.name = $"Ball_{i}";
                instance.GetComponent<SpriteRenderer>().color = BallColor;

                var serialized = new SerializedObject(instance.GetComponent<BallView>());
                serialized.FindProperty("runner").objectReferenceValue = runner;
                serialized.FindProperty("ballIndex").intValue = i;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static GameObject CreateSpriteObject(
            string name, Sprite sprite, Color color, int sortingOrder,
            Vector3 position, Vector3 scale, Transform parent)
        {
            var instance = new GameObject(name, typeof(SpriteRenderer));
            instance.transform.SetParent(parent, worldPositionStays: false);
            instance.transform.position = position;
            instance.transform.localScale = scale;

            var renderer = instance.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            return instance;
        }

        /// <summary>
        /// Gera os tres prefabs. Cada um recarrega o proprio sprite pelo caminho, porque
        /// salvar um prefab importa asset e invalida o que ja estava carregado.
        /// </summary>
        private static void BuildPrefabs(string squarePath, string circlePath)
        {
            SavePrefab(BallPrefabPath, "Ball", circlePath, BallSortingOrder, typeof(BallView));
            SavePrefab(PaddlePrefabPath, "Paddle", squarePath, PaddleSortingOrder, typeof(PaddleView));
            SavePrefab(TowerPrefabPath, "Tower", squarePath, TowerSortingOrder, typeof(TowerView));
        }

        private static void SavePrefab(
            string prefabPath, string name, string spritePath, int sortingOrder, System.Type viewType)
        {
            var template = new GameObject(name, typeof(SpriteRenderer));

            var renderer = template.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            renderer.sortingOrder = sortingOrder;

            template.AddComponent(viewType);

            EnsureFolder(Path.GetDirectoryName(prefabPath));
            PrefabUtility.SaveAsPrefabAsset(template, prefabPath);
            Object.DestroyImmediate(template);
        }

        /// <summary>
        /// Gera um PNG branco (quadrado ou circulo) e o importa como sprite com
        /// pixelsPerUnit igual ao tamanho da textura. Assim o sprite mede exatamente 1x1
        /// unidade de mundo, e a escala do Transform passa a ser o tamanho real em metros —
        /// sem fator de conversao escondido entre a simulacao e a tela.
        /// </summary>
        private static string EnsureSpriteAsset(string fileName, int size, bool circular)
        {
            EnsureFolder(ArtFolder);
            string path = $"{ArtFolder}/{fileName}.png";

            if (!File.Exists(path))
            {
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false);
                var pixels = new Color32[size * size];
                float center = (size - 1) * 0.5f;
                float radius = size * 0.5f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        bool solid = !circular
                                     || (x - center) * (x - center) + (y - center) * (y - center) <= radius * radius;
                        pixels[y * size + x] = solid ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();

                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();

            return path;
        }

        /// <summary>
        /// Cria a pasta no disco sem chamar Refresh. Quem chama e que decide quando
        /// sincronizar o AssetDatabase — Refresh no meio de uma sequencia de operacoes
        /// invalida referencias ja carregadas.
        /// </summary>
        private static void EnsureFolder(string folder)
        {
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }
    }
}
