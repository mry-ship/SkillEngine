using System;
using Lomo.SkillEngine;
using Sirenix.OdinInspector;
using SkillEngine;
using UnityEngine;

namespace Lomo {
    [HideInInspector]
    public struct LinearPort {
    }
    
    /// <summary>
    /// 有时序的节点
    /// </summary>
    public abstract class LinearBaseNode : BaseNode {
        [Input(name = "Start",allowMultiple = true)]
        public LinearPort start;

        [Output(name = "End")]
        public LinearPort end;
    }
    
    
}