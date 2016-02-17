// copyright 2015 by Richard Schmidbauer (visionvortex)
// all rights reserved.
// http://www.visionvortex.de

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace AutoTiles
{
  [CustomEditor(typeof(tk2dAutoTiles))]
  public class tk2dAutoTilesEditor : Editor
  {

    tk2dAutoTiles script;

    bool showVariantsOptions = false;

    void OnEnable() {
      script = (tk2dAutoTiles)target;
      if (script.sourceTileMap == null) {
        script.sourceTileMap = script.GetComponent<tk2dTileMap>();
      }

    }

    override public void OnInspectorGUI() {
      script.RebuildLayerInfo();

      script.RandomSeed = EditorGUILayout.IntField("Random Seed", script.RandomSeed);

      GUILayout.Space(10f);

      showVariantsOptions = EditorGUILayout.Foldout(showVariantsOptions, "Tile Options");
      if (showVariantsOptions) {
        EditorGUILayout.HelpBox("If This box is checked, Special tiles will be treated like solid tiles.", MessageType.Info);
        script.TreatSpecialAsSolid = EditorGUILayout.ToggleLeft("Treat Special as Solid", script.TreatSpecialAsSolid);
        GUILayout.Space(10f);
        EditorGUILayout.HelpBox("If This box is checked, the center block variants will be trated as special tiles and not be changed throug auto mapping.", MessageType.Info);
        script.TreatCenterVariantsAsSpecial = EditorGUILayout.ToggleLeft("Treat Center Variants as Special", script.TreatCenterVariantsAsSpecial);
        GUILayout.Space(5f);
        EditorGUILayout.HelpBox("If This box is checked, the floor variants will be trated as special tiles and not be changed throug auto mapping.", MessageType.Info);
        script.TreatFloorVariantsAsSpecial = EditorGUILayout.ToggleLeft("Treat Floor Variants as Special", script.TreatFloorVariantsAsSpecial);
        GUILayout.Space(5f);
        EditorGUILayout.HelpBox("If This box is checked, the space outside the tilemap border will be treated as solid blocks for all means of auto tile operations.", MessageType.Info);
        script.TreatBorderAsSolid = EditorGUILayout.ToggleLeft("Treat Border as Solid", script.TreatBorderAsSolid);
      }

      GUILayout.Space(10f);



      EditorGUILayout.LabelField("Target Layers");
      for (int i = 0; i < script.LayerFlags.Count; i++) {
        script.LayerFlags[i] = EditorGUILayout.ToggleLeft("Layer " + i, script.LayerFlags[i]);
      }

      if (GUILayout.Button("Update TileMap")) {
        script.RebuildTiles();
      } else if (GUI.changed) {
        EditorUtility.SetDirty(target);
        script.RebuildTiles();
      }

    }

  }

}
