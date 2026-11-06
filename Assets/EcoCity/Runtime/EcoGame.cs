using System;
using System.Collections.Generic;
using System.IO;
using EcoCity.Core;
using UnityEngine;

namespace EcoCity
{
    [DefaultExecutionOrder(-100)]
    public sealed class EcoGame : MonoBehaviour
    {
        public WasteItem[] wasteItems;
        [Tooltip("Use a different slot for each test scene to avoid mixing catalogs.")]
        public string saveSlot = "prototype-v01";
        public bool waitForStart;
        public bool HasSavedProgress { get; private set; }
        private bool savingEnabled = true;
        public EcoSession Session { get; private set; }
        public string StorageNotice { get; private set; }
        public event Action StateChanged;
        public event Action<ActionResult> ResultPublished;
        public event Action<string> MessagePublished;
        private List<WasteSpec> specs;
        private ProgressStore store;

        private void Awake()
        { Initialize(); }

        // Explicit initialization also permits isolated Edit Mode checks with a temporary path.
        public void Initialize(string storagePath = null)
        {
            try
            {
                specs = new List<WasteSpec>();
                foreach (WasteItem item in wasteItems)
                {
                    if (item == null || item.definition == null) throw new ArgumentException("Missing waste item or definition.");
                    specs.Add(item.definition.ToSpec(item.instanceId));
                }
                Session = new EcoSession(specs);
                string safeSlot = string.IsNullOrEmpty(saveSlot) ? "prototype-v01" : saveSlot;
                foreach (char c in Path.GetInvalidFileNameChars()) safeSlot = safeSlot.Replace(c, '_');
                savingEnabled = !waitForStart;
                store = new ProgressStore(storagePath ?? Path.Combine(Application.persistentDataPath, "EcoCity", safeSlot + ".json"));
                string notice;
                HasSavedProgress = store.LoadInto(Session, out notice);
                StorageNotice = notice;
                SyncWorld();
            }
            catch (Exception e)
            {
                Debug.LogError("EcoCity initialization failed: " + e.Message, this);
                Session = null;
                StorageNotice = "초기화 실패: Console에서 쓰레기 ID와 데이터 연결을 확인하세요.";
                enabled = false;
            }
        }

        public void PickUp(WasteItem item) { if (Session != null && item != null) Apply(Session.PickUp(item.instanceId)); }
        public void Prepare(Preparation step) { if (Session != null) Apply(Session.Prepare(step)); }
        public void Dispose(WasteCategory category) { if (Session != null) Apply(Session.Dispose(category)); }
        // Wrong bins for the item in hand only, so earlier mistakes never reveal the next item's bin.
        public int HeldMistakes { get; private set; }
        public bool NeedsBinHint { get { return Session != null && Session.Held != null && HeldMistakes >= 2; } }
        private void Apply(ActionResult result)
        {
            if (result.Code == ResultCode.PickedUp || result.Code == ResultCode.Accepted) HeldMistakes = 0;
            if (result.Code == ResultCode.WrongBin) HeldMistakes++;
            // Picking something up means the player already moved, looked around and found it; never leave the guide behind.
            if (result.Code == ResultCode.PickedUp) Session.ObserveTutorial(TutorialAction.Move | TutorialAction.Look | TutorialAction.Discover | TutorialAction.PickUp);
            if (result.Code == ResultCode.Prepared && (Session.Held.Required & ~Session.CompletedPreparation) == Preparation.None)
                Session.ObserveTutorial(TutorialAction.Prepare);
            if (result.Code == ResultCode.Accepted) Session.ObserveTutorial(TutorialAction.Dispose);
            SyncWorld();
            if (result.IsSuccess || result.Code == ResultCode.WrongBin || result.Code == ResultCode.MissingPreparation) Save();
            if (StateChanged != null) StateChanged();
            if (ResultPublished != null) ResultPublished(result);
        }
        private void SyncWorld()
        {
            if (Session == null) return;
            foreach (WasteItem item in wasteItems)
                if (item != null) item.gameObject.SetActive(!Session.IsUnavailableInWorld(item.instanceId));
        }
        public void Save()
        {
            if (Session == null || store == null || !savingEnabled) return;
            string error;
            bool saved = store.Save(Session.Capture(), out error);
            if (saved) HasSavedProgress = true;
            StorageNotice = saved ? "진행이 저장되었어요." : "저장하지 못했어요. 지금 연습은 계속할 수 있어요.";
            if (!saved) Debug.LogWarning("EcoCity progress save failed: " + error, this);
        }
        public void EnableSaving() { savingEnabled = true; }
        public void ObserveTutorial(TutorialAction action)
        {
            if (Session == null || !Session.ObserveTutorial(action)) return;
            Save();
            if (StateChanged != null) StateChanged();
        }
        public void SkipTutorial()
        {
            if (Session == null) return;
            Session.SkipTutorial(); Save();
            if (StateChanged != null) StateChanged();
        }
        public void Restart()
        {
            if (specs == null) return;
            savingEnabled = true;
            Session = new EcoSession(specs); HeldMistakes = 0;
            SyncWorld(); Save();
            if (StateChanged != null) StateChanged();
            Announce("새로 시작해요! 반짝이는 ! 쓰레기부터 찾아봐요.");
        }
        public void AskGuide(bool requestedByPlayer = false)
        {
            if (Session == null) return;
            if (requestedByPlayer) ObserveTutorial(TutorialAction.Guide);
            if (Session.IsComplete) { Announce("모든 연습을 마쳤어요! 다음에는 생활 속에서 배출 기준을 확인해 보세요."); return; }
            if (Session.Held == null) { Announce(FeedbackText.Level(Session.CurrentLevel) + "\n반짝이는 ! 쓰레기를 찾아 E로 주워요."); return; }
            Preparation next = WasteRules.NextStep(Session.Held.Required & ~Session.CompletedPreparation);
            // The bin is named only after repeated mistakes with this item; before that the helper points to the pictures.
            if (next != Preparation.None) Announce("먼저 '" + FeedbackText.Step(next) + "'를 해요. " + Session.Held.Reason);
            else if (NeedsBinHint) Announce("힌트: " + FeedbackText.Category(Session.Held.Category) + " 통에 넣어요. " + Session.Held.Reason);
            else Announce("손에 든 물건과 닮은 그림이 있는 통을 찾아봐요. 틀려도 괜찮아요!");
        }
        private void Announce(string text) { if (MessagePublished != null) MessagePublished(text); }
        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void OnApplicationQuit() { Save(); }
    }
}
