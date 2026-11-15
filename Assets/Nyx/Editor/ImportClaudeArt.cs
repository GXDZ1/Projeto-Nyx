using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Nyx.Editor {
[InitializeOnLoad]
public static class ImportClaudeArt {
 const string Root="Assets/Nyx/Art/Claude/";
 public const string ScenePath="Assets/Nyx/Scenes/Manutencao_Interativa.unity";
 static Transform art;
 static string Plain(string value) {
  var decomposed=value.Normalize(NormalizationForm.FormD);
  return new string(decomposed.Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant();
 }
 static ImportClaudeArt() { EditorApplication.delayCall+=Check; }
 static void Check() {
  if(!File.Exists("work/import-claude.request")) return;
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating) { EditorApplication.delayCall+=Check; return; }
  File.Delete("work/import-claude.request");
  try { Apply(); File.WriteAllText("work/import-claude.result","OK"); } catch(Exception e) { File.WriteAllText("work/import-claude.result",e.ToString()); Debug.LogException(e); }
 }
 static Sprite S(string name,bool character=false) { return AssetDatabase.LoadAssetAtPath<Sprite>(Root+(character?"Character/":"Environment/")+name+".png"); }
 static Sprite[] Frames(string prefix,int count,bool character=false) { return Enumerable.Range(0,count).Select(i=>S(prefix+i,character)).ToArray(); }
 static SpriteRenderer Place(string sprite,float x,float y,float scale,int order,Color tint) {
  var g=new GameObject(sprite); g.transform.SetParent(art); g.transform.position=new Vector3(x,y,0); g.transform.localScale=Vector3.one*scale;
  var r=g.AddComponent<SpriteRenderer>(); r.sprite=S(sprite); r.sortingOrder=order; r.color=tint; return r;
 }
 static TextMesh Sign(string text,float x,float y) {
  var panel=new GameObject("Tutorial plaque"); panel.transform.SetParent(art); panel.transform.position=new Vector3(x,y,.1f); panel.transform.localScale=new Vector3(3.4f,.72f,1);
  var plate=panel.AddComponent<SpriteRenderer>(); plate.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Nyx/Art/pixel.png");
  plate.color=new Color(.025f,.055f,.09f,.92f); plate.sortingOrder=7;
  var g=new GameObject("Tutorial: "+text); g.transform.SetParent(art); g.transform.position=new Vector3(x,y,-.1f);
  var t=g.AddComponent<TextMesh>(); t.text=text; t.fontSize=32; t.characterSize=.07f; t.anchor=TextAnchor.MiddleCenter; t.alignment=TextAlignment.Center; t.color=new Color(.76f,.91f,1);
  t.GetComponent<MeshRenderer>().sortingOrder=8; return t;
 }
 static NyxCrate EnsureCrate(GameObject target) {
  var crates=target.GetComponents<NyxCrate>();
  if(crates.Length==0) return target.AddComponent<NyxCrate>();
  for(int i=1;i<crates.Length;i++) UnityEngine.Object.DestroyImmediate(crates[i]);
  return crates[0];
 }
 static void ConfigureCrate(SpriteRenderer renderer, Sprite sprite, float scale) {
  renderer.sprite=sprite;
  renderer.enabled=true;
  renderer.color=Color.white;
  renderer.sortingOrder=4;

  var target=renderer.gameObject;
  var bounds=sprite.bounds;
  target.transform.localScale=new Vector3(scale,scale,1f);
  var position=target.transform.position;
  position.y=-bounds.min.y*scale;
  target.transform.position=position;

  target.layer=8;
  var collider=target.GetComponent<BoxCollider2D>();
  if(collider==null) collider=target.AddComponent<BoxCollider2D>();
  collider.size=bounds.size;
  collider.offset=bounds.center;
  EnsureCrate(target);
 }
 static void Import(string path) {
  var ti=(TextureImporter)AssetImporter.GetAtPath(path);
  ti.textureType=TextureImporterType.Sprite; ti.spriteImportMode=SpriteImportMode.Single; ti.spritePixelsPerUnit=24;
  ti.filterMode=FilterMode.Point; ti.mipmapEnabled=false; ti.textureCompression=TextureImporterCompression.Uncompressed; ti.alphaIsTransparency=true; ti.isReadable=true;
  var set=new TextureImporterSettings(); ti.ReadTextureSettings(set); set.spriteAlignment=9;
  set.spritePivot=path.Contains("/Character/")?new Vector2(10f/24,0):path.Contains("/Generated/Fire/")?new Vector2(.5f,0):new Vector2(.5f,.5f); ti.SetTextureSettings(set); ti.SaveAndReimport();
 }
 static Sprite[] GenerateFireSprites() {
  const int width=32,height=40,count=6;
  const string folder="Assets/Nyx/Art/Generated/Fire/";
  Directory.CreateDirectory(folder);
  var frames=new Sprite[count];
  for(int i=0;i<count;i++) {
   var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);
   texture.filterMode=FilterMode.Point; texture.wrapMode=TextureWrapMode.Clamp;
   texture.SetPixels32(NyxFireHazard.CreateFirePixels(i,width,height)); texture.Apply(false,false);
   var path=folder+"fire_"+i+".png";
   File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
   AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate); Import(path);
   frames[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
  }
  return frames;
 }
 static void PlacePitFire(Scene scene, MaintenanceLevel level, Sprite[] frames) {
  var roots=scene.GetRootGameObjects();
  var leftFloor=roots.Single(g=>Plain(g.name)=="chao inicial").GetComponent<BoxCollider2D>().bounds;
  var rightFloor=roots.Single(g=>Plain(g.name)=="chao setor auxiliar").GetComponent<BoxCollider2D>().bounds;
  float start=leftFloor.max.x;
  float width=rightFloor.min.x-start;
  float rim=Mathf.Min(leftFloor.max.y,rightFloor.max.y);

  var pit=new GameObject("Fire pit");
  pit.transform.SetParent(art,false);
  pit.transform.position=new Vector3(start+width*.5f,rim-.42f,0);
  var hazard=pit.AddComponent<BoxCollider2D>();
  hazard.isTrigger=true;
  hazard.size=new Vector2(width,1.04f);
  hazard.offset=new Vector2(0,.156f);
  pit.AddComponent<NyxFireHazard>().level=level;

  int tileCount=Mathf.CeilToInt(width/.75f);
  float tileWidth=width/tileCount;
  for(int i=0;i<tileCount;i++) {
   var flame=new GameObject("Fire hazard");
   flame.transform.SetParent(pit.transform,false);
   flame.transform.localPosition=new Vector3(-width*.5f+(i+.5f)*tileWidth,0,0);
   flame.transform.localScale=new Vector3(tileWidth/frames[0].bounds.size.x,1.3f,1);
   var renderer=flame.AddComponent<SpriteRenderer>();
   renderer.sortingOrder=5;
   renderer.sprite=frames[i%frames.Length];
   var fire=flame.AddComponent<NyxFireHazard>();
   fire.fireRenderer=renderer;
   fire.frames=frames;
  }
 }
 [MenuItem("Nyx/Apply interactive phase")]
 public static void Apply() {
  foreach(var file in Directory.GetFiles(Root,"*.png",SearchOption.AllDirectories)) Import(file.Replace('\\','/'));
  var fireFrames=GenerateFireSprites();
  if(!File.Exists(ScenePath)) File.Copy("Assets/Nyx/Scenes/Manutencao_24.unity",ScenePath);
  AssetDatabase.Refresh();
  var before=SceneManager.GetActiveScene(); var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  if(scene.isDirty) throw new Exception("Save the interactive phase before regenerating its art.");
  var level=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MaintenanceLevel>(true)).Single();
  foreach(var old in scene.GetRootGameObjects().Where(g=>Plain(g.name).StartsWith("interactive tutorial art")).ToArray()) UnityEngine.Object.DestroyImmediate(old);
  foreach(var roof in scene.GetRootGameObjects().Where(g=>Plain(g.name)=="teto portalavel").ToArray()) UnityEngine.Object.DestroyImmediate(roof);
  // Remove legacy objects that were authored outside the generated art root in older scene versions.
  var staleNames=new[]{"camera_0","alarme_on","alarme_off","espinhos","superficie_portal_grande","fire hazard","chao_perigo","caixa_pequena","caixa_grande"};
  foreach(var stale in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
   .Where(t=>staleNames.Contains(Plain(t.name))).ToArray()) if(stale!=null) UnityEngine.Object.DestroyImmediate(stale.gameObject);
  foreach(var oldSurface in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PortalSurface>(true)).ToArray())
   UnityEngine.Object.DestroyImmediate(oldSurface);
  foreach(var text in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TextMesh>(true))) {
   var mesh=text.GetComponent<MeshRenderer>(); if(mesh!=null) mesh.enabled=false;
  }
  art=new GameObject("Interactive Tutorial Art").transform;
  string[] hide={"parede","junta vertical","junta horizontal","rebite","faixa da prisao","tubulacao","suporte tubulacao","luminaria","luz de emergencia","reflexo vermelho","chao inicial","chao setor auxiliar","borda inicial","borda auxiliar","vao","arco eletrico","faisca","moldura do duto","duto aberto","tampa arrancada","moldura da saida"};
  foreach(var root in scene.GetRootGameObjects()) {
   var r=root.GetComponent<SpriteRenderer>(); if(r!=null&&hide.Contains(Plain(root.name))) r.enabled=false;
   if(root.name=="Caixa baixa") ConfigureCrate(r,S("caixa_pequena"),.5f);
   if(root.name=="Caixa alta") ConfigureCrate(r,S("caixa_pequena"),1f);
  }
  var violetFrames=Frames("portal_roxo_",4); var greenFrames=Frames("portal_verde_",4);
  level.purplePortalFrames=violetFrames; level.greenPortalFrames=greenFrames;
  var ceiling=new GameObject("Teto portalavel"); ceiling.layer=8; ceiling.transform.position=new Vector3(31,11.8f,0); ceiling.transform.localScale=new Vector3(72,.8f,1);
  var ceilingArt=ceiling.AddComponent<SpriteRenderer>(); ceilingArt.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Nyx/Art/pixel.png"); ceilingArt.color=new Color(.16f,.23f,.34f); ceilingArt.sortingOrder=-4;
  ceiling.AddComponent<BoxCollider2D>();
  var wallTint=new Color(.56f,.58f,.7f);
  for(int x=-2;x<66;x+=2) for(int y=1;y<13;y+=2) {
   string tile=y==3?"parede_fundo_listra":x%12==4?"parede_fundo_cano_v":y==9?"parede_fundo_cano_h":"parede_fundo";
   Place(tile,x+1,y,2,-20,wallTint);
  }
  foreach(var start in new[]{-2,29}) for(int x=start;x<(start==-2?18:65);x+=2) {
   Place("chao_topo",x+1,-1,2,1,Color.white); Place("chao_miolo",x+1,-3,2,1,new Color(.7f,.7f,.8f));
  }
  Place("duto_saida",2.5f,1,2,2,Color.white); Place("duto_grade_caida",4.3f,.5f,1.4f,3,Color.white);
  var crate=Place("caixa_grande",39,0,1,4,Color.white);
  ConfigureCrate(crate,crate.sprite,1f);
  foreach(float x in new[]{2f,29f,58f}) {
   var lamp=Place("alarme_on",x,5,1.4f,-10,Color.white); var loop=lamp.gameObject.AddComponent<SpriteLoop>(); loop.target=lamp; loop.frames=new[]{S("alarme_on"),S("alarme_off")}; loop.fps=2;
  }
  foreach(float x in new[]{4f,24f,44f,60f}) Place("camera_0",x,7,1.5f,-10,Color.white);
  Place("janela_terra",7,6,1.7f,-12,new Color(.8f,.8f,1)); Place("janela_terra",37,6,1.7f,-12,new Color(.8f,.8f,1));
  Place("armario",45,2,2,-11,wallTint); Place("mesa_lab",35,1,1.5f,-11,wallTint); Place("frasco",34.5f,2,1.5f,-10,Color.white);
  var fan=Place("ventilador_0",22,6,2,-11,wallTint); var fanLoop=fan.gameObject.AddComponent<SpriteLoop>(); fanLoop.target=fan; fanLoop.frames=Frames("ventilador_",3);
  PlacePitFire(scene,level,fireFrames);
  for(int i=0;i<Mathf.Min(2,level.pads.Length);i++) {
   var p=level.pads[i];
   bool floorPad=i==0;
   float panelY=floorPad?-.625f:10.775f;
   var whiteWall=Place("superficie_portal_grande",p.transform.position.x,panelY,1.25f,2,Color.white);
   whiteWall.transform.rotation=Quaternion.Euler(0,0,90);
   whiteWall.gameObject.layer=8;
   var wallCollider=whiteWall.gameObject.AddComponent<BoxCollider2D>(); wallCollider.size=new Vector2(1,2);
   var targetCollider=whiteWall.gameObject.AddComponent<BoxCollider2D>(); targetCollider.size=new Vector2(1,2.2f); targetCollider.isTrigger=true;
   var topFrame=whiteWall.gameObject.AddComponent<BoxCollider2D>(); topFrame.size=new Vector2(1,.18f); topFrame.offset=new Vector2(0,.91f); topFrame.enabled=false;
   var bottomFrame=whiteWall.gameObject.AddComponent<BoxCollider2D>(); bottomFrame.size=new Vector2(1,.18f); bottomFrame.offset=new Vector2(0,-.91f); bottomFrame.enabled=false;
   var wallSurface=whiteWall.gameObject.AddComponent<PortalSurface>(); wallSurface.collider2D=targetCollider; wallSurface.solidCollider=wallCollider; wallSurface.portalFrameColliders=new[]{topFrame,bottomFrame}; wallSurface.fixedNormal=floorPad?Vector2.up:Vector2.down; wallSurface.snapToEdge=false;
   wallSurface.purpleFrames=violetFrames; wallSurface.greenFrames=greenFrames;
  }
  foreach(var p in level.pads) {
   if(p.caption!=null) p.caption.gameObject.SetActive(false);
   p.pixelArt=true; p.purpleFrames=violetFrames; p.greenFrames=greenFrames;
   p.glow.sprite=p.purpleFrames[0]; p.glow.transform.localScale=new Vector3(1.5f,1.5f,1); p.SetSlot(0);
  }
  level.gameCamera.orthographicSize=7.5f;
  var player=level.player; player.visual.localScale=Vector3.one*2; player.visual.localPosition=Vector3.zero;
  player.sprite.sprite=S("idle_0",true); player.weapon.gameObject.SetActive(true);
  var armObject=player.transform.Find("Braco de mira");
  if(armObject==null) {
   armObject=new GameObject("Braco de mira").transform; armObject.SetParent(player.transform,false);
   armObject.localScale=new Vector3(.34f,.13f,1);
   var armSprite=armObject.gameObject.AddComponent<SpriteRenderer>(); armSprite.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Nyx/Art/pixel.png"); armSprite.color=new Color(.54f,.73f,.67f); armSprite.sortingOrder=11;
  }
  player.arm=armObject;
  var weaponRenderer=player.weapon.GetComponent<SpriteRenderer>(); if(weaponRenderer!=null) weaponRenderer.sortingOrder=13;
  foreach(var muzzleRenderer in player.weapon.GetComponentsInChildren<SpriteRenderer>(true)) if(muzzleRenderer!=weaponRenderer) muzzleRenderer.sortingOrder=14;
  var bodyCollider=player.GetComponent<BoxCollider2D>(); bodyCollider.size=new Vector2(.75f,1.8f); bodyCollider.offset=new Vector2(0,.9f); bodyCollider.edgeRadius=.07f;
  player.bodyCollider=bodyCollider; player.groundMask=(1<<0)|(1<<8); player.jumpSpeed=10.8f;
  var anim=player.GetComponent<PrisonerSpriteAnimator>(); if(anim==null) anim=player.gameObject.AddComponent<PrisonerSpriteAnimator>();
  anim.player=player; anim.neckPixel=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Nyx/Art/pixel.png"); anim.idle=Frames("idle_",4,true); anim.walk=Frames("walk_",6,true); anim.walkBack=Frames("walk_",6,true).Reverse().ToArray(); anim.idleGun=Frames("idle_gun_",2,true);
  anim.walkGun=Frames("walk_gun_",6,true); anim.shoot=Frames("shoot_",3,true); anim.jump=S("jump_0",true); anim.fall=S("fall_0",true);
  var phys=new PhysicsMaterial2D("Player traction"){friction=.32f,bounciness=0}; bodyCollider.sharedMaterial=phys;
  var oldSocket=level.socket.GetComponent<SpriteRenderer>(); if(oldSocket!=null) oldSocket.enabled=false;
  Place("terminal_0",42,1,2,1,Color.white);
  var door=level.securityDoor.GetComponent<SpriteRenderer>(); door.sprite=S("porta_fechada"); door.transform.localScale=Vector3.one*1.3f;
  door.transform.position=new Vector3(48,1.95f,0); door.color=Color.white;
  var doorCollider=door.GetComponent<BoxCollider2D>(); doorCollider.size=new Vector2(1.3f,3); doorCollider.offset=Vector2.zero;
  Place("porta_aberta",48,1.95f,1.3f,-2,Color.white);
  var cellRenderer=level.energyCell.GetComponent<SpriteRenderer>(); cellRenderer.sprite=S("caixa_pequena"); cellRenderer.color=Color.white;
  var hand=new GameObject("Auxiliary cell in hand"); hand.transform.SetParent(player.transform,false); hand.transform.localPosition=new Vector3(.48f,1.08f,0);
  var handRenderer=hand.AddComponent<SpriteRenderer>(); handRenderer.sprite=cellRenderer.sprite; handRenderer.sortingOrder=12; hand.transform.localScale=new Vector3(.52f,.62f,1); hand.SetActive(false);
  level.handCell=hand.transform; level.boltSprite=S("portal_roxo_0");
  Sign("CLIQUE: ESQ. ROXO / DIR. VERDE",8,4.15f);
  Sign("ESPACO: PULAR / EMPURRE A CAIXA",13,2.25f);
  Sign("E: PEGAR CELULA AUXILIAR",36,2.45f);
  Sign("E: ATIVAR A PORTA",45,4.1f);
  EditorSceneManager.SaveScene(scene);
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)}.Concat(EditorBuildSettings.scenes.Where(x=>x.path!=ScenePath)).ToArray();
  AssetDatabase.SaveAssets();
  if(before.IsValid()&&before.isLoaded&&before.path!=ScenePath&&Application.isBatchMode) SceneManager.SetActiveScene(before);
  Debug.Log("NYX_INTERACTIVE_ART_IMPORTED");
 }
}
}
