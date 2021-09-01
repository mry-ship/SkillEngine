using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SkillEngine {
    public partial class BaseGraph {
        #region 创建时 Init

        #endregion

        #region 读取时 Init

        /// <summary>
        /// 图被Enable时候触发
        /// </summary>
        void InitializeGraphElements() {
            nodes.RemoveAll(n => n == null);
            foreach (var node in nodes.ToList()) {
                nodesPerGUID[node.GUID] = node;
                node.owner = this;
                node.Deserialize();
            }

            foreach (var edge in edges.ToList()) {
                edge.owner = this;
                edge.Deserialize();
                edgesPerGUID[edge.GUID] = edge;

                // Sanity check for the edge:
                if (edge.inputPort == null || edge.outputPort == null) {
                    Disconnect(edge.GUID);
                    continue;
                }

                // Add the edge to the non-serialized port data
                edge.inputPort.owner.OnEdgeConnected(edge);
                edge.outputPort.owner.OnEdgeConnected(edge);
            }
        }

        public void RegenerateData() {
            InitializeGraphElements();
        }

        #endregion

        #region 编辑器操作

        /// <summary>
        /// Open a pinned element of type viewType
        /// </summary>
        /// <param name="viewType">type of the pinned element</param>
        /// <returns>the pinned element</returns>
        public PinnedElement OpenPinned(Type viewType) {
            var pinned = pinnedElements.Find(p => p.editorType.type == viewType);

            if (pinned == null) {
                pinned = new PinnedElement(viewType);
                pinnedElements.Add(pinned);
            }
            else
                pinned.opened = true;

            return pinned;
        }

        /// <summary>
        /// Closes a pinned element of type viewType
        /// </summary>
        /// <param name="viewType">type of the pinned element</param>
        public void ClosePinned(Type viewType) {
            var pinned = pinnedElements.Find(p => p.editorType.type == viewType);

            pinned.opened = false;
        }

        #endregion

        #region 全局变量

        /// <summary>
        /// Add an exposed parameter
        /// </summary>
        /// <param name="name">parameter name</param>
        /// <param name="type">parameter type (must be a subclass of ExposedParameter)</param>
        /// <param name="value">default value</param>
        /// <returns>The unique id of the parameter</returns>
        public string AddExposedParameter(string name, Type type, object value = null) {
            if (!type.IsSubclassOf(typeof(ExposedParameter))) {
                Debug.LogError($"Can't add parameter of type {type}, the type doesn't inherit from ExposedParameter.");
            }

            var param = Activator.CreateInstance(type) as ExposedParameter;

            // patch value with correct type:
            if (param.GetValueType().IsValueType)
                value = Activator.CreateInstance(param.GetValueType());

            param.Initialize(name, value);
            exposedParameters.Add(param);

            onExposedParameterListChanged?.Invoke();

            return param.guid;
        }

        /// <summary>
        /// Add an already allocated / initialized parameter to the graph
        /// </summary>
        /// <param name="parameter">The parameter to add</param>
        /// <returns>The unique id of the parameter</returns>
        public string AddExposedParameter(ExposedParameter parameter) {
            string guid = Guid.NewGuid().ToString(); // Generated once and unique per parameter

            parameter.guid = guid;
            exposedParameters.Add(parameter);

            onExposedParameterListChanged?.Invoke();

            return guid;
        }

        /// <summary>
        /// Remove an exposed parameter
        /// </summary>
        /// <param name="ep">the parameter to remove</param>
        public void RemoveExposedParameter(ExposedParameter ep) {
            exposedParameters.Remove(ep);

            onExposedParameterListChanged?.Invoke();
        }

        /// <summary>
        /// Remove an exposed parameter
        /// </summary>
        /// <param name="guid">GUID of the parameter</param>
        public void RemoveExposedParameter(string guid) {
            if (exposedParameters.RemoveAll(e => e.guid == guid) != 0)
                onExposedParameterListChanged?.Invoke();
        }

        /// <summary>
        /// Update an exposed parameter value
        /// </summary>
        /// <param name="guid">GUID of the parameter</param>
        /// <param name="value">new value</param>
        public void UpdateExposedParameter(string guid, object value) {
            var param = exposedParameters.Find(e => e.guid == guid);
            if (param == null)
                return;

            if (value != null && !param.GetValueType().IsAssignableFrom(value.GetType()))
                throw new Exception("Type mismatch when updating parameter " + param.name + ": from " +
                                    param.GetValueType() + " to " + value.GetType().AssemblyQualifiedName);

            param.value = value;
            onExposedParameterModified?.Invoke(param);
        }

        /// <summary>
        /// Update the exposed parameter name
        /// </summary>
        /// <param name="parameter">The parameter</param>
        /// <param name="name">new name</param>
        public void UpdateExposedParameterName(ExposedParameter parameter, string name) {
            parameter.name = name;
            onExposedParameterModified?.Invoke(parameter);
        }

        /// <summary>
        /// Update parameter visibility
        /// </summary>
        /// <param name="parameter">The parameter</param>
        /// <param name="isHidden">is Hidden</param>
        public void NotifyExposedParameterChanged(ExposedParameter parameter) {
            onExposedParameterModified?.Invoke(parameter);
        }

        public void NotifyExposedParameterValueChanged(ExposedParameter parameter) {
            onExposedParameterValueChanged?.Invoke(parameter);
        }

        /// <summary>
        /// Get the exposed parameter from name
        /// </summary>
        /// <param name="name">name</param>
        /// <returns>the parameter or null</returns>
        public ExposedParameter GetExposedParameter(string name) {
            return exposedParameters.FirstOrDefault(e => e.name == name);
        }

        /// <summary>
        /// Get exposed parameter from GUID
        /// </summary>
        /// <param name="guid">GUID of the parameter</param>
        /// <returns>The parameter</returns>
        public ExposedParameter GetExposedParameterFromGUID(string guid) {
            return exposedParameters.FirstOrDefault(e => e?.guid == guid);
        }

        /// <summary>
        /// Set parameter value from name. (Warning: the parameter name can be changed by the user)
        /// </summary>
        /// <param name="name">name of the parameter</param>
        /// <param name="value">new value</param>
        /// <returns>true if the value have been assigned</returns>
        public bool SetParameterValue(string name, object value) {
            Debug.LogError(":SetParameterValue");
            var e = exposedParameters.FirstOrDefault(p => p.name == name);

            if (e == null)
                return false;

            e.value = value;

            return true;
        }

        /// <summary>
        /// Get the parameter value
        /// </summary>
        /// <param name="name">parameter name</param>
        /// <returns>value</returns>
        public object GetParameterValue(string name) => exposedParameters.FirstOrDefault(p => p.name == name)?.value;

        /// <summary>
        /// Get the parameter value template
        /// </summary>
        /// <param name="name">parameter name</param>
        /// <typeparam name="T">type of the parameter</typeparam>
        /// <returns>value</returns>
        public T GetParameterValue<T>(string name) => (T) GetParameterValue(name);

        #endregion

        #region Node Api

        public void AddNode(BaseNode node) {
            node.Initialize(this);
            nodes.Add(node);
            nodesPerGUID[node.GUID] = node;
        }

        public void RemoveNode(BaseNode node) {
            nodesPerGUID.Remove(node.GUID);

            nodes.Remove(node);

            onGraphChanges?.Invoke(new GraphChanges {removedNode = node});
        }

        #endregion

        #region Edge Api

        /// <summary>
        /// Connect two ports with an edge
        /// </summary>
        /// <param name="inputPort">input port</param>
        /// <param name="outputPort">output port</param>
        /// <param name="DisconnectInputs">is the edge allowed to disconnect another edge</param>
        /// <returns>the connecting edge</returns>
        public BaseEdge Connect(NodePort inputPort, NodePort outputPort, bool autoDisconnectInputs = true) {
            var edge = BaseEdge.CreateNewEdge(this, inputPort, outputPort);

            //If the input port does not support multi-connection, we remove them
            if (autoDisconnectInputs && !inputPort.portData.acceptMultipleEdges) {
                foreach (var e in inputPort.GetEdges().ToList()) {
                    // TODO: do not disconnect them if the connected port is the same than the old connected
                    Disconnect(e);
                }
            }

            // same for the output port:
            if (autoDisconnectInputs && !outputPort.portData.acceptMultipleEdges) {
                foreach (var e in outputPort.GetEdges().ToList()) {
                    // TODO: do not disconnect them if the connected port is the same than the old connected
                    Disconnect(e);
                }
            }

            edges.Add(edge);

            // Add the edge to the list of connected edges in the nodes
            inputPort.owner.OnEdgeConnected(edge);
            outputPort.owner.OnEdgeConnected(edge);

            onGraphChanges?.Invoke(new GraphChanges {addedEdge = edge});

            return edge;
        }

        /// <summary>
        /// Disconnect an edge
        /// </summary>
        /// <param name="edge"></param>
        public void Disconnect(BaseEdge edge) => Disconnect(edge.GUID);

        /// <summary>
        /// Disconnect an edge
        /// </summary>
        /// <param name="edgeGUID"></param>
        public void Disconnect(string edgeGUID) {
            Debug.Log("[BaseGraph.Disconnect] Disconnect Edge");
            List<(BaseNode, BaseEdge)> disconnectEvents = new List<(BaseNode, BaseEdge)>();

            edges.RemoveAll(r => {
                if (r.GUID == edgeGUID) {
                    disconnectEvents.Add((r.inputNode, r));
                    disconnectEvents.Add((r.outputNode, r));
                    onGraphChanges?.Invoke(new GraphChanges {removedEdge = r});
                }

                return r.GUID == edgeGUID;
            });

            // Delay the edge disconnect event to avoid recursion
            foreach (var (node, edge) in disconnectEvents)
                node?.OnEdgeDisconnected(edge);
        }

        public List<BaseEdge> GetOutputEdges(string nodeGUID) {
            return nodesPerGUID[nodeGUID].GetOutputEdges().ToList();
        }

        #endregion

        #region Port Api

        /// <summary>
        /// 判断两个Port能不能连
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        public static bool TypesAreConnectable(Type t1, Type t2) {
            if (t1 == null || t2 == null)
                return false;

            if (t1 == t2) {
                return true;
            }

            return false;
        }


        public void ClearPortsAndNodePerGUID() {
            nodesPerGUID.Clear();
            if (nodes != null) {
                foreach (var node in nodes) {
                    node.ClearPorts();
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Invoke the onGraphChanges event, can be used as trigger to execute the graph when the content of a node is changed 
        /// </summary>
        /// <param name="node"></param>
        public void NotifyNodeChanged(BaseNode node) => onGraphChanges?.Invoke(new GraphChanges {nodeChanged = node});

        #endregion
    }
}