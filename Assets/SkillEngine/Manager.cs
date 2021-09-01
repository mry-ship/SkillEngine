using System;
using System.Collections.Generic;
using System.Linq;
using SkillEngine;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Lomo.SkillEngine {
    public class Skill : ISkillContext {
        private SkillBaseGraph graph;
        private List<string> timelineNodeList = new List<string>();
        private Action<Skill> onFinish;
        private List<BaseNode> runningNodes = new List<BaseNode>();
        private List<BaseNode> readyToRunNodes = new List<BaseNode>();
        private Dictionary<BaseNode, Owner> nodeDic = new Dictionary<BaseNode, Owner>();
        // 等待所有连接的节点都完成才做，和只要连接Start的任意一个节点完成，就开始做，两种情况

        private int frame;

        public string name {
            get { return graph.name; }
        }

        private enum Owner {
            Running,
            ReadyToRun
        }

        public Skill(SkillBaseGraph graph) {
            this.graph = graph;
            // TravelAllNodes();
        }

        private void TravelAllNodes() {
            var queue = new Queue<string>();
            queue.Enqueue(graph.startNodeGUID);
            while (queue.Count > 0) {
                var curNodeGUID = queue.Dequeue();
                timelineNodeList.Add(curNodeGUID);
                // 广度优先遍历，同一个深度的一起加入到List中
                var edges = graph.GetOutputEdges(curNodeGUID);

                foreach (var edge in edges) {
                    queue.Enqueue(edge.inputNodeGUID);
                }
            }

            Log.Debug("[Skill]SkillName:{0}. TravelAllNodes Over.", graph.name);
        }

        public void Start() {
            frame = 0;
            Log.Debug("[Skill.Start] Start");
            var startNode = graph.startNode;
            var nextNodes = startNode.Walk("GraphStartPoint");
            // 运行这些节点的Start方法，如果没有完成，就将它们放如需要Update的节点数组中，调用节点的Update方法，
            foreach (var item in nextNodes) {
                item.SetCtx(this);
                if (item is LinearBaseNode nextNode) {
                    runningNodes.Add(nextNode);
                    nodeDic.Add(nextNode, Owner.Running);
                    InitNode(nextNode);
                    nextNode?.Start();
                }
            }
        }

        /// <summary>
        /// 只要不存在Update的，都在一帧内做完，只有做到了一帧无法做完的Node，才会进Update
        /// 我希望的是当帧做完，就在当前帧后续部分把剩下的节点做完，直到碰到下一个需要Update的Node
        /// </summary>
        public void Update() {
            Log.Debug("[Skill.Update] frame:{0}", frame);
            for (int i = runningNodes.Count - 1; i >= 0; i--) {
                if (runningNodes[i] is LinearBaseNode node) {
                    node.Update();
                }
            }

            foreach (var node in readyToRunNodes) {
                runningNodes.Add(node);
                nodeDic[node] = Owner.Running;
            }

            readyToRunNodes.Clear();
            frame += 1;
            //当全部节点运行完了，表示图执行完毕
            if (runningNodes.Count == 0) {
                onFinish?.Invoke(this);
            }
        }

        public Skill OnFinish(Action<Skill> fn) {
            onFinish += fn;
            return this;
        }

        /// <summary>
        /// 设置节点的变量
        /// </summary>
        /// <param name="node"></param>
        private void InitNode(BaseNode node) {
            // 所有不是时序Port的Port都要检测一下有没有输入
            foreach (var inputPort in node.inputPorts) {
                var port = inputPort.Value;
                // 时序Port不需要管
                if(port.portData.displayType == typeof(LinearPort)) continue;
                
                // 非时序的Port没有连接也不用管
                if(!node.IsInputPortConnected(inputPort.Key)) continue;
                
                // 连上了就要去拿数据，可能是从上一个时序节点中拿，也可能是从非时序节点中拿
                GetInput(port);
            }
        }

        private void GetInput(NodePort port) {
            // 试试使用filedinfo
            // port.fieldInfo.SetValue(port.owner,"");
            var edge = port.GetEdges()[0];
            // 非时序节点只允许一个边，这是大前提
            // 如果输入节点是时序节点，直接拿
            var outputNode = edge.outputNode;
            if (outputNode is LinearBaseNode) {
                return;
            }
            
            // 如果不是时序节点
            // 用栈来做，广度优先遍历
            var v = outputNode as ParameterNode;
            port.fieldInfo.SetValue(port.owner,v?.output);
        }
        
        private void SetValue(BaseEdge edge) {
            var outputPort = edge.outputPort;
            var inputPort = edge.inputPort;
            edge.inputNode.inputValues[inputPort.fieldName].value= edge.outputNode.outputValues[outputPort.fieldName].value;
        }

        private void Walk(BaseNode node) {
            // 找到当前node的下一个节点，然后运行它的Start方法
            var nextNodes = node.Walk("End");
            foreach (var item in nextNodes) {
                item.SetCtx(this);
                if (item is LinearBaseNode nextNode) {
                    readyToRunNodes.Add(nextNode);
                    nodeDic.Add(nextNode, Owner.ReadyToRun);
                    nextNode?.Start();
                }
            }
        }

        /// <summary>
        /// 当Node完成时，Walk它，将接下来的节点放入list中
        /// 应该是把一连串的不需要Update的节点都走完
        /// </summary>
        /// <param name="node"></param>
        public void CallWhenFinish(BaseNode node) {
            switch (nodeDic[node]) {
                case Owner.Running:
                    runningNodes.Remove(node);
                    break;
                case Owner.ReadyToRun:
                    readyToRunNodes.Remove(node);
                    break;
            }

            // 继续走下去
            Walk(node);
        }
    }

    public class Manager : MonoBehaviour {
        public static Manager I;

        private List<Skill> runningSkills = new List<Skill>();

        private void Awake() {
            I = this;
        }

        /// <summary>
        /// 那一帧调用该方法时
        /// 执行顺序：skill.Start ==> Manager.Update ==>Skill.Update（在这里会判断图有没有完成） ==> 图完成了就会Invoke Finish ==> Manager.OnSkillFinish
        /// </summary>
        /// <param name="skillGraph"></param>
        /// <returns></returns>
        public Skill StartSkill(SkillBaseGraph skillGraph) {
            Log.Debug("[SkillEngine.StartFlow] name:{0}", skillGraph.name);
            var skill = new Skill(skillGraph);
            runningSkills.Add(skill);
            skill.OnFinish(OnSkillFinish);
            skill.Start();
            // 图完成后执行
            return skill;
        }

        private void OnSkillFinish(Skill skill) {
            Log.Debug("[SkillEngine.OnFlowFinish] name:{0}", skill.name);
            runningSkills.Remove(skill);
        }

        private void Update() {
            for (var i = 0; i < runningSkills.Count; i++) {
                var skill = runningSkills[i];
                skill.Update();
            }
        }
    }
}