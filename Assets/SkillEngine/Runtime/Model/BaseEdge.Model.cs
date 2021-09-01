using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SkillEngine {
    [Serializable]
    public partial class BaseEdge:ISerializationCallbackReceiver{
        [HideInInspector]
        public string GUID;

        [SerializeField]
        public string inputNodeGUID;

        [SerializeField]
        public string outputNodeGUID;
        
        // 在graph Enable时，赋值了
        [NonSerialized]
        public BaseGraph owner;

        /// <summary>
        /// 得到输入的接口，一般是连线的右节点
        /// </summary>
        [NonSerialized]
        public NodePort inputPort;
        
        /// <summary>
        /// 进行输出的接口，一般是连线的左节点
        /// </summary>
        [NonSerialized]
        public NodePort outputPort;

        [NonSerialized]
        public BaseNode inputNode;

        [NonSerialized]
        public BaseNode outputNode;

        [HideInInspector]
        public string inputFieldName;
        
        [HideInInspector]
        public bool inputPortAcceptMultipleEdges;

        [HideInInspector]
        public string outputFieldName;
        
        [HideInInspector]
        public bool outputPortacceptMultipleEdges;


        // Use to store the id of the field that generate multiple ports
        [HideInInspector]
        public string inputPortIdentifier;

        [HideInInspector]
        public string outputPortIdentifier;

        public static BaseEdge CreateNewEdge(BaseGraph graph,NodePort inputPort,NodePort outputPort) {
            BaseEdge edge = new BaseEdge();
            edge.owner = graph;
            edge.GUID = System.Guid.NewGuid().ToString();
            
            edge.inputNode = inputPort.owner;
            edge.inputFieldName = inputPort.fieldName;
            edge.inputPort = inputPort;
            edge.inputPortIdentifier = inputPort.portData.identifier;
            edge.inputNodeGUID = edge.inputNode.GUID;
            edge.inputPortAcceptMultipleEdges = inputPort.portData.acceptMultipleEdges;

            edge.outputNode = outputPort.owner;
            edge.outputFieldName = outputPort.fieldName;
            edge.outputPort = outputPort;
            edge.outputPortIdentifier = outputPort.portData.identifier;
            edge.outputNodeGUID = edge.outputNode.GUID;
            edge.outputPortacceptMultipleEdges = outputPort.portData.acceptMultipleEdges;
            
            return edge;
        }
        
        /// <summary>
        /// 将所有NonSerialized的数据构造出来
        /// </summary>
        public void Deserialize()
        {
            if (!owner.nodesPerGUID.ContainsKey(outputNodeGUID) || !owner.nodesPerGUID.ContainsKey(inputNodeGUID))
                return ;

            outputNode = owner.nodesPerGUID[outputNodeGUID];
            inputNode = owner.nodesPerGUID[inputNodeGUID];
            inputPort = inputNode.GetPort(inputFieldName, inputPortIdentifier);
            outputPort = outputNode.GetPort(outputFieldName, outputPortIdentifier);
        }

        public override string ToString() => $"{outputNode.name}:{outputPort.fieldName} -> {inputNode.name}:{inputPort.fieldName}";
        
        public void OnBeforeSerialize() {
            if (outputNode == null || inputNode == null)
                return;

            outputNodeGUID = outputNode.GUID;
            inputNodeGUID = inputNode.GUID;
        }

        public void OnAfterDeserialize() {
           
        }
    }
}