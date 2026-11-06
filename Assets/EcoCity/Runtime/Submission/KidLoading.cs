using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoCity
{
    // A short, friendly loading screen between scenes: the leafy card fades in, shows one recycling tip and a spinning star,
    // the next scene loads behind it, then it fades out. It lives on from the title scene; without it scenes load directly.
    public sealed class KidLoading : MonoBehaviour
    {
        public static KidLoading Instance { get; private set; }
        public CanvasGroup group;
        public TMP_Text tipText;
        public RectTransform spinner;
        public float minimumSeconds = 1.1f, fadeSeconds = 0.25f;
        public string[] tips =
        {
            "페트병의 비닐 라벨은 떼어서 따로 버려요.",
            "캔과 유리병은 속을 비우고 물로 헹궈요.",
            "택배 상자의 테이프와 송장은 떼어 내요.",
            "먹고 남은 음식은 음식물 통에 모아요.",
            "기름이 묻은 종이는 종이로 다시 쓰기 어려워요.",
            "깨끗하게 버리면 다시 쓸 수 있는 자원이 돼요.",
            "모르면 통 위 그림을 보고 같은 통을 찾아요.",
            "집에서는 어른과 함께 우리 동네 분리배출 방법을 확인해요.",
        };
        private bool busy;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            if (group != null) { group.alpha = 0; group.blocksRaycasts = false; group.gameObject.SetActive(false); }
        }
        /// <summary>Loads a scene behind the loading card (or directly when no loading screen exists).</summary>
        public static void Load(string scene)
        {
            if (Instance == null || Instance.group == null) { SceneManager.LoadScene(scene); return; }
            if (!Instance.busy) Instance.StartCoroutine(Instance.Run(scene));
        }
        private IEnumerator Run(string scene)
        {
            busy = true;
            if (tipText != null && tips.Length > 0) tipText.text = tips[Random.Range(0, tips.Length)];
            group.gameObject.SetActive(true); group.blocksRaycasts = true;
            yield return Fade(0, 1);
            float started = Time.unscaledTime;
            var loading = SceneManager.LoadSceneAsync(scene);
            while (!loading.isDone || Time.unscaledTime - started < minimumSeconds) { Spin(); yield return null; }
            yield return Fade(1, 0);
            group.blocksRaycasts = false; group.gameObject.SetActive(false);
            busy = false;
        }
        private IEnumerator Fade(float from, float to)
        {
            for (float t = 0; t < fadeSeconds; t += Time.unscaledDeltaTime) { group.alpha = Mathf.Lerp(from, to, t / fadeSeconds); Spin(); yield return null; }
            group.alpha = to;
        }
        private void Spin() { if (spinner != null) spinner.localRotation = Quaternion.Euler(0, 0, -Time.unscaledTime * 180); }
    }
}
