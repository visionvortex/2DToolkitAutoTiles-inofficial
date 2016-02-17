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

  /// <summary>
  /// This class will go through all tiles and calculate a bitmask value<para/>
  /// this value will be increased by a fixed amount depending on<para/>
  /// if the adjacent tile is available<para/>
  /// the mask values are as follows:<para/>
  /// 128 001 002<para/>
  /// 064 000 004<para/>
  /// 032 016 008<para/>
  /// </summary>
  [ExecuteInEditMode]
  public abstract class AutoTilesBase : MonoBehaviour
  {

    /// <summary>
    /// If set to true, the script will treat all out of border coordinates<para/>
    /// as if they were solid cells insted of empty ones.
    /// </summary>
    public bool TreatBorderAsSolid = false;

    /// <summary>
    /// If set to true, the script will treat all special tiles<para/>
    /// as if they were solid cells insted of empty ones.
    /// </summary>
    public bool TreatSpecialAsSolid = false;

    /// <summary>
    /// If this property is set, the three center variant tiles will<para/>
    /// be treated as special variants and will not be auto tiled.
    /// </summary>
    public bool TreatCenterVariantsAsSpecial = false;

    /// <summary>
    /// If this property is set, all six single block variant tiles will<para/>
    /// be treated as special variants and will not be auto tiled.
    /// </summary>
    public bool TreatFloorVariantsAsSpecial = false;

    /// <summary>
    /// List of variants for distinguising floor variant tiles.
    /// </summary>
    List<int> floorVariantIndices;

    /// <summary>
    /// List of variants for distinguising center variant tiles.
    /// </summary>
    List<int> centerVariantIndices;

    /// <summary>
    /// The indices of all layers of a tk2dTileMap, that need to be auto tiled,<para/>
    /// should be added here. All other layers will be omitted.
    /// </summary>
    [SerializeField]
    public List<bool> LayerFlags;

    /// <summary>
    /// Random Seed for calculations.<para/>
    /// Can be used to set a certain state.<para/>
    /// If set to -1 or smaller, the script will generate a seed.<para/>
    /// The seed can be taken from this property field at runtime
    /// </summary>
    public int RandomSeed {
      get {
        return _randomSeed;
      }

      set {
        _randomSeed = value;
      }

    }

    /// <summary>
    /// This property is used for storing and recording a random seed value
    /// </summary>
    [SerializeField]
    int _randomSeed = 0;

    // The source tk2dTileMap instance
    //public tk2dTileMap sourceTileMap;

    // this map conains all bitmask combinations (as int) and their
    // respective sprite indices
    Dictionary<int, int> indexSpriteMap;

    protected Vector2 _mapDimensions;

    protected int _numberOfMapLayers;

    void Start() {
      // record the current random seed
      if (_randomSeed == 0) {
        _randomSeed = Guid.NewGuid().GetHashCode();
      }

    }

    /// <summary>
    /// This method needs to be implemented by child classes to update all necessary references to the <para/>
    /// target framework they might need for their framework specific operations.<para/>
    /// IMplementations should return true if all references have been found and false otherwise.
    /// </summary>
    protected abstract bool UpdateFrameworkReferences();

    /// <summary>
    /// This method need to be implemented by child classes to get the number of available tilemap layers from the target framework
    /// </summary>
    protected abstract void UpdateNumberOfLayers();

    /// <summary>
    /// This method is used by the editor to check and rebuild the layer dictionary.<para/>
    /// This is done in order to prevent a dead layer entry buildup (memory leaks) by editing the tilemap.
    /// </summary>
    public void RebuildLayerInfo() {
      // initialize the layer dictionary with only false values
      for (int i = 0; i < _numberOfMapLayers; i++) {
        if (i >= LayerFlags.Count) {
          LayerFlags.Add(false);
        }

      }

      if (LayerFlags.Count > _numberOfMapLayers) {
        LayerFlags.RemoveRange(_numberOfMapLayers, LayerFlags.Count - _numberOfMapLayers);
      }

    }

    /// <summary>
    /// This method triggers the rebuild process.<para/>
    /// It should be triggered manually as soon as the tilemap changes
    /// </summary>
    public void RebuildTiles() {
      if (UpdateFrameworkReferences()) {
        UpdateNumberOfLayers();
        RebuildLayerInfo();
        _mapDimensions = GetDimensions();

        if (indexSpriteMap == null) {
          BuildIndexSpriteMap();
        }

        UnityEngine.Random.seed = _randomSeed;

        for (int l = 0; l < _numberOfMapLayers; l++) {
          if (LayerFlags[l]) {
            for (int x = 0; x < _mapDimensions.x; x++) {
              for (int y = 0; y < _mapDimensions.y; y++) {
                int bitmask = CalculateBitmask(x, y, l);
                if (bitmask != -1) {
                  SetTile(bitmask, x, y, l);
                }

              }

            }

          }

        }

        RebuildFramework();
      } else {
        Debug.LogWarning("No tk2dTileMap component available on this gameObject.");
      }
      
    }

    /// <summary>
    /// Child classes must implement this method to trigger a update in the target framework after the data has been written.
    /// </summary>
    protected abstract void RebuildFramework();

    /// <summary>
    /// Child classes must implement this method to retrieve the value of a tile from the target framwork
    /// </summary>
    protected abstract int GetTileValueFromFramwork(int x, int y, int l);

    /// <summary>
    /// Child classes must implement this method to check for and store the map dimensions of the target framework.<para/>
    /// this method needs to return a List<Vector2> that contains a single Vector2 for each layer, where x is with and y is height.
    /// </summary>
    protected abstract Vector2 GetDimensions();

    /// <summary>
    /// This method is used for acually setting the file sprites depending on the bitmask value
    /// </summary>
    void SetTile(int bitmask, int x, int y, int l) {

      int tile = GetTileValueFromFramwork(x, y, l);
      int newValue;

      if (floorVariantIndices.Contains(tile) && !TreatFloorVariantsAsSpecial) {

        // Floor tiles are special in that they don't contribute to the wall structure
        // So floors stay floors
        newValue = floorVariantIndices.GetRandom();
      } else if (floorVariantIndices.Contains(tile) && TreatFloorVariantsAsSpecial) {

        // if the tile is a floor tile and floor tile are to be treated as special
        newValue = tile;
      } else if (bitmask == 255 && !TreatCenterVariantsAsSpecial) {

        // in case we have a center variant tile we need to check if the bitmask
        // is actually inidcating a center tile and center tiles are not treated special.
        // if so choose a random variant
        newValue = centerVariantIndices.GetRandom();
      } else if (centerVariantIndices.Contains(tile) && TreatCenterVariantsAsSpecial) {
        newValue = tile;
      } else if (indexSpriteMap.ContainsValue(tile)) {

        // if the value is available in the map it will be treated regularly
        if (!indexSpriteMap.TryGetValue(bitmask, out newValue)) {
          Debug.LogWarning("bitmask value not contained in index list");
        }

      } else {
        // if the value is not contained in the indexSpriteMap, it means that
        // we are processing a special tile and we won't change those
        newValue = -1;
      }

      CommitTile(x, y, l, newValue);
    }


    /// <summary>
    /// This method needs to be implemented by child classes to actually commit the tile to the target framework.
    /// </summary>
    protected abstract void CommitTile(int x, int y, int l, int value);

    /// <summary>
    /// This method will calculate a bitmask value.<para/>
    /// To do this all surrounding cells are checked and the values will be accumulated. 
    /// </summary>
    int CalculateBitmask(int x, int y, int l) {
      int spriteId = GetTileValueFromFramwork(x, y, l);
      int bitmask = 0;

      bool calculate;
      if (spriteId == -1) {
        calculate = false;
      } else if (SpriteIdIsSpecialTile(spriteId) && !TreatSpecialAsSolid) {
        calculate = false;
      } else if (SpriteIdIsSpecialTile(spriteId) && TreatSpecialAsSolid) {
        calculate = true;
      } else if (floorVariantIndices.Contains(spriteId)) {
        calculate = false;
      } else {
        calculate = true;
      }

      // if the spriteId is -1, it means that the tile itself is empty and
      // we do not need to change anything. Empty tiles stay empty.
      if (calculate) {
        bitmask += GetTileState(x, y + 1, l) * 1;
        bitmask += GetTileState(x + 1, y + 1, l) * 2;
        bitmask += GetTileState(x + 1, y, l) * 4;
        bitmask += GetTileState(x + 1, y - 1, l) * 8;
        bitmask += GetTileState(x, y - 1, l) * 16;
        bitmask += GetTileState(x - 1, y - 1, l) * 32;
        bitmask += GetTileState(x - 1, y, l) * 64;
        bitmask += GetTileState(x - 1, y + 1, l) * 128;
      }

      return bitmask;
    }

    /// <summary>
    /// This method checks if the conditions for a floor-tile or<para/>
    /// center variant tile to become a special tile are met.
    /// </summary>
    bool SpriteIdIsSpecialTile(int id) {
      if ((floorVariantIndices.Contains(id) && TreatFloorVariantsAsSpecial) || (centerVariantIndices.Contains(id) && TreatCenterVariantsAsSpecial && id != 8) || !indexSpriteMap.ContainsValue(id)) {
        return true;
      }

      return false;
    }

    /// <summary>
    /// This method checks the state of a single tile.<para/>
    /// Obeying the tk2dTileMap standard the following value will be returned<para/>
    /// <para/>
    /// 0: empty cell<para/>
    /// 1: solid cell<para/>
    /// <para/>
    /// cells that are out of bounds can count as empty or solid, depending on the<para/>
    /// TreatBorderAsSolid property.
    /// </summary>
    int GetTileState(int x, int y, int l) {
      // if the index is out of bounds
      if (x < 0 || y < 0 || x >= _mapDimensions.x || y >= _mapDimensions.y) {
        if (TreatBorderAsSolid) {
          return 1;
        } else {
          return 0;
        }

      } else {
        int tile = GetTileValueFromFramwork(x, y, l);

        if (tile == -1) {
          // this means we have hit an empty cell
          return 0;
        } else if (SpriteIdIsSpecialTile(tile)) {
          if (TreatSpecialAsSolid) {
            return 1;
          } else {
            return 0;
          }

        } else if (floorVariantIndices.Contains(tile)) {
          return 0;
        } else {
          return 1;
        }

      }

    }

    /// <summary>
    /// this extensive method is used to fill a map with references to the<para/>
    /// correct sprite ids.<para/>
    /// This needs to be done for every possible bitmask combination value.
    /// </summary>
    void BuildIndexSpriteMap() {
      indexSpriteMap = new Dictionary<int, int>();

      indexSpriteMap.Add(28, 0);
      indexSpriteMap.Add(30, 0);
      indexSpriteMap.Add(60, 0);
      indexSpriteMap.Add(156, 0);
      indexSpriteMap.Add(62, 0);
      indexSpriteMap.Add(158, 0);
      indexSpriteMap.Add(188, 0);
      indexSpriteMap.Add(190, 0);

      indexSpriteMap.Add(124, 1);
      indexSpriteMap.Add(252, 1);
      indexSpriteMap.Add(126, 1);
      indexSpriteMap.Add(254, 1);

      indexSpriteMap.Add(112, 2);
      indexSpriteMap.Add(240, 2);
      indexSpriteMap.Add(120, 2);
      indexSpriteMap.Add(114, 2);
      indexSpriteMap.Add(242, 2);
      indexSpriteMap.Add(248, 2);
      indexSpriteMap.Add(122, 2);
      indexSpriteMap.Add(250, 2);

      indexSpriteMap.Add(20, 3);
      indexSpriteMap.Add(150, 3);
      indexSpriteMap.Add(148, 3);
      indexSpriteMap.Add(54, 3);
      indexSpriteMap.Add(52, 3);
      indexSpriteMap.Add(182, 3);
      indexSpriteMap.Add(22, 3);
      indexSpriteMap.Add(180, 3);

      indexSpriteMap.Add(80, 4);
      indexSpriteMap.Add(210, 4);
      indexSpriteMap.Add(82, 4);
      indexSpriteMap.Add(216, 4);
      indexSpriteMap.Add(88, 4);
      indexSpriteMap.Add(90, 4);
      indexSpriteMap.Add(208, 4);
      indexSpriteMap.Add(218, 4);

      indexSpriteMap.Add(93, 5);

      indexSpriteMap.Add(117, 6);

      indexSpriteMap.Add(31, 7);
      indexSpriteMap.Add(63, 7);
      indexSpriteMap.Add(191, 7);
      indexSpriteMap.Add(159, 7);

      // center tile
      indexSpriteMap.Add(255, 8);

      // center variants START
      // these indices are variants of the center tile
      // they will be randomized for greater visual diversity
      indexSpriteMap.Add(256, 27);
      indexSpriteMap.Add(257, 34);
      indexSpriteMap.Add(258, 41);
      centerVariantIndices = new List<int>() { 8, 27, 34, 41 };

      // center variants END

      indexSpriteMap.Add(241, 9);
      indexSpriteMap.Add(243, 9);
      indexSpriteMap.Add(251, 9);
      indexSpriteMap.Add(249, 9);

      indexSpriteMap.Add(5, 10);
      indexSpriteMap.Add(45, 10);
      indexSpriteMap.Add(13, 10);
      indexSpriteMap.Add(141, 10);
      indexSpriteMap.Add(37, 10);
      indexSpriteMap.Add(165, 10);
      indexSpriteMap.Add(133, 10);
      indexSpriteMap.Add(173, 10);

      indexSpriteMap.Add(65, 11);
      indexSpriteMap.Add(75, 11);
      indexSpriteMap.Add(107, 11);
      indexSpriteMap.Add(67, 11);
      indexSpriteMap.Add(99, 11);
      indexSpriteMap.Add(73, 11);
      indexSpriteMap.Add(97, 11);
      indexSpriteMap.Add(105, 11);

      indexSpriteMap.Add(87, 12);

      indexSpriteMap.Add(213, 13);

      indexSpriteMap.Add(7, 14);
      indexSpriteMap.Add(15, 14);
      indexSpriteMap.Add(39, 14);
      indexSpriteMap.Add(135, 14);
      indexSpriteMap.Add(47, 14);
      indexSpriteMap.Add(143, 14);
      indexSpriteMap.Add(167, 14);
      indexSpriteMap.Add(175, 14);

      indexSpriteMap.Add(199, 15);
      indexSpriteMap.Add(207, 15);
      indexSpriteMap.Add(231, 15);
      indexSpriteMap.Add(239, 15);

      indexSpriteMap.Add(193, 16);
      indexSpriteMap.Add(195, 16);
      indexSpriteMap.Add(201, 16);
      indexSpriteMap.Add(225, 16);
      indexSpriteMap.Add(203, 16);
      indexSpriteMap.Add(227, 16);
      indexSpriteMap.Add(233, 16);
      indexSpriteMap.Add(235, 16);

      indexSpriteMap.Add(68, 17);
      indexSpriteMap.Add(70, 17);
      indexSpriteMap.Add(76, 17);
      indexSpriteMap.Add(78, 17);
      indexSpriteMap.Add(100, 17);
      indexSpriteMap.Add(102, 17);
      indexSpriteMap.Add(108, 17);
      indexSpriteMap.Add(110, 17);
      indexSpriteMap.Add(196, 17);
      indexSpriteMap.Add(198, 17);
      indexSpriteMap.Add(204, 17);
      indexSpriteMap.Add(206, 17);
      indexSpriteMap.Add(228, 17);
      indexSpriteMap.Add(230, 17);
      indexSpriteMap.Add(236, 17);
      indexSpriteMap.Add(238, 17);

      indexSpriteMap.Add(17, 18);
      indexSpriteMap.Add(19, 18);
      indexSpriteMap.Add(25, 18);
      indexSpriteMap.Add(27, 18);
      indexSpriteMap.Add(49, 18);
      indexSpriteMap.Add(51, 18);
      indexSpriteMap.Add(57, 18);
      indexSpriteMap.Add(59, 18);
      indexSpriteMap.Add(145, 18);
      indexSpriteMap.Add(147, 18);
      indexSpriteMap.Add(153, 18);
      indexSpriteMap.Add(155, 18);
      indexSpriteMap.Add(177, 18);
      indexSpriteMap.Add(179, 18);
      indexSpriteMap.Add(185, 18);
      indexSpriteMap.Add(187, 18);

      indexSpriteMap.Add(85, 19);

      // single block
      indexSpriteMap.Add(0, 20);
      indexSpriteMap.Add(2, 20);
      indexSpriteMap.Add(8, 20);
      indexSpriteMap.Add(10, 20);
      indexSpriteMap.Add(32, 20);
      indexSpriteMap.Add(34, 20);
      indexSpriteMap.Add(40, 20);
      indexSpriteMap.Add(42, 20);
      indexSpriteMap.Add(128, 20);
      indexSpriteMap.Add(130, 20);
      indexSpriteMap.Add(136, 20);
      indexSpriteMap.Add(138, 20);
      indexSpriteMap.Add(160, 20);
      indexSpriteMap.Add(162, 20);
      indexSpriteMap.Add(168, 20);
      indexSpriteMap.Add(170, 20);

      // floor tile variants START
      indexSpriteMap.Add(259, 46);
      indexSpriteMap.Add(260, 47);
      indexSpriteMap.Add(261, 48);
      indexSpriteMap.Add(262, 53);
      indexSpriteMap.Add(263, 54);
      indexSpriteMap.Add(264, 55);

      floorVariantIndices = new List<int>() { 46, 47, 48, 53, 54, 55 };
      // floor tile variants END

      indexSpriteMap.Add(69, 21);
      indexSpriteMap.Add(77, 21);
      indexSpriteMap.Add(101, 21);
      indexSpriteMap.Add(109, 21);

      indexSpriteMap.Add(21, 22);
      indexSpriteMap.Add(53, 22);
      indexSpriteMap.Add(149, 22);
      indexSpriteMap.Add(181, 22);

      indexSpriteMap.Add(81, 23);
      indexSpriteMap.Add(83, 23);
      indexSpriteMap.Add(89, 23);
      indexSpriteMap.Add(91, 23);

      indexSpriteMap.Add(84, 24);
      indexSpriteMap.Add(86, 24);
      indexSpriteMap.Add(212, 24);
      indexSpriteMap.Add(214, 24);

      indexSpriteMap.Add(247, 25);

      indexSpriteMap.Add(223, 26);

      indexSpriteMap.Add(16, 28);
      indexSpriteMap.Add(18, 28);
      indexSpriteMap.Add(24, 28);
      indexSpriteMap.Add(26, 28);
      indexSpriteMap.Add(48, 28);
      indexSpriteMap.Add(50, 28);
      indexSpriteMap.Add(56, 28);
      indexSpriteMap.Add(58, 28);
      indexSpriteMap.Add(144, 28);
      indexSpriteMap.Add(146, 28);
      indexSpriteMap.Add(152, 28);
      indexSpriteMap.Add(154, 28);
      indexSpriteMap.Add(176, 28);
      indexSpriteMap.Add(178, 28);
      indexSpriteMap.Add(184, 28);
      indexSpriteMap.Add(186, 28);

      indexSpriteMap.Add(64, 29);
      indexSpriteMap.Add(66, 29);
      indexSpriteMap.Add(72, 29);
      indexSpriteMap.Add(74, 29);
      indexSpriteMap.Add(96, 29);
      indexSpriteMap.Add(98, 29);
      indexSpriteMap.Add(104, 29);
      indexSpriteMap.Add(106, 29);
      indexSpriteMap.Add(192, 29);
      indexSpriteMap.Add(194, 29);
      indexSpriteMap.Add(200, 29);
      indexSpriteMap.Add(202, 29);
      indexSpriteMap.Add(224, 29);
      indexSpriteMap.Add(226, 29);
      indexSpriteMap.Add(232, 29);
      indexSpriteMap.Add(234, 29);

      indexSpriteMap.Add(4, 30);
      indexSpriteMap.Add(6, 30);
      indexSpriteMap.Add(12, 30);
      indexSpriteMap.Add(14, 30);
      indexSpriteMap.Add(36, 30);
      indexSpriteMap.Add(38, 30);
      indexSpriteMap.Add(44, 30);
      indexSpriteMap.Add(46, 30);
      indexSpriteMap.Add(132, 30);
      indexSpriteMap.Add(134, 30);
      indexSpriteMap.Add(140, 30);
      indexSpriteMap.Add(142, 30);
      indexSpriteMap.Add(164, 30);
      indexSpriteMap.Add(166, 30);
      indexSpriteMap.Add(172, 30);
      indexSpriteMap.Add(174, 30);

      indexSpriteMap.Add(1, 31);
      indexSpriteMap.Add(3, 31);
      indexSpriteMap.Add(9, 31);
      indexSpriteMap.Add(11, 31);
      indexSpriteMap.Add(33, 31);
      indexSpriteMap.Add(35, 31);
      indexSpriteMap.Add(41, 31);
      indexSpriteMap.Add(43, 31);
      indexSpriteMap.Add(129, 31);
      indexSpriteMap.Add(131, 31);
      indexSpriteMap.Add(137, 31);
      indexSpriteMap.Add(139, 31);
      indexSpriteMap.Add(161, 31);
      indexSpriteMap.Add(163, 31);
      indexSpriteMap.Add(169, 31);
      indexSpriteMap.Add(171, 31);

      indexSpriteMap.Add(253, 32);

      indexSpriteMap.Add(127, 33);

      indexSpriteMap.Add(125, 35);

      indexSpriteMap.Add(245, 36);

      indexSpriteMap.Add(95, 37);

      indexSpriteMap.Add(215, 38);

      indexSpriteMap.Add(119, 39);

      indexSpriteMap.Add(221, 40);

      indexSpriteMap.Add(113, 42);
      indexSpriteMap.Add(115, 42);
      indexSpriteMap.Add(121, 42);
      indexSpriteMap.Add(123, 42);

      indexSpriteMap.Add(29, 43);
      indexSpriteMap.Add(61, 43);
      indexSpriteMap.Add(189, 43);
      indexSpriteMap.Add(157, 43);

      indexSpriteMap.Add(209, 44);
      indexSpriteMap.Add(211, 44);
      indexSpriteMap.Add(217, 44);
      indexSpriteMap.Add(219, 44);

      indexSpriteMap.Add(23, 45);
      indexSpriteMap.Add(55, 45);
      indexSpriteMap.Add(151, 45);
      indexSpriteMap.Add(183, 45);

      indexSpriteMap.Add(197, 49);
      indexSpriteMap.Add(205, 49);
      indexSpriteMap.Add(229, 49);
      indexSpriteMap.Add(237, 49);

      indexSpriteMap.Add(71, 50);
      indexSpriteMap.Add(79, 50);
      indexSpriteMap.Add(103, 50);
      indexSpriteMap.Add(111, 50);

      indexSpriteMap.Add(116, 51);
      indexSpriteMap.Add(118, 51);
      indexSpriteMap.Add(244, 51);
      indexSpriteMap.Add(246, 51);

      indexSpriteMap.Add(92, 52);
      indexSpriteMap.Add(94, 52);
      indexSpriteMap.Add(220, 52);
      indexSpriteMap.Add(222, 52);
    }

  }

}