using System.Collections.Generic;

namespace SkillEngine {
    [System.Serializable]
    public class CopyPasteHelper {
        public string sourceGraphName;
        
        public List<JsonElement> copiedNodes = new List<JsonElement>();

        public List<JsonElement> copiedGroups = new List<JsonElement>();

        public List<JsonElement> copiedEdges = new List<JsonElement>();
    }
}