// copyright 2015 by Richard Schmidbauer (visionvortex)
// all rights reserved.
// http://www.visionvortex.de

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using tk2dRuntime;
using tk2dRuntime.TileMap;
using Extensions;

namespace AutoTiles
{
  public class tk2dAutoTiles: AutoTilesBase
  {

    // The source tk2dTileMap instance
    public tk2dTileMap sourceTileMap;

    protected override bool UpdateFrameworkReferences() {
      sourceTileMap = gameObject.GetComponent<tk2dTileMap>();

      if (sourceTileMap != null) {
        return true;
      } else {
        return false;
      }

    }

    protected override void UpdateNumberOfLayers() {
      _numberOfMapLayers = sourceTileMap.Layers.Length;
    }

    protected override int GetTileValueFromFramwork(int x, int y, int l) {
      return sourceTileMap.GetTile(x, y, l);
    }

    protected override Vector2 GetDimensions() {
      return new Vector2(sourceTileMap.width, sourceTileMap.height);
    }

    

    protected override void RebuildFramework() {
      sourceTileMap.Build();
    }

    protected override void CommitTile(int x, int y, int l, int value) {
      sourceTileMap.SetTile(x, y, l, value);
    }


  }
  
}
