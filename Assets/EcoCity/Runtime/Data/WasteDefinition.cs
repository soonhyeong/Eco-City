using EcoCity.Core;
using UnityEngine;

namespace EcoCity
{
    [CreateAssetMenu(menuName = "EcoCity/Waste Definition")]
    public sealed class WasteDefinition : ScriptableObject
    {
        public string displayName;
        public WasteCategory targetBin;
        public Preparation requiredPreparation;
        public LearningLevel level;
        [TextArea] public string stateDescription;
        public bool fictionalPractice = true;
        public string sourceUrl;
        public string sourceCheckedDate;
        public string applicableRegion = "미확정: 가상 연습 규칙";
        [TextArea] public string educationalReason;
        [TextArea] public string sourceNote = "프로토타입용 가상 규칙. 실제 교육 적용 전 기준 검토 필요.";
        public WasteSpec ToSpec(string instanceId)
        {
            return new WasteSpec(instanceId, displayName, targetBin, requiredPreparation, level, educationalReason);
        }
    }
}
