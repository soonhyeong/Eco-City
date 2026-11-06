#if UNITY_EDITOR
using System.Collections.Generic;
using EcoCity.Core;
using UnityEditor;
using UnityEngine;
using static EcoCity.Editor.SubmissionKidMakeover;

namespace EcoCity.Editor
{
    // Regions 2 and 3 of Eco City, built far east of the park (one stage each): "반짝 바닷가" (empty and wash) and
    // "알록달록 시장" (separate first). Each region gets its own ground, walls, seven bins and three stations (copies of the park's),
    // decorations, an arrival point and a spot for the helper. New items for all three regions are created here;
    // Items() then gives every item its model. Safe to run again: the region roots are rebuilt.
    public static class SubmissionKidRegionsBuilder
    {
        public const float Spacing = 220;
        private const float HalfWidth = 24, North = 20, South = -26;
        private const string Toon = "Assets/Loading Games/Toon City Pack/Prefabs/";
        private const string Hexxx = "Assets/HEXXX/Hyper Casual Urban Asset Pack/Prefab/";
        public static readonly string[] Names = { "새싹 공원", "반짝 바닷가", "알록달록 시장" };
        // Items added for the three regions; the park's six originals stay where the alpha scene put them.
        private static readonly string[][] NewItems = { new[] { "glass-01", "food-01" }, new[] { "can-02", "glass-02" }, new[] { "pet-02", "box-02" } };

        public static void Build(AlphaFlow flow, Transform world)
        {
            foreach (var name in new[] { "지역 1 새 쓰레기", "지역 2 반짝 바닷가", "지역 3 알록달록 시장", SceneryNames[0], SceneryNames[1], SceneryNames[2] })
            { var old = world.Find(name); if (old != null) Object.DestroyImmediate(old.gameObject); }
            var game = flow.game;
            // Keep the park's original items (references to items of the removed roots are gone now).
            var items = new List<WasteItem>();
            foreach (var item in game.wasteItems) if (item != null) items.Add(item);
            var template = items.Find(i => i.instanceId == "composite-01");

            var bins = Object.FindObjectsByType<DisposalBin>(FindObjectsSortMode.None);
            var stations = Object.FindObjectsByType<PreparationStation>(FindObjectsSortMode.None);
            var parkExtras = new GameObject("지역 1 새 쓰레기").transform; parkExtras.SetParent(world, false);
            foreach (var id in NewItems[0]) items.Add(NewItem(id, parkExtras, template));

            var beach = Region(world, "지역 2 반짝 바닷가", 1, new Color(0.97f, 0.89f, 0.68f), bins, stations);
            Beach(beach);
            foreach (var id in NewItems[1]) items.Add(NewItem(id, beach, template));
            var market = Region(world, "지역 3 알록달록 시장", 2, new Color(0.86f, 0.82f, 0.8f), bins, stations);
            Market(market);
            foreach (var id in NewItems[2]) items.Add(NewItem(id, market, template));
            Scenery(world);

            game.wasteItems = items.ToArray();
            EditorUtility.SetDirty(game);
        }
        public static Vector3 Offset(int region) { return new Vector3(Spacing * region, 0, 0); }

        private static WasteItem NewItem(string id, Transform parent, WasteItem template)
        {
            var obj = new GameObject("Waste-" + id, typeof(BoxCollider));
            obj.transform.SetParent(parent, true);
            var item = obj.AddComponent<WasteItem>(); item.instanceId = id; item.definition = template.definition;
            return item;
        }
        // Ground, walls, fence, outer land, bins and stations copied from the park, arrival point.
        private static Transform Region(Transform world, string name, int index, Color groundColour, DisposalBin[] bins, PreparationStation[] stations)
        {
            var offset = Offset(index);
            var root = new GameObject(name).transform; root.SetParent(world, false); root.position = offset;
            float depth = North - South, middle = (North + South) / 2;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "Ground"; ground.transform.SetParent(root, false);
            ground.transform.localPosition = new Vector3(0, -0.3f, middle); ground.transform.localScale = new Vector3(HalfWidth * 2, 0.5f, depth);
            ground.GetComponent<Renderer>().sharedMaterial = Solid("Ground_" + index, groundColour);
            var outer = GameObject.CreatePrimitive(PrimitiveType.Plane); outer.name = "바깥 땅"; outer.transform.SetParent(root, false);
            // 220 m wide: the outer land meets the neighbouring region's land halfway.
            outer.transform.localPosition = new Vector3(0, -0.04f, 0); outer.transform.localScale = new Vector3(22, 1, 22);
            Object.DestroyImmediate(outer.GetComponent<Collider>());
            outer.GetComponent<Renderer>().sharedMaterial = Solid("Outer_" + index, Color.Lerp(groundColour, new Color(0.47f, 0.72f, 0.38f), index == 1 ? 0.15f : 0.55f));
            foreach (var wall in new[] { new[] { -HalfWidth, middle, 0.3f, depth }, new[] { HalfWidth, middle, 0.3f, depth }, new[] { 0, North, HalfWidth * 2, 0.3f }, new[] { 0, South, HalfWidth * 2, 0.3f } })
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Cube); w.name = "Wall"; w.transform.SetParent(root, false);
                w.transform.localPosition = new Vector3(wall[0], 1, wall[1]); w.transform.localScale = new Vector3(wall[2], 2, wall[3]);
                w.GetComponent<Renderer>().enabled = false;
            }
            var fence = new GameObject("울타리").transform; fence.SetParent(root, false);
            for (float x = -23; x <= 23.01f; x += 2) { Spawn(Toon + "Urban Props/wooden-fence.prefab", fence, new Vector3(x, 0, North - 0.2f), 0); Spawn(Toon + "Urban Props/wooden-fence.prefab", fence, new Vector3(x, 0, South + 0.2f), 0); }
            for (float z = South + 1; z <= North - 0.99f; z += 2) { Spawn(Toon + "Urban Props/wooden-fence.prefab", fence, new Vector3(-HalfWidth + 0.2f, 0, z), 90); Spawn(Toon + "Urban Props/wooden-fence.prefab", fence, new Vector3(HalfWidth - 0.2f, 0, z), 90); }
            // Same bin row and preparation column as the park, so every region reads the same way.
            var place = new GameObject("수거함과 준비대").transform; place.SetParent(root, false);
            foreach (var bin in bins)
            {
                var copy = Object.Instantiate(bin.gameObject, place); copy.name = bin.name;
                copy.transform.position = new Vector3(bin.transform.position.x, 0, bin.transform.position.z) + offset;
            }
            foreach (var station in stations)
            {
                var copy = Object.Instantiate(station.gameObject, place); copy.name = station.name;
                copy.transform.position = new Vector3(station.transform.position.x, 0, station.transform.position.z) + offset;
            }
            var path = Solid("Path_" + index, index == 1 ? new Color(0.78f, 0.62f, 0.45f) : new Color(0.97f, 0.9f, 0.74f));
            Flat(root, "가운데 길", new Vector3(0, 0.012f, -6), new Vector3(3.5f, 1, 30), path);
            Flat(root, "통 앞 길", new Vector3(0, 0.012f, 7), new Vector3(24, 1, 3), path);
            Flat(root, "준비대 길", new Vector3(-5, 0.012f, 0), new Vector3(10, 1, 2.2f), path);
            var spawn = new GameObject("도착 지점").transform; spawn.SetParent(root, false); spawn.localPosition = new Vector3(0, 0.1f, -10);
            return root;
        }
        private static GameObject Flat(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var part = Part(parent, PrimitiveType.Cube, position, new Vector3(size.x, 0.02f, size.z), material); part.name = name;
            return part;
        }
        private static GameObject Disc(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool solid)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cylinder); part.name = name; part.transform.SetParent(parent, false);
            part.transform.localPosition = position; part.transform.localScale = new Vector3(size.x, size.y / 2, size.z);
            part.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            if (solid) part.AddComponent<MeshCollider>().sharedMesh = part.GetComponent<MeshFilter>().sharedMesh;
            return part;
        }
        private static void Solid3(GameObject prop) { if (prop != null) AddBoxCollider(prop); }
        private static GameObject Ball(Transform parent, Vector3 at, float size, Color colour)
        {
            var ball = Part(parent, PrimitiveType.Sphere, at + Vector3.up * size / 2, Vector3.one * size, Solid("Ball_" + ColorUtility.ToHtmlStringRGB(colour), colour));
            ball.AddComponent<SphereCollider>();
            return ball;
        }

        // ---------- 반짝 바닷가: sand, sea, parasols, towels, sand castle, rocks, boats on the water ----------
        private static void Beach(Transform root)
        {
            var decor = new GameObject("꾸미기").transform; decor.SetParent(root, false);
            var water = Solid("Sea", new Color(0.36f, 0.72f, 0.95f));
            var foam = Solid("Foam", new Color(0.92f, 0.97f, 1f));
            // The sea fills the north beyond the fence; a strip of foam marks the shore.
            Flat(decor, "바다", new Vector3(0, -0.02f, 60), new Vector3(220, 1, 80), water);
            Flat(decor, "물거품", new Vector3(0, 0.0f, North + 1.2f), new Vector3(220, 1, 1.6f), foam);
            Flat(decor, "젖은 모래", new Vector3(0, -0.01f, North + 4), new Vector3(220, 1, 4.5f), Solid("WetSand", new Color(0.9f, 0.8f, 0.58f)));
            var trees = new[] { "tree-type-C", "tree-type-B" };
            int t = 0;
            for (float x = -22; x <= 22; x += 7) { Solid3(Spawn(Toon + "Vegetation/" + trees[t++ % 2] + ".prefab", decor, new Vector3(x, 0, South + 2.5f), t * 50)); }
            for (float z = -16; z <= 14; z += 10) { Solid3(Spawn(Toon + "Vegetation/tree-type-C.prefab", decor, new Vector3(-22, 0, z), z * 9)); Solid3(Spawn(Toon + "Vegetation/tree-type-C.prefab", decor, new Vector3(22, 0, z + 4), z * 7)); }
            // Parasols with tables and bright beach towels.
            var towels = new[] { new Color(1f, 0.56f, 0.7f), new Color(0.47f, 0.75f, 1f), new Color(1f, 0.84f, 0.38f), new Color(0.45f, 0.86f, 0.66f), new Color(0.72f, 0.62f, 1f) };
            var parasolSpots = new[] { new Vector3(-16, 0, -18), new Vector3(15, 0, -19), new Vector3(-17, 0, 3), new Vector3(17, 0, -1), new Vector3(16, 0, 14), new Vector3(-6, 0, -20) };
            for (int i = 0; i < parasolSpots.Length; i++)
            {
                Solid3(Spawn(Toon + "Urban Props/outdoor-seating.prefab", decor, parasolSpots[i], i * 40));
                Flat(decor, "수건 " + (i + 1), parasolSpots[i] + new Vector3(2.4f, 0.015f, 0.4f), new Vector3(1.1f, 1, 2), Solid("Towel_" + i % towels.Length, towels[i % towels.Length]));
            }
            // Sand castle with a little flag.
            var castle = new GameObject("모래성").transform; castle.SetParent(decor, false); castle.localPosition = new Vector3(-10, 0, -14);
            var sand = Solid("CastleSand", new Color(0.93f, 0.8f, 0.55f));
            Disc(castle, "바닥", new Vector3(0, 0.25f, 0), new Vector3(2.6f, 0.5f, 2.6f), sand, true);
            foreach (var at in new[] { new Vector3(-0.8f, 0, -0.8f), new Vector3(0.8f, 0, -0.8f), new Vector3(-0.8f, 0, 0.8f), new Vector3(0.8f, 0, 0.8f) })
                Disc(castle, "탑", at + new Vector3(0, 0.85f, 0), new Vector3(0.6f, 0.7f, 0.6f), sand, false);
            Disc(castle, "가운데 탑", new Vector3(0, 1.2f, 0), new Vector3(0.9f, 1.4f, 0.9f), sand, false);
            Part(castle, PrimitiveType.Cube, new Vector3(0, 2.25f, 0), new Vector3(0.04f, 0.7f, 0.04f), Solid("Pole", new Color(0.55f, 0.4f, 0.3f)));
            Part(castle, PrimitiveType.Cube, new Vector3(0.18f, 2.45f, 0), new Vector3(0.34f, 0.22f, 0.02f), Solid("Flag", new Color(1f, 0.45f, 0.6f)));
            // Beach balls, rocks, a kiosk and benches facing the sea.
            Ball(decor, new Vector3(-3, 0, -16), 0.7f, new Color(1f, 0.5f, 0.55f)); Ball(decor, new Vector3(12, 0, -8), 0.6f, new Color(0.4f, 0.7f, 1f)); Ball(decor, new Vector3(-14, 0, 10), 0.65f, new Color(1f, 0.85f, 0.35f));
            for (int i = 0; i < 6; i++) Spawn(Toon + "Vegetation/rock-type-" + (i % 2 == 0 ? "A" : "B") + ".prefab", decor, new Vector3(-20 + i * 8, 0, North - 1.5f), i * 60, 0.8f);
            Solid3(Spawn(Toon + "Urban Props/ice-cream-stand.prefab", decor, new Vector3(20, 0, -10), 270));
            Solid3(Spawn(Toon + "Urban Props/lemonade-stand.prefab", decor, new Vector3(-20, 0, -8), 90));
            for (int i = 0; i < 3; i++) Solid3(Spawn(Toon + "Urban Props/public-bench.prefab", decor, new Vector3(-12 + i * 12, 0, 16.5f), 180));
            // Boats bob on the sea (no colliders: they are beyond the fence).
            var boat = Solid("Boat", new Color(1f, 0.62f, 0.5f)); var sail = Solid("Sail", Color.white);
            foreach (var at in new[] { new Vector3(-30, 0, 34), new Vector3(8, 0, 44), new Vector3(36, 0, 30) })
            {
                var b = new GameObject("배").transform; b.SetParent(decor, false); b.localPosition = at;
                Part(b, PrimitiveType.Cube, new Vector3(0, 0.25f, 0), new Vector3(3.4f, 0.6f, 1.3f), boat);
                Part(b, PrimitiveType.Cube, new Vector3(0, 1.6f, 0), new Vector3(0.08f, 2.4f, 0.08f), Solid("Pole", new Color(0.55f, 0.4f, 0.3f)));
                Part(b, PrimitiveType.Cube, new Vector3(0.55f, 1.6f, 0), new Vector3(1, 1.8f, 0.04f), sail);
            }
        }

        // ---------- 알록달록 시장: stalls, crates, bunting, cafe tables, shops around ----------
        private static void Market(Transform root)
        {
            var decor = new GameObject("꾸미기").transform; decor.SetParent(root, false);
            string props = Toon + "Urban Props/", shops = Toon + "Buildings/Commercial Buildings/", civil = Toon + "Buildings/Civil Buildings/";
            // Shops all around outside the fence.
            Spawn(shops + "commercial-building-ice-cream-shop.prefab", decor, new Vector3(-14, 0, 29), 180);
            Spawn(shops + "commercial-building-coffee-shop.prefab", decor, new Vector3(6, 0, 29), 180);
            Spawn(civil + "suburban-house-C.prefab", decor, new Vector3(24, 0, 28), 200);
            Spawn(civil + "cb-apartment-A.prefab", decor, new Vector3(-34, 0, 0), 90);
            Spawn(shops + "commercial-building-coffee-shop.prefab", decor, new Vector3(34, 0, -4), 270);
            Spawn(civil + "suburban-house-B.prefab", decor, new Vector3(-26, 0, -36), 0);
            Spawn(civil + "cb-apartment-B.prefab", decor, new Vector3(22, 0, -36), 0);
            // Market stalls in two lanes.
            var stalls = new[] { "hot-dog-stand", "lemonade-stand", "ice-cream-stand", "hot-dog-stand", "lemonade-stand" };
            for (int i = 0; i < stalls.Length; i++)
            {
                Solid3(Spawn(props + stalls[i] + ".prefab", decor, new Vector3(-18 + i * 9, 0, -20), 0));
                if (i % 2 == 1) Solid3(Spawn(props + "outdoor-seating.prefab", decor, new Vector3(-18 + i * 9, 0, -14.5f), i * 30));
            }
            Solid3(Spawn(Hexxx + "Newspaper_stand.prefab", decor, new Vector3(20, 0, 12), 270));
            Solid3(Spawn(Hexxx + "Vending_machine_red.prefab", decor, new Vector3(22.8f, 0, 3), 270));
            Solid3(Spawn(Hexxx + "Vending_machine_blue.prefab", decor, new Vector3(22.8f, 0, 4.6f), 270));
            // Crates, barrels and pallets stacked by the stalls.
            foreach (var at in new[] { new Vector3(-21, 0, -8), new Vector3(-20, 0, 12), new Vector3(16, 0, -6), new Vector3(12, 0, 16) })
            {
                Solid3(Spawn(Hexxx + "Pallet.prefab", decor, at, 0));
                Spawn(Hexxx + "Box.prefab", decor, at + new Vector3(0, 0.28f, 0), 20);
                Spawn(Hexxx + "Barrel.prefab", decor, at + new Vector3(1.4f, 0, 0.6f), 0);
            }
            // Street lights with colourful bunting between them.
            var flags = new[] { new Color(1f, 0.56f, 0.7f), new Color(1f, 0.84f, 0.38f), new Color(0.47f, 0.75f, 1f), new Color(0.45f, 0.86f, 0.66f), new Color(0.72f, 0.62f, 1f) };
            var poles = new[] { new Vector3(-22, 0, -11), new Vector3(-8, 0, -11), new Vector3(8, 0, -11), new Vector3(22, 0, -11) };
            foreach (var at in poles) Spawn(Toon + "Infrastructure/Props/street-light-single.prefab", decor, at, 0);
            for (int p = 0; p + 1 < poles.Length; p++)
                for (int k = 1; k < 12; k++)
                {
                    float f = k / 12f;
                    var at = Vector3.Lerp(poles[p], poles[p + 1], f) + Vector3.up * (4.1f - Mathf.Sin(f * Mathf.PI) * 0.7f);
                    var flag = Part(decor, PrimitiveType.Cube, at, new Vector3(0.42f, 0.5f, 0.03f), Solid("Bunting_" + k % flags.Length, flags[k % flags.Length]));
                    flag.transform.localRotation = Quaternion.Euler(0, 0, 45);
                }
            // Flower planters and trees.
            var trees = new[] { "tree-type-A", "tree-type-D" };
            for (int i = 0; i < 6; i++) Solid3(Spawn(Toon + "Vegetation/" + trees[i % 2] + ".prefab", decor, new Vector3(-21 + (i % 3) * 3, 0, 14 + (i / 3) * 3), i * 40));
            foreach (var x in new[] { -14f, 14f }) { Spawn(Hexxx + "Garden_bed.prefab", decor, new Vector3(x, 0, -2), 90, 1, true); }
            Solid3(Spawn(Toon + "Infrastructure/Props/bus-stop.prefab", decor, new Vector3(-20, 0, -24), 0));
        }

        // ---------- Scenery around every region (outside the fence, no colliders) ----------
        // Landmark buildings, roads and rails, and land shapes: hills with trees and wind turbines, a river, islands,
        // sand dunes, low-poly mountains with snow on the horizon, clouds and hot-air balloons in the sky.
        public static readonly string[] SceneryNames = { "풍경 1 새싹 공원", "풍경 2 반짝 바닷가", "풍경 3 알록달록 시장" };
        private const string Civil = Toon + "Buildings/Civil Buildings/", Shop = Toon + "Buildings/Commercial Buildings/",
            Gov = Toon + "Buildings/Government Buildings/", Infra = Toon + "Infrastructure/Infrastructure Buildings/", Vehicles = Toon + "Vehicles/";
        private static readonly Color Grass = new Color(0.52f, 0.78f, 0.42f), Meadow = new Color(0.62f, 0.84f, 0.46f), Dune = new Color(0.95f, 0.86f, 0.62f);

        private static void Scenery(Transform world)
        {
            for (int i = 0; i < 3; i++)
            {
                var root = new GameObject(SceneryNames[i]).transform; root.SetParent(world, false); root.position = Offset(i);
                if (i == 0) ParkScenery(root); else if (i == 1) BeachScenery(root); else MarketScenery(root);
                Skyline(root, i);
            }
        }
        // 새싹 공원: the town hall street, a river with a bridge, sports courts, wind turbines and solar panels on the hills.
        private static void ParkScenery(Transform root)
        {
            Spawn(Gov + "government-building-city-hall.prefab", root, new Vector3(0, 0, 54), 180);
            Spawn(Gov + "government-building-museum.prefab", root, new Vector3(-34, 0, 52), 180);
            Spawn(Infra + "infrastructure-building-clock-tower.prefab", root, new Vector3(26, 0, 50), 180);
            Spawn(Infra + "infrastructure-building-church.prefab", root, new Vector3(46, 0, 54), 200);
            Spawn(Gov + "government-building-fire-station.prefab", root, new Vector3(64, 0, 22), 270);
            Spawn(Gov + "government-building-police-station.prefab", root, new Vector3(62, 0, -16), 270);
            Spawn(Infra + "infrastructure-building-hospital.prefab", root, new Vector3(68, 0, -52), 270);
            Spawn(Infra + "infrastructure-building-gas-station.prefab", root, new Vector3(40, 0, -46), 0);
            Spawn(Infra + "infrastructure-building-train-station.prefab", root, new Vector3(0, 0, -66), 90);
            Spawn(Infra + "infrastructure-building-tennis-court.prefab", root, new Vector3(-70, 0, -14), 0);
            Spawn(Infra + "infrastructure-building-basketball-court.prefab", root, new Vector3(-70, 0, 16), 0);
            Spawn(Civil + "cb-apartment-E.prefab", root, new Vector3(-94, 0, -36), 90);
            Spawn(Civil + "cb-apartment-G.prefab", root, new Vector3(-94, 0, 2), 90);
            Spawn(Civil + "suburban-house-B.prefab", root, new Vector3(-92, 0, 34), 90);
            Spawn(Vehicles + "Government Vehicles/city-bus.prefab", root, new Vector3(34, 0, -32), 270);
            Spawn(Vehicles + "Government Vehicles/taxi.prefab", root, new Vector3(-30, 0, -29), 90);
            Spawn(Vehicles + "Government Vehicles/fire-truck.prefab", root, new Vector3(50, 0, 14), 0);
            // A river west of the park with a little wooden bridge.
            var water = Solid("Water", new Color(0.45f, 0.76f, 1f));
            Flat(root, "강", new Vector3(-50, 0, 0), new Vector3(7, 1, 220), water);
            Flat(root, "강가 모래", new Vector3(-50, -0.005f, 0), new Vector3(9.5f, 1, 220), Solid("RiverBank", new Color(0.93f, 0.86f, 0.66f)));
            Bridge(root, new Vector3(-50, 0, 2));
            Rails(root, -86);
            Spawn(Vehicles + "train.prefab", root, new Vector3(-30, 0, -86), 90);
            SolarField(root, new Vector3(-80, 0, -66));
            Hill(root, new Vector3(-82, 0, 62), 16, 9, Grass, 3); Turbine(root, new Vector3(-82, 8.6f, 62), 0, 0);
            Hill(root, new Vector3(80, 0, 70), 18, 8, Meadow, 3); Turbine(root, new Vector3(80, 7.6f, 70), 10, 1);
            Hill(root, new Vector3(40, 0, 88), 22, 9, Grass, 4); Turbine(root, new Vector3(40, 8.6f, 88), -10, 2);
            Hill(root, new Vector3(-28, 0, 84), 20, 7, Meadow, 5);
            Hill(root, new Vector3(-86, 0, -88), 18, 9, Grass, 3);
            Hill(root, new Vector3(88, 0, -84), 18, 8, Meadow, 4);
        }
        // 반짝 바닷가: a seaside town with a hotel, a pier, a lifeguard tower, islands with a tower and sand dunes.
        private static void BeachScenery(Transform root)
        {
            Spawn(Shop + "commercial-building-hotel.prefab", root, new Vector3(-36, 0, -6), 90);
            Spawn(Civil + "cb-apartment-J.prefab", root, new Vector3(-36, 0, 11), 90);
            Spawn(Shop + "commercial-building-fastfood-shop.prefab", root, new Vector3(37, 0, -14), 270);
            Spawn(Shop + "commercial-building-ice-cream-shop.prefab", root, new Vector3(37, 0, 5), 270);
            Spawn(Civil + "suburban-house-D.prefab", root, new Vector3(-24, 0, -41), 0);
            Spawn(Civil + "suburban-house-E.prefab", root, new Vector3(-8, 0, -41), 0);
            Spawn(Shop + "commercial-building-pizza-shop.prefab", root, new Vector3(8, 0, -41), 0);
            Spawn(Civil + "suburban-house-A.prefab", root, new Vector3(24, 0, -41), 0);
            Spawn(Civil + "cb-apartment-C.prefab", root, new Vector3(-62, 0, -40), 0);
            Spawn(Shop + "commercial-building-office.prefab", root, new Vector3(62, 0, -42), 0);
            Road(root, -31.5f, 110);
            Spawn(Vehicles + "Government Vehicles/taxi.prefab", root, new Vector3(-16, 0, -30), 90);
            Spawn(Vehicles + "Civil Vehicles/civil-car-D.prefab", root, new Vector3(12, 0, -33), 270);
            Spawn(Vehicles + "Civil Vehicles/civil-car-C.prefab", root, new Vector3(30, 0, -30), 90);
            Spawn(Vehicles + "Civil Vehicles/civil-car-B.prefab", root, new Vector3(-40, 0, -33), 270);
            var trees = new[] { "tree-type-C", "tree-type-B", "tree-type-A" };
            for (int i = 0; i < 8; i++) Spawn(Toon + "Vegetation/" + trees[i % 3] + ".prefab", root, new Vector3(i % 2 == 0 ? -27 : 27, 0, -24 + (i / 2) * 11), i * 45);
            // Wooden pier out into the sea.
            var wood = Solid("PierWood", new Color(0.72f, 0.52f, 0.36f));
            Part(root, PrimitiveType.Cube, new Vector3(-4, 0.3f, 31), new Vector3(3, 0.25f, 20), wood).name = "부두";
            Part(root, PrimitiveType.Cube, new Vector3(-4, 0.3f, 42.5f), new Vector3(7, 0.25f, 4), wood).name = "부두 끝";
            for (float z = 23; z <= 43; z += 4) foreach (var x in new[] { -5.6f, -2.4f }) Part(root, PrimitiveType.Cylinder, new Vector3(x, 0.15f, z), new Vector3(0.3f, 0.6f, 0.3f), wood);
            LifeguardTower(root, new Vector3(-17, 0, 24));
            // Islands: one green with a tall tower like a lighthouse, one of rocks.
            var sand = Solid("IslandSand", Dune);
            Disc(root, "섬", new Vector3(36, 0.05f, 60), new Vector3(28, 0.2f, 20), sand, false);
            Hill(root, new Vector3(38, 0, 61), 9, 4.5f, Grass, 3);
            Spawn(Infra + "infrastructure-building-clock-tower.prefab", root, new Vector3(28, 0, 56), 200);
            Disc(root, "바위섬", new Vector3(-42, 0.05f, 48), new Vector3(12, 0.2f, 9), sand, false);
            for (int i = 0; i < 4; i++) Spawn(Toon + "Vegetation/rock-type-" + (i % 2 == 0 ? "A" : "B") + ".prefab", root, new Vector3(-44 + i * 1.6f, 0, 47 + (i % 2) * 1.5f), i * 70, 2.2f);
            var boat = Solid("Boat", new Color(1f, 0.62f, 0.5f)); var sail = Solid("Sail", Color.white);
            foreach (var at in new[] { new Vector3(-66, 0, 40), new Vector3(66, 0, 34), new Vector3(-12, 0, 74), new Vector3(70, 0, 82) }) Boat(root, at, boat, sail);
            // Sand dunes behind the town.
            Hill(root, new Vector3(-74, 0, -12), 18, 6, Dune, 2);
            Hill(root, new Vector3(74, 0, -14), 20, 7, Dune, 2);
            Hill(root, new Vector3(-64, 0, -80), 22, 8, Meadow, 4);
            Hill(root, new Vector3(64, 0, -78), 20, 7, Grass, 3); Turbine(root, new Vector3(64, 6.6f, -78), 180, 3);
            Hill(root, new Vector3(0, 0, -84), 26, 7, Grass, 5);
        }
        // 알록달록 시장: a shopping street with a bus road, a cinema, a bank, a concert hall, a plaza with a clock tower and a ferris wheel.
        private static void MarketScenery(Transform root)
        {
            Spawn(Gov + "government-building-bank.prefab", root, new Vector3(-40, 0, 27.5f), 180);
            Spawn(Shop + "commercial-building-cinema.prefab", root, new Vector3(-37, 0, -18), 90);
            Spawn(Shop + "commercial-building-pizza-shop.prefab", root, new Vector3(36, 0, 15), 270);
            Spawn(Shop + "commercial-building-office.prefab", root, new Vector3(38, 0, -22), 270);
            Spawn(Shop + "commercial-building-shopping-center.prefab", root, new Vector3(0, 0, 55), 180);
            Spawn(Shop + "commercial-building-concert-hall.prefab", root, new Vector3(72, 0, 8), 270);
            Spawn(Infra + "infrastructure-building-train-station.prefab", root, new Vector3(0, 0, -62), 90);
            Spawn(Infra + "infrastructure-building-plaza.prefab", root, new Vector3(-72, 0, -4), 0);
            Spawn(Infra + "infrastructure-building-clock-tower.prefab", root, new Vector3(-72, 0, -22), 90);
            Spawn(Civil + "cb-apartment-H.prefab", root, new Vector3(-94, 0, 24), 90);
            Spawn(Civil + "cb-apartment-F.prefab", root, new Vector3(-94, 0, -40), 90);
            Spawn(Civil + "cb-apartment-D.prefab", root, new Vector3(60, 0, -44), 0);
            Spawn(Infra + "infrastructure-building-parking-lot.prefab", root, new Vector3(36, 0, -64), 0);
            Road(root, 39.5f, 110);
            Spawn(Vehicles + "Government Vehicles/city-bus.prefab", root, new Vector3(-24, 0, 38), 90);
            Spawn(Vehicles + "Government Vehicles/taxi.prefab", root, new Vector3(8, 0, 41), 270);
            Spawn(Vehicles + "Government Vehicles/police-car.prefab", root, new Vector3(30, 0, 38), 90);
            Spawn(Vehicles + "Civil Vehicles/civil-car-B.prefab", root, new Vector3(34, 0, -62), 0);
            Spawn(Vehicles + "Civil Vehicles/civil-car-A.prefab", root, new Vector3(39, 0, -66), 180);
            Rails(root, -82);
            Spawn(Vehicles + "subway-metro.prefab", root, new Vector3(30, 0, -82), 90);
            FerrisWheel(root, new Vector3(-66, 0, 46), 15);
            Hill(root, new Vector3(-80, 0, -66), 18, 8, Grass, 3);
            Hill(root, new Vector3(82, 0, -70), 20, 9, Meadow, 3); Turbine(root, new Vector3(82, 8.6f, -70), 200, 4);
            Hill(root, new Vector3(60, 0, 76), 16, 7, Grass, 3);
            Hill(root, new Vector3(-46, 0, 82), 18, 7, Meadow, 4);
        }
        // Mountains on the horizon (not where the sea is), clouds and hot-air balloons.
        private static void Skyline(Transform root, int region)
        {
            Color[] tones = { new Color(0.7f, 0.68f, 0.9f), new Color(0.56f, 0.76f, 0.8f), new Color(0.62f, 0.8f, 0.68f) };
            var random = new System.Random(4100 + region);
            System.Action<float, float, int> range = (fixedValue, sign, axis) =>
            {
                float[] along = { -88, -46, -4, 40, 86 };
                for (int k = 0; k < along.Length; k++)
                {
                    float jitter = (float)random.NextDouble() * 10 - 5;
                    var at = axis == 0 ? new Vector3(along[k] + jitter, 0, fixedValue) : new Vector3(fixedValue, 0, along[k] + jitter);
                    Mountain(root, at, 22 + (float)random.NextDouble() * 10, 30 + (float)random.NextDouble() * 18, tones[(k + region) % 3], k % 2 == 0);
                }
            };
            if (region != 1) range(124, 1, 0);
            range(-126, -1, 0);
            if (region == 0) range(-124, 0, 1);
            if (region == 2) range(124, 0, 1);
            var cloud = Solid("Cloud", Color.white);
            for (int k = 0; k < 8; k++)
            {
                float angle = (k * 47 + region * 20) * Mathf.Deg2Rad, distance = 62 + (k % 3) * 18;
                Cloud(root, new Vector3(Mathf.Cos(angle) * distance, 46 + (k % 3) * 6, Mathf.Sin(angle) * distance), cloud, k);
            }
            Color[][] balloons = { new[] { new Color(1f, 0.55f, 0.65f), new Color(1f, 0.88f, 0.45f) }, new[] { new Color(0.55f, 0.75f, 1f), Color.white }, new[] { new Color(0.66f, 0.56f, 1f), new Color(0.5f, 0.9f, 0.7f) } };
            Balloon(root, new Vector3(-34, 24, 30), balloons[region], 0);
            Balloon(root, new Vector3(48, 28, 36), balloons[(region + 1) % 3], 1);
        }

        private static void NoShadow(GameObject part) { foreach (var r in part.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
        private static string Hex(Color colour) { return ColorUtility.ToHtmlStringRGB(colour); }
        // A round grassy hill (half a squashed sphere) with a few trees on its slopes.
        private static void Hill(Transform parent, Vector3 at, float radius, float height, Color colour, int trees)
        {
            var hill = Part(parent, PrimitiveType.Sphere, at, new Vector3(radius * 2, height * 2, radius * 2), Solid("Hill_" + Hex(colour), colour)); hill.name = "언덕";
            string[] kinds = { "tree-type-A", "tree-type-B", "tree-type-C", "tree-type-D" };
            for (int k = 0; k < trees; k++)
            {
                float angle = (k * 137 + at.x) * Mathf.Deg2Rad, d = radius * (0.35f + 0.15f * (k % 3));
                float y = height * Mathf.Sqrt(Mathf.Max(0, 1 - (d / radius) * (d / radius)));
                Spawn(Toon + "Vegetation/" + kinds[k % 4] + ".prefab", parent, at + new Vector3(Mathf.Cos(angle) * d, y - 0.3f, Mathf.Sin(angle) * d), k * 60, 1.4f);
            }
        }
        private static void Mountain(Transform parent, Vector3 at, float radius, float height, Color colour, bool snow)
        {
            var mountain = new GameObject("산", typeof(MeshFilter), typeof(MeshRenderer));
            mountain.transform.SetParent(parent, false); mountain.transform.localPosition = at;
            mountain.transform.localRotation = Quaternion.Euler(0, Mathf.Abs(at.x * 7 + at.z * 3) % 360, 0); mountain.transform.localScale = new Vector3(radius, height, radius);
            mountain.GetComponent<MeshFilter>().sharedMesh = LowCone(); mountain.GetComponent<MeshRenderer>().sharedMaterial = Solid("Mountain_" + Hex(colour), colour);
            if (snow)
            {
                var cap = new GameObject("눈", typeof(MeshFilter), typeof(MeshRenderer));
                cap.transform.SetParent(mountain.transform, false); cap.transform.localPosition = new Vector3(0, 0.7f, 0); cap.transform.localScale = Vector3.one * 0.31f;
                cap.GetComponent<MeshFilter>().sharedMesh = LowCone(); cap.GetComponent<MeshRenderer>().sharedMaterial = Solid("Snow", new Color(0.98f, 0.98f, 1f));
            }
            NoShadow(mountain);
        }
        // Flat-shaded seven-sided cone (radius 1, height 1), saved once as an asset so the scene can reference it.
        private static Mesh LowCone()
        {
            const string path = "Assets/EcoCity/Generated/Submission/KidWorld/LowCone.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = new Mesh { name = "LowCone" };
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            const int sides = 7;
            for (int k = 0; k < sides; k++)
            {
                float a0 = k * Mathf.PI * 2 / sides, a1 = (k + 1) * Mathf.PI * 2 / sides;
                vertices.Add(new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0))); vertices.Add(Vector3.up); vertices.Add(new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)));
                triangles.Add(k * 3); triangles.Add(k * 3 + 1); triangles.Add(k * 3 + 2);
            }
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
        private static void Cloud(Transform parent, Vector3 at, Material material, int seed)
        {
            var cloud = new GameObject("구름").transform; cloud.SetParent(parent, false); cloud.localPosition = at;
            for (int k = 0; k < 4; k++)
                Part(cloud, PrimitiveType.Sphere, new Vector3(k * 2.6f - 4, (k % 2) * 0.9f, ((k + seed) % 2) * 1.2f), new Vector3(5 + (k % 2) * 1.6f, 3, 4), material);
            NoShadow(cloud.gameObject);
        }
        private static void Balloon(Transform parent, Vector3 at, Color[] colours, int seed)
        {
            var balloon = new GameObject("열기구").transform; balloon.SetParent(parent, false); balloon.localPosition = at;
            Part(balloon, PrimitiveType.Sphere, Vector3.zero, new Vector3(5, 6, 5), Solid("Balloon_" + Hex(colours[0]), colours[0]));
            Part(balloon, PrimitiveType.Sphere, new Vector3(0, 0.2f, 0), new Vector3(5.1f, 1.4f, 5.1f), Solid("Balloon_" + Hex(colours[1]), colours[1]));
            Part(balloon, PrimitiveType.Cube, new Vector3(0, -4.4f, 0), new Vector3(1.4f, 1, 1.4f), Solid("Basket", new Color(0.7f, 0.5f, 0.32f)));
            foreach (var corner in new[] { new Vector3(-0.6f, 0, -0.6f), new Vector3(0.6f, 0, -0.6f), new Vector3(-0.6f, 0, 0.6f), new Vector3(0.6f, 0, 0.6f) })
                Part(balloon, PrimitiveType.Cube, corner + new Vector3(0, -3.3f, 0), new Vector3(0.05f, 1.6f, 0.05f), Solid("Rope", new Color(0.5f, 0.4f, 0.32f)));
            balloon.localRotation = Quaternion.Euler(0, seed * 50, 0);
            NoShadow(balloon.gameObject);
        }
        // Eco City runs on clean energy: white wind turbines whose blades turn slowly.
        private static void Turbine(Transform parent, Vector3 at, float yaw, int seed)
        {
            var turbine = new GameObject("풍력 발전기").transform; turbine.SetParent(parent, false); turbine.localPosition = at; turbine.localRotation = Quaternion.Euler(0, yaw, 0);
            var white = Solid("TurbineWhite", new Color(0.97f, 0.97f, 1f));
            Part(turbine, PrimitiveType.Cylinder, new Vector3(0, 7, 0), new Vector3(0.6f, 7, 0.6f), white);
            Part(turbine, PrimitiveType.Cube, new Vector3(0, 14.2f, 0.3f), new Vector3(1, 1, 2.2f), white);
            var hub = new GameObject("날개").transform; hub.SetParent(turbine, false); hub.localPosition = new Vector3(0, 14.2f, -0.95f);
            Part(hub, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.8f, white);
            for (int k = 0; k < 3; k++)
            {
                var blade = Part(hub, PrimitiveType.Cube, Quaternion.Euler(0, 0, k * 120) * new Vector3(0, 3.3f, 0), new Vector3(0.5f, 6.4f, 0.12f), white);
                blade.transform.localRotation = Quaternion.Euler(0, 0, k * 120);
            }
            hub.localRotation = Quaternion.Euler(0, 0, seed * 37);
            var spin = hub.gameObject.AddComponent<KidSpin>(); spin.axis = Vector3.forward; spin.speed = 34 + seed * 5;
        }
        // Rows of solar panels on short legs.
        private static void SolarField(Transform parent, Vector3 at)
        {
            var field = new GameObject("태양광 발전").transform; field.SetParent(parent, false); field.localPosition = at;
            var panel = Solid("SolarPanel", new Color(0.24f, 0.38f, 0.68f)); var leg = Solid("SolarLeg", new Color(0.75f, 0.76f, 0.8f));
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 6; col++)
                {
                    var spot = new Vector3(col * 3.6f, 0, row * 4);
                    Part(field, PrimitiveType.Cube, spot + new Vector3(0, 0.55f, 0), new Vector3(0.14f, 1.1f, 0.14f), leg);
                    Part(field, PrimitiveType.Cube, spot + new Vector3(0, 1.2f, 0), new Vector3(3.2f, 0.12f, 2), panel).transform.localRotation = Quaternion.Euler(-25, 0, 0);
                }
        }
        private static void Bridge(Transform parent, Vector3 at)
        {
            var bridge = new GameObject("다리").transform; bridge.SetParent(parent, false); bridge.localPosition = at;
            var wood = Solid("BridgeWood", new Color(0.74f, 0.52f, 0.36f)); var rail = Solid("BridgeRail", new Color(0.98f, 0.7f, 0.5f));
            Part(bridge, PrimitiveType.Cube, new Vector3(0, 0.35f, 0), new Vector3(12, 0.3f, 4), wood);
            foreach (var z in new[] { -1.9f, 1.9f })
            {
                Part(bridge, PrimitiveType.Cube, new Vector3(0, 1.1f, z), new Vector3(12, 0.16f, 0.16f), rail);
                for (float x = -5.5f; x <= 5.51f; x += 2.2f) Part(bridge, PrimitiveType.Cube, new Vector3(x, 0.75f, z), new Vector3(0.18f, 0.8f, 0.18f), rail);
            }
        }
        private static void Road(Transform parent, float z, float length)
        {
            Flat(parent, "찻길", new Vector3(0, 0.01f, z), new Vector3(length, 1, 7), Solid("Road", new Color(0.42f, 0.42f, 0.48f)));
            for (float x = -length / 2 + 2; x < length / 2; x += 6) Flat(parent, "찻길 선", new Vector3(x, 0.02f, z), new Vector3(3, 1, 0.25f), Solid("RoadLine", new Color(1f, 0.92f, 0.55f)));
        }
        private static void Rails(Transform parent, float z)
        {
            var rails = new GameObject("기찻길").transform; rails.SetParent(parent, false); rails.localPosition = new Vector3(0, 0, z);
            Flat(rails, "자갈", new Vector3(0, 0.01f, 0), new Vector3(220, 1, 3.6f), Solid("RailBed", new Color(0.66f, 0.62f, 0.64f)));
            var sleeper = Solid("Sleeper", new Color(0.55f, 0.4f, 0.3f)); var steel = Solid("Rail", new Color(0.72f, 0.74f, 0.8f));
            for (float x = -109; x <= 109; x += 2.5f) Part(rails, PrimitiveType.Cube, new Vector3(x, 0.06f, 0), new Vector3(0.5f, 0.1f, 2.6f), sleeper);
            foreach (var side in new[] { -0.72f, 0.72f }) Part(rails, PrimitiveType.Cube, new Vector3(0, 0.14f, side), new Vector3(220, 0.12f, 0.14f), steel);
        }
        private static void Boat(Transform parent, Vector3 at, Material hull, Material sail)
        {
            var b = new GameObject("배").transform; b.SetParent(parent, false); b.localPosition = at; b.localRotation = Quaternion.Euler(0, at.x * 3, 0);
            Part(b, PrimitiveType.Cube, new Vector3(0, 0.25f, 0), new Vector3(3.4f, 0.6f, 1.3f), hull);
            Part(b, PrimitiveType.Cube, new Vector3(0, 1.6f, 0), new Vector3(0.08f, 2.4f, 0.08f), Solid("Pole", new Color(0.55f, 0.4f, 0.3f)));
            Part(b, PrimitiveType.Cube, new Vector3(0.55f, 1.6f, 0), new Vector3(1, 1.8f, 0.04f), sail);
        }
        private static void LifeguardTower(Transform parent, Vector3 at)
        {
            var tower = new GameObject("구조대 망루").transform; tower.SetParent(parent, false); tower.localPosition = at;
            var white = Solid("TowerWhite", new Color(0.98f, 0.97f, 0.95f)); var red = Solid("TowerRed", new Color(1f, 0.45f, 0.45f));
            foreach (var corner in new[] { new Vector3(-0.9f, 0, -0.9f), new Vector3(0.9f, 0, -0.9f), new Vector3(-0.9f, 0, 0.9f), new Vector3(0.9f, 0, 0.9f) })
                Part(tower, PrimitiveType.Cube, corner + new Vector3(0, 1.3f, 0), new Vector3(0.18f, 2.6f, 0.18f), white);
            Part(tower, PrimitiveType.Cube, new Vector3(0, 2.7f, 0), new Vector3(2.4f, 0.2f, 2.4f), white);
            Part(tower, PrimitiveType.Cube, new Vector3(0, 3.5f, 0), new Vector3(2, 1.4f, 2), red);
            Part(tower, PrimitiveType.Cube, new Vector3(0, 4.35f, 0), new Vector3(2.6f, 0.2f, 2.6f), white);
        }
        // A colourful ferris wheel that turns slowly.
        private static void FerrisWheel(Transform parent, Vector3 at, float yaw)
        {
            var root = new GameObject("대관람차").transform; root.SetParent(parent, false); root.localPosition = at; root.localRotation = Quaternion.Euler(0, yaw, 0);
            var frame = Solid("WheelFrame", new Color(0.97f, 0.96f, 1f)); const float radius = 9, hubHeight = 11;
            foreach (var z in new[] { -1.4f, 1.4f })
                foreach (var side in new[] { -1f, 1f })
                {
                    var leg = Part(root, PrimitiveType.Cube, new Vector3(side * 2.9f, hubHeight / 2, z), new Vector3(0.5f, hubHeight + 0.6f, 0.5f), frame);
                    leg.transform.localRotation = Quaternion.Euler(0, 0, side * 15);
                }
            var wheel = new GameObject("바퀴").transform; wheel.SetParent(root, false); wheel.localPosition = new Vector3(0, hubHeight, 0);
            Part(wheel, PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.2f, 1.6f, 1.2f), frame).transform.localRotation = Quaternion.Euler(90, 0, 0);
            const int segments = 16;
            for (int k = 0; k < segments; k++)
            {
                float a = k * 360f / segments;
                var rim = Part(wheel, PrimitiveType.Cube, Quaternion.Euler(0, 0, a) * new Vector3(radius, 0, 0), new Vector3(0.3f, 2 * radius * Mathf.Sin(Mathf.PI / segments) + 0.2f, 0.3f), frame);
                rim.transform.localRotation = Quaternion.Euler(0, 0, a);
                if (a < 180) Part(wheel, PrimitiveType.Cube, Vector3.zero, new Vector3(2 * radius, 0.16f, 0.16f), frame).transform.localRotation = Quaternion.Euler(0, 0, a);
            }
            Color[] cabins = { new Color(1f, 0.56f, 0.7f), new Color(1f, 0.84f, 0.38f), new Color(0.47f, 0.75f, 1f), new Color(0.45f, 0.86f, 0.66f), new Color(0.72f, 0.62f, 1f), new Color(1f, 0.62f, 0.45f) };
            for (int k = 0; k < 8; k++)
                Part(wheel, PrimitiveType.Sphere, Quaternion.Euler(0, 0, k * 45 + 22.5f) * new Vector3(radius, 0, 0), Vector3.one * 2f, Solid("Cabin_" + k % cabins.Length, cabins[k % cabins.Length]));
            var spin = wheel.gameObject.AddComponent<KidSpin>(); spin.axis = Vector3.forward; spin.speed = 8;
        }
    }
}
#endif
