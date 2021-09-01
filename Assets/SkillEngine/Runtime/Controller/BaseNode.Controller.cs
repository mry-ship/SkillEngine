using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SkillEngine {
    public partial class BaseNode {
        /// <summary>
        /// Called when the node is enabled
        /// </summary>
        protected virtual void Enable() {
        }

        /// <summary>
        /// Called when the node is disabled
        /// </summary>
        protected virtual void Disable() {
        }

        /// <summary>
        /// Called when the node is removed
        /// </summary>
        protected virtual void Destroy() {
        }

        #region 生成节点时 Init

        /// <summary>
        /// Creates a node of type T at a certain position
        /// </summary>
        /// <param name="position">position in the graph in pixels</param>
        /// <typeparam name="T">type of the node</typeparam>
        /// <returns>the node instance</returns>
        public static T CreateFromType<T>(Vector2 position) where T : BaseNode {
            return CreateFromType(typeof(T), position) as T;
        }

        /// <summary>
        /// Creates a node of type nodeType at a certain position
        /// </summary>
        /// <param name="position">position in the graph in pixels</param>
        /// <typeparam name="nodeType">type of the node</typeparam>
        /// <returns>the node instance</returns>
        public static BaseNode CreateFromType(Type nodeType, Vector2 position) {
            if (!nodeType.IsSubclassOf(typeof(BaseNode)))
                return null;

            var node = Activator.CreateInstance(nodeType) as BaseNode;

            node.position = new Rect(position, new Vector2(100, 100));
            node.GUID = Guid.NewGuid().ToString();
            node.expanded = false;

            return node;
        }

        #endregion

        #region 读取Graph时 Init

        public void Deserialize() {
            // 如果是全局变量节点，添加方向信息
            if (this is ParameterNode node) {
                if (node.accessor == ParameterAccessor.Get) {
                    nodeFields["input"].needAdd = false;
                }
                else {
                    nodeFields["output"].needAdd = false;
                }
                node.LoadExposedParameter();
            }
            InitializePorts();
        }

        // called by the BaseGraph when the node is added to the graph
        public virtual void Initialize(BaseGraph graph)
        {
            this.owner = graph;

            ExceptionToLog.Call(() => Enable());

            InitializePorts();
        }
        
        public void InitializePorts() {
            // 总而言之先做了一个排序，但不知道有啥用
            foreach (var key in OverrideFieldOrder(nodeFields.Values.Select(k => k.info))) {
                var nodeField = nodeFields[key.Name];
                
                if (nodeField.info.FieldType.ToString() != "Lomo.LinearPort") {
                    if (nodeField.input) {
                        inputValues.Add(nodeField.fieldName,new ExposedParameter());
                    }
                    else {
                        outputValues.Add(nodeField.fieldName,new ExposedParameter());
                    }
                }
                
                
                if(!nodeField.needAdd) continue;
                
                AddPort(nodeField.input, nodeField.fieldName,
                    new PortData {
                        acceptMultipleEdges = nodeField.isMultiple, displayName = nodeField.name,
                        tooltip = nodeField.tooltip, vertical = nodeField.vertical
                    });

                
            }
        }
        
        
        /// <summary>
        /// 重写节点内的字段顺序。它允许在UI中对所有端口和字段重新排序。
        /// 排序，按继承顺序
        /// </summary>
        /// <param name="fields">List of fields to sort</param>
        /// <returns>Sorted list of fields</returns>
        public virtual IEnumerable<FieldInfo> OverrideFieldOrder(IEnumerable<FieldInfo> fields) {
            long GetFieldInheritanceLevel(FieldInfo f) {
                int level = 0;
                var t = f.DeclaringType;
                while (t != null) {
                    t = t.BaseType;
                    level++;
                }

                return level;
            }

            // 先显示基类的，再显示子类的Port
            // Order by MetadataToken and inheritance level to sync the order with the port order (make sure FieldDrawers are next to the correct port)
            return fields.OrderByDescending(
                f => -(long) (((GetFieldInheritanceLevel(f) << 32)) | (long) f.MetadataToken));
        }

        /// <summary>
        /// 给Port的edges 列表赋值
        /// </summary>
        public void InitPortEdges(List<BaseEdge> edges) {
            foreach (var edge in edges) {
                if (edge.inputNodeGUID == this.GUID) {
                    if (inputPorts.Keys.Contains(edge.inputFieldName)) {
                        inputPorts[edge.inputFieldName].GetEdges().Add(edge);
                    }
                    else {
                        Debug.LogError("[BaseNode.InitPortEdges] 没找到这个inputPort的Key");
                    }
                }

                if (edge.outputNodeGUID == this.GUID) {
                    if (outputPorts.Keys.Contains(edge.outputFieldName)) {
                        outputPorts[edge.outputFieldName].GetEdges().Add(edge);
                    }
                    else {
                        Debug.LogError("[BaseNode.InitPortEdges] 没找到这个outputPort的Key");
                    }
                }
            }
        }

        #endregion

        #region Port Api

        /// <summary>
        /// 读取时和编辑时都需要先生成节点的属性信息
        /// 保存时不需要保存
        /// </summary>
        protected void InitNodePortAttributes() {
            // 利用反射获得脚本中的全部变量
            var fields = GetNodeFields();
            // 获取脚本中的全部方法
            var methods = GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields) {
                // 获取变量的 Attribute
                var inputAttribute = field.GetCustomAttribute<InputAttribute>();
                var outputAttribute = field.GetCustomAttribute<OutputAttribute>();
                var tooltipAttribute = field.GetCustomAttribute<TooltipAttribute>();
                // var showInInspector = field.GetCustomAttribute< ShowInInspector >();
                var vertical = field.GetCustomAttribute<VerticalAttribute>();
                bool isMultiple = false;
                bool input = false;
                string name = field.Name;
                string tooltip = null;

                
                // if (showInInspector != null)
                // 	_needsInspector = true;

                if (inputAttribute == null && outputAttribute == null)
                    continue;

                //check if field is a collection type
                isMultiple = (inputAttribute != null) ? inputAttribute.allowMultiple : (outputAttribute.allowMultiple);
                input = inputAttribute != null;
                tooltip = tooltipAttribute?.tooltip;

                if (!String.IsNullOrEmpty(inputAttribute?.name))
                    name = inputAttribute.name;
                if (!String.IsNullOrEmpty(outputAttribute?.name))
                    name = outputAttribute.name;

                // By default we set the behavior to null, if the field have a custom behavior, it will be set in the loop just below
                nodeFields[field.Name] =
                    new NodeFieldInformation(field, name, input, isMultiple, tooltip, vertical != null);
            }
        }


        /// <summary>
        /// Add a port
        /// </summary>
        /// <param name="input">is input port</param>
        /// <param name="fieldName">C# field name</param>
        /// <param name="portData">Data of the port</param>
        public void AddPort(bool input, string fieldName, PortData portData) {
            // Fixup port data info if needed:
            if (portData.displayType == null)
                portData.displayType = nodeFields[fieldName].info.FieldType;

            if (this is ParameterNode node) {
                portData.displayType = (node.parameter == null) ? typeof(object) : node.parameter.GetValueType();
            }

            if (input)
                inputPorts.Add(fieldName, new NodePort(this, fieldName, portData));
            else {
                outputPorts.Add(fieldName, new NodePort(this, fieldName, portData));
            }
        }

        /// <summary>
        /// Remove a port
        /// </summary>
        /// <param name="input">is input port</param>
        /// <param name="port">the port to delete</param>
        public void RemovePort(bool input, NodePort port) {
            if (input)
                inputPorts.Remove(port.fieldName);
            else
                outputPorts.Remove(port.fieldName);
        }

        public NodePort GetPort(string fieldName, string identifier) {
            var result = inputPorts.Concat(outputPorts).FirstOrDefault(p => {
                var bothNull = String.IsNullOrEmpty(identifier) && String.IsNullOrEmpty(p.Value.portData.identifier);
                return p.Value.fieldName == fieldName && (bothNull || identifier == p.Value.portData.identifier);
            });

            return result.Value;
        }

        public NodePort GetOutPutPort(string disPlayName) {
            var resultKv = outputPorts.FirstOrDefault(p => p.Value.portData.displayName == disPlayName);

            return resultKv.Value;
        }

        /// <summary>
        /// Return all the ports of the node
        /// </summary>
        /// <returns></returns>
        public IEnumerable<NodePort> GetAllPorts() {
            foreach (var port in inputPorts.Values)
                yield return port;
            foreach (var port in outputPorts.Values)
                yield return port;
        }

        /// <summary>
        /// Return all the ports of the node
        /// </summary>
        /// <returns></returns>
        public IEnumerable<NodePort> GetOutputPorts() {
            foreach (var port in outputPorts.Values)
                yield return port;
        }

        /// <summary>
        /// Return all the connected edges of the node
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BaseEdge> GetAllEdges() {
            foreach (var port in GetAllPorts())
                foreach (var edge in port.GetEdges())
                    yield return edge;
        }

        /// <summary>
        /// Return all the connected edges of the node
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BaseEdge> GetOutputEdges() {
            foreach (var port in GetOutputPorts())
                foreach (var edge in port.GetEdges())
                    yield return edge;
        }

        public void ClearPorts() {
            inputPorts.Clear();
            outputPorts.Clear();
        }

        #endregion

        #region 通用方法

        public virtual FieldInfo[] GetNodeFields()
            => GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        protected virtual bool CanResetPort(NodePort port) => true;


        /// <summary>
        /// Set the custom name of the node. This is intended to be used by renamable nodes.
        /// This custom name will be serialized inside the node.
        /// </summary>
        /// <param name="customNodeName">New name of the node.</param>
        public void SetCustomName(string customName) => nodeCustomName = customName;

        /// <summary>
        /// Get the name of the node. If the node have a custom name (set using the UI by double clicking on the node title) then it will return this name first, otherwise it returns the value of the name field.
        /// </summary>
        /// <returns>The name of the node as written in the title</returns>
        public string GetCustomName() => String.IsNullOrEmpty(nodeCustomName) ? name : nodeCustomName;

        #endregion

        #region Events

        public void OnEdgeConnected(BaseEdge edge) {
            bool input = edge.inputNode == this;
            Dictionary<string, NodePort> portCollection =
                (input) ? (Dictionary<string, NodePort>) inputPorts : outputPorts;

            // 将Edge添加搭配NodePort里
            string portFieldName = (edge.inputNode == this) ? edge.inputFieldName : edge.outputFieldName;
            string portIdentifier = (edge.inputNode == this) ? edge.inputPortIdentifier : edge.outputPortIdentifier;

            // Force empty string to null since portIdentifier is a serialized value
            if (String.IsNullOrEmpty(portIdentifier))
                portIdentifier = null;

            var port = portCollection.Values.FirstOrDefault(p => {
                return p.fieldName == portFieldName && p.portData.identifier == portIdentifier;
            });

            if (port == null) {
                Debug.LogError("[BaseNode.OnEdgeConnected] 没有找到接口");
                return;
            }

            port.Add(edge);

            onAfterEdgeConnected?.Invoke(edge);
        }


        public void OnEdgeDisconnected(BaseEdge edge) {
            if (edge == null)
                return;

            bool input = edge.inputNode == this;
            Dictionary<string, NodePort> portCollection =
                (input) ? (Dictionary<string, NodePort>) inputPorts : outputPorts;

            // 删除port里存的边
            foreach (var nodePort in portCollection) {
                var p = nodePort.Value;
                p.Remove(edge);
            }

            // Reset default values of input port:
            bool haveConnectedEdges = edge.inputNode.inputPorts.Where(p => p.Value.fieldName == edge.inputFieldName)
                .Any(p => p.Value.GetEdges().Count != 0);
            if (edge.inputNode == this && !haveConnectedEdges && CanResetPort(edge.inputPort))
                edge.inputPort?.ResetToDefault();

            // 不是把数据删掉就行了嘛？？这是在干嘛？？？
            // UpdateAllPorts();

            // 断开连接后执行
            onAfterEdgeDisconnected?.Invoke(edge);
        }

        #endregion

        #region 引擎运行时

        public List<BaseNode> Walk(string portDisPlayName) {
            var nextNodes = new List<BaseNode>();
            var port = GetOutPutPort(portDisPlayName);
            var edges = port.GetEdges();
            foreach (var edge in edges) {
                nextNodes.Add(edge.inputNode);
            }

            return nextNodes;
        }
        
        protected ISkillContext ctx;
        
        public virtual void Start() {
        }

        public virtual void Update() {
            
        }
        
        public void CallWhenFinish() {
            ctx.CallWhenFinish(this);
        }
        

        public void SetCtx(ISkillContext ctx) {
            this.ctx = ctx;
        }
        
        #endregion

        #region 运行时Api
        
        /// <summary>
        ///
        /// </summary>
        /// <param name="portName"> port的变量名 </param>
        /// <returns></returns>
        public bool IsInputPortConnected(string portName) {
            if (inputPorts.TryGetValue(portName, out var port)) {
                if (port.GetEdges().Count > 0) return true;
            }
            return false;
        }
        
        /// <summary>
        ///
        /// </summary>
        /// <param name="portName"> port的变量名 </param>
        /// <returns></returns>
        public bool IsOutputPortConnected(string portName) {
            if (outputPorts.TryGetValue(portName, out var port)) {
                if (port.GetEdges().Count > 0) return true;
            }
            return false;
        }

        // public object GetValue(NodePort port) {
        //     switch (port.portType) {
        //         
        //     }
        // }
        #endregion
        
    }
}