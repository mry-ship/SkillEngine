using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SkillEngine {
    [System.Serializable]
    public class ParameterNode : BaseNode {
        [Input]
        public object input;

        [Output]
        public object output;

        public override string name => "Parameter";

        // We serialize the GUID of the exposed parameter in the graph so we can retrieve the true ExposedParameter from the graph
        [SerializeField, HideInInspector]
        public string parameterGUID;

        [ShowInInspector]
        public ExposedParameter parameter { get; private set; }

        public event Action onParameterChanged;

        public ParameterAccessor accessor;

        protected override void Enable() {
            // load the parameter
            LoadExposedParameter();

            owner.onExposedParameterModified += OnParamChanged;
            if (onParameterChanged != null)
                onParameterChanged?.Invoke();
        }

        public void LoadExposedParameter() {
            parameter = owner.GetExposedParameterFromGUID(parameterGUID);

            if (parameter == null) {
                Debug.Log("Property \"" + parameterGUID + "\" Can't be found !");

                // Delete this node as the property can't be found
                owner.RemoveNode(this);
                return;
            }

            output = parameter.value;
        }

        void OnParamChanged(ExposedParameter modifiedParam) {
            if (parameter == modifiedParam) {
                onParameterChanged?.Invoke();
            }
        }

        public override void Initialize(BaseGraph graph) {
            // 如果是全局变量节点，添加方向信息

            if (this.accessor == ParameterAccessor.Get) {
                nodeFields["input"].needAdd = false;
            }
            else {
                nodeFields["output"].needAdd = false;
            }
            
            base.Initialize(graph);
        }

        [CustomPortBehavior(nameof(output))]
        IEnumerable<PortData> GetOutputPort(List<BaseEdge> edges) {
            if (accessor == ParameterAccessor.Get) {
                yield return new PortData {
                    identifier = "output",
                    displayName = "Value",
                    displayType = (parameter == null) ? typeof(object) : parameter.GetValueType(),
                    acceptMultipleEdges = true
                };
            }
        }

        [CustomPortBehavior(nameof(input))]
        IEnumerable<PortData> GetInputPort(List<BaseEdge> edges) {
            if (accessor == ParameterAccessor.Set) {
                yield return new PortData {
                    identifier = "input",
                    displayName = "Value",
                    displayType = (parameter == null) ? typeof(object) : parameter.GetValueType(),
                };
            }
        }


        #region 配合全局变量节点更新Port

        /// <summary>
        /// Update all ports of the node
        /// 两件事：
        /// 1、删除所有之前相连的node
        /// 2、更新port
        /// </summary>
        public void UpdateAllPorts() {
            var inputFiledInfo = nodeFields["input"];
            var outputFiledInfo = nodeFields["output"];
            switch (accessor) {
                case ParameterAccessor.Get:
                    if (inputPorts.Count == 1) {
                        var edges = inputPorts["input"].GetEdges();
                        foreach (var baseEdge in edges) {
                            owner.Disconnect(baseEdge);
                        }
                    }

                    inputFiledInfo.needAdd = false;
                    outputFiledInfo.needAdd = true;
                    inputPorts.Clear();
                    AddPort(outputFiledInfo.input, outputFiledInfo.fieldName,
                        new PortData {
                            acceptMultipleEdges = outputFiledInfo.isMultiple, displayName = outputFiledInfo.name,
                            tooltip = outputFiledInfo.tooltip, vertical = outputFiledInfo.vertical
                        });
                    break;
                case ParameterAccessor.Set:
                    if (outputPorts.Count == 1) {
                        var edges = outputPorts["output"].GetEdges();
                        foreach (var baseEdge in edges) {
                            owner.Disconnect(baseEdge);
                        }
                    }

                    inputFiledInfo.needAdd = true;
                    outputFiledInfo.needAdd = false;
                    outputPorts.Clear();
                    AddPort(inputFiledInfo.input, inputFiledInfo.fieldName,
                        new PortData {
                            acceptMultipleEdges = inputFiledInfo.isMultiple, displayName = inputFiledInfo.name,
                            tooltip = inputFiledInfo.tooltip, vertical = inputFiledInfo.vertical
                        });
                    break;
            }
        }

        #endregion
    }


    public enum ParameterAccessor {
        Get,
        Set
    }
}