using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Nyx.Editor
{
    // Monta a ala nova da fase com cenário, inimigos e caminhos de portal.
    public static partial class ImportClaudeArt
    {
        public const string SecurityScenePath = "Assets/Nyx/Scenes/Fuga_02.unity";
        const int WingLeft = 64, WingRight = 284;
        const float Vigilancia = 103f, Laboratorio = 144f, Contencao = 186f, Doca = 228f;

        public static void SaveSecurityWing()
        {
            var previousScene = SceneManager.GetActiveScene();
            bool hasSavedScene = !string.IsNullOrEmpty(previousScene.path);
            if (!hasSavedScene && previousScene.isDirty)
                throw new InvalidOperationException("Salve a cena aberta antes de montar a ala de segurança.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                hasSavedScene ? NewSceneMode.Additive : NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            try
            {
                art = new GameObject("Ala de segurança").transform;
                var level = new GameObject("Fase 02 - Fuga").AddComponent<MaintenanceLevel>();
                level.phaseTwo = true;
                level.cameraMinX = 74f;
                level.cameraMaxX = WingRight - 1;
                level.cameraRoom = Rect.MinMaxRect(WingLeft, -4f, WingRight, 12.2f);
                level.boltSprite = S("portal_roxo_0");
                level.purplePortalFrames = Frames("portal_roxo_", 4);
                level.greenPortalFrames = Frames("portal_verde_", 4);
                level.weaponBulletSprite = Extra("Armadilhas", "bala_jogador");

                var camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.orthographic = true;
                camera.orthographicSize = 7.5f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.03f, .05f, .09f);
                camera.transform.position = new Vector3(74f, 4.2f, -10f);
                camera.gameObject.AddComponent<AudioListener>();
                level.gameCamera = camera;

                PlaceWallTiles(WingLeft, WingRight);
                BuildRoomFrame();
                BuildSecurityWing(level);
                CreateSecurityPlayer(level);

                var pistol = PlaceExtra("Armadilhas", "pistola_item", 70.5f, .65f, 2f, 8);
                pistol.gameObject.name = "Pistola de segurança";
                level.weaponPickup = pistol.gameObject;
                var exitDoor = Place("porta_fechada", 280.5f, 1.95f, 1.3f, 3, Color.white);
                exitDoor.gameObject.name = "Saída da ala de segurança";
                level.exit = exitDoor.transform;
                level.openDoorSprite = S("porta_aberta");

                if (!EditorSceneManager.SaveScene(scene, SecurityScenePath))
                    throw new IOException("A Unity não conseguiu salvar a cena da ala de segurança.");
            }
            finally
            {
                if (previousScene.IsValid() && previousScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousScene);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        static void BuildRoomFrame()
        {
            foreach (float x in new[] { WingLeft + .5f, WingRight - .5f })
            {
                var wall = new GameObject(x < 100f ? "Parede de entrada" : "Parede de fundo");
                wall.transform.SetParent(art, false);
                wall.transform.position = new Vector3(x, 4.45f, 0);
                wall.layer = 8;
                wall.AddComponent<BoxCollider2D>().size = new Vector2(1f, 13.9f);
                BuildLimitWallArt(x, x < 100f, wall.transform);
            }

            var roof = new GameObject("Teto da ala");
            roof.transform.SetParent(art, false);
            roof.transform.position = new Vector3((WingLeft + WingRight) * .5f, 11.8f, 0);
            roof.layer = 8;
            roof.AddComponent<BoxCollider2D>().size = new Vector2(WingRight - WingLeft, .8f);
            for (int x = WingLeft; x < WingRight; x++)
            {
                string tile = x % 9 == 0 ? "teto_luz_on" : "teto";
                var ceilingTile = Place(tile, x + .5f, 11.8f, 1f, 2, Color.white);
                ceilingTile.transform.localScale = new Vector3(1f, .8f, 1f);
                ceilingTile.transform.SetParent(roof.transform, true);
            }
        }

        static void BuildLimitWallArt(float x, bool left, Transform parent = null)
        {
            string side = left ? "esq" : "dir";
            for (int y = -4; y < 13; y++)
            {
                string surface = y switch
                {
                    -1 => "base",
                    3 => "perigo",
                    6 => "luz_on",
                    8 => "cano",
                    11 => "topo",
                    _ => y % 3 == 0 ? "lisa_b" : "lisa"
                };
                string name = $"parede_limite_{side}_{surface}";
                var tile = Extra("Limites", name) != null
                    ? PlaceExtra("Limites", name, x, y + .5f, 1f, 2)
                    : Place("parede", x, y + .5f, 1f, 2, Color.white);
                if (parent != null)
                    tile.transform.SetParent(parent, true);
            }
        }

        static void CreateSecurityPlayer(MaintenanceLevel level)
        {
            var character = new GameObject("Prisioneiro");
            character.transform.position = new Vector3(65.8f, .15f, 0);
            var player = character.AddComponent<Prisoner>();
            player.level = level;
            level.player = player;

            player.body = character.AddComponent<Rigidbody2D>();
            player.body.gravityScale = 2.4f;
            var collider = character.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(.75f, 1.8f);
            collider.offset = new Vector2(0, .9f);
            collider.edgeRadius = .07f;
            player.bodyCollider = collider;

            var visual = new GameObject("Sprite").transform;
            visual.SetParent(character.transform, false);
            visual.localScale = Vector3.one * 2f;
            player.visual = visual;
            player.sprite = visual.gameObject.AddComponent<SpriteRenderer>();
            player.sprite.sprite = S("idle_0", true);
            player.sprite.sortingOrder = 10;

            var animator = character.AddComponent<PrisonerSpriteAnimator>();
            animator.player = player;
            animator.neckPixel = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Nyx/Art/pixel.png");
            animator.idle = Frames("idle_", 4, true);
            animator.walk = Frames("walk_", 6, true);
            animator.walkBack = (Sprite[])animator.walk.Clone();
            Array.Reverse(animator.walkBack);
            animator.idleGun = Frames("idle_gun_", 2, true);
            animator.walkGun = Frames("walk_gun_", 6, true);
            animator.shoot = Frames("shoot_", 3, true);
            animator.jump = S("jump_0", true);
            animator.fall = S("fall_0", true);

            player.portalWeapon = AttachEquipment(character.transform, "Lançador de portais",
                Extra("Armadilhas", "arma_portal_icone"), 13, Color.white);
            player.weapon = player.portalWeapon;
            player.firearm = AttachEquipment(character.transform, "Pistola",
                Extra("Armadilhas", "pistola_icone"), 13, Color.white);
            player.firearm.gameObject.SetActive(false);
            player.arm = AttachEquipment(character.transform, "Braço de mira",
                animator.neckPixel, 11, new Color(.54f, .73f, .67f));
            player.arm.localScale = new Vector3(.34f, .13f, 1f);
        }

        static Transform AttachEquipment(Transform character, string name, Sprite sprite, int order, Color color)
        {
            var item = new GameObject(name).transform;
            item.SetParent(character, false);
            var renderer = item.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            renderer.color = color;
            return item;
        }

        static void BuildSecurityWing(MaintenanceLevel level)
        {
            const string triagem = "TRIAGEM", vigilancia = "VIGILÂNCIA", laboratorio = "LABORATÓRIO";
            const string contencao = "CONTENÇÃO", doca = "DOCA DE EVACUAÇÃO";
            SalaDeTreino(level, 65f, Vigilancia, triagem);
            SalaDeTreino(level, Vigilancia, Laboratorio, vigilancia);
            SalaDeTreino(level, Laboratorio, Contencao, laboratorio);
            SalaDeTreino(level, Contencao, Doca, contencao);
            SalaDeTreino(level, Doca, WingRight - 1f, doca, temVao: true);

            CreateGuard(level, "Guarda do corredor", "guarda_cassetete", "guarda_cassetete_golpe_", false, 86, 91, 1.25f)
                .pursuitRange = new Vector2(81.5f, 93.5f);
            CreateRetractingSpikes(98f, 2.8f, level);
            CreateCrate(77f, .8f);

            CreateGuard(level, "Guarda armado", "guarda_pistola", "guarda_pistola_atirar_", true, 124, 132, 1.1f)
                .pursuitRange = new Vector2(119f, 134.8f);
            CreateLaserGate(139f, level);
            CreateCrate(118f, 1f);
            CreateFloorPortal(112f, level);
            CreateFloorPortal(135.8f, level);

            var armored = CreateGuard(level, "Guarda antimotim", "guarda_antimotim", null, false, 164, 173, .98f);
            armored.pursuitRange = new Vector2(158f, 176f);
            armored.frontArmor = true;
            CreateSteamVent(180.5f, level);

            CreateGuard(level, "Guarda socador", "guarda_socador", "guarda_socador_soco_", false, 205, 214, 1.45f)
                .pursuitRange = new Vector2(199f, 216.5f);
            CreateCrusher(222f, level);

            CreateFloorSegment("Piso antes do tanque", Doca, 259f);
            CreateFloorSegment("Piso depois do tanque", 266f, WingRight - 1f);
            CreateTurret(level, 248.5f);
            CreateCrate(239.2f, 1f);
            CreateAcidPit(259f, 266f, level);
            CreatePortalWall(259f, Vector2.left, level);
            CreateCeilingPortal(270f, level);
            CreateCheckpoint(272f, level);

            CreateLab("Triagem", 76.2f, new Color(.7f, .87f, .94f), false);
            CreateLab("Análise de amostras", 151.5f, new Color(.72f, .92f, .82f), true);
            CreateLab("Monitoramento", 194.5f, new Color(.83f, .79f, .99f), false);
            PlaceOnSurface("terminal_1", 233.5f, 0f, 1.9f, -10, Color.white);
            PlaceOnSurface("armario", 235.3f, 0f, 1.5f, -12, new Color(.73f,.77f,.93f));
            foreach (float windowX in new[]{70f, 113.5f, 154f, 197.5f, 236.7f, 275f})
                Place("janela_terra", windowX, 6.25f, 1.4f, -12, new Color(.81f,.86f,1f));

            CreateReactiveScientist(level, "Ana", "ana", 75f, 1.85f, 2.4f);
            CreateBackgroundScientist("Leo analisando amostras", "cientista_leo_parado_", 2, 151.5f, .2f);
            CreateBackgroundScientist("Pesquisador no monitoramento", "cientista_velho_sentado_", 3, 194.5f, .18f);
            CreateBackgroundScientist("Operadora preparando o posto", "cientista_ana_sentado_", 3, 77.9f, .18f);
            CreateBackgroundScientist("Técnico da doca", "cientista_leo_parado_", 2, 234.4f, .03f);

            ConfigurarTutorial(level,
                Dica(64f,81.5f,triagem,"Uma pistola no chão","Chegue perto dela e pressione <b>E</b>. Há espaço aqui para experimentar a mira.",
                    AssuntoTutorial.Equipamento,TutorialFrames("painel_interagir_",8)),
                Dica(81.5f,94.8f,triagem,"Guarda com cassetete","Ele precisa se aproximar para bater. Se aparecer um <b>!</b>, recue e mantenha distância.",
                    quadros:TutorialFrames("painel_inimigo_cassetete_",15)),
                Dica(94.8f,Vigilancia,triagem,"Espinhos retráteis","Espere as pontas baixarem. Atravesse o trecho de uma vez, antes que elas subam."),
                Dica(Vigilancia,136.5f,vigilancia,"Pistoleiro","Use a caixa como cobertura. As placas brancas do piso também permitem sair pelo outro lado do guarda.",
                    quadros:TutorialFrames("painel_inimigo_pistoleiro_",15)),
                Dica(136.5f,Laboratorio,vigilancia,"Barreira a laser","O alarme avisa antes do feixe. Espere a luz e o laser apagarem para passar."),
                Dica(Laboratorio,177f,laboratorio,"Guarda antimotim","O escudo segura boa parte dos tiros pela frente. Passe por ele e atire pelas costas.",
                    quadros:TutorialFrames("painel_inimigo_antimotim_",15)),
                Dica(177f,Contencao,laboratorio,"Saída de vapor","O bocal avisa antes de soltar vapor. Fique fora da grade, espere o jato parar e siga."),
                Dica(Contencao,218f,contencao,"Guarda socador","Esse corre para encurtar a distância. Dê espaço para trás quando ele levantar os punhos.",
                    quadros:TutorialFrames("painel_inimigo_socador_",15)),
                Dica(218f,Doca,contencao,"Esmagador","Quando o alarme piscar, espere. Passe assim que a cabeça começar a voltar para o alto."),
                Dica(Doca,254f,doca,"Torreta","A linha vermelha mostra para onde ela mira. Use a caixa para se esconder ou saia da linha de tiro.",
                    quadros:TutorialFrames("painel_inimigo_torreta_",12)),
                Dica(254f,273.5f,doca,"Tanque de ácido","Abra um portal na placa junto ao tanque e outro no teto do outro lado. Use <b>Q</b> se a pistola estiver na mão."),
                Dica(273.5f,WingRight,doca,"Saída à vista","Aproxime-se da porta e aperte <b>E</b> para sair da ala."));
        }

        static void SalaDeTreino(MaintenanceLevel level,float inicio,float fim,string name,bool temVao=false)
        {
            if(!temVao) CreateFloorSegment("Piso - "+name,inicio,fim);
            MarcaDeSetor(inicio,name);
            CreateCheckpoint(inicio+1.45f,level);
            Place("camera_0",inicio+7.3f,8f,1.3f,-10,Color.white);
            Place("alarme_off",fim-5f,5.6f,1.18f,-10,Color.white);
        }

        static void MarcaDeSetor(float x,string name)
        {
            var tone=new Color(.64f,.76f,.87f);
            PlaceScaled("parede",x,5.75f,.7f,11.5f,-15,tone);
            PlaceScaled("parede_listra",x,3.1f,.72f,.8f,-14,Color.white);
            PlaceScaled("teto_luz_on",x+2.4f,10.32f,4.4f,.52f,-12,tone);
            CreateSectorLabel(name,x+7f);
        }

        static void CreateFloorSegment(string name, float startX, float endX)
        {
            var floor = new GameObject(name);
            floor.transform.SetParent(art, true);
            floor.transform.position = new Vector3((startX + endX) * .5f, -.5f, 0);
            floor.layer = 8;

            var collider = floor.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(endX - startX, 1f);
            PlaceFloorTiles(startX, endX);
        }

        static void PlaceFloorTiles(float startX, float endX)
        {
            for (float x = startX; x < endX; x += 2f)
            {
                float width = Mathf.Min(2f, endX - x);
                var top = Place("chao_topo", x + width * .5f, -1, 2, 1, Color.white);
                var bottom = Place("chao_miolo", x + width * .5f, -3, 2, 1, new Color(.7f, .7f, .8f));
                top.transform.localScale = bottom.transform.localScale = new Vector3(width, 2f, 1f);
            }
        }

        static void CreateCeilingPortal(float x, MaintenanceLevel level)
        {
            var surface = CreatePortalWall(x, Vector2.down, level);
            surface.transform.SetPositionAndRotation(new Vector3(x, 11f, 0), Quaternion.Euler(0, 0, 90));
            PlaceScaled("parede", x, 11.55f, 4f, .5f, 0, new Color(.72f, .82f, .93f));
            foreach (float side in new[] { -1f, 1f })
                PlaceScaled("parede", x + side * 1.7f, 11.12f, .38f, .68f, 2, Color.white);
        }

        static void CreateFloorPortal(float x,MaintenanceLevel level)
        {
            var surface=CreatePortalWall(x,Vector2.up,level);
            surface.transform.SetPositionAndRotation(new Vector3(x,-.5f,0f),Quaternion.Euler(0,0,90));
        }

        static PortalSurface CreatePortalWall(float x, Vector2 normal, MaintenanceLevel level)
        {
            var panel = Place("superficie_portal_grande", x, 1.5f, 1f, 2, Color.white);
            var wall = panel.gameObject;
            wall.layer = 8;

            var solid = wall.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(1f, 3f);

            var target = wall.AddComponent<BoxCollider2D>();
            target.size = new Vector2(1f, 3f);
            target.isTrigger = true;

            var surface = wall.AddComponent<PortalSurface>();
            surface.collider2D = target;
            surface.solidCollider = solid;
            surface.fixedNormal = normal;
            surface.purpleFrames = level.purplePortalFrames;
            surface.greenFrames = level.greenPortalFrames;
            if (Mathf.Abs(normal.x) > .7f)
            {
                PlaceScaled("parede", x - .2f, .18f, 1.5f, .36f, 1, Color.white);
                PlaceScaled("parede_cano_v", x + .25f, 7.2f, .66f, 8.4f, -14,
                    new Color(.76f, .84f, .92f));
            }
            return surface;
        }

        static void CreateAcidPit(float left, float right, MaintenanceLevel level)
        {
            float width = right - left;
            float centerX = (left + right) * .5f;
            var frames = ExtraFrames("Armadilhas", "acido_", 3);
            int tileCount = Mathf.CeilToInt(width);
            float tileWidth = width / tileCount;

            for (int i = 0; i < tileCount; i++)
            {
                float x = centerX - width * .5f + tileWidth * (i + .5f);
                var acid = PlaceExtra("Armadilhas", "acido_0", x, -.94f, 1f, 4);
                acid.transform.localScale = new Vector3(tileWidth, 1.2f, 1f);
                var animation = acid.gameObject.AddComponent<SpriteLoop>();
                animation.target = acid;
                animation.frames = frames;
                animation.fps = 3f;
            }

            var acidBottom = PlaceExtra("Armadilhas", "acido_miolo", centerX, -2.55f, 1f, 3);
            acidBottom.transform.localScale = new Vector3(width, 2.25f, 1f);
            PlaceScaled("parede", centerX, -3.75f, width, .55f, 2, new Color(.66f, .74f, .82f));
            PlaceScaled("chao_perigo", left - .36f, -.18f, .72f, .36f, 3, Color.white);
            PlaceScaled("chao_perigo", right + .36f, -.18f, .72f, .36f, 3, Color.white);

            var zone = new GameObject("Ácido no fundo do vão");
            zone.transform.SetParent(art, true);
            zone.transform.position = new Vector3(centerX, -2.25f, 0);
            var trigger = zone.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(width, 3.7f);
            zone.AddComponent<NyxDamageZone>().level = level;
        }

        static void CreateCheckpoint(float x, MaintenanceLevel level)
        {
            var marker = PlaceExtra("Armadilhas", "checkpoint_off", x, .65f, 1.2f, 5);
            var collider = marker.gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.65f, 8.5f);
            collider.offset = new Vector2(0, 3.1f);

            var checkpoint = marker.gameObject.AddComponent<NyxCheckpoint>();
            checkpoint.level = level;
            checkpoint.markerVisual = marker;
            checkpoint.activeSprite = Extra("Armadilhas", "checkpoint_on");
            checkpoint.spawnOffset = new Vector2(0, -.57f);
        }

        static void CreateRetractingSpikes(float centerX, float width, MaintenanceLevel level)
        {
            var trap = new GameObject("Espinhos retráteis");
            trap.transform.SetParent(art, true);
            trap.transform.position = new Vector3(centerX, .22f, 0);
            trap.transform.localScale = new Vector3(width, 1f, 1f);

            var trigger = trap.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(.94f, .7f);

            var warning = new GameObject("Aviso dos espinhos").transform;
            warning.SetParent(trap.transform, false);
            var warningSprite = warning.gameObject.AddComponent<SpriteRenderer>();
            warningSprite.sprite = Extra("Armadilhas", "espinhos_retratil_0");
            warningSprite.sortingOrder = 6;

            var danger = new GameObject("Espinhos ativos").transform;
            danger.SetParent(trap.transform, false);
            var dangerSprite = danger.gameObject.AddComponent<SpriteRenderer>();
            dangerSprite.sprite = Extra("Armadilhas", "espinhos_retratil_1");
            dangerSprite.sortingOrder = 6;

            var timedTrap = trap.AddComponent<NyxTimedTrap>();
            timedTrap.level = level;
            timedTrap.dangerSprite = dangerSprite;
            timedTrap.warningSprite = warningSprite;
            timedTrap.dangerFrames = ExtraFrames("Armadilhas", "espinhos_retratil_", 3);
            timedTrap.warningSeconds = .65f;
            timedTrap.activeSeconds = .85f;
            timedTrap.restSeconds = 2.25f;
            timedTrap.phaseOffset = 1.1f;
        }

        static void CreateLaserGate(float x, MaintenanceLevel level)
        {
            var gate = new GameObject("Laser temporizado");
            gate.transform.SetParent(art, true);
            gate.transform.position = new Vector3(x, 2.65f, 0);

            var trigger = gate.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(.7f, 5.3f);

            var barrier = gate.AddComponent<BoxCollider2D>();
            barrier.size = trigger.size;

            var beam = new GameObject("Feixe de laser").transform;
            beam.SetParent(gate.transform, false);
            beam.localScale = new Vector3(.55f, 5.3f, 1f);
            var beamSprite = beam.gameObject.AddComponent<SpriteRenderer>();
            beamSprite.sprite = S("laser_feixe_0");
            beamSprite.sortingOrder = 5;

            var warning = Place("alarme_on", x, 5.8f, 1.15f, 6, Color.white);
            Place("laser_emissor_base", x, .25f, 1f, 4, Color.white);
            Place("laser_emissor_topo", x, 5.05f, 1f, 4, Color.white);

            var timedTrap = gate.AddComponent<NyxTimedTrap>();
            timedTrap.level = level;
            timedTrap.dangerSprite = beamSprite;
            timedTrap.warningSprite = warning;
            timedTrap.dangerFrames = new[] { S("laser_feixe_0"), S("laser_feixe_1") };
            timedTrap.warningSeconds = .55f;
            timedTrap.activeSeconds = 1.1f;
            timedTrap.restSeconds = 2.4f;
            timedTrap.phaseOffset = .35f;
        }

        static void CreateSteamVent(float x, MaintenanceLevel level)
        {
            PlaceExtra("Armadilhas", "vapor_bocal", x, .12f, 1.15f, 5);
            var warning = PlaceExtra("Armadilhas", "vapor_bocal_aviso", x, .12f, 1.15f, 6);

            var vent = new GameObject("Jato de vapor temporizado");
            vent.transform.SetParent(art, true);
            vent.transform.position = new Vector3(x, 1.2f, 0);

            var trigger = vent.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(.85f, 2.2f);

            var jet = new GameObject("Vapor quente").transform;
            jet.SetParent(vent.transform, false);
            jet.localScale = new Vector3(1f, 1.4f, 1f);
            var jetSprite = jet.gameObject.AddComponent<SpriteRenderer>();
            jetSprite.sprite = Extra("Armadilhas", "vapor_jato_0");
            jetSprite.sortingOrder = 5;

            var timedTrap = vent.AddComponent<NyxTimedTrap>();
            timedTrap.level = level;
            timedTrap.dangerSprite = jetSprite;
            timedTrap.warningSprite = warning;
            timedTrap.dangerFrames = ExtraFrames("Armadilhas", "vapor_jato_", 4);
            timedTrap.warningSeconds = .7f;
            timedTrap.activeSeconds = 1f;
            timedTrap.restSeconds = 2.5f;
            timedTrap.phaseOffset = 1.8f;
        }

        static void CreateCrusher(float x, MaintenanceLevel level)
        {
            PlaceExtra("Armadilhas", "esmagador_base", x, 6f, 1.3f, 3);
            var rod = PlaceExtra("Armadilhas", "esmagador_haste", x, 5.15f, 1.3f, 3);

            var warning = Place("alarme_on", x, 4.15f, .9f, 6, Color.white);
            var head = PlaceExtra("Armadilhas", "esmagador_cabeca", x, 3.6f, 1.5f, 7);
            var trigger = head.gameObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(.62f, .82f);

            var crusher = head.gameObject.AddComponent<NyxCrusherTrap>();
            crusher.level = level;
            crusher.warningLight = warning;
            crusher.rod = rod;
            crusher.rodAnchorY = 5.8f;
            crusher.topY = 4.15f;
            crusher.bottomY = .55f;
            crusher.warningSeconds = .72f;
            crusher.dropSeconds = .31f;
            crusher.phaseOffset = 2.1f;
        }

        static void CreateCrate(float x, float scale)
        {
            var crate = Place("caixa_pequena", x, 0, scale, 4, Color.white);
            ConfigureCrate(crate, crate.sprite, scale);
        }

        static GuardaSeguranca CreateGuard(MaintenanceLevel level, string objectName, string style, string attackPrefix,
            bool hasGun, float left, float right, float speed)
        {
            var idle = ExtraFrames("Guardas", style + "_parado_", 2);
            var walking = ExtraFrames("Guardas", style + "_andar_", 6);
            var attack = string.IsNullOrEmpty(attackPrefix)
                ? new Sprite[0]
                : ExtraFrames("Guardas", attackPrefix, hasGun ? 2 : 4);
            var death = ExtraFrames("Guardas", style + "_morte_", 4);

            var guardObject = new GameObject(objectName);
            guardObject.transform.SetParent(art, true);
            guardObject.transform.position = new Vector3(left, 0, 0);
            guardObject.transform.localScale = Vector3.one * 1.75f;

            var renderer = guardObject.AddComponent<SpriteRenderer>();
            renderer.sprite = idle[0];
            renderer.sortingOrder = 5;

            var collider = guardObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(.5f, .85f);
            collider.offset = new Vector2(0, .5f);

            var guard = guardObject.AddComponent<GuardaSeguranca>();
            guard.level = level;
            guard.spriteRenderer = renderer;
            guard.idleFrames = idle;
            guard.walkFrames = walking;
            guard.attackFrames = attack;
            guard.deathFrames = death;
            guard.bulletSprite = Extra("Armadilhas", "bala_inimigo");
            guard.patrolStart = left;
            guard.patrolEnd = right;
            guard.walkSpeed = speed;
            guard.usesGun = hasGun;
            guard.alertMark = CreateGuardAlert(guard.transform, 1.18f);
            return guard;
        }

        static void CreateTurret(MaintenanceLevel level, float x)
        {
            PlaceScaled("parede", x, .75f, 1.5f, 1.5f, -7, new Color(.77f,.83f,.9f));
            var turret = PlaceExtra("Armadilhas", "torreta_idle", x, .4f, 1.5f, 5);
            var collider = turret.gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(.8f, .65f);
            collider.offset = new Vector2(0, .12f);

            var guard = turret.gameObject.AddComponent<GuardaSeguranca>();
            guard.level = level;
            guard.spriteRenderer = turret;
            guard.idleFrames = new[] { Extra("Armadilhas", "torreta_idle") };
            guard.aimFrames = new[] { Extra("Armadilhas", "torreta_aim") };
            guard.attackFrames = new[] { Extra("Armadilhas", "torreta_shoot") };
            guard.bulletSprite = Extra("Armadilhas", "bala_inimigo");
            guard.patrolStart = x;
            guard.patrolEnd = x;
            guard.usesGun = true;
            guard.startsFacingLeft = true;
            guard.detectionRange = 11f;
            guard.shotCooldown = 2.8f;
            guard.alertMark = CreateGuardAlert(guard.transform, .75f);
            turret.flipX = true;
            var beam = new GameObject("Mira vermelha da torreta");
            beam.transform.SetParent(art, false);
            guard.laserSight = beam.AddComponent<SpriteRenderer>();
            guard.laserSight.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Nyx/Art/pixel.png");
            guard.laserSight.color = new Color(1f,.18f,.22f,.65f);
            guard.laserSight.sortingOrder = 14;
            guard.laserSight.enabled = false;
        }

        static TextMesh CreateGuardAlert(Transform guard, float height)
        {
            var indicator = new GameObject("Guarda avistou o jogador");
            indicator.transform.SetParent(guard, false);
            indicator.transform.localPosition = new Vector3(0f, height, 0f);
            var mark = indicator.AddComponent<TextMesh>();
            mark.text = "!";
            mark.anchor = TextAnchor.MiddleCenter;
            mark.fontSize = 38;
            mark.fontStyle = FontStyle.Bold;
            mark.characterSize = .075f;
            mark.color = new Color(1f, .67f, .24f);
            mark.GetComponent<MeshRenderer>().sortingOrder = 18;
            indicator.SetActive(false);
            return mark;
        }

        static void AddScientist(string prefix, int frameCount, float x, float y)
        {
            var renderer = PlaceExtra("Personagens", prefix + "0", x, y, 1.72f, -6);
            var animation = renderer.gameObject.AddComponent<SpriteLoop>();
            animation.target = renderer;
            animation.frames = ExtraFrames("Personagens", prefix, frameCount);
            animation.fps = 2.2f;
        }

        static void CreateBackgroundScientist(string name, string prefix, int frameCount, float x, float y)
        {
            var renderer = PlaceExtra("Personagens", prefix + "0", x, y, 1.68f, -12);
            renderer.gameObject.name = name;
            renderer.color = new Color(.75f, .84f, .94f);
            var animation = renderer.gameObject.AddComponent<SpriteLoop>();
            animation.target = renderer;
            animation.frames = ExtraFrames("Personagens", prefix, frameCount);
            animation.fps = 1.7f;
        }

        static void CreateReactiveScientist(MaintenanceLevel level, string name, string portrait,
            float x, float scale, float moveRange)
        {
            string prefix = "cientista_" + portrait + "_";
            var renderer = PlaceExtra("Personagens", prefix + "parado_0", x, .02f, scale, -6);
            renderer.gameObject.name = name;
            var scientist = renderer.gameObject.AddComponent<CientistaLaboratorio>();
            scientist.level = level;
            scientist.spriteRenderer = renderer;
            scientist.idleFrames = ExtraFrames("Personagens", prefix + "parado_", 2);
            scientist.startledFrames = ExtraFrames("Personagens", prefix + "panico_", 2);
            scientist.walkFrames = ExtraFrames("Personagens", prefix + "andar_", 4);
            scientist.followPlayer = moveRange > 0f;
            scientist.moveRange = moveRange;
        }

        static void CreateLab(string name, float x, Color tint, bool samples)
        {
            var station = new GameObject("Laboratório - " + name).transform;
            station.SetParent(art, false);
            station.position = new Vector3(x, 0, 0);
            var previousArt = art;
            art = station;
            try
            {
                PlaceScaled("parede_fundo", x, 2.9f, 5.6f, 5.8f, -18, tint * .64f);
                PlaceScaled("parede", x, 3.95f, 5.65f, .18f, -15, tint);
                foreach (float side in new[] { -1f, 1f })
                    PlaceScaled("parede", x + 2.86f * side, 2.9f, .22f, 5.8f, -14, tint);
                PlaceScaled("parede_fundo", x, 1.03f, 5.55f, 2.05f, -11, new Color(.54f, .76f, .9f, .16f));

                float benchScale = samples ? 2.15f : 1.7f;
                var bench = PlaceOnSurface("mesa_lab", x, 0f, benchScale, -10, tint);
                bench.gameObject.name = "Bancada apoiada no chão";
                float benchTop = benchScale * 16f / 24f;
                if (samples)
                    PlaceOnSurface("frasco", x - 1.18f, benchTop, 1.03f, -9, Color.white);
                PlaceOnSurface(samples ? "terminal_0" : "terminal_1", x + .9f, benchTop, 1.15f, -9, tint);
                if (!samples)
                    PlaceOnSurface("armario", x - 2.2f, 0f, 1.55f, -12, tint);

                var light = PlaceScaled("teto_luz_on", x, 4.2f, 2.7f, .54f, -13, tint);
                light.gameObject.name = "Luminária da bancada";
            }
            finally
            {
                art = previousArt;
            }
        }

        static SpriteRenderer PlaceOnSurface(string name, float x, float floorY, float scale, int order, Color tint)
        {
            var prop = Place(name, x, floorY, scale, order, tint);
            prop.transform.position += Vector3.up * (prop.sprite.pivot.y / prop.sprite.pixelsPerUnit * scale);
            return prop;
        }

        static SpriteRenderer PlaceScaled(string name, float x, float y, float width, float height, int order, Color tint)
        {
            var prop = Place(name, x, y, 1f, order, tint);
            var size = prop.sprite.bounds.size;
            prop.transform.localScale = new Vector3(width / size.x, height / size.y, 1f);
            return prop;
        }

        static void CreateSectorLabel(string text, float x)
        {
            var label = new GameObject(text);
            label.transform.SetParent(art, true);
            label.transform.position = new Vector3(x, 9.4f, 0);

            var textMesh = label.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.fontSize = 40;
            textMesh.characterSize = .09f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = new Color(.68f, .82f, .9f);

            var renderer = label.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sortingOrder = -5;
        }
    }
}
