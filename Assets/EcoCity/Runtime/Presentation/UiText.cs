using TMPro;
using UnityEngine.UI;

namespace EcoCity
{
    // UI labels may be legacy uGUI Text (alpha/prototype scenes) or TextMeshPro (crisp SDF text in the submission scene).
    public static class UiText
    {
        public static string Get(Graphic label)
        {
            var legacy = label as Text; if (legacy != null) return legacy.text;
            var tmp = label as TMP_Text; return tmp != null ? tmp.text : null;
        }
        public static void Set(Graphic label, string value)
        {
            var legacy = label as Text;
            if (legacy != null) { if (legacy.text != value) legacy.text = value; return; }
            var tmp = label as TMP_Text;
            if (tmp != null && tmp.text != value) tmp.text = value;
        }
    }
}
