using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nyx.Editor
{
    public static partial class ImportClaudeArt
    {
        public const string CoreScenePath = "Assets/Nyx/Scenes/Nucleo_03.unity";
        const string ParkourFolder = "Parkour";
        const float CoreStepThickness = .56f;
        const float ShaftShift = 55f;
        static readonly Color ColdLab = new Color(.70f, .85f, .98f);
        static readonly Color WarmLab = new Color(.95f, .79f, .69f);

        [MenuItem("Nyx/Criar fase 03")]
        public static void BuildCoreCampaign()
        {
            if (SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Salve a cena aberta antes de atualizar a campanha.");
            SaveCorePhase();
            var security = EditorSceneManager.OpenScene(SecurityScenePath);
            var previousLevel = UnityEngine.Object.FindAnyObjectByType<MaintenanceLevel>();
            previousLevel.nextScene = Path.GetFileNameWithoutExtension(CoreScenePath);
            EditorSceneManager.MarkSceneDirty(security);
            if (!EditorSceneManager.SaveScene(security))
                throw new IOException("Não foi possível conectar a saída da ala de segurança.");
            OrganizarCenas.Aplicar();
        }

        public static void SaveCorePhase()
        {
            ImportParkourArt();
            var original = SceneManager.GetActiveScene();
            bool additive = original.IsValid() && original.isLoaded && !string.IsNullOrEmpty(original.path);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                additive ? NewSceneMode.Additive : NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            try
            {
                art = new GameObject("Núcleo de manutenção").transform;
                var root = art;
                var level = new GameObject("Fase 03").AddComponent<MaintenanceLevel>();
                level.phaseTwo = true;
                level.weaponUnlocked = true;
                level.followVertical = true;
                level.fallLimit = -18f;
                level.cameraRoom = Rect.MinMaxRect(0f, -16f, 225f, 32f);
                level.boltSprite = S("portal_roxo_0");
                level.purplePortalFrames = Frames("portal_roxo_", 4);
                level.greenPortalFrames = Frames("portal_verde_", 4);
                level.weaponBulletSprite = Extra("Armadilhas", "bala_jogador");

                var camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.orthographic = true;
                camera.orthographicSize = 6f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.03f, .05f, .09f);
                camera.transform.position = new Vector3(11f, 3f, -10f);
                camera.gameObject.AddComponent<AudioListener>();
                level.gameCamera = camera;
                CreateSecurityPlayer(level);
                level.player.transform.position = new Vector3(5f, .15f);
                ConfigurarInventario(level);
                ConfigurarTutorial(level);

                for (int x = 0; x < 225; x += 2)
                    for (int y = -16; y < 32; y += 2)
                    {
                        string tile = x % 12 == 4 ? "parede_fundo_cano_v" : y % 10 == 2
                            ? "parede_fundo_listra" : "parede_fundo";
                        Place(tile, x + 1f, y + 1f, 2f, -20,
                            y < 0 ? new Color(.46f, .59f, .65f) : new Color(.57f, .62f, .75f));
                    }
                CoreWall("Limite oeste", .75f, -16f, 32f, "parede_lisa_dir");
                CoreWall("Limite leste", 224.25f, -16f, 32f, "parede_lisa_esq");
                CorePlatform("Teto", 112.5f, 32f, 223f, "plataforma_grossa", 1.2f);
                CoreWall("Suporte da galeria", 65f, -12f, 8.65f, "parede_canos_esq");
                CoreWall("Suporte da central", 125f, 9.8f, 18.45f, "parede_canos_esq");

                CoreDeck(root, "01 · Corredor de serviço", 0f, () =>
                {
                    CorePlatform("Piso do corredor", 32.75f, 0f, 62.5f, "plataforma_grossa", 1.15f);
                    CreateCheckpoint(6.5f, level);
                    CreateCrate(12f, .85f);
                    CreateGuard(level, "Guarda do corredor", "guarda_cassetete", "guarda_cassetete_golpe_", false,
                        18f, 22f, 1.25f).pursuitRange = new Vector2(13f, 25f);
                    CreateSteamVent(27f, level);
                    CriarCaixaMunicao(level, 30f);
                    CoreWindow(15f, 5.4f, 2f);
                    CoreWindow(41f, 6.1f, 1.75f);
                    CoreAlarm(23f, 4.8f, 1.7f);
                    CoreAlarm(46f, 4.6f, 2.2f);
                    CreateLab("Posto de vigilância", 10f, ColdLab, false);
                    CreateBackgroundScientist("Pesquisadora no posto", "cientista_ana_sentado_", 3, 9.5f, .07f);
                    CreateCheckpoint(33f, level);
                    CreateCrate(36f, 1f);
                    CreateGuard(level, "Guarda de ronda", "guarda_pistola", "guarda_pistola_atirar_", true,
                        40f, 44f, 1.1f).pursuitRange = new Vector2(36f, 46f);
                    CreateRetractingSpikes(47f, 2.4f, level);
                    CoreStairs(50f);
                });

                CoreDeck(root, "02 · Galeria elevada", 9.8f, () =>
                {
                    CorePlatform("Passarela da galeria", 94f, 0f, 60f, "plataforma_grossa", 1.15f);
                    CreateCheckpoint(68f, level);
                    CreateFloorPortal(72f, level);
                    CreateLaserGate(80f, level);
                    CreateFloorPortal(89f, level);
                    CreateCrate(92f, .8f);
                    CreateGuard(level, "Guarda da galeria", "guarda_pistola", "guarda_pistola_atirar_", true,
                        97f, 101f, 1.2f).pursuitRange = new Vector2(92f, 104f);
                    CreateSteamVent(106f, level);
                    CriarCaixaMunicao(level, 103f);
                    CoreWindow(92f, 5.6f, 2f);
                    CoreWindow(107f, 6.9f, 1.75f);
                    CoreAlarm(84f, 5.6f, 2f);
                    CreateLab("Análise de amostras", 101f, ColdLab, true);
                    CreateReactiveScientist(level, "Técnico de amostras", "leo", 99.7f, 1.65f, 1.1f);
                    CoreStairs(110f);
                });

                CoreDeck(root, "03 · Central de controle", 19.6f, () =>
                {
                    CorePlatform("Passarela da central", 138f, 0f, 28f, "plataforma_grossa", 1.15f);
                    CreateCheckpoint(128f, level);
                    CreateLab("Controle da pressão", 132f, ColdLab, false);
                    CreateBackgroundScientist("Pesquisador no painel", "cientista_velho_sentado_", 3, 131.7f, .06f);
                    CoreWindow(145f, 5.9f, 1.9f);
                    CoreAlarm(147f, 4.5f, 2.4f);
                    CreateCrusher(138f, level);
                    CreateGuard(level, "Guarda da central", "guarda_socador", "guarda_socador_soco_", false,
                        143f, 146f, 1.3f).pursuitRange = new Vector2(140f, 149f);
                    CreateCheckpoint(149.5f, level);
                });

                level.exitControls = new Transform[2];
                level.exitControlLamps = new SpriteRenderer[2];
                CoreDeck(root, "04 · Laboratório de observação", 19.6f, () =>
                {
                    CorePlatform("Passarela do observatório", 179.5f, 0f, 55f, "plataforma_grossa", 1.15f);
                    CreateLab("Microscopia", 159f, ColdLab, true);
                    CreateReactiveScientist(level, "Ana observando o corredor", "ana", 160.8f, 1.72f, 1.4f);
                    CoreWindow(170f, 6f, 2.2f);
                    CoreWindow(187f, 6f, 2.2f);
                    CoreWindow(204f, 6f, 2.2f);
                    CoreAlarm(175f, 6.1f, 1.8f);
                    CoreAlarm(199f, 4.9f, 2.6f);
                    CreateFloorPortal(167f, level);
                    CreateLaserGate(177f, level);
                    CreateFloorPortal(184f, level);
                    CreateCheckpoint(188f, level);
                    CreateCrate(191f, 1f);
                    CreateGuard(level, "Guarda do observatório", "guarda_pistola", "guarda_pistola_atirar_", true,
                        195f, 199f, 1.25f).pursuitRange = new Vector2(190f, 202f);
                    CoreExitControl(level, 0, 203f, "TERMINAL A");
                    ParkourTile("seta_trajeto_baixo", 210f, 3f, .85f, .85f, -8);
                });

                CoreWall("Parede do poço", 166.75f + ShaftShift, -12f, 21f, "parede_canos_esq");
                for (int index = 0; index < 6; index++)
                {
                    float height = 15f - index * 4.55f;
                    float x = (index % 2 == 0 ? 161.5f : 155.25f) + ShaftShift;
                    CorePlatform("Patamar do poço", x, height, 4.2f);
                    ParkourTile("seta_trajeto_baixo", 158.2f + ShaftShift, height + 1.4f, .7f, .7f, -8);
                }
                CoreWall("Parede da doca", 65.25f, -16f, 0f, "parede_lisa_dir");

                CoreDeck(root, "05 · Tratamento térmico", -12f, () =>
                {
                    CorePlatform("Passarela do tanque", 174.125f, 0f, 15.75f, "plataforma_grossa", 1.15f);
                    CorePlatform("Piso de tratamento", 203.65f, 0f, 35.2f, "plataforma_grossa", 1.15f);
                    CreateAcidPit(182f, 186.05f, level);
                    CreateFloorPortal(178f, level);
                    CreateFloorPortal(188.8f, level);
                    CreateCheckpoint(215.5f, level);
                    CriarCaixaMunicao(level, 212f);
                    CreateLab("Bombas do resfriamento", 207f, WarmLab, false);
                    CreateBackgroundScientist("Técnica do resfriamento", "cientista_ana_parado_", 2, 206.2f, .03f);
                    CoreAlarm(198f, 5f, 2.3f);
                    CoreAlarm(184f, 3.7f, 1.9f);
                    CreateSteamVent(205f, level);
                    CreateGuard(level, "Guarda da refrigeração", "guarda_cassetete", "guarda_cassetete_golpe_", false,
                        193.7f, 199f, 1.3f).pursuitRange = new Vector2(190f, 204f);
                    CreateCheckpoint(189f, level);
                });

                CoreDeck(root, "06 · Doca subterrânea", -12f, () =>
                {
                    CorePlatform("Piso da doca", 116f, 0f, 100.5f, "plataforma_grossa", 1.15f);
                    CreateCheckpoint(158.25f, level);
                    CriarCaixaMunicao(level, 157f);
                    CreateCrate(152f, 1.25f);
                    CreateTurret(level, 140f);
                    art.GetComponentInChildren<GuardaSeguranca>().startsFacingLeft = false;
                    CreateCheckpoint(131f, level);
                    CreateCrusher(122f, level);
                    CoreExitControl(level, 1, 117f, "TERMINAL B");
                    CriarCaixaMunicao(level, 115f);
                    CreateCrate(108f, .95f);
                    CreateRetractingSpikes(99f, 3.4f, level);
                    CreateCheckpoint(94f, level);
                    CreateLab("Supervisão da doca", 103f, WarmLab, false);
                    CreateBackgroundScientist("Operador da doca", "cientista_leo_sentado_", 3, 102.8f, .04f);
                    CoreAlarm(109f, 4.8f, 1.7f);
                    CoreAlarm(77f, 4.5f, 2.2f);
                    var armored = CreateGuard(level, "Guarda antimotim da doca", "guarda_antimotim", null, false,
                        79f, 87f, .95f);
                    armored.pursuitRange = new Vector2(75f, 92f);
                    armored.frontArmor = true;
                    armored.health = 100f;
                    level.exit = Place("porta_fechada", 69f, 1.95f, 1.3f, 3, Color.white).transform;
                    level.openDoorSprite = S("porta_aberta");
                });

                if (!EditorSceneManager.SaveScene(scene, CoreScenePath))
                    throw new IOException("Não foi possível salvar o Núcleo de manutenção.");
                Debug.Log("NYX_CORE_SAVED");
            }
            finally
            {
                if (additive)
                {
                    SceneManager.SetActiveScene(original);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        static void CoreDeck(Transform root, string name, float height, Action build)
        {
            var deck = new GameObject(name).transform;
            deck.SetParent(root, false);
            art = deck;
            build();
            deck.localPosition = Vector3.up * height;
            art = root;
        }

        static void CoreWindow(float x, float y, float scale)
        {
            var window = Place("janela_terra", x, y, scale, -12, new Color(.87f, .91f, 1f));
            window.name = "Janela para a superfície lunar";
        }

        static void CoreAlarm(float x, float y, float fps)
        {
            var lamp = Place("alarme_on", x, y, 1.15f, -8, Color.white);
            lamp.name = "Alarme de emergência";
            var blink = lamp.gameObject.AddComponent<SpriteLoop>();
            blink.target = lamp;
            blink.frames = new[] { S("alarme_on"), S("alarme_off") };
            blink.fps = fps;
        }

        static void CoreExitControl(MaintenanceLevel level, int index, float x, string label)
        {
            var terminal = PlaceOnSurface("terminal_1", x, 0f, 1.9f, -5, Color.white);
            terminal.name = label;
            level.exitControls[index] = terminal.transform;
            var lamp = Place("alarme_on", x, 2.5f, .67f, -4, new Color(1f, .53f, .24f));
            lamp.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Nyx/Art/pixel.png");
            lamp.transform.localScale = new Vector3(.24f, .14f, 1f);
            level.exitControlLamps[index] = lamp;
            CreateSectorLabel(label + "  ·  E", x, 3.65f);
        }

        static void ImportParkourArt()
        {
            var folder = Extras + ParkourFolder;
            if (!Directory.Exists(folder))
                throw new DirectoryNotFoundException(folder);
            foreach (var file in Directory.GetFiles(folder, "*.png"))
            {
                string path = file.Replace('\\', '/');
                if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                    (importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Point))
                    Import(path);
            }
        }

        static void CoreStairs(float startX)
        {
            for (int index = 0; index < 6; index++)
            {
                float x = startX + index * 2.5f;
                float top = (index + 1) * 1.4f;
                CorePlatform("Degrau de manutenção", x, top, 3.2f);
                CoreStairSupport(x, top - CoreStepThickness);
            }
        }

        static void CoreStairSupport(float x, float underside)
        {
            var column = new GameObject("Treliça do degrau").transform;
            column.SetParent(art, false);
            column.localPosition = new Vector3(x, 0f, 0f);
            float height = underside + .04f;
            int segments = Mathf.CeilToInt(height / 1.4f);
            float segmentHeight = height / segments;
            for (int index = 0; index < segments; index++)
                ParkourTile("suporte_trelica", x, (index + .5f) * segmentHeight,
                    .85f, segmentHeight, -9, column);
        }

        static void CorePlatform(string name, float x, float top, float width,
            string style = "plataforma_fina", float height = CoreStepThickness)
        {
            var piece = CoreCollider(name, x, top - height * .5f, width, height);
            int segments = Mathf.Max(2, Mathf.CeilToInt(width / 1.45f));
            float tileWidth = width / segments;
            for (int index = 0; index < segments; index++)
            {
                string side = index == 0 ? "_esq" : index == segments - 1 ? "_dir" : "_meio";
                float tileX = x - width * .5f + tileWidth * (index + .5f);
                ParkourTile(style + side, tileX, top - height * .5f, tileWidth, height, 1, piece);
            }
        }

        static void CoreWall(string name, float x, float bottom, float top, string style)
        {
            const float width = 1.5f;
            float height = top - bottom;
            var piece = CoreCollider(name, x, bottom + height * .5f, width, height);
            int segments = Mathf.CeilToInt(height / width);
            float tileHeight = height / segments;
            for (int index = 0; index < segments; index++)
                ParkourTile(style, x, bottom + tileHeight * (index + .5f), width, tileHeight, 1, piece);
        }

        static Transform CoreCollider(string name, float x, float y, float width, float height)
        {
            var piece = new GameObject(name);
            piece.transform.SetParent(art, false);
            piece.transform.position = new Vector3(x, y, 0f);
            piece.layer = 8;
            piece.AddComponent<BoxCollider2D>().size = new Vector2(width, height);
            return piece.transform;
        }

        static void ParkourTile(string name, float x, float y, float width, float height, int order,
            Transform parent = null)
        {
            var tile = PlaceExtra(ParkourFolder, name, x, y, 1f, order);
            if (tile.sprite == null)
                throw new InvalidDataException("Sprite de parkour não encontrado: " + name);
            var bounds = tile.sprite.bounds;
            tile.transform.localScale = new Vector3(width / bounds.size.x, height / bounds.size.y, 1f);
            if (parent != null)
                tile.transform.SetParent(parent, true);
        }
    }
}
