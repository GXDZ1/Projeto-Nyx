using System;
using System.IO;
using UnityEditor;
namespace Nyx.Editor {
[InitializeOnLoad]
public static class BuildInteractiveOnRequest {
 static BuildInteractiveOnRequest() { EditorApplication.update+=Check; }
 static void Check() {
  const string request="work/build-interactive.request";
  if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode) return;
  File.Delete(request);
  try { BuildMaintenance.Build(); File.WriteAllText("work/build-interactive.result","SUCCESS"); }
  catch(Exception e) { File.WriteAllText("work/build-interactive.result",e.ToString()); UnityEngine.Debug.LogException(e); }
  EditorApplication.update-=Check;
 }
}
}