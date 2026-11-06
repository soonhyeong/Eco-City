using System;
using System.IO;
using EcoCity.Core;
using UnityEngine;

namespace EcoCity
{
    public sealed class ProgressStore
    {
        public string FilePath { get; private set; }
        private bool preserveBeforeWrite;
        [Serializable] private sealed class SaveHeader { public int schemaVersion = 0; }
        public ProgressStore(string path) { FilePath = path; }

        public bool Save(ProgressData data, out string error)
        {
            error = "";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                if (preserveBeforeWrite)
                {
                    string suffix = ".rejected-" + Guid.NewGuid().ToString("N");
                    foreach (string original in new[] { FilePath, FilePath + ".bak" })
                        if (File.Exists(original)) File.Copy(original, original + suffix, false);
                    preserveBeforeWrite = false;
                }
                string temporary = FilePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                if (!File.Exists(FilePath)) File.Move(temporary, FilePath);
                else
                {
                    try { File.Replace(temporary, FilePath, FilePath + ".bak"); }
                    catch (PlatformNotSupportedException)
                    {
                        // Portable fallback is not atomic; previous save stays in .bak.
                        File.Copy(FilePath, FilePath + ".bak", true);
                        File.Copy(temporary, FilePath, true);
                        File.Delete(temporary);
                    }
                }
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { error = e.Message; return false; }
        }

        public bool LoadInto(EcoSession session, out string notice)
        {
            notice = "";
            bool found = false;
            foreach (string path in new[] { FilePath, FilePath + ".bak" })
            {
                if (!File.Exists(path)) continue;
                found = true;
                try
                {
                    string error;
                    string json = File.ReadAllText(path);
                    SaveHeader header = JsonUtility.FromJson<SaveHeader>(json);
                    if (header == null || header.schemaVersion != 1)
                        throw new ArgumentException("Missing or unsupported save version.");
                    ProgressData data = JsonUtility.FromJson<ProgressData>(json);
                    if (session.Restore(data, out error))
                    {
                        notice = path.EndsWith(".bak", StringComparison.Ordinal) ? "이전 백업에서 진행을 복구했어요." : "저장한 진행이 있어요. 이어하기를 선택할 수 있어요.";
                        if (preserveBeforeWrite) notice += " 읽지 못한 파일은 다음 저장 전에 별도 보존해요.";
                        return true;
                    }
                    Debug.LogWarning("EcoCity save rejected: " + error);
                    preserveBeforeWrite = true;
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
                { preserveBeforeWrite = true; Debug.LogWarning("EcoCity save read failed: " + e.Message); }
            }
            if (found) notice = "저장 파일을 읽을 수 없어 새로 시작했어요. 기존 파일은 남아 있어요.";
            return false;
        }
    }
}
