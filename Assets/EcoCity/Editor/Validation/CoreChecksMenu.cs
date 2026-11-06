#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EcoCity.Validation
{
    public static class CoreChecksMenu
    {
        [MenuItem("EcoCity/Run Core Checks (No Play Mode)")]
        public static void Run()
        {
            string[] failures = CoreChecks.RunAll();
            foreach (string failure in failures) Debug.LogError("EcoCity check failed: " + failure);
            string summary = "EcoCity core checks: " + (CoreChecks.CheckCount - failures.Length) + "/" + CoreChecks.CheckCount + " passed";
            if (failures.Length == 0) Debug.Log(summary);
            else Debug.LogError(summary);
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("EcoCity Core Checks", summary, "OK");
        }
        public static void RunBatch()
        {
            string[] failures = CoreChecks.RunAll();
            foreach (string failure in failures) Debug.LogError(failure);
            Debug.Log("EcoCity checks: " + CoreChecks.CheckCount + ", failures: " + failures.Length);
            EditorApplication.Exit(failures.Length == 0 ? 0 : 1);
        }
    }
}
#endif
