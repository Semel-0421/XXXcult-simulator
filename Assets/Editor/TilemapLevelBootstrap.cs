using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Creates a small, editable RuleTile showcase level the first time this file is imported.
/// All generated content lives under Assets/TilemapLevel; the game's existing scenes are untouched.
/// </summary>
[InitializeOnLoad]
public static class TilemapLevelBootstrap
{
    private const string Root = "Assets/TilemapLevel";
    private const string CompleteKey = "TilemapLevelBootstrap.Created.v1";

    static TilemapLevelBootstrap()
    {
        EditorApplication.delayCall += CreateOnFirstImport;
    }

    private static void CreateOnFirstImport()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorPrefs.GetBool(CompleteKey)) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Root + "/Scenes/RuleTileLevel.unity") != null)
        {
            EditorPrefs.SetBool(CompleteKey, true);
            return;
        }

        try
        {
            CreateLevel();
            EditorPrefs.SetBool(CompleteKey, true);
            Debug.Log("Created the Rule Tile Valley sample level at Assets/TilemapLevel/Scenes/RuleTileLevel.unity.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/2D/Create Rule Tile Valley Level")]
    public static void CreateLevel()
    {
        EnsureFolder("Assets", "TilemapLevel");
        EnsureFolder(Root, "Art");
        EnsureFolder(Root, "Tiles");
        EnsureFolder(Root, "Palettes");
        EnsureFolder(Root, "Scenes");

        CreatePixelArt("GrassBlock", PixelKind.Grass);
        CreatePixelArt("RockBlock", PixelKind.Rock);
        CreatePixelArt("Water", PixelKind.Water);
        CreatePixelArt("WoodPlatform", PixelKind.Wood);
        AssetDatabase.Refresh();

        Sprite grassSprite = LoadSprite("GrassBlock");
        Sprite rockSprite = LoadSprite("RockBlock");
        Sprite waterSprite = LoadSprite("Water");
        Sprite woodSprite = LoadSprite("WoodPlatform");
        RuleTile grass = CreateRuleTile("GrassRuleTile", grassSprite, true);
        RuleTile rock = CreateRuleTile("RockRuleTile", rockSprite, false);
        Tile water = CreateTile("WaterTile", waterSprite, Tile.ColliderType.None);
        Tile wood = CreateTile("WoodPlatformTile", woodSprite, Tile.ColliderType.Grid);

        CreatePalette(grass, rock, water, wood);
        CreateScene(grass, rock, water, wood);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(Root + "/Scenes/RuleTileLevel.unity", OpenSceneMode.Single);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(Root + "/Scenes/RuleTileLevel.unity");
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
    }

    private enum PixelKind { Grass, Rock, Water, Wood }

    private static void CreatePixelArt(string name, PixelKind kind)
    {
        string path = Root + "/Art/" + name + ".png";
        if (File.Exists(path)) return;
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[size * size];
        var random = new System.Random((int)kind * 103 + 41);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Color32 color;
            if (kind == PixelKind.Grass)
            {
                if (y >= 26) color = Pick(random, new Color32(74, 145, 78, 255), new Color32(88, 164, 83, 255), new Color32(66, 130, 73, 255));
                else color = Pick(random, new Color32(119, 79, 55, 255), new Color32(132, 88, 59, 255), new Color32(105, 69, 51, 255));
                if (y >= 23 && y < 27 && ((x + y) % 7 < 2)) color = new Color32(91, 177, 91, 255);
                if (y < 21 && random.NextDouble() < 0.045) color = new Color32(151, 105, 72, 255);
            }
            else if (kind == PixelKind.Rock)
            {
                color = Pick(random, new Color32(80, 91, 108, 255), new Color32(91, 103, 119, 255), new Color32(71, 81, 97, 255));
                if (random.NextDouble() < 0.12) color = new Color32(119, 132, 145, 255);
            }
            else if (kind == PixelKind.Water)
            {
                color = new Color32(45, 130, 177, 210);
                if ((y == 24 || y == 25) && (x + (y == 24 ? 0 : 9)) % 15 < 6) color = new Color32(132, 219, 222, 235);
                else if (random.NextDouble() < 0.08) color = new Color32(68, 160, 198, 220);
            }
            else
            {
                color = y > 25 ? new Color32(184, 130, 73, 255) : new Color32(126, 80, 48, 255);
                if (y == 24 || y == 25) color = new Color32(221, 177, 105, 255);
                if (x % 8 == 0 && y < 24) color = new Color32(94, 59, 41, 255);
            }
            pixels[y * size + x] = color;
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = kind == PixelKind.Water;
        importer.SaveAndReimport();
    }

    private static Color32 Pick(System.Random random, Color32 a, Color32 b, Color32 c) => random.Next(3) == 0 ? a : random.Next(2) == 0 ? b : c;

    private static Sprite LoadSprite(string name)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/" + name + ".png");
        if (sprite == null) throw new InvalidOperationException("Could not import sprite " + name);
        return sprite;
    }

    private static RuleTile CreateRuleTile(string name, Sprite sprite, bool surface)
    {
        string path = Root + "/Tiles/" + name + ".asset";
        var ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(path);
        if (ruleTile != null) return ruleTile;
        ruleTile = ScriptableObject.CreateInstance<RuleTile>();
        ruleTile.name = name;
        ruleTile.m_DefaultSprite = sprite;
        ruleTile.m_DefaultColliderType = Tile.ColliderType.Grid;
        if (surface)
        {
            var edge = new RuleTile.TilingRule { m_Sprites = new[] { sprite }, m_RuleTransform = RuleTile.TilingRuleOutput.Transform.Rotated, m_ColliderType = Tile.ColliderType.Grid };
            var positions = new[] { new Vector3Int(-1, 1, 0), new Vector3Int(0, 1, 0), new Vector3Int(1, 1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0), new Vector3Int(1, -1, 0) };
            int[] conditions = { 2, 2, 2, 1, 1, 1, 1, 1 };
            edge.m_NeighborPositions.Clear();
            edge.m_Neighbors.Clear();
            for (int i = 0; i < positions.Length; i++) { edge.m_NeighborPositions.Add(positions[i]); edge.m_Neighbors.Add(conditions[i]); }
            ruleTile.m_TilingRules.Add(edge);
        }
        var fill = new RuleTile.TilingRule { m_Sprites = new[] { sprite }, m_ColliderType = Tile.ColliderType.Grid };
        ruleTile.m_TilingRules.Add(fill);
        AssetDatabase.CreateAsset(ruleTile, path);
        return ruleTile;
    }

    private static Tile CreateTile(string name, Sprite sprite, Tile.ColliderType collider)
    {
        string path = Root + "/Tiles/" + name + ".asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile != null) return tile;
        tile = ScriptableObject.CreateInstance<Tile>();
        tile.name = name;
        tile.sprite = sprite;
        tile.colliderType = collider;
        AssetDatabase.CreateAsset(tile, path);
        return tile;
    }

    private static void CreatePalette(RuleTile grass, RuleTile rock, Tile water, Tile wood)
    {
        string path = Root + "/Palettes/ValleyPalette.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        var palette = GridPaletteUtility.CreateNewPalette(Root + "/Palettes", "ValleyPalette", GridLayout.CellLayout.Rectangle, GridPalette.CellSizing.Automatic, Vector3.one, GridLayout.CellSwizzle.XYZ);
        if (palette == null) return;
        var contents = PrefabUtility.LoadPrefabContents(path);
        var tilemap = contents.GetComponentInChildren<Tilemap>();
        if (tilemap != null)
        {
            tilemap.SetTile(new Vector3Int(0, 0, 0), grass);
            tilemap.SetTile(new Vector3Int(1, 0, 0), rock);
            tilemap.SetTile(new Vector3Int(2, 0, 0), water);
            tilemap.SetTile(new Vector3Int(3, 0, 0), wood);
            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static void CreateScene(RuleTile grass, RuleTile rock, Tile water, Tile wood)
    {
        string path = Root + "/Scenes/RuleTileLevel.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var gridObject = new GameObject("Level Grid", typeof(Grid));
        Grid grid = gridObject.GetComponent<Grid>();
        grid.cellSize = Vector3.one;

        Tilemap ground = CreateTilemap(gridObject.transform, "Ground (RuleTile)", 0, true);
        Tilemap platforms = CreateTilemap(gridObject.transform, "Wood Platforms", 1, false);
        Tilemap waterMap = CreateTilemap(gridObject.transform, "River", -1, false);
        int[] heights = { -4,-4,-4,-3,-3,-3,-3,-4,-4,-5,-6,-7,-8,-8,-8,-7,-6,-5,-4,-3,-3,-3,-3,-4,-4,-4,-3,-3,-3,-4,-4,-4,-3,-3,-3,-4,-4,-4,-4 };
        int minX = -19;
        for (int x = 0; x < heights.Length; x++)
        {
            int worldX = minX + x;
            for (int y = -12; y <= heights[x]; y++)
            {
                Vector3Int cell = new Vector3Int(worldX, y, 0);
                ground.SetTile(cell, y <= -10 ? (TileBase)rock : grass);
            }
        }
        for (int x = -13; x <= -9; x++) platforms.SetTile(new Vector3Int(x, 0, 0), wood);
        for (int x = -6; x <= -2; x++) platforms.SetTile(new Vector3Int(x, 2, 0), wood);
        for (int x = 4; x <= 8; x++) platforms.SetTile(new Vector3Int(x, -1, 0), wood);
        for (int x = 11; x <= 15; x++) platforms.SetTile(new Vector3Int(x, 1, 0), wood);
        for (int x = -5; x <= 2; x++) waterMap.SetTile(new Vector3Int(x, -8, 0), water);
        ground.RefreshAllTiles();
        platforms.RefreshAllTiles();
        waterMap.RefreshAllTiles();
        var body = gridObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        var composite = gridObject.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        var groundCollider = ground.gameObject.AddComponent<TilemapCollider2D>();
        groundCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0, -1, -10);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 10;
        camera.backgroundColor = new Color(0.56f, 0.78f, 0.88f);
        camera.clearFlags = CameraClearFlags.SolidColor;

        EditorSceneManager.SaveScene(scene, path);
        AssetDatabase.SaveAssets();
    }

    private static Tilemap CreateTilemap(Transform parent, string name, int sortingOrder, bool collider)
    {
        var child = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        child.transform.SetParent(parent, false);
        child.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
        return child.GetComponent<Tilemap>();
    }
}
