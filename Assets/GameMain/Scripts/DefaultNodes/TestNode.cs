using System;
using SkillEngine;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Lomo {
    [NodeMenuItem("Test"),Serializable]
    public class TestNode:LinearBaseNode {
        [Input,SerializeField]
        public string test;
        
        private int times;
        public override void Start() {
            Log.Debug(this.GUID);
            times = 0;
            if (test != null) {
                Log.Debug("[TestNode]{0}",test);
            }
        }

        public override void Update() {
            Log.Debug("GUID:{0},Update,times:{1}",GUID,times);
            if (times > 3) {
                CallWhenFinish();
            }
            times++;
        }
    }
}
