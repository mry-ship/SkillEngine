using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkillEngine {
    [Serializable]
    public class ExposedParameterWorkaround : ScriptableObject
    {
        [SerializeReference]
        public List<ExposedParameter>   parameters = new List<ExposedParameter>();
        public BaseGraph                graph;
    }
}