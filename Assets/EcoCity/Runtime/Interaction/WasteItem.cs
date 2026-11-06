using EcoCity.Core;
using UnityEngine;

namespace EcoCity
{
    public sealed class WasteItem : Interactable
    {
        [Tooltip("Unique for each placed instance, including copies of the same item.")]
        public string instanceId;
        public WasteDefinition definition;
        public override string GetPrompt(EcoGame game)
        {
            if (definition == null) return "데이터 연결 필요";
            WasteSpec spec = definition.ToSpec(instanceId);
            if (!game.Session.IsUnlocked(spec)) return definition.displayName + " · 다음 단계에서 주워요";
            return "E  " + definition.displayName + " 줍기";
        }
        public override void Interact(EcoGame game) { game.PickUp(this); }
    }
}
