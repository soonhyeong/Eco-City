#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using EcoCity.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;

namespace EcoCity.Editor
{
    // Kid-friendly makeover of the submission scene using the imported asset packs:
    // Toon City park/town, SpaceZeta bins, Mess Maker / Hash Game / HEXXX trash and props, Kids Character helper,
    // plus a picture-first UI. Gameplay roots, ids, categories and references stay; only visuals and layout change.
    // Safe to run again: generated children are replaced, pack assets are never modified.
    public static class SubmissionKidMakeover
    {
        private const string Root = "Assets/EcoCity/Generated/Submission/KidWorld";
        private const string Toon = "Assets/Loading Games/Toon City Pack/Prefabs/";
        private const string Hexxx = "Assets/HEXXX/Hyper Casual Urban Asset Pack/Prefab/";
        private const string Hash = "Assets/Hash Game studios/Trash Bag & Trash Box/Resources/Prefabs/";
        private const string Mess = "Assets/Mess Maker Free/Prefabs/Low Poly/";
        private const string Bin = "Assets/SpaceZeta_PlasticTrashBins/Prefabs/TrashbinBlue.prefab";
        private const string Helper = "Assets/KidsCharacterFree/Prefabs/Boy0_Humanoid.prefab";
        private const string HelperController = "Assets/KidsCharacterFree/Animators/AnimatorBoy0Humanoid.controller";
        private const string ModelName = "모델";
        public const string KidFontPath = "Assets/EcoCity/Fonts/Jua/Jua-Regular.ttf";
        private static readonly Color Ink = new Color(0.17f, 0.2f, 0.26f);
        private static readonly Color Sun = new Color(1f, 0.8f, 0.2f);
        private static readonly Dictionary<Material, Material> converted = new Dictionary<Material, Material>();
        private static Font font;
        private static Camera viewCamera;

        private struct BinLook { public WasteCategory category; public Color color; public string icon; public Vector3 iconEuler; public float iconScale; }
        internal static Color BinColor(WasteCategory category) { return System.Array.Find(Bins, b => b.category == category).color; }
        private static readonly BinLook[] Bins =
        {
            new BinLook { category = WasteCategory.Paper, color = new Color(0.22f, 0.52f, 0.95f), icon = Hexxx + "Cartboard.prefab", iconEuler = new Vector3(0, 35, 0) },
            new BinLook { category = WasteCategory.Plastic, color = new Color(1f, 0.6f, 0.15f), icon = Hash + "Garbage/Bottles/2/Bottle 2.prefab", iconEuler = new Vector3(0, 20, -15) },
            new BinLook { category = WasteCategory.Metal, color = new Color(0.9f, 0.27f, 0.27f), icon = Mess + "Cans/Soda Can Red.prefab", iconEuler = new Vector3(-90, 0, 20) },
            new BinLook { category = WasteCategory.Glass, color = new Color(0.2f, 0.7f, 0.38f), icon = Hash + "Garbage/Bottles/1/Bottle 1 green glass.prefab", iconEuler = new Vector3(0, 0, -15) },
            new BinLook { category = WasteCategory.Vinyl, color = new Color(0.66f, 0.42f, 0.92f), icon = Hash + "Trash bags/Trash bag 1/Trash bag 1 clean green.prefab", iconEuler = new Vector3(0, 30, 0) },
            new BinLook { category = WasteCategory.Food, color = new Color(0.62f, 0.45f, 0.25f), icon = Mess + "Food/Apple Core Red.prefab", iconEuler = new Vector3(-90, 0, 30) },
            new BinLook { category = WasteCategory.General, color = new Color(0.5f, 0.52f, 0.56f), icon = Hash + "Trash bags/Trash bag 1/Trash bag 1 clean black.prefab", iconEuler = new Vector3(0, 30, 0) },
        };

        [MenuItem("EcoCity/Submission/Apply Kid-Friendly World and UI")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(SubmissionSceneBuilder.ScenePath);
            var flow = Object.FindFirstObjectByType<AlphaFlow>();
            if (flow.player.GetComponent<CharacterSelection>() == null) throw new IOException("Apply the Little Guys player first.");
            foreach (var folder in new[] { Root, Root + "/Materials", Root + "/Icons", Root + "/Definitions" }) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            converted.Clear();
            // Jua (OFL, Assets/EcoCity/Fonts/Jua) is the rounded display font for the kid UI and signs.
            font = AssetDatabase.LoadAssetAtPath<Font>(KidFontPath);
            if (font == null) throw new FileNotFoundException("Missing kid font: " + KidFontPath);
            AlphaSceneBuilder.UseFont(font);
            // Crisp UI kit: TextMeshPro SDF fonts and high-resolution shape sprites, shared with the title scene.
            SubmissionKidUi.Prepare();
            viewCamera = flow.player.viewCamera;
            Transform world = flow.game.transform.parent;

            var decor = world.Find("꾸미기");
            if (decor != null) Object.DestroyImmediate(decor.gameObject);
            decor = new GameObject("꾸미기").transform; decor.SetParent(world, false);
            RemoveOldSigns(world);
            Lighting(viewCamera);
            Park(world, decor, flow.game);
            Expand(world, decor);
            // Beach and market regions (with their own bins, stations and new items), far east of the park.
            SubmissionKidRegionsBuilder.Build(flow, world);
            var itemIcons = Items(flow.game);
            BinsAndStations(world);
            HelperNpc(world);
            Tune(flow);
            Extras(flow);
            Finale(flow);
            Regions(flow);
            Atmosphere(flow);
            Soundscape(flow);
            LivingTown(flow);
            Rewards(flow);
            var cues = Cues(flow, itemIcons.Key);
            var title = TitleBackground();
            Ui(flow, itemIcons.Value, title);
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save submission scene.");
            Debug.Log("EcoCity: kid-friendly world and UI applied (" + cues + " item markers). No Play Mode.");
        }

        // ---------- World ----------
        private static void RemoveOldSigns(Transform world)
        {
            foreach (var sign in world.GetComponentsInChildren<TextMesh>(true)) if (sign != null) Object.DestroyImmediate(sign.gameObject);
            foreach (var sign in world.GetComponentsInChildren<TextMeshPro>(true)) if (sign != null) Object.DestroyImmediate(sign.gameObject);
            var doomed = new List<GameObject>();
            foreach (Transform child in world)
                if (child.name == "수거함 입구" || child.name.StartsWith("준비 도구") || child.name == "도우미 모자" || child.name.StartsWith("벤치")) doomed.Add(child.gameObject);
            foreach (var obj in doomed) Object.DestroyImmediate(obj);
        }
        private static void Lighting(Camera camera)
        {
            var sun = GameObject.Find("Sun").GetComponent<Light>();
            sun.transform.rotation = Quaternion.Euler(48, 325, 0); sun.intensity = 1.05f; sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.55f;
            sun.color = new Color(1f, 0.97f, 0.9f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.82f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.68f, 0.62f);
            RenderSettings.ambientGroundColor = new Color(0.38f, 0.4f, 0.34f);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.62f, 0.83f, 0.97f); camera.farClipPlane = 320;
        }
        private static void Park(Transform world, Transform decor, EcoGame game)
        {
            var ground = world.Find("Ground").GetComponent<Renderer>();
            ground.sharedMaterial = Solid("Grass", new Color(0.52f, 0.8f, 0.4f));
            var city = Object.FindFirstObjectByType<CityEnvironmentView>();
            city.groundDirty = new Color(0.68f, 0.7f, 0.46f); city.groundClean = new Color(0.52f, 0.8f, 0.4f);
            city.greeneryDirty = new Color(0.72f, 0.62f, 0.42f); city.greeneryClean = Color.white;
            var path = Solid("Path", new Color(0.96f, 0.88f, 0.7f));
            foreach (var name in new[] { "중앙 산책길", "준비 구역 길", "분리배출 구역 길" }) world.Find(name).GetComponent<Renderer>().sharedMaterial = path;
            // Outer lawn so the town does not float in the sky colour.
            var lawn = GameObject.CreatePrimitive(PrimitiveType.Plane); lawn.name = "바깥 잔디"; lawn.transform.SetParent(decor, false);
            lawn.transform.localPosition = new Vector3(0, -0.02f, 0); lawn.transform.localScale = new Vector3(22, 1, 22);
            Object.DestroyImmediate(lawn.GetComponent<Collider>()); lawn.GetComponent<Renderer>().sharedMaterial = Solid("Lawn", new Color(0.47f, 0.72f, 0.38f));
            // Walls keep their colliders; a wooden fence is the visible edge.
            foreach (var wall in new[] { "WestWall", "EastWall", "NorthWall", "SouthWall" }) world.Find(wall).GetComponent<Renderer>().enabled = false;
            var fence = new GameObject("울타리").transform; fence.SetParent(decor, false);
            for (float x = -23; x <= 23.01f; x += 2) { Spawn(Toon + "Urban Props/wooden-fence.prefab", fence, new Vector3(x, 0, ParkNorth - 0.2f), 0); Spawn(Toon + "Urban Props/wooden-fence.prefab", fence, new Vector3(x, 0, ParkSouth + 0.2f), 0); }
            for (float z = ParkSouth + 1; z <= ParkNorth - 0.99f; z += 2) { Spawn(Toon + "Urban Props/wooden-fence.prefab", fence, new Vector3(-ParkHalfWidth + 0.2f, 0, z), 90); Spawn(Toon + "Urban Props/wooden-fence.prefab", fence, new Vector3(ParkHalfWidth - 0.2f, 0, z), 90); }
            // Neighbourhood outside the fence.
            var town = new GameObject("동네").transform; town.SetParent(decor, false);
            string civil = Toon + "Buildings/Civil Buildings/", shop = Toon + "Buildings/Commercial Buildings/", gov = Toon + "Buildings/Government Buildings/";
            Spawn(civil + "suburban-house-A.prefab", town, new Vector3(-20, 0, 30), 180); Spawn(shop + "commercial-building-ice-cream-shop.prefab", town, new Vector3(-4, 0, 30), 180);
            Spawn(civil + "suburban-house-C.prefab", town, new Vector3(12, 0, 30), 180); Spawn(civil + "cb-apartment-B.prefab", town, new Vector3(36, 0, 28), 225);
            Spawn(civil + "suburban-house-B.prefab", town, new Vector3(34, 0, 6), 270); Spawn(civil + "suburban-house-D.prefab", town, new Vector3(34, 0, -12), 270);
            Spawn(civil + "cb-apartment-A.prefab", town, new Vector3(-36, 0, 26), 135); Spawn(civil + "suburban-house-E.prefab", town, new Vector3(-34, 0, 4), 90);
            Spawn(shop + "commercial-building-coffee-shop.prefab", town, new Vector3(-34, 0, -14), 90); Spawn(gov + "government-building-school.prefab", town, new Vector3(0, 0, -40), 0);
            Spawn(civil + "suburban-house-A.prefab", town, new Vector3(-24, 0, -38), 0); Spawn(shop + "commercial-building-coffee-shop.prefab", town, new Vector3(24, 0, -38), 0);
            var trees = new[] { "tree-type-A", "tree-type-B", "tree-type-C", "tree-type-D" };
            int t = 0;
            for (float x = -24; x <= 24.01f; x += 4) Spawn(Toon + "Vegetation/" + trees[t++ % 4] + ".prefab", town, new Vector3(x, 0, ParkNorth + 3), t * 40);
            for (float z = ParkSouth + 2; z <= ParkNorth; z += 5) { Spawn(Toon + "Vegetation/" + trees[t++ % 4] + ".prefab", town, new Vector3(-ParkHalfWidth - 3, 0, z), t * 40); Spawn(Toon + "Vegetation/" + trees[t++ % 4] + ".prefab", town, new Vector3(ParkHalfWidth + 3, 0, z), t * 40); }
            // Inside the park: the old sphere trees keep their colliders, real trees and bushes show the cleanliness tint.
            var greenery = new List<Renderer>();
            for (int i = 0; i < 5; i++)
            {
                var sphere = world.Find("Tree-" + i);
                sphere.GetComponent<Renderer>().enabled = false;
                var tree = Spawn(Toon + "Vegetation/" + trees[i % 4] + ".prefab", decor, new Vector3(sphere.position.x, 0, sphere.position.z), i * 70);
                var bush = Spawn(Toon + "Vegetation/bush-type-" + (i % 2 == 0 ? "A" : "B") + ".prefab", decor, new Vector3(sphere.position.x + 1.4f, 0, sphere.position.z - 0.6f), i * 50);
                greenery.AddRange(tree.GetComponentsInChildren<Renderer>()); greenery.AddRange(bush.GetComponentsInChildren<Renderer>());
            }
            city.greenery = greenery.ToArray();
            for (int i = 0; i < 3; i++)
            {
                var bench = Spawn(Toon + "Urban Props/public-bench.prefab", decor, new Vector3(10.4f, 0, -2 + i * 4), 270);
                AddBoxCollider(bench);
            }
            foreach (var corner in new[] { new Vector3(-12.5f, 0, -12.5f), new Vector3(12.5f, 0, -12.5f), new Vector3(-12.5f, 0, 13.5f), new Vector3(12.5f, 0, 13.5f) })
                Spawn(Toon + "Infrastructure/Props/street-light-single.prefab", decor, corner, corner.x < 0 ? 180 : 0);
            Spawn(Toon + "Urban Props/ice-cream-stand.prefab", decor, new Vector3(11, 0, -11), 270);
            Spawn(Toon + "Vegetation/bush-type-A.prefab", decor, new Vector3(-11.5f, 0, -9), 0);
            Spawn(Toon + "Vegetation/bush-type-B.prefab", decor, new Vector3(-11.5f, 0, 8), 0);
            // Restored gardens: same roots (AlphaCityView), new flower beds.
            var gardens = Object.FindFirstObjectByType<AlphaCityView>().restoredGardens;
            float[] gardenX = { -5.6f, 0.5f, 6.5f };
            for (int i = 0; i < gardens.Length; i++)
            {
                var garden = gardens[i].transform;
                for (int c = garden.childCount - 1; c >= 0; c--) Object.DestroyImmediate(garden.GetChild(c).gameObject);
                var bed = Spawn(Hexxx + "Garden_bed.prefab", garden, new Vector3(gardenX[i], 0, 12.3f), 0, 1, true);
                Spawn(Toon + "Vegetation/bush-type-A.prefab", garden, new Vector3(gardenX[i] - 0.8f, 0.9f, 12.3f), 0, 0.6f);
                Spawn(Toon + "Vegetation/bush-type-B.prefab", garden, new Vector3(gardenX[i] + 0.8f, 0.9f, 12.3f), 0, 0.6f);
                bed.name = "꽃밭";
            }
        }

        // ---------- Bigger park ----------
        private const float ParkHalfWidth = 24, ParkNorth = 20, ParkSouth = -26;
        // The play area grows around the original mission area (which stays untouched in the middle):
        // a fountain plaza with the mini-game booth in the south, a picnic pond in the east, a flower garden in the west,
        // and a street with parked cars outside the fence.
        private static void Expand(Transform world, Transform decor)
        {
            float depth = ParkNorth - ParkSouth, middle = (ParkNorth + ParkSouth) / 2;
            var ground = world.Find("Ground");
            ground.position = new Vector3(0, -0.3f, middle); ground.localScale = new Vector3(ParkHalfWidth * 2, 0.5f, depth);
            Wall(world, "WestWall", new Vector3(-ParkHalfWidth, 1, middle), new Vector3(0.3f, 2, depth));
            Wall(world, "EastWall", new Vector3(ParkHalfWidth, 1, middle), new Vector3(0.3f, 2, depth));
            Wall(world, "NorthWall", new Vector3(0, 1, ParkNorth), new Vector3(ParkHalfWidth * 2, 2, 0.3f));
            Wall(world, "SouthWall", new Vector3(0, 1, ParkSouth), new Vector3(ParkHalfWidth * 2, 2, 0.3f));
            var area = new GameObject("넓어진 공원").transform; area.SetParent(decor, false);
            var path = Solid("Path", new Color(0.96f, 0.88f, 0.7f));
            var stone = Solid("Stone", new Color(0.86f, 0.84f, 0.9f));
            var water = Solid("Water", new Color(0.45f, 0.76f, 1f));
            var trees = new[] { "tree-type-A", "tree-type-B", "tree-type-C", "tree-type-D" };

            // Paths joining the new corners to the old walk.
            Flat(area, "남쪽 길", new Vector3(0, 0.012f, -16.5f), new Vector3(3.5f, 1, 13), path);
            Flat(area, "동쪽 길", new Vector3(16.5f, 0.012f, 0), new Vector3(13, 1, 3), path);
            Flat(area, "서쪽 길", new Vector3(-17, 0.012f, -6), new Vector3(12, 1, 3), path);
            Disc(area, "광장", new Vector3(0, 0.011f, -18), 11, 0.02f, path, false);

            // Fountain plaza (south).
            var fountain = new GameObject("분수").transform; fountain.SetParent(area, false); fountain.localPosition = new Vector3(0, 0, -18);
            Disc(fountain, "받침", new Vector3(0, 0.2f, 0), 4.6f, 0.4f, stone, true);
            Disc(fountain, "물", new Vector3(0, 0.42f, 0), 4f, 0.04f, water, false);
            Disc(fountain, "기둥", new Vector3(0, 1f, 0), 0.6f, 1.2f, stone, false);
            Disc(fountain, "위 그릇", new Vector3(0, 1.75f, 0), 1.8f, 0.2f, stone, false);
            Disc(fountain, "위 물", new Vector3(0, 1.86f, 0), 1.5f, 0.04f, water, false);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90 + 45; var at = Quaternion.Euler(0, angle, 0) * new Vector3(0, 0, 4.4f);
                var bench = Spawn(Toon + "Urban Props/public-bench.prefab", fountain, at, angle + 180); AddBoxCollider(bench);
            }
            foreach (var x in new[] { -4f, 4f }) Spawn(Toon + "Infrastructure/Props/street-light-double.prefab", area, new Vector3(x, 0, -12.5f), 0);
            foreach (var x in new[] { -6.5f, 6.5f })
            {
                Spawn(Hexxx + "Garden_bed.prefab", area, new Vector3(x, 0, -23), 0, 1, true);
                Spawn(Toon + "Vegetation/bush-type-A.prefab", area, new Vector3(x - 0.8f, 0.9f, -23), 0, 0.6f);
                Spawn(Toon + "Vegetation/bush-type-B.prefab", area, new Vector3(x + 0.8f, 0.9f, -23), 0, 0.6f);
            }
            Solid3(Spawn(Toon + "Urban Props/hot-dog-stand.prefab", area, new Vector3(11, 0, -20), 270));
            Solid3(Spawn(Toon + "Urban Props/outdoor-seating.prefab", area, new Vector3(13.5f, 0, -16), 0));
            Spawn(Toon + "Urban Props/mailbox.prefab", area, new Vector3(-3.5f, 0, -24.6f), 0);
            Spawn(Toon + "Urban Props/fire-hydrant.prefab", area, new Vector3(3.5f, 0, -24.6f), 0);

            // Mini-game booth (south-west of the plaza).
            for (int i = 0; i < Booths.Length; i++) BuildMiniGameBooth(world, Booths[i]);

            // Picnic pond (east).
            var pond = Disc(area, "연못", new Vector3(17, 0.03f, 11), 7, 0.06f, water, true);
            pond.transform.localScale = new Vector3(8, 0.06f, 5.5f);
            for (int i = 0; i < 7; i++)
            {
                float angle = i * 51; var at = new Vector3(17, 0, 11) + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * 4.4f, 0, Mathf.Sin(angle * Mathf.Deg2Rad) * 3.1f);
                Spawn(Toon + "Vegetation/rock-type-" + (i % 2 == 0 ? "A" : "B") + ".prefab", area, at, angle, 0.7f);
            }
            Solid3(Spawn(Toon + "Urban Props/outdoor-seating.prefab", area, new Vector3(18, 0, 2.5f), 90));
            Solid3(Spawn(Toon + "Urban Props/outdoor-seating.prefab", area, new Vector3(18, 0, -4.5f), 90));
            Solid3(Spawn(Hexxx + "Vending_machine_blue.prefab", area, new Vector3(22.8f, 0, -1), 270));
            Solid3(Spawn(Hexxx + "Vending_machine_red.prefab", area, new Vector3(22.8f, 0, -2.6f), 270));
            Solid3(Spawn(Toon + "Urban Props/lemonade-stand.prefab", area, new Vector3(20, 0, -11), 270));
            foreach (var spot in new[] { new Vector3(21.5f, 0, 17), new Vector3(13, 0, 17.5f), new Vector3(22, 0, 6), new Vector3(15, 0, -9), new Vector3(21.5f, 0, -20) })
                Solid3(Spawn(Toon + "Vegetation/" + trees[(int)Mathf.Abs(spot.z) % 4] + ".prefab", area, spot, spot.x * 13));

            // Flower garden (west).
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 2; col++)
                {
                    var at = new Vector3(-21 + col * 4.2f, 0, -1 + row * 5);
                    Spawn(Hexxx + "Garden_bed.prefab", area, at, 90, 1, true);
                    Spawn(Toon + "Vegetation/bush-type-" + (row % 2 == 0 ? "A" : "B") + ".prefab", area, at + new Vector3(0, 0.9f, -0.7f), row * 40, 0.6f);
                    Spawn(Toon + "Vegetation/bush-type-" + (row % 2 == 0 ? "B" : "A") + ".prefab", area, at + new Vector3(0, 0.9f, 0.7f), row * 70, 0.6f);
                }
            foreach (var spot in new[] { new Vector3(-21, 0, 16), new Vector3(-14.5f, 0, 17), new Vector3(-22, 0, -12), new Vector3(-15, 0, -20), new Vector3(-21.5f, 0, -22) })
                Solid3(Spawn(Toon + "Vegetation/" + trees[(int)Mathf.Abs(spot.x) % 4] + ".prefab", area, spot, spot.z * 17));
            for (int i = 0; i < 2; i++) { var bench = Spawn(Toon + "Urban Props/public-bench.prefab", area, new Vector3(-15.5f, 0, -12 + i * 3.2f), 90); AddBoxCollider(bench); }
            Spawn(Toon + "Vegetation/rock-type-A.prefab", area, new Vector3(-16, 0, 12), 30, 0.9f);
            Spawn(Toon + "Vegetation/rock-type-B.prefab", area, new Vector3(-19.5f, 0, 12.5f), 80, 0.8f);
            Spawn(Toon + "Infrastructure/Props/bus-stop.prefab", area, new Vector3(-20, 0, -18), 90);

            // Street with parked cars outside the south fence.
            Flat(area, "찻길", new Vector3(0, 0.01f, ParkSouth - 4.5f), new Vector3(90, 1, 7), Solid("Road", new Color(0.42f, 0.42f, 0.48f)));
            Flat(area, "찻길 선", new Vector3(0, 0.02f, ParkSouth - 4.5f), new Vector3(90, 1, 0.25f), Solid("RoadLine", new Color(1f, 0.92f, 0.55f)));
            string cars = Toon + "Vehicles/Civil Vehicles/";
            Spawn(cars + "civil-car-A.prefab", area, new Vector3(-14, 0, ParkSouth - 3), 90);
            Spawn(cars + "civil-car-C.prefab", area, new Vector3(6, 0, ParkSouth - 6), 270);
            Spawn(cars + "civil-car-B.prefab", area, new Vector3(18, 0, ParkSouth - 3), 90);
            foreach (var x in new[] { -18f, 0f, 18f }) Spawn(Toon + "Infrastructure/Props/street-light-single.prefab", area, new Vector3(x, 0, ParkSouth - 1), 0);
        }
        private static void Wall(Transform world, string name, Vector3 position, Vector3 scale)
        {
            var wall = world.Find(name); wall.position = position; wall.localScale = scale; wall.GetComponent<Renderer>().enabled = false;
        }
        private static GameObject Flat(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var part = Part(parent, PrimitiveType.Cube, position, new Vector3(size.x, 0.02f, size.z), material); part.name = name;
            return part;
        }
        private static GameObject Disc(Transform parent, string name, Vector3 position, float diameter, float height, Material material, bool solid)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cylinder); part.name = name; part.transform.SetParent(parent, false);
            part.transform.localPosition = position; part.transform.localScale = new Vector3(diameter, height / 2, diameter);
            part.GetComponent<Renderer>().sharedMaterial = material;
            // Cylinder colliders are capsules; a mesh collider keeps the flat shape for blocking.
            Object.DestroyImmediate(part.GetComponent<Collider>());
            if (solid) part.AddComponent<MeshCollider>().sharedMesh = part.GetComponent<MeshFilter>().sharedMesh;
            return part;
        }
        // Large props block walking, like the old benches.
        private static void Solid3(GameObject prop) { if (prop != null) AddBoxCollider(prop); }
        // One booth per region, each a different stand with its own game, on open ground near the region's start.
        public struct BoothSpot
        {
            public string name, prefab, label; public Vector3 position; public float yaw; public KidMiniGame.Kind kind;
            public BoothSpot(string name, Vector3 position, string prefab, float yaw, string label, KidMiniGame.Kind kind)
            { this.name = name; this.position = position; this.prefab = prefab; this.yaw = yaw; this.label = label; this.kind = kind; }
        }
        public static readonly BoothSpot[] Booths =
        {
            new BoothSpot("MiniGame-Booth", new Vector3(-9, 0, -16), "Urban Props/lemonade-stand.prefab", 90, "미니 게임", KidMiniGame.Kind.Bins),
            // Right of the path, away from the preparation stations on the left (so E near a station never opens the game).
            new BoothSpot("MiniGame-Booth-Beach", new Vector3(232, 0, -4), "Urban Props/ice-cream-stand.prefab", -90, "미니 게임", KidMiniGame.Kind.FirstStep),
            new BoothSpot("MiniGame-Booth-Market", new Vector3(452, 0, 2), "Urban Props/hot-dog-stand.prefab", -90, "미니 게임", KidMiniGame.Kind.TrueFalse),
        };
        private static void BuildMiniGameBooth(Transform world, BoothSpot spot)
        {
            var old = world.Find(spot.name); if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject(spot.name).transform; root.SetParent(world, false); root.position = spot.position;
            var booth = root.gameObject.AddComponent<MiniGameBooth>(); booth.kind = spot.kind;
            var model = Spawn(Toon + spot.prefab, root, Vector3.zero, spot.yaw);
            FinishModel(booth, model, 1);
            Board(root, SubmissionKidUi.StarSprite.texture, spot.label, new Color(1f, 0.5f, 0.72f), ModelTop(model, root) + 1.05f);
        }
        /// <summary>Adds a region's booth to a scene built before it existed: a copy of the park booth (same sign) with the stand swapped.</summary>
        public static MiniGameBooth CopyBooth(MiniGameBooth park, BoothSpot spot)
        {
            var world = park.transform.parent;
            var old = world.Find(spot.name); if (old != null) Object.DestroyImmediate(old.gameObject);
            var copy = Object.Instantiate(park.gameObject, world); copy.name = spot.name;
            copy.transform.position = spot.position;
            var booth = copy.GetComponent<MiniGameBooth>(); booth.kind = spot.kind; booth.miniGame = park.miniGame;
            var oldModel = copy.transform.Find(ModelName); if (oldModel != null) Object.DestroyImmediate(oldModel.gameObject);
            var model = Spawn(Toon + spot.prefab, copy.transform, Vector3.zero, spot.yaw);
            FinishModel(booth, model, 1);
            var board = copy.transform.Find("표지판");
            if (board != null) board.localPosition = new Vector3(0, ModelTop(model, copy.transform) + 1.05f, 0);
            return booth;
        }

        // ---------- Gameplay objects ----------
        private static void Rule(WasteDefinition definition, WasteCategory bin, Preparation steps, LearningLevel level)
        { definition.targetBin = bin; definition.requiredPreparation = steps; definition.level = level; }
        // Region 1 (park, x 0): clean items. Region 2 (beach, x 220): empty and wash. Region 3 (market, x 440): separate first.
        private static readonly Dictionary<string, Vector3> Spots = new Dictionary<string, Vector3>
        {
            { "paper-01", new Vector3(-7, 0, -8) }, { "plastic-01", new Vector3(6, 0, -12) }, { "metal-01", new Vector3(11, 0, -4) },
            { "glass-01", new Vector3(-13, 0, -2.5f) }, { "food-01", new Vector3(8, 0, 4) },
            { "container-01", new Vector3(208, 0, -10) }, { "cup-01", new Vector3(234, 0, -14) }, { "can-02", new Vector3(236, 0, 4) }, { "glass-02", new Vector3(204, 0, 6) },
            { "composite-01", new Vector3(426, 0, -12) }, { "pet-02", new Vector3(455, 0, -6) }, { "box-02", new Vector3(442, 0, 15) },
        };
        // New pack models come in very different sizes; fit the largest side.
        private static GameObject Fit(GameObject model, float size)
        {
            var bounds = RendererBounds(model);
            float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largest > 0.0001f) model.transform.localScale *= size / largest;
            return model;
        }
        // Nearest spot (searching outwards) where nothing solid stands, so an item never ends up inside a bench, tree or stand.
        private static Vector3 FreeSpot(Vector3 wanted, WasteItem item)
        {
            Physics.SyncTransforms();
            for (int ring = 0; ring < 8; ring++)
                for (int k = 0; k < Mathf.Max(1, ring * 8); k++)
                {
                    float angle = ring == 0 ? 0 : k * Mathf.PI * 2 / (ring * 8);
                    var at = wanted + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * ring * 0.8f;
                    bool blocked = false;
                    foreach (var hit in Physics.OverlapSphere(at + Vector3.up * 0.9f, 0.8f, ~0, QueryTriggerInteraction.Ignore))
                        if (!hit.transform.IsChildOf(item.transform) && hit.gameObject.name != "Ground") { blocked = true; break; }
                    if (!blocked) return at;
                }
            return wanted;
        }
        private static KeyValuePair<Transform[], Dictionary<string, Texture2D>> Items(EcoGame game)
        {
            var icons = new Dictionary<string, Texture2D>();
            var markers = new Transform[game.wasteItems.Length];
            for (int i = 0; i < game.wasteItems.Length; i++)
            {
                var item = game.wasteItems[i];
                item.definition = KidDefinition(item.definition, item.instanceId);
                // Spread over the whole wider park; first-stage items stay closest to the start.
                Vector3 spot;
                if (Spots.TryGetValue(item.instanceId, out spot)) item.transform.position = FreeSpot(spot, item);
                var root = PrepareRoot(item, 0);
                GameObject model;
                switch (item.instanceId)
                {
                    case "paper-01": model = Spawn(Hexxx + "Cartboard.prefab", root, Vector3.zero, 25, 0.6f); break;
                    case "plastic-01": model = Spawn(Hash + "Garbage/Bottles/2/Bottle 2.prefab", root, Vector3.zero, 0, 5f); break;
                    case "metal-01": model = Spawn(Mess + "Cans/Soda Can Red.prefab", root, Vector3.zero, 0, 2.4f); model.transform.localRotation = Quaternion.Euler(-90, 0, 0); break;
                    case "container-01": model = Spawn(Hash + "Garbage/Gallon/Gallon 2 plastic.prefab", root, Vector3.zero, 30, 1.5f); break;
                    case "cup-01": model = Cup(root); break;
                    case "glass-01": model = Fit(Spawn(Hash + "Garbage/Bottles/1/Bottle 1 green glass.prefab", root, Vector3.zero, 0, 1), 0.5f); break;
                    case "food-01": model = Fit(Spawn(Mess + "Food/Apple Core Red.prefab", root, Vector3.zero, 30, 1), 0.38f); break;
                    case "can-02": model = Fit(Spawn(Hash + "Garbage/Canned/Soda/Soda can 1 blue dirt.prefab", root, Vector3.zero, 0, 1), 0.4f); break;
                    case "glass-02": model = Fit(Spawn(Hash + "Garbage/Bottles/1/Bottle 1 glass dirt.prefab", root, Vector3.zero, 0, 1), 0.5f); break;
                    case "pet-02": model = Fit(Spawn(Hash + "Garbage/Bottles/2/Bottle 2 crumple.prefab", root, Vector3.zero, 0, 1), 0.5f); break;
                    case "box-02": model = Fit(Spawn(Hexxx + "Cartboard_2.prefab", root, Vector3.zero, 20, 1), 0.7f); break;
                    default: model = Spawn(Hash + "Garbage/Pizza box/Pizza box 1.prefab", root, Vector3.zero, 15, 2.6f); break;
                }
                FinishModel(item, model, 0.55f);
                icons[item.instanceId] = Icon(model, "Item_" + item.instanceId, new Color(0.93f, 0.97f, 1f));
                var marker = Label(root, "!", new Vector3(0, ModelTop(model, root) + 0.45f, 0), 0.075f, Sun, true, SubmissionKidUi.WorldOutlineMaterial);
                marker.name = "반짝 표시";
                markers[i] = marker.transform;
            }
            return new KeyValuePair<Transform[], Dictionary<string, Texture2D>>(markers, icons);
        }
        private static WasteDefinition KidDefinition(WasteDefinition source, string id)
        {
            string path = Root + "/Definitions/" + id + ".asset";
            var copy = AssetDatabase.LoadAssetAtPath<WasteDefinition>(path);
            if (copy == null)
            {
                // A pack-independent copy so the prototype/alpha scenes keep their original data.
                copy = Object.Instantiate(source); copy.name = "Kid-" + id;
                AssetDatabase.CreateAsset(copy, path);
            }
            // The reason is shown with hints, so it never names the bin.
            string name, state, reason = "물건을 만든 재료와 같은 그림의 통을 찾아요.";
            switch (id)
            {
                case "paper-01": name = "종이 상자"; state = "깨끗하고 마른 종이 상자예요."; break;
                case "plastic-01": name = "플라스틱 병"; state = "속이 빈 깨끗한 병이에요."; break;
                case "metal-01": name = "음료 캔"; state = "속이 빈 깨끗한 캔이에요."; break;
                case "container-01": name = "더러운 플라스틱 통"; state = "안에 음식이 남았어요. 비우고 씻어요."; reason = "음식이 남은 통은 비우고 씻어야 다시 쓸 수 있어요."; break;
                case "cup-01": name = "빨대 꽂힌 컵"; state = "음료가 남았고 뚜껑과 빨대가 달려 있어요."; reason = "뚜껑과 빨대는 떼어내고, 컵은 비우고 씻어요."; break;
                case "composite-01": name = "피자 상자"; state = "기름과 음식이 묻었어요. 먼저 떼어내요. (게임 연습 규칙)"; reason = "이 게임에서만 쓰는 연습 규칙이에요. 진짜 물건은 어른과 함께 확인해요."; break;
                // New items for the three regions (rules as children learn them in Korea: empty, rinse, remove labels and tape).
                case "glass-01": name = "유리병"; state = "속이 빈 깨끗한 유리병이에요."; Rule(copy, WasteCategory.Glass, Preparation.None, LearningLevel.Basic); break;
                case "food-01": name = "사과 속"; state = "먹고 남은 사과 속이에요."; reason = "먹고 남은 음식은 따로 모으는 통이 있어요."; Rule(copy, WasteCategory.Food, Preparation.None, LearningLevel.Basic); break;
                case "can-02": name = "음료가 남은 캔"; state = "음료가 조금 남았어요. 비우고 헹궈요."; reason = "캔은 속을 비우고 헹궈야 다시 쓸 수 있어요."; Rule(copy, WasteCategory.Metal, Preparation.Empty | Preparation.Wash, LearningLevel.State); break;
                case "glass-02": name = "소스 묻은 유리병"; state = "안에 소스가 남았어요. 비우고 씻어요."; reason = "유리병도 속을 비우고 씻어서 넣어요."; Rule(copy, WasteCategory.Glass, Preparation.Empty | Preparation.Wash, LearningLevel.State); break;
                case "pet-02": name = "라벨 붙은 페트병"; state = "비닐 라벨이 붙어 있어요. 먼저 떼어내요."; reason = "페트병의 비닐 라벨은 떼어서 따로 버려요."; Rule(copy, WasteCategory.Plastic, Preparation.Separate, LearningLevel.Composite); break;
                default: name = "테이프 붙은 택배 상자"; state = "테이프와 송장이 붙어 있어요. 먼저 떼어내요."; reason = "상자에 붙은 테이프는 떼어내야 종이로 다시 쓸 수 있어요."; Rule(copy, WasteCategory.Paper, Preparation.Separate, LearningLevel.Composite); break;
            }
            copy.displayName = name; copy.stateDescription = state; copy.educationalReason = reason;
            EditorUtility.SetDirty(copy);
            return copy;
        }
        private static void BinsAndStations(Transform world)
        {
            foreach (var bin in Object.FindObjectsByType<DisposalBin>(FindObjectsSortMode.None))
            {
                var look = System.Array.Find(Bins, b => b.category == bin.category);
                var root = PrepareRoot(bin, 0);
                var model = Spawn(Bin, root, Vector3.zero, 180, 1.4f);
                var material = TintedBin(look);
                foreach (var renderer in model.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = material;
                FinishModel(bin, model, 0.6f);
                var iconSource = Spawn(look.icon, null, Vector3.zero, 0);
                iconSource.transform.rotation = Quaternion.Euler(look.iconEuler);
                var icon = Icon(iconSource, "Bin_" + bin.category, Color.white);
                Object.DestroyImmediate(iconSource);
                Board(root, icon, FeedbackText.Category(bin.category), look.color, ModelTop(model, root) + 1.05f);
            }
            var stationProps = new Dictionary<Preparation, string> { { Preparation.Empty, Hexxx + "Barrel.prefab" }, { Preparation.Separate, Hexxx + "Box.prefab" }, { Preparation.Wash, Hexxx + "Water_canister.prefab" } };
            var stationColor = new Color(0.25f, 0.62f, 0.82f);
            // Every region has its own three stations; each step's picture is drawn once.
            var stepIcons = new Dictionary<Preparation, Texture2D>();
            foreach (var station in Object.FindObjectsByType<PreparationStation>(FindObjectsSortMode.None))
            {
                var step = station.step;
                int number = step == Preparation.Empty ? 1 : step == Preparation.Separate ? 2 : 3;
                if (!stepIcons.ContainsKey(step)) stepIcons[step] = StepIcon(step);
                var root = PrepareRoot(station, 0);
                var holder = new GameObject(ModelName).transform; holder.SetParent(root, false);
                var table = Spawn(Hexxx + "Pallet.prefab", holder, Vector3.zero, 90, 1f);
                Spawn(stationProps[step], holder, new Vector3(0, 0.28f, 0), 20, 1.15f);
                FinishModel(station, holder.gameObject, 0.7f);
                Board(root, stepIcons[step], number + " " + FeedbackText.Step(step), stationColor, ModelTop(holder.gameObject, root) + 1.05f);
                table.name = "받침";
            }
        }
        private static void HelperNpc(Transform world)
        {
            var npc = Object.FindFirstObjectByType<GuideNpc>();
            var root = npc.transform;
            root.GetComponent<Renderer>().enabled = false;
            var old = root.Find(ModelName); if (old != null) Object.DestroyImmediate(old.gameObject);
            var model = Spawn(Helper, root, Vector3.zero, 0, 1f);
            model.name = ModelName;
            var animator = model.GetComponentInChildren<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(HelperController); animator.applyRootMotion = false;
            float height = RendererBounds(model).size.y;
            model.transform.localScale = Vector3.one * (1.45f / Mathf.Max(0.1f, height));
            var bounds = RendererBounds(model);
            model.transform.position += new Vector3(0, -bounds.min.y, 0);
            model.transform.rotation = Quaternion.LookRotation(new Vector3(-0.8f, 0, -1f));
            npc.visualRenderer = model.GetComponentInChildren<Renderer>();
            var bubble = Label(root, "?", new Vector3(0, 1.05f, 0), 0.08f, Sun, true, SubmissionKidUi.WorldOutlineMaterial); bubble.name = "물음표";
            var name = Label(root, "도우미", new Vector3(0, 0.62f, 0), 0.04f, Color.white, true, SubmissionKidUi.WorldOutlineMaterial); name.name = "이름표";
        }
        // Play-test tuning for young players.
        private static void Tune(AlphaFlow flow)
        {
            // Fixed camera heading: holding one arrow key keeps walking one way instead of circling behind the character.
            flow.player.cameraFollowSpeed = 0;
            // Bins look closer than they are from the camera; a longer reach forgives that.
            flow.interactor.reach = 2.6f;
            // Locomotion clips fire OnFootstep/OnLand events; give every character Animator a receiver.
            var animators = new List<Animator>(flow.player.GetComponent<CharacterSelection>().animators);
            animators.Add(Object.FindFirstObjectByType<GuideNpc>().GetComponentInChildren<Animator>());
            foreach (var animator in animators)
                if (animator != null && animator.GetComponent<AnimationEventSink>() == null) animator.gameObject.AddComponent<AnimationEventSink>();
        }
        // Title-scene handoff, item carried in front of the character, correct/wrong effects and spoken guidance.
        private static void Extras(AlphaFlow flow)
        {
            var systems = flow.game.gameObject;
            var bridge = systems.GetComponent<TitleSceneBridge>(); if (bridge == null) bridge = systems.AddComponent<TitleSceneBridge>();
            bridge.flow = flow;

            var player = flow.player.transform;
            var carry = player.Find("들고 있는 곳");
            if (carry == null) { carry = new GameObject("들고 있는 곳").transform; carry.SetParent(player, false); }
            // Above the head: the camera sits behind the character, so an item held in front would be hidden by the body.
            carry.localPosition = new Vector3(0, 1.55f, 0.05f); carry.localRotation = Quaternion.identity;
            var held = player.GetComponent<KidHeldVisual>(); if (held == null) held = player.gameObject.AddComponent<KidHeldVisual>();
            held.game = flow.game; held.carryPoint = carry; held.selection = flow.player.GetComponent<CharacterSelection>(); held.size = 0.34f; held.sideways = 0.3f;

            // Ground arrow toward the next "!" item (or the next preparation station), never toward a bin.
            var guide = player.GetComponent<KidGuideArrow>(); if (guide == null) guide = player.gameObject.AddComponent<KidGuideArrow>();
            guide.game = flow.game; guide.interactor = flow.interactor;
            var oldArrow = flow.game.transform.parent.Find("길 안내 화살표"); if (oldArrow != null) Object.DestroyImmediate(oldArrow.gameObject);
            var arrow = GameObject.CreatePrimitive(PrimitiveType.Quad); arrow.name = "길 안내 화살표"; arrow.transform.SetParent(flow.game.transform.parent, false);
            Object.DestroyImmediate(arrow.GetComponent<Collider>());
            arrow.GetComponent<Renderer>().sharedMaterial = Unlit("GuideArrow", SubmissionKidUi.ArrowSprite.texture, new Color(1f, 0.82f, 0.3f, 0.92f));
            arrow.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            arrow.SetActive(false); guide.arrow = arrow.transform;

            var fx = systems.GetComponent<KidFeedbackFx>(); if (fx == null) fx = systems.AddComponent<KidFeedbackFx>();
            fx.game = flow.game; fx.interactor = flow.interactor; fx.held = held;
            var world = flow.game.transform.parent;
            var oldSparkles = world.Find("반짝이 효과"); if (oldSparkles != null) Object.DestroyImmediate(oldSparkles.gameObject);
            var sparkleObject = new GameObject("반짝이 효과", typeof(ParticleSystem)); sparkleObject.transform.SetParent(world, false);
            var sparkles = sparkleObject.GetComponent<ParticleSystem>();
            sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = sparkles.main; main.playOnAwake = false; main.loop = false; main.duration = 1; main.maxParticles = 300;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f); main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.3f); main.gravityModifier = 0.55f; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.2f), Color.white);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var emission = sparkles.emission; emission.rateOverTime = 0;
            var shape = sparkles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.25f;
            var fade = sparkles.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            var particleRenderer = sparkleObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = Unlit("Sparkle", Painted("UI_Star", 128, StarPixel), Color.white);
            fx.sparkles = sparkles;
            var oldMark = world.Find("표시 원본"); if (oldMark != null) Object.DestroyImmediate(oldMark.gameObject);
            var mark = Label(world, "O", Vector3.zero, 0.11f, Color.white, false, SubmissionKidUi.MarkMaterial); mark.name = "표시 원본"; mark.SetActive(false);
            mark.GetComponent<MeshRenderer>().sortingOrder = 5;
            fx.markPrefab = mark.GetComponent<TextMeshPro>();

            var voice = systems.GetComponent<KidVoice>(); if (voice == null) voice = systems.AddComponent<KidVoice>();
            var voiceObject = world.Find("음성"); if (voiceObject == null) { voiceObject = new GameObject("음성", typeof(AudioSource)).transform; voiceObject.SetParent(world, false); }
            var voiceSource = voiceObject.GetComponent<AudioSource>(); voiceSource.playOnAwake = false; voiceSource.spatialBlend = 0; voiceSource.volume = 0.8f;
            voice.flow = flow; voice.source = voiceSource; voice.volumeFrom = systems.GetComponent<AudioSource>();
            var keys = new List<string>(); var clips = new List<AudioClip>();
            // Spoken lines are off (KidVoice.Enabled): the clips stay in the project but are not linked, so they are not built.
            if (KidVoice.Enabled)
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Root + "/Voice" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                keys.Add(Path.GetFileNameWithoutExtension(path)); clips.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
            }
            voice.keys = keys.ToArray(); voice.clips = clips.ToArray();
        }
        // Map, travel and "region saved" flow over the three regions.
        // Gradient sky, moving water, a breeze through the plants and floating boats, balloons, clouds and shore foam.
        private static void Atmosphere(AlphaFlow flow)
        {
            var world = flow.game.transform.parent;
            var camera = flow.player.viewCamera;
            var sun = GameObject.Find("Sun").GetComponent<Light>();
            string skyPath = Root + "/Materials/KidSky.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null) { sky = new Material(Shader.Find("EcoCity/KidSky")); AssetDatabase.CreateAsset(sky, skyPath); }
            sky.shader = Shader.Find("EcoCity/KidSky");
            sky.SetColor("_Top", new Color(0.36f, 0.64f, 0.96f)); sky.SetColor("_Horizon", new Color(0.82f, 0.92f, 1f)); sky.SetColor("_Bottom", new Color(0.7f, 0.82f, 0.9f));
            sky.SetVector("_SunDirection", -sun.transform.forward); sky.SetFloat("_SunSize", 60); sky.SetFloat("_Exponent", 0.8f);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky; camera.clearFlags = CameraClearFlags.Skybox;
            var mood = flow.game.GetComponent<KidEnvironmentMood>(); if (mood != null) { mood.sky = sky; EditorUtility.SetDirty(mood); }
            // Screen look: bloom, pastel colour, vignette; colours follow the cleanliness.
            var post = camera.GetComponent<KidPostFx>(); if (post == null) post = camera.gameObject.AddComponent<KidPostFx>();
            post.shader = Shader.Find("Hidden/EcoCity/KidPost"); post.mood = mood;
            post.threshold = 0.95f; post.knee = 0.2f; post.bloom = 0.25f; post.bloomSaved = 0.38f;

            // Water surfaces: moving wave bands and glints instead of flat colour.
            var waterShader = Shader.Find("EcoCity/KidWater");
            foreach (var name in new[] { "Water", "Sea" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/" + name + ".mat");
                if (material == null || waterShader == null) continue;
                Color colour = material.color;
                material.shader = waterShader; material.color = colour;
                material.SetColor("_WaveColor", Color.Lerp(colour, Color.white, 0.55f));
                material.SetFloat("_WaveScale", name == "Sea" ? 0.22f : 0.45f); material.SetFloat("_Speed", name == "Sea" ? 0.55f : 0.8f);
                material.SetFloat("_Glossiness", 0.85f);
                EditorUtility.SetDirty(material);
            }

            // Breeze: trees, bushes and flower beds everywhere.
            var plants = new List<Transform>();
            foreach (var t in world.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("tree-type") || t.name.StartsWith("bush-type") || t.name.StartsWith("꽃밭 ")) plants.Add(t);
            var sway = flow.game.GetComponent<KidSway>(); if (sway == null) sway = flow.game.gameObject.AddComponent<KidSway>();
            sway.plants = plants.ToArray();

            // Floating things.
            foreach (var t in world.GetComponentsInChildren<Transform>(true))
            {
                KidBob bob = null;
                if (t.name == "배") { bob = Bob(t); bob.move = new Vector3(0, 0.12f, 0); bob.tilt = new Vector3(4, 0, 6); bob.speed = 1.3f; }
                else if (t.name == "열기구") { bob = Bob(t); bob.move = new Vector3(0.8f, 1.4f, 0.8f); bob.tilt = new Vector3(1.5f, 0, 1.5f); bob.speed = 0.35f; }
                else if (t.name == "구름") { bob = Bob(t); bob.move = new Vector3(5, 0.4f, 0); bob.tilt = Vector3.zero; bob.speed = 0.07f; }
                else if (t.name == "물거품") { bob = Bob(t); bob.move = new Vector3(0, 0, 0.7f); bob.tilt = Vector3.zero; bob.speed = 0.9f; }
            }
        }
        // Music per region, ambience that follows the cleanliness, footsteps and action sounds (all generated by Validation/GenerateKidSounds.py).
        private static void Soundscape(AlphaFlow flow)
        {
            AssetDatabase.Refresh();
            var sound = flow.game.GetComponent<KidSoundscape>(); if (sound == null) sound = flow.game.gameObject.AddComponent<KidSoundscape>();
            sound.flow = flow; sound.regions = flow.game.GetComponent<KidRegions>(); sound.mood = flow.game.GetComponent<KidEnvironmentMood>(); sound.finale = flow.game.GetComponent<KidFinale>();
            sound.menuMusic = Sound("music/title");
            sound.regionMusic = new[] { Sound("music/park"), Sound("music/beach"), Sound("music/market") };
            sound.regionSavedJingle = Sound("music/region_saved"); sound.finaleJingle = Sound("music/finale");
            sound.regionAmbience = new[] { Sound("ambience/birds"), Sound("ambience/waves"), Sound("ambience/market") };
            sound.smogAmbience = Sound("ambience/smog");
            sound.grassSteps = new[] { Sound("sfx/step_grass_1"), Sound("sfx/step_grass_2"), Sound("sfx/step_grass_3") };
            sound.sandSteps = new[] { Sound("sfx/step_sand_1"), Sound("sfx/step_sand_2"), Sound("sfx/step_sand_3") };
            sound.pathSteps = new[] { Sound("sfx/step_path_1"), Sound("sfx/step_path_2"), Sound("sfx/step_path_3") };
            sound.pickup = Sound("sfx/pickup"); sound.throwSound = Sound("sfx/throw"); sound.lid = Sound("sfx/lid");
            sound.empty = Sound("sfx/empty"); sound.separate = Sound("sfx/separate"); sound.wash = Sound("sfx/wash"); sound.wrong = Sound("sfx/wrong");
            if (sound.regions != null) { sound.regions.stampClip = Sound("sfx/stamp"); sound.regions.clickClip = Sound("sfx/pop"); sound.regions.lockedClip = Sound("sfx/wrong"); }
            // Buttons pop like the rest of the world; the old result tones would double the action sounds above.
            var tones = flow.game.GetComponent<SubmissionAudio>();
            if (tones != null) { tones.clickClip = Sound("sfx/pop"); tones.resultTones = false; EditorUtility.SetDirty(tones); }
            EditorUtility.SetDirty(sound);
        }
        // A town that lives: kids walking outside the fences, cars driving the roads, trains on the tracks, birds and gulls overhead,
        // and flies buzzing over every piece of trash (with a grey stain under it) until it is picked up.
        private static void LivingTown(AlphaFlow flow)
        {
            var world = flow.game.transform.parent;
            var regions = flow.game.GetComponent<KidRegions>(); var finale = flow.game.GetComponent<KidFinale>();
            var old = world.Find("살아있는 마을"); if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("살아있는 마을").transform; root.SetParent(world, false);

            // Walkers: back and forth along the pavement in front of and behind each region.
            string controllerPath = Root + "/WalkerAnimator.controller";
            var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
            if (controller == null)
                controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath,
                    AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/KidsCharacterFree/AnimationClips/Humanoid/boy_move_walk.anim"));
            Color[] tints = { new Color(1f, 1f, 1f), new Color(0.82f, 0.9f, 1f), new Color(1f, 0.88f, 0.82f), new Color(0.9f, 0.85f, 1f) };
            float[][] lanes = { new[] { -27.2f, 24.6f }, new[] { -27.5f, 23.5f }, new[] { -28f, 22.8f } };
            int count = 0;
            for (int r = 0; r < 3; r++)
            {
                var offset = SubmissionKidRegionsBuilder.Offset(r);
                for (int k = 0; k < 4; k++)
                {
                    float z = lanes[r][k % 2], span = k % 2 == 0 ? 40 : 21;
                    var a = offset + new Vector3(-span, 0, z); var b = offset + new Vector3(span, 0, z);
                    var walker = new GameObject("동네 친구 " + (++count)).transform; walker.SetParent(root, false);
                    walker.position = Vector3.Lerp(a, b, k < 2 ? 0.25f : 0.7f);
                    var model = Spawn(Helper, walker, Vector3.zero, 0, 1f);
                    float height = RendererBounds(model).size.y;
                    model.transform.localScale = Vector3.one * ((1.25f + 0.08f * (k % 3)) / Mathf.Max(0.1f, height));
                    var bounds = RendererBounds(model); model.transform.position += new Vector3(0, -bounds.min.y, 0);
                    var animator = model.GetComponentInChildren<Animator>();
                    animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
                    if (animator.GetComponent<AnimationEventSink>() == null) animator.gameObject.AddComponent<AnimationEventSink>();
                    var mover = walker.gameObject.AddComponent<KidWalker>();
                    mover.points = k < 2 ? new[] { a, b } : new[] { b, a }; mover.region = r; mover.regions = regions; mover.finale = finale; mover.animator = animator;
                    mover.speed = 1.0f + 0.1f * k; mover.tint = tints[(k + r) % tints.Length];
                }
            }

            // Vehicles already standing on the roads start driving along their lane; trains run along the tracks.
            foreach (var t in world.GetComponentsInChildren<Transform>(true))
            {
                bool vehicle = t.name.StartsWith("civil-car") || t.name == "city-bus" || t.name == "taxi" || t.name == "police-car";
                bool train = t.name == "train" || t.name == "subway-metro";
                if (!vehicle && !train) continue;
                int r = regions != null ? regions.RegionAt(t.position) : 0;
                var centre = SubmissionKidRegionsBuilder.Offset(r);
                // Only what stands on a road or a track (not the cars in the parking lot).
                float z = t.position.z - centre.z;
                bool onRoad = Mathf.Abs(z + 30.5f) < 4 || Mathf.Abs(z + 31.5f) < 4 || Mathf.Abs(z - 39.5f) < 4;
                bool onTrack = Mathf.Abs(z + 86) < 2 || Mathf.Abs(z + 82) < 2;
                if (vehicle ? !onRoad : !onTrack) continue;
                bool east = Vector3.Dot(t.forward, Vector3.right) > 0;
                float reach = train ? 108 : 54;
                Vector3 from = centre + new Vector3(east ? -reach : reach, t.position.y, t.position.z), to = centre + new Vector3(east ? reach : -reach, t.position.y, t.position.z);
                var mover = t.gameObject.AddComponent<KidMover>();
                mover.mode = KidMover.Mode.Line; mover.speed = train ? 7 : 4.5f;
                mover.from = t.parent != null ? t.parent.InverseTransformPoint(from) : from;
                mover.to = t.parent != null ? t.parent.InverseTransformPoint(to) : to;
            }

            // Birds: little birds over the park, white gulls over the beach, pigeons over the market.
            Color[] birdColours = { new Color(0.45f, 0.42f, 0.5f), Color.white, new Color(0.7f, 0.72f, 0.78f) };
            int[] birdCounts = { 5, 7, 5 };
            var random = new System.Random(77);
            for (int r = 0; r < 3; r++)
                for (int k = 0; k < birdCounts[r]; k++)
                {
                    var bird = new GameObject("새").transform; bird.SetParent(root, false);
                    var body = Solid("Bird_" + r, birdColours[r]);
                    float scale = r == 1 ? 1.2f : 0.8f;
                    Part(bird, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.22f, 0.18f, 0.5f) * scale, body);
                    var wings = new Transform[2];
                    for (int w = 0; w < 2; w++)
                    {
                        var hinge = new GameObject(w == 0 ? "왼 날개" : "오른 날개").transform; hinge.SetParent(bird, false);
                        Part(hinge, PrimitiveType.Cube, new Vector3((w == 0 ? -0.32f : 0.32f) * scale, 0, 0), new Vector3(0.6f, 0.03f, 0.22f) * scale, body);
                        wings[w] = hinge;
                    }
                    NoShadowParts(bird.gameObject);
                    var mover = bird.gameObject.AddComponent<KidMover>();
                    mover.mode = KidMover.Mode.Circle; mover.wings = wings;
                    mover.centre = SubmissionKidRegionsBuilder.Offset(r) + new Vector3((float)random.NextDouble() * 20 - 10, 0, (float)random.NextDouble() * 20 - 5 + (r == 1 ? 20 : 0));
                    mover.radius = 12 + (float)random.NextDouble() * 18; mover.height = 9 + (float)random.NextDouble() * 8;
                    mover.speed = (4 + (float)random.NextDouble() * 3) * (random.Next(2) == 0 ? 1 : -1);
                    bird.position = mover.centre + new Vector3(mover.radius, mover.height, 0);
                }

            // Trash looks like trash: buzzing flies and a grey stain, both hidden with the item when it is picked up.
            var flyMaterial = Unlit("Fly", SubmissionKidUi.CircleSprite.texture, new Color(0.12f, 0.12f, 0.14f, 0.9f));
            var stain = Solid("TrashStain", new Color(0.42f, 0.4f, 0.37f));
            foreach (var item in flow.game.wasteItems)
            {
                if (item == null) continue;
                foreach (var name in new[] { "파리", "얼룩" }) { var o = item.transform.Find(name); if (o != null) Object.DestroyImmediate(o.gameObject); }
                var spot = Part(item.transform, PrimitiveType.Cylinder, new Vector3(0, 0.012f, 0), new Vector3(1.0f, 0.004f, 1.0f), stain); spot.name = "얼룩";
                spot.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var flies = new GameObject("파리", typeof(ParticleSystem)).GetComponent<ParticleSystem>();
                flies.transform.SetParent(item.transform, false); flies.transform.localPosition = new Vector3(0, 0.45f, 0);
                flies.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = flies.main; main.loop = true; main.playOnAwake = true; main.startLifetime = 1.4f; main.startSpeed = 0.25f; main.startSize = 0.05f; main.maxParticles = 12;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                var emission = flies.emission; emission.rateOverTime = 5;
                var shape = flies.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.25f;
                var buzz = flies.noise; buzz.enabled = true; buzz.strength = 0.9f; buzz.frequency = 2.5f; buzz.scrollSpeed = 1.5f;
                flies.GetComponent<ParticleSystemRenderer>().sharedMaterial = flyMaterial;
            }
        }
        // Rewards worn on the head: a striped party hat for 5 stars and a golden crown for all 9.
        private static void Rewards(AlphaFlow flow)
        {
            var player = flow.player.transform;
            foreach (var name in new[] { "보상 고깔모자", "보상 왕관" }) { var o = player.Find(name); if (o != null) Object.DestroyImmediate(o.gameObject); }
            var hat = new GameObject("보상 고깔모자", typeof(MeshFilter), typeof(MeshRenderer)).transform; hat.SetParent(player, false);
            hat.localScale = new Vector3(0.13f, 0.3f, 0.13f);
            hat.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/LowCone.asset");
            hat.GetComponent<MeshRenderer>().sharedMaterial = Solid("PartyHat", new Color(1f, 0.55f, 0.72f));
            Part(hat, PrimitiveType.Sphere, new Vector3(0, 1.02f, 0), new Vector3(0.6f, 0.26f, 0.6f), Solid("PartyHatPom", new Color(1f, 0.9f, 0.4f)));
            var band = Part(hat, PrimitiveType.Cylinder, new Vector3(0, 0.42f, 0), new Vector3(0.62f, 0.05f, 0.62f), Solid("PartyHatBand", new Color(0.55f, 0.8f, 1f)));
            var crown = new GameObject("보상 왕관").transform; crown.SetParent(player, false);
            var gold = Solid("CrownGold", new Color(1f, 0.8f, 0.25f));
            Part(crown, PrimitiveType.Cylinder, new Vector3(0, 0.03f, 0), new Vector3(0.24f, 0.03f, 0.24f), gold);
            for (int k = 0; k < 5; k++)
            {
                var at = Quaternion.Euler(0, k * 72, 0) * new Vector3(0, 0, 0.11f);
                Part(crown, PrimitiveType.Cube, at + new Vector3(0, 0.08f, 0), new Vector3(0.05f, 0.1f, 0.05f), gold).transform.localRotation = Quaternion.Euler(0, k * 72, 45);
                Part(crown, PrimitiveType.Sphere, at + new Vector3(0, 0.14f, 0), Vector3.one * 0.035f, Solid("CrownGem_" + k % 3, k % 3 == 0 ? new Color(1f, 0.4f, 0.55f) : k % 3 == 1 ? new Color(0.45f, 0.75f, 1f) : new Color(0.5f, 0.9f, 0.6f)));
            }
            hat.gameObject.SetActive(false); crown.gameObject.SetActive(false);
            var rewards = player.GetComponent<KidRewards>(); if (rewards == null) rewards = player.gameObject.AddComponent<KidRewards>();
            rewards.regions = flow.game.GetComponent<KidRegions>(); rewards.selection = player.GetComponent<CharacterSelection>();
            rewards.partyHat = hat.gameObject; rewards.crown = crown.gameObject;
        }
        private static void NoShadowParts(GameObject target) { foreach (var r in target.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
        internal static AudioClip Sound(string name) { return AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Sound/" + name + ".wav"); }
        private static KidBob Bob(Transform t) { var bob = t.GetComponent<KidBob>(); return bob != null ? bob : t.gameObject.AddComponent<KidBob>(); }
        private static void Regions(AlphaFlow flow)
        {
            var systems = flow.game.gameObject;
            var world = flow.game.transform.parent;
            var regions = systems.GetComponent<KidRegions>(); if (regions == null) regions = systems.AddComponent<KidRegions>();
            regions.flow = flow; regions.finale = systems.GetComponent<KidFinale>(); regions.voice = systems.GetComponent<KidVoice>();
            var helper = Object.FindFirstObjectByType<GuideNpc>().transform; regions.helper = helper;
            regions.spacing = SubmissionKidRegionsBuilder.Spacing;
            regions.spawns = new[] { flow.spawn, world.Find("지역 2 반짝 바닷가/도착 지점"), world.Find("지역 3 알록달록 시장/도착 지점") };
            regions.helperSpots = new Vector3[3]; regions.centres = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                regions.helperSpots[i] = new Vector3(helper.position.x, 0, helper.position.z) + SubmissionKidRegionsBuilder.Offset(i);
                regions.centres[i] = new Vector3(0, 0, -2) + SubmissionKidRegionsBuilder.Offset(i);
            }
            var audio = systems.GetComponent<SubmissionAudio>();
            regions.sound = systems.GetComponent<AudioSource>(); regions.clickClip = audio != null ? audio.clickClip : null; regions.lockedClip = Sound("sfx/wrong");
            var mood = systems.GetComponent<KidEnvironmentMood>(); if (mood != null) mood.regions = regions;
            var arrow = flow.player.GetComponent<KidGuideArrow>(); if (arrow != null) arrow.regionSpacing = SubmissionKidRegionsBuilder.Spacing;
        }
        // "에코시티를 구했어요!" ending: fireworks, confetti, rainbow and the timing that holds the results screen.
        private const float FinaleSeconds = 9;
        private static void Finale(AlphaFlow flow)
        {
            var systems = flow.game.gameObject;
            var world = flow.game.transform.parent;
            var finale = systems.GetComponent<KidFinale>(); if (finale == null) finale = systems.AddComponent<KidFinale>();
            finale.flow = flow; finale.voice = systems.GetComponent<KidVoice>(); finale.duration = FinaleSeconds; flow.finishDelay = FinaleSeconds;
            finale.parkCentre = new Vector3(0, 0, -2);
            finale.helper = Object.FindFirstObjectByType<GuideNpc>().transform;
            var old = world.Find("구출 효과"); if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("구출 효과").transform; root.SetParent(world, false);

            // Fireworks: bursts emitted from script at random spots above the park.
            var fireworks = new GameObject("불꽃놀이", typeof(ParticleSystem)).GetComponent<ParticleSystem>();
            fireworks.transform.SetParent(root, false);
            fireworks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = fireworks.main; main.playOnAwake = false; main.loop = false; main.maxParticles = 3000; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.7f); main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.6f); main.gravityModifier = 0.45f; main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var fireEmission = fireworks.emission; fireEmission.rateOverTime = 0;
            var fireShape = fireworks.shape; fireShape.shapeType = ParticleSystemShapeType.Sphere; fireShape.radius = 0.2f;
            var fireFade = fireworks.colorOverLifetime; fireFade.enabled = true;
            var fade = new Gradient(); fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.55f), new GradientAlphaKey(0, 1) });
            fireFade.color = fade;
            var shrink = fireworks.sizeOverLifetime; shrink.enabled = true; shrink.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0.3f));
            var star = Unlit("Sparkle", Painted("UI_Star", 128, StarPixel), Color.white);
            fireworks.GetComponent<ParticleSystemRenderer>().sharedMaterial = star;
            finale.fireworks = fireworks;

            // Confetti shower over the whole park.
            var confetti = new GameObject("색종이", typeof(ParticleSystem)).GetComponent<ParticleSystem>();
            confetti.transform.SetParent(root, false); confetti.transform.localPosition = new Vector3(0, 14, -2);
            confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var paper = confetti.main; paper.playOnAwake = false; paper.loop = true; paper.maxParticles = 2500; paper.simulationSpace = ParticleSystemSimulationSpace.World;
            paper.startLifetime = 6; paper.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f); paper.gravityModifier = 0.18f;
            paper.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.34f); paper.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var colours = new Gradient(); colours.mode = GradientMode.Fixed;
            colours.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.56f, 0.75f), 0.2f), new GradientColorKey(new Color(0.64f, 0.55f, 1f), 0.4f), new GradientColorKey(new Color(0.47f, 0.75f, 1f), 0.6f),
                new GradientColorKey(new Color(0.45f, 0.88f, 0.66f), 0.8f), new GradientColorKey(new Color(1f, 0.84f, 0.38f), 1f) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            paper.startColor = new ParticleSystem.MinMaxGradient(colours) { mode = ParticleSystemGradientMode.RandomColor };
            var rain = confetti.emission; rain.rateOverTime = 140;
            var area = confetti.shape; area.shapeType = ParticleSystemShapeType.Box; area.scale = new Vector3(40, 1, 34);
            var spin = confetti.rotationOverLifetime; spin.enabled = true; spin.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
            var sway = confetti.velocityOverLifetime; sway.enabled = true; sway.space = ParticleSystemSimulationSpace.World;
            sway.x = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f); sway.y = new ParticleSystem.MinMaxCurve(0f, 0f); sway.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
            confetti.GetComponent<ParticleSystemRenderer>().sharedMaterial = Unlit("Confetti", SubmissionKidUi.RoundSprite.texture, Color.white);
            finale.confetti = confetti;

            // Rainbow behind the bins, facing the camera while it shows.
            var rainbow = GameObject.CreatePrimitive(PrimitiveType.Quad); rainbow.name = "무지개"; rainbow.transform.SetParent(root, false);
            Object.DestroyImmediate(rainbow.GetComponent<Collider>());
            rainbow.transform.localPosition = new Vector3(0, 5, 30); rainbow.transform.localScale = new Vector3(56, 56, 1);
            rainbow.GetComponent<Renderer>().sharedMaterial = Unlit("Rainbow", SubmissionKidUi.RainbowSprite.texture, new Color(1, 1, 1, 0.85f));
            rainbow.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rainbow.SetActive(false);
            finale.rainbow = rainbow.GetComponent<Renderer>();
            Restoration(flow, finale, root);
        }
        // Pictures of a park coming back to life: smog that thins as items are cleared, flower patches that bloom,
        // a fountain that starts spraying, butterflies, a restoration ring for the ending and trees that bounce.
        private static void Restoration(AlphaFlow flow, KidFinale finale, Transform root)
        {
            var world = flow.game.transform.parent;
            var mood = flow.game.GetComponent<KidEnvironmentMood>(); if (mood == null) mood = flow.game.gameObject.AddComponent<KidEnvironmentMood>();
            mood.game = flow.game; mood.view = flow.player.viewCamera;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (light.type == LightType.Directional) { mood.sun = light; break; }
            mood.skyClean = flow.player.viewCamera.backgroundColor;
            finale.mood = mood;

            // Flower patches over the lawns (not on paths, plaza, pond or next to anything the player uses).
            var patches = FlowerPatches();
            var garden = new GameObject("꽃 피는 자리").transform; garden.SetParent(root, false);
            var random = new System.Random(20261023);
            var blocked = new List<Vector3>();
            foreach (var target in Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None)) blocked.Add(target.transform.position);
            blocked.Add(flow.spawn.position);
            var flowers = new List<Transform>();
            Physics.SyncTransforms();
            int[] perRegion = { 120, 70, 60 };
            for (int region = 0; region < 3; region++)
            {
                var offset = SubmissionKidRegionsBuilder.Offset(region);
                var placed = new List<Transform>();
                for (int tries = 0; tries < 5000 && placed.Count < perRegion[region]; tries++)
                {
                    var local = new Vector3((float)(random.NextDouble() * 44 - 22), 0, (float)(random.NextDouble() * 42 - 24));
                    var at = local + offset;
                    if ((region == 0 ? OnPath(local) : RegionPath(local)) || blocked.Exists(b => Vector3.Distance(new Vector3(b.x, 0, b.z), at) < 2.2f) ||
                        placed.Exists(f => Vector3.Distance(f.position, at) < 1.6f) || Solid(at)) continue;
                    var patch = new GameObject("꽃밭 " + (region + 1) + "-" + (placed.Count + 1), typeof(MeshFilter), typeof(MeshRenderer)).transform;
                    patch.SetParent(garden, false); patch.position = at; patch.rotation = Quaternion.Euler(0, (float)(random.NextDouble() * 360), 0);
                    patch.localScale = Vector3.one * (float)(1.7 + random.NextDouble() * 0.7);
                    int kind = random.Next(patches.Value.Length);
                    patch.GetComponent<MeshFilter>().sharedMesh = patches.Key;
                    var renderer = patch.GetComponent<MeshRenderer>();
                    renderer.sharedMaterials = new[] { patches.Value[0][0], patches.Value[kind][1], patches.Value[0][2] };
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    placed.Add(patch);
                }
                // Reveal order: scattered, so each cleared item blooms flowers all over its region.
                for (int i = placed.Count - 1; i > 0; i--) { int j = random.Next(i + 1); var swap = placed[i]; placed[i] = placed[j]; placed[j] = swap; }
                flowers.AddRange(placed);
            }
            mood.flowers = flowers.ToArray();

            // Fountain spray, switched on by the mood once the park is half clean.
            var fountain = new GameObject("분수 물", typeof(ParticleSystem)).GetComponent<ParticleSystem>();
            fountain.transform.SetParent(root, false); fountain.transform.position = new Vector3(0, 1.95f, -18); fountain.transform.rotation = Quaternion.Euler(-90, 0, 0);
            fountain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var spray = fountain.main; spray.playOnAwake = false; spray.loop = true; spray.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.2f);
            spray.startSpeed = new ParticleSystem.MinMaxCurve(3.2f, 4.2f); spray.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f); spray.gravityModifier = 1f;
            spray.startColor = new Color(0.75f, 0.9f, 1f, 0.9f); spray.maxParticles = 500; spray.simulationSpace = ParticleSystemSimulationSpace.World;
            var sprayRate = fountain.emission; sprayRate.rateOverTime = 90;
            var sprayShape = fountain.shape; sprayShape.shapeType = ParticleSystemShapeType.Cone; sprayShape.angle = 14; sprayShape.radius = 0.15f;
            fountain.GetComponent<ParticleSystemRenderer>().sharedMaterial = Unlit("WaterDrop", SubmissionKidUi.CircleSprite.texture, Color.white);
            mood.fountain = fountain;

            // Butterflies released over the park in the ending.
            var butterflies = new GameObject("나비", typeof(ParticleSystem)).GetComponent<ParticleSystem>();
            butterflies.transform.SetParent(root, false); butterflies.transform.localPosition = new Vector3(0, 2.5f, -2);
            butterflies.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var wing = butterflies.main; wing.playOnAwake = false; wing.loop = false; wing.startLifetime = 14; wing.maxParticles = 200;
            wing.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f); wing.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.75f); wing.simulationSpace = ParticleSystemSimulationSpace.World;
            var wingColours = new Gradient(); wingColours.mode = GradientMode.Fixed;
            wingColours.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.6f, 0.8f), 0.25f), new GradientColorKey(new Color(1f, 0.85f, 0.4f), 0.5f), new GradientColorKey(new Color(0.55f, 0.8f, 1f), 0.75f), new GradientColorKey(new Color(0.72f, 0.62f, 1f), 1f) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            wing.startColor = new ParticleSystem.MinMaxGradient(wingColours) { mode = ParticleSystemGradientMode.RandomColor };
            var wingRate = butterflies.emission; wingRate.rateOverTime = 0;
            var wingArea = butterflies.shape; wingArea.shapeType = ParticleSystemShapeType.Box; wingArea.scale = new Vector3(38, 3, 34);
            var flutter = butterflies.noise; flutter.enabled = true; flutter.strength = 1.4f; flutter.frequency = 0.6f; flutter.scrollSpeed = 0.5f;
            var flap = butterflies.sizeOverLifetime; flap.enabled = true; flap.separateAxes = true;
            var flapCurve = new AnimationCurve(); for (int k = 0; k <= 56; k++) flapCurve.AddKey(k / 56f, k % 2 == 0 ? 1f : 0.35f);
            flap.x = new ParticleSystem.MinMaxCurve(1, flapCurve); flap.y = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Constant(0, 1, 1));
            butterflies.GetComponent<ParticleSystemRenderer>().sharedMaterial = Unlit("Butterfly", SubmissionKidUi.ButterflySprite.texture, Color.white);
            finale.butterflies = butterflies; mood.butterflies = butterflies;

            // Twinkling stars over the saved city: small stars that pop in and out all over the last region.
            var twinkles = new GameObject("반짝임", typeof(ParticleSystem)).GetComponent<ParticleSystem>();
            twinkles.transform.SetParent(root, false); twinkles.transform.position = SubmissionKidRegionsBuilder.Offset(2) + new Vector3(0, 1.5f, -2);
            twinkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var glint = twinkles.main; glint.playOnAwake = false; glint.loop = true; glint.maxParticles = 1500; glint.simulationSpace = ParticleSystemSimulationSpace.World;
            glint.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f); glint.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.3f);
            glint.startSize = new ParticleSystem.MinMaxCurve(0.45f, 1.0f); glint.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var glintColours = new Gradient(); glintColours.mode = GradientMode.Fixed;
            glintColours.SetKeys(new[] { new GradientColorKey(Color.white, 0.4f), new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0.7f), new GradientColorKey(new Color(0.8f, 0.92f, 1f), 0.85f), new GradientColorKey(new Color(1f, 0.82f, 0.95f), 1f) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            glint.startColor = new ParticleSystem.MinMaxGradient(glintColours) { mode = ParticleSystemGradientMode.RandomColor };
            var glintRate = twinkles.emission; glintRate.rateOverTime = 240;
            var glintArea = twinkles.shape; glintArea.shapeType = ParticleSystemShapeType.Box; glintArea.scale = new Vector3(44, 5, 42);
            var pulse = twinkles.sizeOverLifetime; pulse.enabled = true;
            pulse.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.35f, 1), new Keyframe(0.5f, 0.55f), new Keyframe(0.7f, 1), new Keyframe(1, 0)));
            var turn = twinkles.rotationOverLifetime; turn.enabled = true; turn.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
            twinkles.GetComponent<ParticleSystemRenderer>().sharedMaterial = Unlit("Twinkle", Painted("UI_Star", 128, StarPixel), Color.white);
            mood.twinkles = twinkles; mood.ambientBoost = 1.12f; mood.sunIntensitySparkle = 1.2f;

            // Restoration ring lying on the ground.
            var ring = GameObject.CreatePrimitive(PrimitiveType.Quad); ring.name = "되살아나는 물결"; ring.transform.SetParent(root, false);
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            ring.transform.rotation = Quaternion.Euler(90, 0, 0); ring.transform.localScale = Vector3.one;
            ring.GetComponent<Renderer>().sharedMaterial = Unlit("RestoreRing", SubmissionKidUi.RingSprite.texture, new Color(1f, 0.93f, 0.55f, 1f));
            ring.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.SetActive(false);
            finale.ring = ring.GetComponent<Renderer>();

            // Trees and bushes of the park bounce when the ring reaches them.
            var bounce = new List<Transform>();
            foreach (var part in world.GetComponentsInChildren<Transform>(true))
                if (part.name.StartsWith("tree-type") || part.name.StartsWith("bush-type")) bounce.Add(part);
            finale.bounce = bounce.ToArray();
        }
        // Small UV sphere (rings > 1) or capped cylinder (cylinder = true, unit radius 1 and height 2 like Unity's primitives).
        private static Mesh LowShape(int sides, int rings, bool cylinder)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            if (cylinder)
            {
                for (int i = 0; i < sides; i++) { float a = i * Mathf.PI * 2 / sides; vertices.Add(new Vector3(Mathf.Cos(a), -1, Mathf.Sin(a))); vertices.Add(new Vector3(Mathf.Cos(a), 1, Mathf.Sin(a))); }
                for (int i = 0; i < sides; i++)
                {
                    int a0 = i * 2, a1 = i * 2 + 1, b0 = (i + 1) % sides * 2, b1 = b0 + 1;
                    triangles.AddRange(new[] { a0, a1, b1, a0, b1, b0 });
                }
            }
            else
            {
                for (int r = 0; r <= rings; r++)
                    for (int s = 0; s <= sides; s++)
                    {
                        float lat = Mathf.PI * r / rings, lon = Mathf.PI * 2 * s / sides;
                        vertices.Add(new Vector3(Mathf.Sin(lat) * Mathf.Cos(lon), Mathf.Cos(lat), Mathf.Sin(lat) * Mathf.Sin(lon)));
                    }
                for (int r = 0; r < rings; r++)
                    for (int s = 0; s < sides; s++)
                    {
                        int a = r * (sides + 1) + s, b = a + sides + 1;
                        triangles.AddRange(new[] { a, a + 1, b + 1, a, b + 1, b });
                    }
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
            return mesh;
        }
        // Beach and market: keep the walks, the bin row and the station column free.
        private static bool RegionPath(Vector3 p)
        {
            if (Mathf.Abs(p.x) < 2.6f && p.z < 12.5f) return true;
            if (Mathf.Abs(p.z - 7) < 2.6f && Mathf.Abs(p.x) < 13) return true;
            if (p.x > -11.5f && p.x < 0 && Mathf.Abs(p.z) < 6) return true;
            return false;
        }
        // Something solid (stall, tree, bench...) already stands here.
        private static bool Solid(Vector3 at)
        {
            foreach (var hit in Physics.OverlapSphere(at + Vector3.up * 0.6f, 0.7f, ~0, QueryTriggerInteraction.Ignore))
                if (hit.gameObject.name != "Ground") return true;
            return false;
        }
        private static bool OnPath(Vector3 p)
        {
            if (Mathf.Abs(p.x) < 2.6f && p.z > -26 && p.z < 12.5f) return true;                 // central and south walk
            if (Mathf.Abs(p.z - 7) < 2.2f && Mathf.Abs(p.x) < 13) return true;                    // bin walk
            if (Mathf.Abs(p.z) < 1.6f && p.x > -10.5f && p.x < 0) return true;                    // preparation walk
            if (Mathf.Abs(p.z) < 2f && p.x > 9.5f) return true;                                   // east walk
            if (Mathf.Abs(p.z + 6) < 2f && p.x < -10.5f) return true;                             // west walk
            if (Vector3.Distance(p, new Vector3(0, 0, -18)) < 6.2f) return true;                  // plaza
            if (Mathf.Pow((p.x - 17) / 5.2f, 2) + Mathf.Pow((p.z - 11) / 3.8f, 2) < 1) return true; // pond
            if (p.x < -14.5f && p.z > -3.5f && p.z < 12.5f) return true;                         // flower garden beds
            if (p.z > 11.4f && p.z < 13.4f && Mathf.Abs(p.x) < 9) return true;                    // restored gardens
            if (p.x > 15.5f && p.z > -6.5f && p.z < 4.5f) return true;                            // picnic tables
            return false;
        }
        // One shared mesh for a patch of six little flowers (stems, petals, centres as three sub-meshes), and the
        // material sets for each petal colour. Instanced, so a hundred patches stay cheap.
        private static KeyValuePair<Mesh, Material[][]> FlowerPatches()
        {
            string path = Root + "/FlowerPatch.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                var scene = new GameObject("flower patch");
                try
                {
                    var stems = new List<CombineInstance>(); var petals = new List<CombineInstance>(); var centres = new List<CombineInstance>();
                    var random = new System.Random(7);
                    // Low-poly shapes: a hundred patches of six flowers must stay light.
                    var cylinder = LowShape(6, 1, true); var sphere = LowShape(7, 4, false);
                    for (int f = 0; f < 6; f++)
                    {
                        float a = f * 60 + (float)random.NextDouble() * 30, r = f == 0 ? 0 : 0.22f + (float)random.NextDouble() * 0.12f, h = 0.2f + (float)random.NextDouble() * 0.12f;
                        var at = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * r, 0, Mathf.Sin(a * Mathf.Deg2Rad) * r);
                        stems.Add(new CombineInstance { mesh = cylinder, transform = Matrix4x4.TRS(at + Vector3.up * h / 2, Quaternion.identity, new Vector3(0.03f, h / 2, 0.03f)) });
                        stems.Add(new CombineInstance { mesh = sphere, transform = Matrix4x4.TRS(at + new Vector3(0.05f, h * 0.4f, 0), Quaternion.Euler(0, 0, 40), new Vector3(0.09f, 0.03f, 0.05f)) });
                        for (int p = 0; p < 5; p++)
                        {
                            var dir = Quaternion.Euler(0, p * 72 + a, 0) * Vector3.forward;
                            petals.Add(new CombineInstance { mesh = sphere, transform = Matrix4x4.TRS(at + Vector3.up * h + dir * 0.055f, Quaternion.LookRotation(dir), new Vector3(0.07f, 0.025f, 0.09f)) });
                        }
                        centres.Add(new CombineInstance { mesh = sphere, transform = Matrix4x4.TRS(at + Vector3.up * (h + 0.012f), Quaternion.identity, Vector3.one * 0.05f) });
                    }
                    var parts = new[] { stems, petals, centres }; var subs = new CombineInstance[3];
                    for (int i = 0; i < 3; i++) { var m = new Mesh(); m.CombineMeshes(parts[i].ToArray(), true, true); subs[i] = new CombineInstance { mesh = m, transform = Matrix4x4.identity }; }
                    mesh = new Mesh { name = "FlowerPatch" }; mesh.CombineMeshes(subs, false, true); mesh.RecalculateBounds();
                    AssetDatabase.CreateAsset(mesh, path);
                }
                finally { Object.DestroyImmediate(scene); }
            }
            Color[] petalColours = { new Color(1f, 0.62f, 0.78f), new Color(1f, 0.85f, 0.4f), new Color(0.72f, 0.62f, 1f), new Color(0.55f, 0.8f, 1f), new Color(1f, 0.7f, 0.55f), Color.white };
            var stem = Solid("FlowerStem", new Color(0.35f, 0.7f, 0.32f)); var centre = Solid("FlowerCentre", new Color(1f, 0.82f, 0.25f));
            stem.enableInstancing = true; centre.enableInstancing = true;
            var sets = new Material[petalColours.Length][];
            for (int i = 0; i < petalColours.Length; i++)
            {
                var petal = Solid("FlowerPetal_" + i, petalColours[i]); petal.enableInstancing = true;
                sets[i] = new[] { stem, petal, centre };
            }
            return new KeyValuePair<Mesh, Material[][]>(mesh, sets);
        }
        private static int Cues(AlphaFlow flow, Transform[] markers)
        {
            var cues = flow.game.GetComponent<KidWorldCues>();
            if (cues == null) cues = flow.game.gameObject.AddComponent<KidWorldCues>();
            cues.game = flow.game; cues.interactor = flow.interactor; cues.items = flow.game.wasteItems; cues.markers = markers;
            var old = flow.game.transform.parent.Find("E 표시"); if (old != null) Object.DestroyImmediate(old.gameObject);
            var bubble = new GameObject("E 표시").transform; bubble.SetParent(flow.game.transform.parent, false);
            var back = GameObject.CreatePrimitive(PrimitiveType.Quad); back.name = "동그라미"; back.transform.SetParent(bubble, false);
            Object.DestroyImmediate(back.GetComponent<Collider>()); back.transform.localScale = Vector3.one * 0.62f;
            back.GetComponent<Renderer>().sharedMaterial = Unlit("Bubble", SubmissionKidUi.CircleSprite.texture, Sun);
            var letter = Label(bubble, "E", new Vector3(0, 0, -0.01f), 0.05f, Ink, false); letter.name = "글자";
            bubble.gameObject.AddComponent<AlphaSign>().viewCamera = viewCamera;
            cues.targetBubble = bubble; bubble.gameObject.SetActive(false);
            return markers.Length;
        }

        // ---------- Model helpers ----------
        private static Transform PrepareRoot(Interactable target, float groundY)
        {
            var root = target.transform;
            var old = root.Find(ModelName); if (old != null) Object.DestroyImmediate(old.gameObject);
            var board = root.Find("표지판"); if (board != null) Object.DestroyImmediate(board.gameObject);
            var mark = root.Find("반짝 표시"); if (mark != null) Object.DestroyImmediate(mark.gameObject);
            root.localScale = Vector3.one; root.rotation = Quaternion.identity;
            root.position = new Vector3(root.position.x, groundY, root.position.z);
            var renderer = root.GetComponent<Renderer>(); if (renderer != null) renderer.enabled = false;
            return root;
        }
        private static void FinishModel(Interactable target, GameObject model, float minSize)
        {
            var root = target.transform;
            model.name = ModelName;
            if (model.transform.parent != root) model.transform.SetParent(root, true);
            var bounds = RendererBounds(model);
            model.transform.position += new Vector3(root.position.x - bounds.center.x, root.position.y - bounds.min.y, root.position.z - bounds.center.z);
            bounds = RendererBounds(model);
            foreach (var collider in root.GetComponents<Collider>()) Object.DestroyImmediate(collider);
            var box = root.gameObject.AddComponent<BoxCollider>();
            box.center = bounds.center - root.position;
            box.size = Vector3.Max(bounds.size, new Vector3(minSize, minSize, minSize));
            box.center = new Vector3(box.center.x, box.size.y / 2, box.center.z);
            Renderer largest = null; float volume = -1;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            { float v = renderer.bounds.size.x * renderer.bounds.size.y * renderer.bounds.size.z; if (v > volume) { volume = v; largest = renderer; } }
            target.visualRenderer = largest;
        }
        private static float ModelTop(GameObject model, Transform root) { return RendererBounds(model).max.y - root.position.y; }
        internal static GameObject Spawn(string path, Transform parent, Vector3 localPosition, float yaw, float scale = 1, bool keepColliders = false)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new FileNotFoundException("Missing asset: " + path);
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            if (parent != null) obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition; obj.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            obj.transform.localScale = obj.transform.localScale * scale;
            // Demo scripts (lights, kid controller) and loose physics are not part of this scene.
            foreach (var behaviour in obj.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(behaviour);
            foreach (var body in obj.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(body);
            foreach (var controller in obj.GetComponentsInChildren<CharacterController>(true)) Object.DestroyImmediate(controller);
            if (!keepColliders) foreach (var collider in obj.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = Compatible(materials[i]);
                renderer.sharedMaterials = materials;
            }
            foreach (var light in obj.GetComponentsInChildren<Light>(true)) light.gameObject.SetActive(false);
            return obj;
        }
        internal static void AddBoxCollider(GameObject obj)
        {
            var bounds = RendererBounds(obj);
            var box = obj.AddComponent<BoxCollider>();
            box.center = obj.transform.InverseTransformPoint(bounds.center);
            var s = obj.transform.lossyScale;
            box.size = Quaternion.Inverse(obj.transform.rotation) * bounds.size;
            box.size = new Vector3(Mathf.Abs(box.size.x / s.x), Mathf.Abs(box.size.y / s.y), Mathf.Abs(box.size.z / s.z));
        }
        internal static Bounds RendererBounds(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.zero);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
        private static GameObject Cup(Transform parent)
        {
            var cup = new GameObject(ModelName); cup.transform.SetParent(parent, false);
            Part(cup.transform, PrimitiveType.Cylinder, new Vector3(0, 0.25f, 0), new Vector3(0.34f, 0.25f, 0.34f), Fade("Cup", new Color(0.92f, 0.96f, 1f, 0.55f)));
            Part(cup.transform, PrimitiveType.Cylinder, new Vector3(0, 0.15f, 0), new Vector3(0.3f, 0.14f, 0.3f), Solid("Drink", new Color(0.55f, 0.32f, 0.2f)));
            Part(cup.transform, PrimitiveType.Sphere, new Vector3(0, 0.5f, 0), new Vector3(0.36f, 0.09f, 0.36f), Solid("Lid", new Color(0.97f, 0.97f, 0.97f)));
            var straw = Part(cup.transform, PrimitiveType.Cylinder, new Vector3(0.05f, 0.62f, 0), new Vector3(0.035f, 0.2f, 0.035f), Solid("Straw", new Color(0.92f, 0.25f, 0.3f)));
            straw.transform.localRotation = Quaternion.Euler(0, 0, -12);
            cup.transform.localScale = Vector3.one * 1.35f;
            return cup;
        }
        internal static GameObject Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(type); part.transform.SetParent(parent, false);
            part.transform.localPosition = position; part.transform.localScale = scale;
            Object.DestroyImmediate(part.GetComponent<Collider>()); part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

        // ---------- Signs ----------
        private static void Board(Transform root, Texture2D icon, string text, Color color, float height)
        {
            var board = new GameObject("표지판").transform; board.SetParent(root, false);
            board.localPosition = new Vector3(0, height, 0);
            var back = GameObject.CreatePrimitive(PrimitiveType.Quad); back.name = "판"; back.transform.SetParent(board, false);
            Object.DestroyImmediate(back.GetComponent<Collider>());
            back.transform.localScale = new Vector3(1.2f, 1.55f, 1); back.transform.localPosition = new Vector3(0, -0.12f, 0.01f);
            back.GetComponent<Renderer>().sharedMaterial = Unlit("Board_" + ColorUtility.ToHtmlStringRGB(color), SubmissionKidUi.RoundSprite.texture, color);
            back.GetComponent<Renderer>().sortingOrder = 0;
            var face = GameObject.CreatePrimitive(PrimitiveType.Quad); face.name = "그림"; face.transform.SetParent(board, false);
            Object.DestroyImmediate(face.GetComponent<Collider>());
            face.transform.localScale = new Vector3(1.02f, 1.02f, 1); face.transform.localPosition = new Vector3(0, 0.1f, 0);
            face.GetComponent<Renderer>().sharedMaterial = Unlit("Icon_" + icon.name, icon, Color.white);
            // Same queue for all three layers, so draw order is fixed explicitly: board, picture, label.
            face.GetComponent<Renderer>().sortingOrder = 1;
            var label = Label(board, text, new Vector3(0, -0.66f, -0.01f), 0.04f, Color.white, false, SubmissionKidUi.SoftMaterial);
            label.GetComponent<MeshRenderer>().sortingOrder = 2;
            var sign = board.gameObject.AddComponent<AlphaSign>(); sign.viewCamera = viewCamera;
            // Boards fill the third-person view when the camera passes right by them.
            sign.hideCloserThan = 4.2f;
        }
        // World text is TextMeshPro SDF: sharp at any distance, unlike bitmap TextMesh glyphs.
        // size is the old TextMesh character size (at font size 96); TextMeshPro font size 96 x size gives the same height.
        private static GameObject Label(Transform parent, string text, Vector3 localPosition, float size, Color color, bool faceCamera, Material material = null)
        {
            var obj = new GameObject("글씨"); obj.transform.SetParent(parent, false);
            var mesh = obj.AddComponent<TextMeshPro>();
            mesh.font = SubmissionKidUi.Font; mesh.fontSharedMaterial = material != null ? material : SubmissionKidUi.PlainMaterial;
            mesh.text = text; mesh.fontSize = size * 96; mesh.color = color; mesh.alignment = TextAlignmentOptions.Center;
            mesh.textWrappingMode = TextWrappingModes.NoWrap; mesh.rectTransform.sizeDelta = new Vector2(4, 1);
            obj.transform.localPosition = localPosition;
            if (faceCamera) obj.AddComponent<AlphaSign>().viewCamera = viewCamera;
            return obj;
        }

        // ---------- Materials ----------
        private static Material Compatible(Material source)
        {
            if (source == null) return null;
            if (source.shader != null && source.shader.isSupported && source.shader.name != "Hidden/InternalErrorShader") return source;
            Material result;
            if (converted.TryGetValue(source, out result)) return result;
            string path = Root + "/Materials/" + source.name.Replace('/', '_') + "_BuiltIn.mat";
            result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null) { result = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(result, path); }
            // A missing shader hides its properties from the Material API, so read the values saved in the asset.
            var saved = new SerializedObject(source).FindProperty("m_SavedProperties");
            result.renderQueue = -1; result.shader = Shader.Find("Standard");
            result.SetTexture("_MainTex", SavedTexture(saved, "_BaseMap", "_MainTex", "_Texture2D", "_BaseColorMap"));
            Color color = SavedColor(saved, "_BaseColor", SavedColor(saved, "_Color", Color.white));
            result.SetColor("_Color", color);
            Texture normal = SavedTexture(saved, "_BumpMap", "_NormalMap");
            result.SetTexture("_BumpMap", normal);
            if (normal != null) result.EnableKeyword("_NORMALMAP"); else result.DisableKeyword("_NORMALMAP");
            result.SetFloat("_Glossiness", Mathf.Min(0.6f, SavedFloat(saved, "_Smoothness", 0.3f)));
            result.SetFloat("_Metallic", SavedFloat(saved, "_Metallic", 0));
            if (SavedFloat(saved, "_Surface", 0) > 0.5f) MakeFade(result, Mathf.Max(0.45f, color.a));
            EditorUtility.SetDirty(result);
            converted[source] = result;
            return result;
        }
        private static Texture SavedTexture(SerializedProperty saved, params string[] names)
        {
            var textures = saved.FindPropertyRelative("m_TexEnvs");
            foreach (var name in names)
                for (int i = 0; i < textures.arraySize; i++)
                {
                    var entry = textures.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("first").stringValue != name) continue;
                    var texture = entry.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
                    if (texture != null) return texture;
                }
            return null;
        }
        private static Color SavedColor(SerializedProperty saved, string name, Color fallback)
        {
            var colors = saved.FindPropertyRelative("m_Colors");
            for (int i = 0; i < colors.arraySize; i++)
            {
                var entry = colors.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("first").stringValue == name) return entry.FindPropertyRelative("second").colorValue;
            }
            return fallback;
        }
        private static float SavedFloat(SerializedProperty saved, string name, float fallback)
        {
            var floats = saved.FindPropertyRelative("m_Floats");
            for (int i = 0; i < floats.arraySize; i++)
            {
                var entry = floats.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("first").stringValue == name) return entry.FindPropertyRelative("second").floatValue;
            }
            return fallback;
        }
        internal static Material Solid(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, path); }
            material.color = color; material.SetFloat("_Glossiness", 0.15f);
            EditorUtility.SetDirty(material);
            return material;
        }
        private static Material Fade(string name, Color color)
        {
            var material = Solid(name, color); MakeFade(material, color.a); material.SetFloat("_Glossiness", 0.7f); return material;
        }
        private static void MakeFade(Material material, float alpha)
        {
            material.SetFloat("_Mode", 2); material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_ALPHABLEND_ON"); material.renderQueue = 3000;
            var c = material.color; c.a = alpha; material.color = c;
        }
        internal static Material Unlit(string name, Texture texture, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            // Sprites/Default is unlit, keeps alpha and, unlike Unlit/Transparent, applies the tint colour.
            if (material == null) { material = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(material, path); }
            material.shader = Shader.Find("Sprites/Default"); material.mainTexture = texture; material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }
        private static Material TintedBin(BinLook look)
        {
            string path = Root + "/Materials/Bin_" + look.category + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Bin).GetComponentInChildren<Renderer>().sharedMaterial;
            if (material == null) { material = new Material(source); AssetDatabase.CreateAsset(material, path); }
            // Flat category colour (game-only colours); the pack's normal map keeps the moulded detail.
            material.SetTexture("_MainTex", null); material.color = look.color;
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---------- Pictures ----------
        /// <summary>Draws every 2D picture again at SubmissionKidUi.Detail (UI shapes, item/bin/step/helper icons, title picture,
        /// map stickers, portraits and title cut-outs) without rebuilding any scene. Files keep their paths and GUIDs, so every
        /// scene and material that uses them picks up the sharper version.</summary>
        [MenuItem("EcoCity/Submission/Redraw Sharp Pictures (No Play Mode)")]
        public static void RedrawPictures()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SubmissionKidUi.Prepare();
            EditorSceneManager.OpenScene(SubmissionSceneBuilder.ScenePath);
            var flow = Object.FindFirstObjectByType<AlphaFlow>();
            viewCamera = flow.player.viewCamera;
            foreach (var item in flow.game.wasteItems)
            {
                var model = item.transform.Find(ModelName);
                if (model != null) SubmissionKidUi.Picture(Icon(model.gameObject, "Item_" + item.instanceId, new Color(0.93f, 0.97f, 1f)));
            }
            foreach (var look in Bins)
            {
                var iconSource = Spawn(look.icon, null, Vector3.zero, 0);
                iconSource.transform.rotation = Quaternion.Euler(look.iconEuler);
                try { SubmissionKidUi.Picture(Icon(iconSource, "Bin_" + look.category, Color.white)); }
                finally { Object.DestroyImmediate(iconSource); }
            }
            foreach (var step in new[] { Preparation.Empty, Preparation.Separate, Preparation.Wash }) SubmissionKidUi.Picture(StepIcon(step));
            var npc = Object.FindFirstObjectByType<GuideNpc>().transform.Find(ModelName).gameObject;
            SubmissionKidUi.Picture(Icon(npc, "Helper", new Color(1f, 0.93f, 0.7f), true));
            SubmissionKidUi.Picture(TitleBackground(), false);
            for (int i = 0; i < 3; i++) SubmissionKidGameUi.RegionPicture(flow, i, out _, out _);
            SubmissionPlayerUpgrade.RedrawPortraits();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Player/Common/DEMO/Animator/StarterAssetsThirdPerson.controller");
            for (int i = 0; i < 2; i++) SubmissionCharacterBuilder.CutOut(i, controller);
            AssetDatabase.SaveAssets();
            Debug.Log("EcoCity: pictures redrawn at " + SubmissionKidUi.Detail + "x detail. No Play Mode.");
        }
        private static Texture2D Icon(GameObject source, string name, Color background, bool pose = false)
        {
            var preview = new PreviewRenderUtility();
            try
            {
                var clone = Object.Instantiate(source);
                clone.transform.position = Vector3.zero;
                preview.AddSingleGO(clone);
                // Characters: evaluate the idle clip so the portrait is not a T-pose.
                var animator = pose ? clone.GetComponentInChildren<Animator>() : null;
                AnimationClip idle = null;
                if (animator != null && animator.runtimeAnimatorController != null)
                    foreach (var clip in animator.runtimeAnimatorController.animationClips) if (clip.name.ToLower().Contains("idle")) { idle = clip; break; }
                if (idle != null)
                {
                    // Humanoid clips sample through the Animator's avatar; face the portrait camera.
                    clone.transform.rotation = Quaternion.identity;
                    idle.SampleAnimation(animator.gameObject, 0.3f);
                }
                var bounds = RendererBounds(clone);
                float size = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
                var camera = preview.camera;
                camera.fieldOfView = 25; camera.nearClipPlane = 0.01f; camera.farClipPlane = size * 30 + 5;
                camera.transform.position = bounds.center + new Vector3(0.35f, 0.45f, 1f).normalized * size * 5.2f;
                camera.transform.LookAt(bounds.center);
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = background;
                preview.lights[0].intensity = 1.15f; preview.lights[0].transform.rotation = Quaternion.Euler(40, 150, 0);
                preview.lights[1].intensity = 0.55f; preview.ambientColor = new Color(0.55f, 0.55f, 0.55f);
                preview.BeginStaticPreview(new Rect(0, 0, 256 * SubmissionKidUi.Detail, 256 * SubmissionKidUi.Detail));
                camera.Render();
                return SavePicture(preview.EndStaticPreview(), "Icons/" + name);
            }
            finally { preview.Cleanup(); }
        }
        private static Texture2D StepIcon(Preparation step)
        {
            // A small scene built from the same cup: tipped with drops (empty), lid and straw lifted (separate), drops from above (wash).
            var scene = new GameObject("icon");
            try
            {
                var cup = Cup(scene.transform);
                var water = Solid("Water", new Color(0.3f, 0.65f, 1f));
                if (step == Preparation.Empty)
                {
                    cup.transform.localRotation = Quaternion.Euler(0, 0, 115);
                    for (int i = 0; i < 3; i++) Part(scene.transform, PrimitiveType.Sphere, new Vector3(-0.55f - i * 0.08f, 0.1f - i * 0.22f, 0), Vector3.one * 0.09f, Solid("Drink", new Color(0.55f, 0.32f, 0.2f)));
                }
                else if (step == Preparation.Separate)
                {
                    cup.transform.GetChild(2).localPosition += new Vector3(0, 0.25f, 0);
                    cup.transform.GetChild(3).localPosition += new Vector3(0.18f, 0.42f, 0);
                }
                else for (int i = 0; i < 5; i++) Part(scene.transform, PrimitiveType.Sphere, new Vector3(-0.25f + i * 0.12f, 1.15f + (i % 2) * 0.12f, 0), new Vector3(0.07f, 0.11f, 0.07f), water);
                return Icon(scene, "Step_" + step, Color.white);
            }
            finally { Object.DestroyImmediate(scene); }
        }
        private static readonly List<Renderer> hidden = new List<Renderer>();
        private static Texture2D TitleBackground()
        {
            var obj = new GameObject("title camera"); obj.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var camera = obj.AddComponent<Camera>();
                camera.CopyFrom(viewCamera); camera.fieldOfView = 52;
                obj.transform.position = new Vector3(-15, 8.5f, -22); obj.transform.LookAt(new Vector3(2, 0.5f, 5));
                // A clean postcard: no characters, signs, markers or bubbles in the title picture.
                hidden.Clear();
                foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    bool character = renderer.GetComponentInParent<PrototypePlayer>() != null || renderer.GetComponentInParent<GuideNpc>() != null;
                    bool sign = renderer.GetComponent<TextMesh>() != null || renderer.GetComponent<TMP_Text>() != null || renderer.GetComponentInParent<AlphaSign>() != null;
                    if (renderer.enabled && (character || sign)) { renderer.enabled = false; hidden.Add(renderer); }
                }
                // 4K: the title slowly pans across a slightly zoomed-in crop, which must not look soft on big screens.
                const int width = 3840, height = 2160;
                var target = new RenderTexture(width, height, 24) { antiAliasing = 8 }; camera.targetTexture = target; camera.Render();
                var previous = RenderTexture.active; RenderTexture.active = target;
                var picture = new Texture2D(width, height, TextureFormat.RGB24, false); picture.ReadPixels(new Rect(0, 0, width, height), 0, 0); picture.Apply();
                RenderTexture.active = previous; camera.targetTexture = null; Object.DestroyImmediate(target);
                return SavePicture(picture, "TitleBackground");
            }
            finally { foreach (var renderer in hidden) if (renderer != null) renderer.enabled = true; Object.DestroyImmediate(obj); }
        }
        private static Texture2D SavePicture(Texture2D picture, string name)
        {
            string path = Root + "/" + name + ".png";
            File.WriteAllBytes(path, picture.EncodeToPNG()); Object.DestroyImmediate(picture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            // Full size and uncompressed (wide pictures were squeezed to power-of-two sizes before), mipmapped for small views.
            importer.mipmapEnabled = true; importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter; importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 4096; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 4; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        // ---------- UI ----------
        private static void Ui(AlphaFlow flow, Dictionary<string, Texture2D> itemIcons, Texture2D titlePicture)
        {
            var npc = Object.FindFirstObjectByType<GuideNpc>().transform.Find(ModelName).gameObject;
            var helperFace = Icon(npc, "Helper", new Color(1f, 0.93f, 0.7f), true);
            var binColors = new Color[7];
            for (int i = 0; i < binColors.Length; i++) binColors[i] = BinColor((WasteCategory)i);
            SubmissionKidGameUi.Build(flow, itemIcons, helperFace, titlePicture, binColors);
        }
        private static Texture2D Painted(string name, int size, System.Func<float, float, Color> pixel)
        {
            // 4x4 supersampling keeps the hand-drawn shapes smooth.
            string path = Root + "/" + name + ".png";
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Color sum = Color.clear;
                    for (int sy = 0; sy < 4; sy++) for (int sx = 0; sx < 4; sx++)
                        { var p = pixel((x + (sx + 0.5f) / 4) / size * 2 - 1, (y + (sy + 0.5f) / 4) / size * 2 - 1); sum += new Color(p.r * p.a, p.g * p.a, p.b * p.a, p.a); }
                    sum /= 16; texture.SetPixel(x, y, sum.a > 0 ? new Color(sum.r / sum.a, sum.g / sum.a, sum.b / sum.a, sum.a) : Color.clear);
                }
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static Color StarPixel(float x, float y)
        {
            // White five-point star (tinted by the Image colour); the radius limit eases between tips and valleys for chubby points.
            float angle = Mathf.Atan2(x, y), radius = Mathf.Sqrt(x * x + y * y);
            float t = Mathf.Abs(Mathf.Repeat(angle, Mathf.PI * 2 / 5) - Mathf.PI / 5) / (Mathf.PI / 5);
            return radius <= Mathf.Lerp(0.95f, 0.47f, Mathf.SmoothStep(0, 1, 1 - t)) ? Color.white : Color.clear;
        }
    }
}
#endif
