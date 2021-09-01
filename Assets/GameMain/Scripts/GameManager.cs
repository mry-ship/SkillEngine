using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lomo {
    public class GameManager : MonoBehaviour {
        public SkillBaseGraph graph;
        
        private void Start() {
            Debug.Log(graph.nodesPerGUID.Count);
        }
        
    }
}