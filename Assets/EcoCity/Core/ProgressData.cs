using System;
using System.Collections.Generic;

namespace EcoCity.Core
{
    [Serializable]
    public sealed class AttemptRecord
    {
        public string itemId;
        public WasteCategory selectedBin;
        public ResultCode result;
        public string utc;
    }

    // Public fields are intentional: compatible with Unity JsonUtility.
    [Serializable]
    public sealed class ProgressData
    {
        public int schemaVersion = 1;
        public List<string> disposedIds = new List<string>();
        public string heldId = "";
        public Preparation heldPreparation;
        public int totalAttempts;
        public int incorrectAttempts;
        // Additive v1 extension. Revision 0 is a legacy prototype save.
        public int tutorialRevision;
        public TutorialAction tutorialActions;
        public bool tutorialSkipped;
        public List<AttemptRecord> recentAttempts = new List<AttemptRecord>();
    }
}
