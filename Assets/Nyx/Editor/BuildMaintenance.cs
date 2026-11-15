using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Nyx.Editor {
public static class BuildMaintenance {
 static Sprite square;
 static Color C(string hex) { ColorUtility.TryParseHtmlString("#"+hex,out var c); return c; }
 static SpriteRenderer Box(string name,float x,float y,float w,float h,string color,int order=0, bool solid=false) {
  var g=new GameObject(name); g.transform.position=new Vector3(x,y,0); g.transform.localScale=new Vector3(w,h,1);
  var s=g.AddComponent<SpriteRenderer>(); s.sprite=square; s.color=C(color); s.sortingOrder=order;
  if(solid) { g.layer=8; g.AddComponent<BoxCollider2D>(); }
  return s;
 }
 static TextMesh Label(string text,float x,float y,float size=.25f) {
  var g=new GameObject(text); g.transform.position=new Vector3(x,y,0);
  var t=g.AddComponent<TextMesh>(); t.text=text; t.fontSize=48; t.characterSize=size; t.anchor=TextAnchor.MiddleCenter;
  t.color=C("91a6b8"); t.GetComponent<MeshRenderer>().sortingOrder=6; return t;
 }
 public static void Create() {
  Directory.CreateDirectory("Assets/Nyx/Art"); Directory.CreateDirectory("Assets/Nyx/Scenes");
  var tex=new Texture2D(1,1); tex.SetPixel(0,0,Color.white); tex.Apply();
  File.WriteAllBytes("Assets/Nyx/Art/pixel.png",tex.EncodeToPNG()); AssetDatabase.Refresh();
  Configure("Assets/Nyx/Art/pixel.png",1,new Vector2(.5f,.5f));
  Configure("Assets/Art/Characters/prisioneiro_16x16.png",16,new Vector2(.5f,0));
  square=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Nyx/Art/pixel.png");
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var level=new GameObject("Fase — manutenção").AddComponent<MaintenanceLevel>();
  var cam=new GameObject("Main Camera").AddComponent<Camera>(); cam.tag="MainCamera"; cam.orthographic=true; cam.orthographicSize=9;
  cam.transform.position=new Vector3(14,4.2f,-10); cam.backgroundColor=C("080e19"); cam.clearFlags=CameraClearFlags.SolidColor; cam.gameObject.AddComponent<AudioListener>();
  level.gameCamera=cam;
  Box("Parede",31,5,72,18,"121e2d",-20);
  for(int x=-4;x<68;x+=2) {
   Box("Junta vertical",x,5,.035f,18,"233246",-19);
   for(int y=0;y<14;y+=2) {
    Box("Junta horizontal",x+1,y,2,.035f,"233246",-19);
    Box("Rebite",x+.15f,y+.15f,.07f,.07f,"425269",-18);
   }
  }
  Box("Faixa da prisão",31,3.1f,72,.22f,"67464a",-17);
  Box("Tubulação",31,7.4f,72,.3f,"334358",-15);
  for(int x=0;x<66;x+=6) {
   Box("Suporte tubulação",x,7.4f,.15f,.6f,"172536",-14);
   Box("Luminária",x,5.7f,1.6f,.35f,"334358",-13);
   Box("Luz de emergência",x,5.66f,1.15f,.12f,"e46a6a",-12);
   Box("Reflexo vermelho",x,4.9f,2.5f,1.2f,"382333",-16);
  }
  Box("Chão inicial",8,-.65f,20,1.3f,"334459",0,true);
  Box("Chão setor auxiliar",46.5f,-.65f,35,1.3f,"334459",0,true);
  Box("Borda inicial",8,-.06f,20,.12f,"6b7b8d",1);
  Box("Borda auxiliar",46.5f,-.06f,35,.12f,"6b7b8d",1);
  Box("Caixa baixa",8,.25f,1,.5f,"576272",1,true);
  Box("Caixa alta",11.5f,.5f,1.2f,1,"576272",1,true);
  Box("Parede esquerda",-2,4,1,8,"263448",0,true);
  Box("Fim do corredor",64,4,1,8,"263448",0,true);
  Box("Vão",23.5f,-1.2f,11,2.4f,"080b14",2);
  for(float x=18.3f;x<29;x+=.65f) {
   Box("Arco elétrico",x,-1.5f,.1f,.9f,"9f64e9",3);
   Box("Faísca",x+.14f,-1.25f,.35f,.08f,"d4b3ff",3);
  }
  Label("RISCO ELÉTRICO",23.5f,3.8f);
  Box("Moldura do duto",2.5f,1.9f,1.6f,1.4f,"455468",-5);
  Box("Duto aberto",2.5f,1.9f,1.25f,1.05f,"080e16",-4);
  var grille=Box("Tampa arrancada",4.1f,.2f,1.1f,.15f,"6a7786",2); grille.transform.rotation=Quaternion.Euler(0,0,-12);
  Label("B-3   /   ACESSO TÉCNICO",5,4.4f,.22f);
  Label("ESPAÇO  /  PULAR",10,2.5f,.18f);
  level.pads=new PortalPad[2];
  for(int i=0;i<2;i++) {
   float x=i==0?16:31;
   var g=new GameObject("Painel de portal "+(i+1)); g.transform.position=new Vector3(x,1.3f,0); g.layer=9;
   var trigger=g.AddComponent<BoxCollider2D>(); trigger.isTrigger=true; trigger.size=new Vector2(1,2.5f);
   var p=g.AddComponent<PortalPad>(); p.level=level;
   p.glow=Box("Aro",x,1.3f,1.25f,2.6f,"748aa8",3);
   p.core=Box("Núcleo",x,1.3f,.85f,2.2f,"35455c",4);
   p.caption=Label("PAINEL",x,3,.18f); p.SetSlot(0); level.pads[i]=p;
   p.glow.transform.SetParent(g.transform,true); p.core.transform.SetParent(g.transform,true); p.caption.transform.SetParent(g.transform,true);
  }
  var pg=new GameObject("Prisioneiro"); pg.transform.position=new Vector3(3,.15f,0);
  var player=pg.AddComponent<Prisoner>(); player.level=level; level.player=player;
  player.body=pg.AddComponent<Rigidbody2D>(); player.body.gravityScale=2.4f; player.body.constraints=RigidbodyConstraints2D.FreezeRotation;
  player.body.collisionDetectionMode=CollisionDetectionMode2D.Continuous; player.body.interpolation=RigidbodyInterpolation2D.Interpolate;
  var pc=pg.AddComponent<BoxCollider2D>(); pc.size=new Vector2(.62f,1.3f); pc.offset=new Vector2(0,.65f);
  var visual=new GameObject("Sprite"); visual.transform.SetParent(pg.transform,false); visual.transform.localScale=Vector3.one*1.5f;
  player.visual=visual.transform; player.sprite=visual.AddComponent<SpriteRenderer>();
  player.sprite.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/prisioneiro_16x16.png"); player.sprite.sortingOrder=10;
  var gun=Box("Arma de portais",0,0,.5f,.22f,"7542a8",11);
  var aimArm=Box("Braco de mira",0,0,.34f,.13f,"89b9ac",10); aimArm.transform.SetParent(pg.transform,false); player.arm=aimArm.transform;
  gun.transform.SetParent(pg.transform,false); gun.transform.localPosition=new Vector3(.42f,.75f,0); player.weapon=gun.transform;
  var muzzle=Box("Energia roxa",0,0,.13f,.14f,"d19cff",12); muzzle.transform.SetParent(gun.transform,false);
  muzzle.transform.localPosition=new Vector3(.38f,0,0); muzzle.transform.localScale=new Vector3(.25f,.6f,1);
  var cell=Box("Célula de energia",36,.5f,.55f,.7f,"b3f2e8",8);
  cell.gameObject.AddComponent<BoxCollider2D>(); level.energyCell=cell.gameObject.AddComponent<Rigidbody2D>();
  level.energyCell.constraints=RigidbodyConstraints2D.FreezeRotation; level.energyCell.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
  Label("CÉLULA  [E]",36,2,.18f);
  var terminal=Box("Terminal auxiliar",42,1,1.3f,2,"465468",1);
  level.socket=terminal.transform; level.socketIndicator=Box("Indicador",42,1.45f,.75f,.35f,"b95763",2);
  Label("ENERGIA AUXILIAR  [E]",42,3,.18f);
  Box("Moldura da saída",48,2,2.4f,4.3f,"445369",-1);
  level.securityDoor=Box("Porta sem energia",48,1.9f,1.7f,3.8f,"5b6374",3,true).gameObject;
  Label("SETOR C  →",54,3,.3f);
  level.exit=new GameObject("Fim da primeira fase").transform; level.exit.position=new Vector3(57,0,0);
  EditorSceneManager.SaveScene(scene,"Assets/Nyx/Scenes/Manutencao.unity");
  var path="Assets/Nyx/Scenes/Manutencao.unity";
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(path,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=path)).ToArray();
  AssetDatabase.SaveAssets();
  Debug.Log("NYX_SCENE_CREATED");
 }
 static void Configure(string path,int ppu,Vector2 pivot) {
  var importer=(TextureImporter)AssetImporter.GetAtPath(path);
  importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
  importer.spritePixelsPerUnit=ppu; importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed;
  var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteAlignment=9; settings.spritePivot=pivot; importer.SetTextureSettings(settings); importer.SaveAndReimport();
 }
 [MenuItem("Nyx/Build interactive Windows preview")]
 public static void Build() {
  if(!System.IO.File.Exists(ImportClaudeArt.ScenePath)) ImportClaudeArt.Apply();
  CreateNextPhase();
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
   scenes=new[]{Nyx.Editor.ImportClaudeArt.ScenePath,"Assets/Nyx/Scenes/Fuga_02.unity"}, locationPathName="Builds/Nyx-Manutencao-Interativa/Nyx.exe",
   target=BuildTarget.StandaloneWindows64, options=BuildOptions.None
  });
  if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Build failed");
  Debug.Log("NYX_BUILD_OK");
 }
 public static void BuildLatest() { ImportClaudeArt.Apply(); Build(); }
 public static void CreateNextPhase() {
  var path="Assets/Nyx/Scenes/Fuga_02.unity";
  if(File.Exists(path)) return;
  var previous=SceneManager.GetActiveScene();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var level=new GameObject("Fase 02 — Corredor de fuga").AddComponent<MaintenanceLevel>();
  var cam=new GameObject("Main Camera").AddComponent<Camera>(); cam.tag="MainCamera"; cam.orthographic=true; cam.orthographicSize=7.5f; cam.transform.position=new Vector3(0,3,-10); cam.backgroundColor=C("080e19"); cam.clearFlags=CameraClearFlags.SolidColor; cam.gameObject.AddComponent<AudioListener>(); level.gameCamera=cam;
  Box("Parede lunar",0,4,42,14,"121e2d",-10);
  for(int x=-20;x<22;x+=2) Box("Painel",x,4,1.94f,13,"1b2a3c",-9);
  Box("Piso",0,-.65f,42,1.3f,"334459",0,true);
  for(int x=-20;x<22;x+=2) Box("Borda do piso",x,-.06f,2,.12f,"6b7b8d",1);
  Box("Parede fim",20,3,1,7,"263448",0,true);
  Label("SETOR C  /  FUGA",0,5,.32f);
  Label("A manutencao caiu. Encontre uma saida.",0,3.5f,.18f);
  var pg=new GameObject("Prisioneiro"); pg.transform.position=new Vector3(-17,.15f,0);
  var player=pg.AddComponent<Prisoner>(); player.level=level; level.player=player;
  player.body=pg.AddComponent<Rigidbody2D>(); player.body.gravityScale=2.4f; player.body.constraints=RigidbodyConstraints2D.FreezeRotation; player.body.collisionDetectionMode=CollisionDetectionMode2D.Continuous; player.body.interpolation=RigidbodyInterpolation2D.Interpolate;
  var col=pg.AddComponent<BoxCollider2D>(); col.size=new Vector2(.75f,1.8f); col.offset=new Vector2(0,.9f); player.bodyCollider=col;
  var visual=new GameObject("Sprite"); visual.transform.SetParent(pg.transform,false); visual.transform.localScale=Vector3.one*2; player.visual=visual.transform; player.sprite=visual.AddComponent<SpriteRenderer>();
  var anim=pg.AddComponent<PrisonerSpriteAnimator>(); anim.player=player;
  var root="Assets/Nyx/Art/Claude/Character/";
  anim.idle=Enumerable.Range(0,4).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>(root+"idle_"+i+".png")).ToArray();
  anim.walk=Enumerable.Range(0,6).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>(root+"walk_"+i+".png")).ToArray();
  anim.idleGun=Enumerable.Range(0,2).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>(root+"idle_gun_"+i+".png")).ToArray();
  anim.walkGun=Enumerable.Range(0,6).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>(root+"walk_gun_"+i+".png")).ToArray(); anim.jump=AssetDatabase.LoadAssetAtPath<Sprite>(root+"jump_0.png"); anim.fall=AssetDatabase.LoadAssetAtPath<Sprite>(root+"fall_0.png");
  var gun=Box("Arma de portais",0,0,.5f,.22f,"7542a8",11); gun.transform.SetParent(pg.transform,false); player.weapon=gun.transform;
  var phaseArm=Box("Braco de mira",0,0,.34f,.13f,"89b9ac",10); phaseArm.transform.SetParent(pg.transform,false); player.arm=phaseArm.transform;
  var exit=new GameObject("Saida provisoria").transform; exit.position=new Vector3(19,0,0); level.exit=exit;
  Label("CONTINUA",16,2,.24f);
  EditorSceneManager.SaveScene(scene,path);
  var scenes=EditorBuildSettings.scenes.Where(s=>s.path!="Assets/Nyx/Scenes/Fuga_02.unity").ToList(); scenes.Add(new EditorBuildSettingsScene(path,true)); EditorBuildSettings.scenes=scenes.ToArray();
  AssetDatabase.SaveAssets();
  if(previous.IsValid()&&previous.isLoaded) SceneManager.SetActiveScene(previous);
 }
}
}
