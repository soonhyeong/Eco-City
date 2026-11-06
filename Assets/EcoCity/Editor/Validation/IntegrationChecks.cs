#if UNITY_EDITOR
using System;
using System.IO;
using EcoCity.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EcoCity.Validation
{
    public static class IntegrationChecks
    {
        [MenuItem("EcoCity/Run Integration Checks (No Play Mode)")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode only.");
            string[] failures = CoreChecks.RunAll();
            Require(failures.Length == 0, string.Join("\n", failures));
            Debug.Log("EcoCity core: " + CoreChecks.CheckCount + "/" + CoreChecks.CheckCount);
            EcoCity.Editor.PrototypeSceneBuilder.Create();
            string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            var game = UnityEngine.Object.FindFirstObjectByType<EcoGame>();
            Require(game != null && game.wasteItems.Length == 6, "Six registered items required.");
            Require(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length == 1, "One camera required.");
            Require(UnityEngine.Object.FindObjectsByType<EcoGame>(FindObjectsSortMode.None).Length == 1, "One game required.");
            var hud = UnityEngine.Object.FindFirstObjectByType<PrototypeHud>();
            Require(hud.game == game && hud.player != null && hud.interactor != null && hud.feedback != null, "HUD references missing.");
            Require(hud.player.viewCamera == hud.interactor.viewCamera && hud.interactor.game == game, "Player references missing.");
            Require(hud.player.GetComponent<CharacterController>() != null, "Controller missing.");
            CheckPlayerSetupAndMovement(hud.player);
            var specs = new System.Collections.Generic.List<WasteSpec>();
            foreach (var item in game.wasteItems)
            {
                Require(item != null && item.definition != null && item.GetComponent<Collider>() != null, "Waste references missing.");
                specs.Add(item.definition.ToSpec(item.instanceId));
            }
            Require(UnityEngine.Object.FindObjectsByType<DisposalBin>(FindObjectsSortMode.None).Length == 7, "Seven bins required.");
            Require(UnityEngine.Object.FindObjectsByType<PreparationStation>(FindObjectsSortMode.None).Length == 3, "Three stations required.");
            foreach (var target in UnityEngine.Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None))
                Require(target.GetComponent<Collider>() != null, "Interaction collider missing: " + target.name);
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0, "Missing script.");
                    foreach (var component in t.GetComponents<Component>())
                    {
                        var serialized = new SerializedObject(component);
                        var property = serialized.GetIterator();
                        while (property.Next(true))
                            if (property.propertyType == SerializedPropertyType.ObjectReference)
                                Require(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0,
                                    "Broken reference: " + t.name + "/" + property.propertyPath);
                    }
                }
            var session = new EcoSession(specs);
            foreach (var spec in specs)
            {
                Require(session.PickUp(spec.Id).IsSuccess, "Pickup failed.");
                while ((spec.Required & ~session.CompletedPreparation) != Preparation.None)
                    Require(session.Prepare(WasteRules.NextStep(spec.Required & ~session.CompletedPreparation)).IsSuccess, "Prepare failed.");
                Require(session.Dispose(spec.Category).IsSuccess, "Dispose failed.");
            }
            Require(session.IsComplete && session.Score == 210 && session.CompletedMissions == 3, "Six-item result mismatch.");
            string folder = Path.Combine(Path.GetTempPath(), "EcoCityChecks-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                string path = Path.Combine(folder, "progress.json");
                var store = new ProgressStore(path);
                string message;
                var held = new EcoSession(specs);
                held.PickUp(specs[0].Id);
                Require(store.Save(held.Capture(), out message), message);
                var restored = new EcoSession(specs);
                Require(store.LoadInto(restored, out message) && restored.Held.Id == specs[0].Id, "Held JSON round trip.");
                Require(store.Save(session.Capture(), out message), message);
                File.WriteAllText(path, "{broken");
                Require(store.LoadInto(new EcoSession(specs), out message), "Backup recovery failed.");
                Require(store.Save(session.Capture(), out message), message);
                Require(Directory.GetFiles(folder, "*.rejected-*").Length == 2, "Rejected originals not preserved.");
                foreach (string json in new[] { "", "{}", "{\"schemaVersion\":99}", "{\"schemaVersion\":1,\"disposedIds\":[\"removed-id\"]}" })
                {
                    string isolated = Path.Combine(folder, Guid.NewGuid().ToString("N") + ".json");
                    File.WriteAllText(isolated, json);
                    Require(!new ProgressStore(isolated).LoadInto(new EcoSession(specs), out message), "Invalid save accepted: " + json);
                }
                var partial = new EcoSession(specs);
                for (int i = 0; i < 3; i++) { partial.PickUp(specs[i].Id); partial.Dispose(specs[i].Category); }
                partial.PickUp(specs[3].Id); partial.Prepare(Preparation.Empty);
                Require(store.Save(partial.Capture(), out message), message);
                restored = new EcoSession(specs);
                Require(store.LoadInto(restored, out message) && restored.CompletedPreparation == Preparation.Empty && restored.DisposedCount == 3,
                    "Preparation JSON round trip.");
                string blocked = Path.Combine(folder, "directory.json"); Directory.CreateDirectory(blocked);
                Require(!new ProgressStore(blocked).Save(session.Capture(), out message) && session.IsComplete, "Save failure must preserve session.");
                // Exercise the Unity bridge explicitly in Edit Mode with an isolated save path.
                // Awake/Play and the user's persistent save folder are never invoked.
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(EcoGame).GetField("specs", flags).SetValue(game, specs);
                typeof(EcoGame).GetField("store", flags).SetValue(game, new ProgressStore(Path.Combine(folder, "bridge.json")));
                int changes = 0;
                game.StateChanged += () => changes++;
                game.Restart();
                game.PickUp(game.wasteItems[0]);
                Require(!game.wasteItems[0].gameObject.activeSelf && game.Session.Held != null, "Pickup world sync failed.");
                game.Dispose(WasteCategory.General);
                Require(game.Session.Held != null && game.Session.IncorrectAttempts == 1, "Wrong disposal lost item.");
                game.Dispose(specs[0].Category);
                Require(game.Session.DisposedCount == 1 && !game.wasteItems[0].gameObject.activeSelf, "Dispose world sync failed.");
                game.Restart();
                Require(game.Session.DisposedCount == 0 && game.Session.TotalAttempts == 0 && game.Session.Held == null && changes == 5,
                    "Restart counters/events failed.");
                foreach (var item in game.wasteItems) Require(item.gameObject.activeSelf, "Restart did not restore world item.");
                restored = new EcoSession(specs);
                Require(new ProgressStore(Path.Combine(folder, "bridge.json")).LoadInto(restored, out message) && restored.TotalAttempts == 0,
                    "Restart did not reset saved state.");
            }
            finally { Directory.Delete(folder, true); }
            // Reopening must reuse the existing asset and preserve its GUID.
            string guid = AssetDatabase.AssetPathToGUID(scenePath);
            EcoCity.Editor.PrototypeSceneBuilder.Create();
            Require(AssetDatabase.AssetPathToGUID(scenePath) == guid, "Scene generation was not idempotent.");
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("EcoCity integration PASSED: scene references, six-item loop, JSON/backup/rejection/write failure, repeat generation. Scene: " + scenePath);
        }
        public static void RunBatch()
        {
            try { Run(); EditorApplication.Exit(0); }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }
        private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
        internal static void CheckPlayerSetupAndMovement(PrototypePlayer player)
        {
            var controller = player.GetComponent<CharacterController>();
            Require(player.viewCamera.transform.parent == player.transform, "Camera must follow the player root, not a deleted player or animated bone.");
            Require(Vector3.Distance(player.transform.lossyScale, Vector3.one) < 0.001f, "Player root scale must be one.");
            var body = player.GetComponent<Rigidbody>();
            Require(body == null || (body.isKinematic && !body.useGravity), "Dynamic Rigidbody conflicts with CharacterController.");
            foreach (var collider in player.GetComponents<Collider>())
                Require(collider == controller || !collider.enabled || collider.isTrigger, "Duplicate solid collider on player.");
            Physics.SyncTransforms();
            RaycastHit floor;
            Require(Physics.Raycast(player.transform.position + Vector3.up * 0.1f, Vector3.down, out floor, 2f,
                ~0, QueryTriggerInteraction.Ignore), "No floor under spawn.");
            Require(player.transform.position.y - floor.point.y >= -0.01f && player.transform.position.y - floor.point.y <= 0.15f,
                "Player spawns above or inside floor.");
            // The third-person camera sits behind and above the smaller character.
            float cameraLimit = player.thirdPerson ? 4.5f : 2f;
            Require(player.viewCamera.transform.position.y - floor.point.y < cameraLimit, "Camera is too high.");
            Vector3 original = player.transform.position;
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var step = typeof(PrototypePlayer).GetMethod("StepMovement", flags);
            try
            {
                for (int i = 0; i < 120; i++) step.Invoke(player, new object[] { Vector2.zero, 1f / 60 });
                Require(Vector2.Distance(new Vector2(original.x, original.z), new Vector2(player.transform.position.x, player.transform.position.z)) < 0.01f,
                    "Menu gravity caused horizontal drift.");
                Require(controller.isGrounded && Mathf.Abs(controller.bounds.min.y - floor.point.y) < 0.12f,
                    "CharacterController failed to settle on floor without Play Mode.");
                Vector3 settled = player.transform.position;
                for (int i = 0; i < 30; i++) step.Invoke(player, new object[] { Vector2.up, 1f / 60 });
                Require(Vector3.Dot(player.transform.position - settled, player.transform.forward) > 1.5f,
                    "CharacterController forward movement blocked.");
                Require(player.viewCamera.transform.position.y - player.transform.position.y < cameraLimit, "Camera detached during movement.");
                Debug.Log("EcoCity player checks PASSED: HUD references, camera parent, floor spawn, menu gravity, grounded controller and forward movement (Edit Mode; synthetic input).");
            }
            finally
            {
                controller.enabled = false;
                player.transform.position = original;
                controller.enabled = true;
                typeof(PrototypePlayer).GetField("verticalSpeed", flags).SetValue(player, 0f);
                Physics.SyncTransforms();
            }
        }
    }
}
#endif
