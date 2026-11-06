using EcoCity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EcoCity
{
    // Picture-first HUD additions for young players: held item picture, stage progress dots and a park cleanliness bar.
    // AlphaUi still owns every text and button; this only drives images.
    public sealed class KidHud : MonoBehaviour
    {
        public AlphaFlow flow;
        public RawImage heldIcon;
        public Image cleanFill;
        public Image[] progressDots;
        [Tooltip("Parallel to icons: WasteItem.instanceId of each picture.")]
        public string[] iconIds;
        public Texture2D[] icons;
        public Color dotDone = new Color(1f, 0.78f, 0.2f);
        public Color dotTodo = new Color(0.82f, 0.82f, 0.78f);
        [Tooltip("Banners that should only show while their text has something to say.")]
        public GameObject celebrationPanel, noticePanel;
        [Tooltip("E key cap shown beside the action prompt when the target can be used.")]
        public GameObject promptKey;
        [Tooltip("Hidden while its text is empty.")]
        public GameObject tutorialPanel;
        [Tooltip("\"n / 7\" practice counter on the what-to-do card; hidden after the practice.")]
        public TMPro.TMP_Text tutorialStep;
        [Tooltip("One mark per stage (repeat in groups of three for several stage tracks): done, current, later.")]
        public Image[] stageMarks;
        public Color stageCurrent = new Color(0.3f, 0.69f, 0.31f);
        [Tooltip("Placeholder shown in the held-item window while the hands are empty.")]
        public GameObject heldEmpty;
        [Header("Next step badge: the preparation still needed, then the bin once hints name it")]
        public GameObject nextPanel;
        public RawImage nextIcon;
        public Image nextRing;
        [Tooltip("Empty, Separate, Wash.")]
        public Texture2D[] stepIcons;
        [Tooltip("One per WasteCategory, in enum order.")]
        public Texture2D[] binIcons;
        public Color nextStepColor = new Color(0.47f, 0.7f, 1f), nextBinColor = new Color(0.36f, 0.8f, 0.62f);
        [Tooltip("Seconds a save/device notice stays on screen after it changes.")]
        public float noticeSeconds = 3f;
        private string lastNotice;
        private float noticeUntil;
        private void LateUpdate()
        {
            if (flow == null) return;
            if (flow.ui != null)
            {
                // Banners sit above every screen, so keep them off menus and results.
                AlphaScreen screen = flow.Screens.Current;
                // Hidden while an overlay (mini game, ending scene) covers the park.
                ShowWhenText(celebrationPanel, flow.ui.celebrationText, screen == AlphaScreen.Playing && !flow.ui.externalOverlay);
                string notice = UiText.Get(flow.ui.noticeText);
                if (notice != lastNotice) { lastNotice = notice; noticeUntil = Time.unscaledTime + noticeSeconds; }
                ShowWhenText(noticePanel, flow.ui.noticeText, (screen == AlphaScreen.Playing || screen == AlphaScreen.Title) && !flow.ui.externalOverlay && Time.unscaledTime < noticeUntil);
                ShowWhenText(tutorialPanel, flow.ui.tutorialText, true);
            }
            if (flow.game == null || flow.game.Session == null) return;
            if (promptKey != null)
            {
                var target = flow.Screens.CanExplore && flow.interactor != null ? flow.interactor.Target : null;
                bool usable = target != null && target.GetPrompt(flow.game).StartsWith("E");
                if (promptKey.activeSelf != usable) promptKey.SetActive(usable);
            }
            EcoSession session = flow.game.Session;
            if (tutorialStep != null)
            {
                int step = TutorialText.Step(session);
                if (tutorialStep.gameObject.activeSelf != (step > 0)) tutorialStep.gameObject.SetActive(step > 0);
                string count = step + " / " + TutorialText.Steps;
                if (step > 0 && tutorialStep.text != count) tutorialStep.text = count;
            }
            if (stageMarks != null)
                for (int i = 0; i < stageMarks.Length; i++)
                {
                    if (stageMarks[i] == null) continue;
                    int stage = i % 3;
                    bool passed = session.IsComplete || stage < (int)session.CurrentLevel;
                    stageMarks[i].color = passed ? dotDone : stage == (int)session.CurrentLevel ? stageCurrent : dotTodo;
                }
            Texture2D icon = session.Held == null ? null : Find(session.Held.Id);
            if (heldIcon != null) { heldIcon.texture = icon; heldIcon.enabled = icon != null; }
            if (heldEmpty != null && heldEmpty.activeSelf != (icon == null)) heldEmpty.SetActive(icon == null);
            NextStep(session);
            if (cleanFill != null)
            {
                float amount = session.Cleanliness / 100f;
                // Rounded (sliced) bars grow by their right anchor; filled images use fillAmount.
                if (cleanFill.type == Image.Type.Filled) cleanFill.fillAmount = amount;
                else cleanFill.rectTransform.anchorMax = new Vector2(amount, 1);
            }
            if (progressDots == null) return;
            int goal = session.IsComplete ? progressDots.Length : session.Goal(session.CurrentLevel);
            int done = session.IsComplete ? goal : session.Cleared(session.CurrentLevel);
            for (int i = 0; i < progressDots.Length; i++)
            {
                if (progressDots[i] == null) continue;
                progressDots[i].gameObject.SetActive(i < goal);
                progressDots[i].color = i < done ? dotDone : dotTodo;
            }
        }
        // Same rule as the written hints: the step is always shown, the bin only after repeated mistakes with this item.
        private void NextStep(EcoSession session)
        {
            if (nextPanel == null || nextIcon == null) return;
            Texture2D picture = null; bool bin = false;
            if (session.Held != null)
            {
                Preparation step = WasteRules.NextStep(session.Held.Required & ~session.CompletedPreparation);
                int stepIndex = step == Preparation.Empty ? 0 : step == Preparation.Separate ? 1 : step == Preparation.Wash ? 2 : -1;
                if (stepIndex >= 0) picture = Pick(stepIcons, stepIndex);
                else if (flow.game.NeedsBinHint) { picture = Pick(binIcons, (int)session.Held.Category); bin = true; }
            }
            bool show = picture != null && flow.Screens.CanExplore;
            if (nextPanel.activeSelf != show) nextPanel.SetActive(show);
            if (!show) return;
            if (nextIcon.texture != picture) nextIcon.texture = picture;
            if (nextRing != null) nextRing.color = bin ? nextBinColor : nextStepColor;
        }
        private static Texture2D Pick(Texture2D[] pictures, int index) { return pictures != null && index >= 0 && index < pictures.Length ? pictures[index] : null; }
        private static void ShowWhenText(GameObject panel, Graphic label, bool allowed)
        {
            if (panel == null || label == null) return;
            string text = UiText.Get(label);
            bool show = allowed && !string.IsNullOrEmpty(text) && text.Trim().Length > 0;
            if (panel.activeSelf != show) panel.SetActive(show);
        }
        public Texture2D Find(string id)
        {
            if (iconIds == null || icons == null) return null;
            for (int i = 0; i < iconIds.Length && i < icons.Length; i++) if (iconIds[i] == id) return icons[i];
            return null;
        }
    }
}
