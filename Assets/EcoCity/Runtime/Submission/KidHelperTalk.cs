using EcoCity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EcoCity
{
    // The helper talks when the player presses E next to it: a speech bubble pops up over the helper's head
    // with a different line each time (cheering, a fun recycling fact or a how-to tip) plus a hint for the item in hand.
    // Results of the player's own actions (picked up, correct, wrong...) show as a short toast with an O / X / ! badge instead.
    public sealed class KidHelperTalk : MonoBehaviour
    {
        public AlphaFlow flow;
        public NpcFeedbackPresenter feedback;
        public Transform helper;
        [Tooltip("Speech bubble that follows the helper's head on screen.")]
        public RectTransform bubble;
        public TMP_Text bubbleText;
        public float talkDistance = 3.2f, bubbleSeconds = 9f, headHeight = 1.75f;
        public GameObject toastPanel;
        public TMP_Text toastText, toastMark;
        public Image toastBadge;
        public float toastSeconds = 3.5f;
        public Color goodColor = new Color(0.36f, 0.8f, 0.62f), badColor = new Color(1f, 0.5f, 0.55f),
            careColor = new Color(1f, 0.66f, 0.45f), infoColor = new Color(0.47f, 0.7f, 1f);
        private static readonly string[] Cheers =
        {
            "우와, 벌써 {0}개나 치웠어! 최고야!", "천천히 해도 괜찮아. 내가 옆에서 도와줄게!", "공원이 점점 반짝반짝해지고 있어!",
            "틀려도 괜찮아. 다시 생각하면 꼭 맞힐 수 있어!", "너는 분리수거 박사가 될 것 같아!", "같이 하니까 정말 신나!",
        };
        private static readonly string[] Facts =
        {
            "알고 있니? 페트병은 비우고 라벨을 떼면 새 옷이 될 수도 있대!", "알고 있니? 캔은 녹여서 다시 새 캔으로 만들 수 있어!",
            "알고 있니? 종이 상자는 테이프를 떼고 납작하게 접으면 좋아.", "알고 있니? 음식물이 많이 묻은 종이는 재활용이 어려워.",
            "알고 있니? 유리병은 깨지지 않게 조심조심 넣어야 해.", "알고 있니? 비닐은 깨끗하게 모으면 다시 쓸 수 있어.",
        };
        private int line, cheer, fact;
        private float headTop = -1;
        private bool resultPending;
        private ResultCode pendingCode;
        private float toastUntil, bubbleUntil;
        public bool Talking { get { return bubble != null && bubble.gameObject.activeSelf; } }

        private void OnEnable()
        {
            if (flow == null || flow.game == null) return;
            flow.game.ResultPublished += OnResult; flow.game.MessagePublished += OnMessage;
            cheer = Random.Range(0, Cheers.Length); fact = Random.Range(0, Facts.Length);
        }
        private void OnDisable()
        {
            if (flow == null || flow.game == null) return;
            flow.game.ResultPublished -= OnResult; flow.game.MessagePublished -= OnMessage;
        }
        private void OnResult(ActionResult result) { resultPending = true; pendingCode = result.Code; }
        // Guide messages come from pressing E at the helper; only then does the bubble appear.
        private void OnMessage(string hint)
        {
            if (!Near() || bubbleText == null) return;
            bubbleText.text = "<color=#7B61F0>" + NextLine() + "</color>\n" + hint;
            bubbleUntil = Time.unscaledTime + bubbleSeconds;
        }
        // Cheering and a fun fact take turns, each list moving on by one, so no two talks in a row sound the same.
        private string NextLine()
        {
            line++;
            var session = flow.game.Session;
            if (line % 2 == 0) return Facts[fact++ % Facts.Length];
            return string.Format(Cheers[cheer++ % Cheers.Length], session != null ? session.DisposedCount : 0);
        }
        // Top of the helper's visible model (its root sits above the ground), measured once.
        private float HeadTop()
        {
            if (headTop >= 0) return headTop;
            var renderers = helper.GetComponentsInChildren<Renderer>();
            float top = helper.position.y + headHeight;
            bool found = false;
            foreach (var renderer in renderers)
                if (renderer.enabled && !(renderer is ParticleSystemRenderer) && renderer.GetComponent<TMP_Text>() == null)
                { top = found ? Mathf.Max(top, renderer.bounds.max.y) : renderer.bounds.max.y; found = true; }
            headTop = top + 0.15f;
            return headTop;
        }
        private bool Near()
        {
            return flow.Screens.Current == AlphaScreen.Playing && flow.game.Session != null && helper != null && flow.player != null &&
                Flat(flow.player.transform.position - helper.position) < talkDistance;
        }
        private void LateUpdate()
        {
            if (flow == null || flow.game == null) return;
            bool playing = flow.Screens.Current == AlphaScreen.Playing && flow.game.Session != null;
            bool talking = playing && Near() && Time.unscaledTime < bubbleUntil && Place();
            if (bubble != null && bubble.gameObject.activeSelf != talking) bubble.gameObject.SetActive(talking);
            if (!talking && !Near()) bubbleUntil = 0;

            if (resultPending && feedback != null)
            {
                resultPending = false;
                // The presenter has already turned the result into a sentence; show it with a badge for its kind.
                if (toastText != null) toastText.text = feedback.Message;
                Badge(pendingCode);
                toastUntil = Time.unscaledTime + toastSeconds;
            }
            bool toast = playing && !talking && Time.unscaledTime < toastUntil;
            if (toastPanel != null && toastPanel.activeSelf != toast) toastPanel.SetActive(toast);
        }
        // Keeps the bubble over the helper's head, inside the screen; false when the head is behind the camera.
        private bool Place()
        {
            var view = flow.player != null ? flow.player.viewCamera : null;
            var canvas = transform as RectTransform;
            if (bubble == null || view == null || canvas == null) return false;
            Vector3 screen = view.WorldToScreenPoint(new Vector3(helper.position.x, HeadTop(), helper.position.z));
            if (screen.z <= 0) return false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out Vector2 local);
            Vector2 half = canvas.rect.size / 2, size = bubble.rect.size;
            local.x = Mathf.Clamp(local.x, -half.x + size.x / 2 + 20, half.x - size.x / 2 - 20);
            local.y = Mathf.Clamp(local.y, -half.y + 140, half.y - size.y - 20);
            bubble.anchoredPosition = local;
            return true;
        }
        private void Badge(ResultCode code)
        {
            string mark; Color color;
            switch (code)
            {
                case ResultCode.Accepted: mark = "O"; color = goodColor; break;
                case ResultCode.WrongBin: mark = "X"; color = badColor; break;
                case ResultCode.PickedUp: case ResultCode.Prepared: mark = "★"; color = infoColor; break;
                default: mark = "!"; color = careColor; break;
            }
            if (toastMark != null) toastMark.text = mark;
            if (toastBadge != null) toastBadge.color = color;
        }
        private static float Flat(Vector3 offset) { offset.y = 0; return offset.magnitude; }
    }
}
