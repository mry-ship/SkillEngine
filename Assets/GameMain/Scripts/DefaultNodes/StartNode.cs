using SkillEngine;

namespace Lomo {
    public class StartNode : BaseNode{
        [Output(name = "GraphStartPoint")]
        public LinearPort startPoint;
    }
}
