#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EcoCity.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EcoCity.Validation
{
    // Inspects existing files only. Never calls a scene builder or saves an inspected scene.
    public static class ProjectAuditChecks
    {
        public static void RunBatch()
        {
            int failures = 0;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode only.");
                string actualGame = "Assets/1. Scene/EcoCitySubmission.unity";
                string[] paths = { Editor.SubmissionTitleBuilder.ScenePath, Editor.SubmissionCharacterBuilder.ScenePath, actualGame,
                    Editor.AlphaSceneBuilder.ScenePath, "Assets/EcoCity/Generated/Submission/Prototype/EcoCityPrototype.unity" };
                if (!File.Exists(Editor.SubmissionSceneBuilder.ScenePath))
                {
                    failures++;
                    Debug.LogError("AUDIT: configured submission builder path is absent: " + Editor.SubmissionSceneBuilder.ScenePath);
                }
                var enabled = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                if (!enabled.SequenceEqual(paths.Take(3)))
                {
                    failures++;
                    Debug.LogError("AUDIT: enabled scene order differs from title/character/game: " + string.Join(", ", enabled));
                }
                foreach (string path in paths)
                {
                    if (!File.Exists(path)) { failures++; Debug.LogError("AUDIT: missing scene " + path); continue; }
                    var scene = EditorSceneManager.OpenScene(path);
                    int missing = 0, broken = 0, components = 0;
                    foreach (var root in scene.GetRootGameObjects())
                        foreach (var child in root.GetComponentsInChildren<Transform>(true))
                        {
                            missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
                            foreach (var component in child.GetComponents<Component>())
                            {
                                if (component == null) continue;
                                components++;
                                var property = new SerializedObject(component).GetIterator();
                                while (property.Next(true))
                                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                                        property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                                    {
                                        broken++;
                                        if (broken <= 5) Debug.LogError("AUDIT: broken reference " + child.name + "." + property.propertyPath);
                                    }
                            }
                        }
                    failures += missing + broken;
                    Debug.Log("AUDIT SCENE: " + path + "; components=" + components + "; missingScripts=" + missing + "; brokenReferences=" + broken);
                    if (path != actualGame) continue;
                    var game = UnityEngine.Object.FindFirstObjectByType<EcoGame>();
                    if (game == null) { failures++; Debug.LogError("AUDIT: game missing."); continue; }
                    var specs = new List<WasteSpec>();
                    foreach (var item in game.wasteItems) specs.Add(item.definition.ToSpec(item.instanceId));
                    var session = new EcoSession(specs);
                    int completed = 0;
                    foreach (var spec in specs.OrderBy(s => s.Level))
                    {
                        if (!session.PickUp(spec.Id).IsSuccess) throw new Exception("Cannot pick up " + spec.Id);
                        foreach (Preparation step in new[] { Preparation.Empty, Preparation.Separate, Preparation.Wash })
                            if ((spec.Required & step) != 0 && !session.Prepare(step).IsSuccess) throw new Exception("Cannot prepare " + spec.Id);
                        if (!session.Dispose(spec.Category).IsSuccess) throw new Exception("Cannot dispose " + spec.Id);
                        completed++;
                    }
                    if (!session.IsComplete) throw new Exception("Actual scene catalog cannot finish.");
                    Debug.Log("AUDIT CATALOG PASSED: " + completed + " items; stages=" + string.Join("/", specs.GroupBy(s => s.Level).OrderBy(g => g.Key).Select(g => g.Count())) + "; score=" + session.Score);
                    // Reproduce an old six-item save in memory; never touch the user's saved files.
                    var oldIds = new[] { "paper-01", "plastic-01", "metal-01", "container-01", "cup-01", "composite-01" };
                    var oldSpecs = oldIds.Select(id => AssetDatabase.LoadAssetAtPath<WasteDefinition>(
                        "Assets/EcoCity/Generated/Submission/Prototype/" + id + ".asset").ToSpec(id)).ToArray();
                    var oldSession = new EcoSession(oldSpecs);
                    foreach (var spec in oldSpecs.OrderBy(s => s.Level))
                    {
                        oldSession.PickUp(spec.Id);
                        foreach (Preparation step in new[] { Preparation.Empty, Preparation.Separate, Preparation.Wash })
                            if ((spec.Required & step) != 0) oldSession.Prepare(step);
                        oldSession.Dispose(spec.Category);
                    }
                    string restoreError;
                    bool restored = new EcoSession(specs).Restore(oldSession.Capture(), out restoreError);
                    Debug.Log("AUDIT LEGACY SAVE: oldComplete=" + oldSession.IsComplete + "; restoredIntoCurrent=" + restored + "; reason=" + restoreError);
                    if (!restored) failures++;
                }
            }
            catch (Exception error) { failures++; Debug.LogException(error); }
            Debug.Log("AUDIT COMPLETE: failures=" + failures + "; No Play Mode; no scene files saved.");
            EditorApplication.Exit(failures == 0 ? 0 : 1);
        }
    }
}
#endif
