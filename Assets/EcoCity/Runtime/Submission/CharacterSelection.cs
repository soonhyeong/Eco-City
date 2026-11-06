using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EcoCity
{
    // Boy/girl choice on the title screen. Only the visual child and its Animator change;
    // movement, interaction and save data stay on the shared player root.
    public sealed class CharacterSelection : MonoBehaviour
    {
        public const string PreferenceKey = "EcoCity.Character";
        public PlayerAnimationView animationView;
        public GameObject[] characters;
        public Animator[] animators;
        public Button[] buttons;
        public Image[] frames;
        public Color selectedColor = new Color(1f, 0.78f, 0.2f);
        public Color idleColor = new Color(1f, 1f, 1f, 0.85f);
        public int Current { get; private set; }
        private readonly List<KeyValuePair<Button, UnityAction>> listeners = new List<KeyValuePair<Button, UnityAction>>();

        private void OnEnable()
        {
            Unbind();
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                int index = i;
                UnityAction action = () => Select(index);
                buttons[i].onClick.AddListener(action); listeners.Add(new KeyValuePair<Button, UnityAction>(buttons[i], action));
            }
            Select(PlayerPrefs.GetInt(PreferenceKey, 0), false);
        }
        private void OnDisable() { Unbind(); }
        private void Unbind()
        {
            foreach (var listener in listeners) if (listener.Key != null) listener.Key.onClick.RemoveListener(listener.Value);
            listeners.Clear();
        }
        public void Select(int index) { Select(index, true); }
        public void Select(int index, bool remember)
        {
            if (characters == null || characters.Length == 0) return;
            Current = Mathf.Clamp(index, 0, characters.Length - 1);
            for (int i = 0; i < characters.Length; i++)
                if (characters[i] != null) characters[i].SetActive(i == Current);
            if (animationView != null && animators != null && Current < animators.Length) animationView.animator = animators[Current];
            if (frames != null)
                for (int i = 0; i < frames.Length; i++)
                    if (frames[i] != null) frames[i].color = i == Current ? selectedColor : idleColor;
            if (remember) { PlayerPrefs.SetInt(PreferenceKey, Current); PlayerPrefs.Save(); }
        }
    }
}
