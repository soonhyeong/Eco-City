using EcoCity.Core;

namespace EcoCity
{
    public static class FeedbackText
    {
        public static string Category(WasteCategory category)
        {
            switch (category)
            {
                case WasteCategory.Paper: return "종이";
                case WasteCategory.Plastic: return "플라스틱";
                case WasteCategory.Metal: return "캔";
                case WasteCategory.Glass: return "유리";
                case WasteCategory.Vinyl: return "비닐";
                case WasteCategory.Food: return "음식물";
                default: return "일반";
            }
        }
        public static string Step(Preparation step)
        {
            switch (step)
            {
                case Preparation.Empty: return "비우기";
                case Preparation.Separate: return "떼어내기";
                case Preparation.Wash: return "씻기";
                default: return "할 일 없음";
            }
        }
        /// <summary>Optional names per level (the submission scene names its three regions); null keeps the default wording.</summary>
        public static string[] LevelNames;
        public static string Level(LearningLevel level)
        {
            if (LevelNames != null && (int)level < LevelNames.Length && !string.IsNullOrEmpty(LevelNames[(int)level])) return LevelNames[(int)level];
            return level == LearningLevel.Basic ? "1단계 · 깨끗한 쓰레기" : level == LearningLevel.State ? "2단계 · 비우고 씻기" : "3단계 · 떼어내고 버리기";
        }
        // Short, friendly sentences: young players read one line at most.
        public static string Build(ActionResult result, bool detailed)
        {
            string name = result.Item == null ? "쓰레기" : result.Item.Name;
            string text;
            switch (result.Code)
            {
                case ResultCode.PickedUp: text = name + " 주웠어요!"; break;
                case ResultCode.Prepared: text = Step(result.Step) + " 끝! 잘했어요."; break;
                case ResultCode.Accepted: text = "딩동댕! 맞는 통에 쏙 넣었어요."; break;
                case ResultCode.EmptyHands: return "먼저 쓰레기를 하나 주워요.";
                case ResultCode.HandsFull: return "손에 든 쓰레기를 먼저 통에 넣어요.";
                case ResultCode.Locked: return "아직이에요. 앞 단계를 먼저 끝내요.";
                case ResultCode.AlreadyCollected: return "이미 치운 쓰레기예요.";
                case ResultCode.MissingPreparation:
                    text = "잠깐! 먼저 '" + Step(result.Step) + "'를 해요."; break;
                case ResultCode.WrongBin:
                    text = "앗, 이 통이 아니에요. 손에 든 물건과 통 위 그림을 비교해 봐요."; break;
                case ResultCode.NotRequired: return "이 쓰레기는 그 일을 안 해도 돼요.";
                case ResultCode.AlreadyPrepared: return "벌써 했어요!";
                case ResultCode.PrerequisiteMissing: return "먼저 '" + Step(result.Step) + "'부터 해요.";
                case ResultCode.SessionComplete: return "에코시티 청소 끝! 결과를 봐요.";
                default: return "쓰레기를 알 수 없어요.";
            }
            if (detailed && result.Item != null)
            {
                // After success this confirms the answer; after repeated mistakes it is the hint.
                text += result.Code == ResultCode.Accepted
                    ? "\n" + Category(result.Item.Category) + " 통이 맞아요!"
                    : "\n힌트: " + Category(result.Item.Category) + " 통에 넣어요.";
                if (result.Code != ResultCode.Accepted && !string.IsNullOrEmpty(result.Item.Reason)) text += " " + result.Item.Reason;
            }
            return text;
        }
    }
}
