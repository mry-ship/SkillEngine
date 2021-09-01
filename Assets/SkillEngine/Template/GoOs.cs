using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkillEngine {
    [CreateAssetMenu(fileName = "测试", menuName = "test", order = 0)]
    public class GoOs : ScriptableObject {
        public List<GOtest> go;
    }
    
    [Serializable]
    public class GOtest {
        public GameObject go;
        public int id;
    }
    

    [Serializable]
    public class SecGo : GOtest {
        public GameObject go2;
    }
}