using EcoCity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EcoCity
{
    // 분리배출 도감: a sticker for every kind of trash the player has put in the right bin, kept across games.
    // Stickers not met yet show a dark shape and "?"; the pause menu button gets a dot when a new sticker arrives.
    public sealed class KidStickerBook : MonoBehaviour
    {
        public const string Key = "EcoCity.Kid.Stickers";
        public AlphaFlow flow;
        public GameObject panel, newDot;
        public Button open, close;
        public string[] ids;
        public RawImage[] pictures;
        public GameObject[] lockedMarks, details;
        public TMP_Text countText;
        public AudioSource sound;
        public AudioClip stickerClip;
        private string held;
        private bool fresh;

        private void Awake()
        {
            if (open != null) open.onClick.AddListener(Open);
            if (close != null) close.onClick.AddListener(Close);
            if (panel != null) panel.SetActive(false);
        }
        private void OnEnable() { if (flow != null && flow.game != null) flow.game.ResultPublished += OnResult; }
        private void OnDisable() { if (flow != null && flow.game != null) flow.game.ResultPublished -= OnResult; }
        private void Update()
        {
            if (newDot != null && newDot.activeSelf != fresh) newDot.SetActive(fresh);
            if (panel != null && panel.activeSelf && flow.Screens.Current != AlphaScreen.Paused) panel.SetActive(false);
        }
        public static bool Has(string id) { return ("," + PlayerPrefs.GetString(Key, "") + ",").Contains("," + id + ","); }
        private void OnResult(ActionResult result)
        {
            var session = flow.game.Session;
            if (result.Code == ResultCode.PickedUp && session.Held != null) held = session.Held.Id;
            if (result.Code != ResultCode.Accepted || string.IsNullOrEmpty(held)) return;
            if (!Has(held))
            {
                string list = PlayerPrefs.GetString(Key, "");
                PlayerPrefs.SetString(Key, list.Length == 0 ? held : list + "," + held); PlayerPrefs.Save();
                fresh = true;
            }
            held = null;
        }
        public void Open()
        {
            if (panel == null || flow.Screens.Current != AlphaScreen.Paused) return;
            Refresh();
            panel.SetActive(true); panel.transform.SetAsLastSibling();
            fresh = false;
            if (sound != null && stickerClip != null) sound.PlayOneShot(stickerClip);
        }
        public void Close() { if (panel != null) panel.SetActive(false); }
        private void Refresh()
        {
            int found = 0;
            for (int i = 0; i < ids.Length; i++)
            {
                bool has = Has(ids[i]); if (has) found++;
                if (pictures[i] != null) pictures[i].color = has ? Color.white : new Color(0.22f, 0.2f, 0.3f, 0.9f);
                if (lockedMarks[i] != null) lockedMarks[i].SetActive(!has);
                if (details[i] != null) details[i].SetActive(has);
            }
            if (countText != null) countText.text = "모은 스티커 " + found + " / " + ids.Length;
        }
    }
}
