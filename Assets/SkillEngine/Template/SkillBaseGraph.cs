using System;
using System.Linq;
using Lomo;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using SkillEngine;
using UnityEngine;

public class SkillBaseGraph : BaseGraph {
    [ReadOnly]
    public string startNodeGUID;
    
    public StartNode startNode {
        get {
            return nodesPerGUID[startNodeGUID] as StartNode;
        }
    }
    
    protected override void OnEnable() {
        // 如果图里没有，就添加一个起始点
        if (nodes.All(x => x.GetType() != typeof(StartNode))) {
            var startNode = BaseNode.CreateFromType<StartNode>(new Vector2(0f, 0f));
            nodes.Add(startNode);
            startNodeGUID = startNode.GUID;
        }

        base.OnEnable();
    }
    
}