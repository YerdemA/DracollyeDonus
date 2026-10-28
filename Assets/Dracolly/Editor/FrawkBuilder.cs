using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Dracolly.Player;

namespace Dracolly.EditorTools
{
    /// <summary>
    /// Menü: Dracolly > Frawk'ı ve test sahnesini kur
    /// Sprite ayarlarını yapar, Frawk'ı kemik hiyerarşisiyle kurar, prefab'ını kaydeder
    /// ve üzerinde yürüyüp zıplanabilecek küçük bir test sahnesi oluşturur.
    /// </summary>
    public static class FrawkBuilder
    {
        const string Root = "Assets/Dracolly";
        const string FrawkDir = Root + "/Characters/Frawk";
        const string RigPath = FrawkDir + "/frawk_rig.json";
        const string SpriteDir = FrawkDir + "/Sprites";
        const string TilePath = Root + "/Environment/stone_tile.png";
        const string ScenePath = Root + "/Scenes/Prototype.unity";
        const string PrefabPath = FrawkDir + "/Frawk.prefab";
        const string PhysMatPath = Root + "/Environment/NoFriction.physicsMaterial2D";

        [System.Serializable]
        class PartData
        {
            public string name, file, parent;
            public float pivotX, pivotY, posX, posY;
            public int order;
        }

        [System.Serializable]
        class RigData
        {
            public float ppu;
            public float hipHeight;
            public PartData[] parts;
        }

        [MenuItem("Dracolly/Frawk'ı ve test sahnesini kur")]
        public static void Build()
        {
            if (!File.Exists(RigPath))
            {
                EditorUtility.DisplayDialog("Dracolly", "Bulunamadı: " + RigPath +
                    "\n\nDracolly klasörünü doğrudan Assets klasörünün içine koyduğundan emin ol.", "Tamam");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var rig = JsonUtility.FromJson<RigData>(File.ReadAllText(RigPath));

            // 1) Sprite ayarları
            foreach (var p in rig.parts)
                ConfigureSprite(SpriteDir + "/" + p.file, rig.ppu, new Vector2(p.pivotX, p.pivotY), SpriteMeshType.Tight, TextureWrapMode.Clamp);
            ConfigureSprite(TilePath, 128f, new Vector2(0.5f, 0.5f), SpriteMeshType.FullRect, TextureWrapMode.Repeat);

            Directory.CreateDirectory(Root + "/Scenes");
            AssetDatabase.Refresh();

            var noFriction = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(noFriction, PhysMatPath);

            // 2) Yeni sahne
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 3) Karakter
            GameObject frawk = BuildCharacter(rig, noFriction);
            frawk.transform.position = new Vector3(0f, 0.02f, 0f);
            PrefabUtility.SaveAsPrefabAssetAndConnect(frawk, PrefabPath, InteractionMode.AutomatedAction);

            // 4) Test bölümü
            var tile = AssetDatabase.LoadAssetAtPath<Sprite>(TilePath);
            var level = new GameObject("Level").transform;
            Platform(level, tile, "Zemin", new Vector2(4f, -0.5f), new Vector2(40f, 1f));
            Platform(level, tile, "Platform 1", new Vector2(4.5f, 0.75f), new Vector2(3f, 0.5f));
            Platform(level, tile, "Platform 2", new Vector2(8.5f, 1.75f), new Vector2(3f, 0.5f));
            Platform(level, tile, "Duvar Sol", new Vector2(-15.5f, 3f), new Vector2(1f, 8f));
            Platform(level, tile, "Duvar Sag", new Vector2(23.5f, 3f), new Vector2(1f, 8f));

            // 5) Kamera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 3f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.10f, 0.15f);
            camGo.transform.position = new Vector3(1.2f, 1.3f, -10f);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CameraFollow2D>().target = frawk.transform;

            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = frawk;
            Debug.Log("Dracolly: Frawk kuruldu. Play'e bas; A/D ile yürü, Boşluk ile zıpla.");
        }

        static GameObject BuildCharacter(RigData rig, PhysicsMaterial2D mat)
        {
            var root = new GameObject("Frawk");
            var rb = root.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.gravityScale = 3f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = root.AddComponent<CapsuleCollider2D>();
            col.direction = CapsuleDirection2D.Vertical;
            col.size = new Vector2(0.4f, rig.hipHeight * 2.05f);
            col.offset = new Vector2(0f, rig.hipHeight * 1.025f);
            col.sharedMaterial = mat;

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            visual.gameObject.AddComponent<SortingGroup>();

            var hip = new GameObject("Hip").transform;
            hip.SetParent(visual, false);
            hip.localPosition = new Vector3(0f, rig.hipHeight, 0f);

            var bones = new Dictionary<string, Transform> { { "Hip", hip } };
            var pos = new Dictionary<string, Vector2> { { "Hip", Vector2.zero } };

            // önce hepsini oluştur, sonra ebeveynlere bağla (sıra çizim sırasıdır, hiyerarşi değil)
            foreach (var p in rig.parts)
            {
                var go = new GameObject(p.name);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "/" + p.file);
                sr.sortingOrder = p.order;
                if (sr.sprite == null) Debug.LogWarning("Dracolly: sprite yüklenemedi " + p.file);
                bones[p.name] = go.transform;
                pos[p.name] = new Vector2(p.posX, p.posY);
            }
            foreach (var p in rig.parts)
            {
                var t = bones[p.name];
                t.SetParent(bones[p.parent], false);
                var rel = pos[p.name] - pos[p.parent];
                t.localPosition = new Vector3(rel.x, rel.y, 0f);
            }

            var walk = visual.gameObject.AddComponent<CutoutWalk>();
            walk.body = rb;
            walk.hip = hip;
            walk.torso = Get(bones, "torso");
            walk.head = Get(bones, "head");
            walk.upperArm = Get(bones, "upper_arm");
            walk.forearm = Get(bones, "forearm");
            walk.hand = Get(bones, "hand");
            walk.thigh = Get(bones, "thigh");
            walk.shin = Get(bones, "shin");
            walk.upperArmBack = Get(bones, "upper_arm_back");
            walk.forearmBack = Get(bones, "forearm_back");
            walk.handBack = Get(bones, "hand_back");
            walk.thighBack = Get(bones, "thigh_back");
            walk.shinBack = Get(bones, "shin_back");

            var pc = root.AddComponent<PlayerController2D>();
            pc.visual = visual;
            pc.walk = walk;
            return root;
        }

        static Transform Get(Dictionary<string, Transform> d, string key) =>
            d.TryGetValue(key, out var t) ? t : null;

        static void Platform(Transform parent, Sprite tile, string name, Vector2 center, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = tile;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            sr.sortingOrder = -10;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
        }

        static void ConfigureSprite(string path, float ppu, Vector2 pivot, SpriteMeshType mesh, TextureWrapMode wrap)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null)
            {
                Debug.LogWarning("Dracolly: görsel bulunamadı " + path);
                return;
            }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ppu;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.wrapMode = wrap;
            ti.filterMode = FilterMode.Bilinear;

            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot = pivot;
            s.spriteMeshType = mesh;
            ti.SetTextureSettings(s);
            ti.SaveAndReimport();
        }
    }
}
