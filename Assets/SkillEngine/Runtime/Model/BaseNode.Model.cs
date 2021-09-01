using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SkillEngine {
    [Serializable]
    public abstract partial class BaseNode {
        //id
        [HideInInspector]
        public string GUID;
        
        [SerializeField]
        internal string nodeCustomName = null;

        [NonSerialized]
        public BaseGraph owner;

        [NonSerialized]
        public Dictionary<string, NodeFieldInformation> nodeFields = new Dictionary<string, NodeFieldInformation>();

        // id,NodePort
        [NonSerialized, EnableGUI,ShowInInspector]
        [GUIColor(0.3f, 0.8f, 0.8f, 1f)]
        public Dictionary<string, NodePort> inputPorts;

        [NonSerialized, EnableGUI,ShowInInspector]
        public Dictionary<string, NodePort> outputPorts;

        [NonSerialized]
        public Dictionary<string, ExposedParameter> inputValues = new Dictionary<string, ExposedParameter>();

        [NonSerialized]
        public Dictionary<string, ExposedParameter> outputValues = new Dictionary<string, ExposedParameter>();
        
        //Node view datas
        public Rect position;

        /// <summary>
        /// Is the node expanded
        /// </summary>
        public bool expanded;

        /// <summary>
        /// Node locked state
        /// </summary>
        public bool nodeLock = false;

        /// <summary>
        /// Is the node is locked (if locked it can't be moved)
        /// </summary>
        public virtual bool isLocked => nodeLock;

        /// <summary>
        /// Name of the node, it will be displayed in the title section
        /// </summary>
        /// <returns></returns>
        public virtual string name => GetType().Name;

        /// <summary>
        /// The accent color of the node
        /// </summary>
        public virtual Color color => Color.clear;

        /// <summary>
        /// Set a custom uss file for the node. We use a Resources.Load to get the stylesheet so be sure to put the correct resources path
        /// https://docs.unity3d.com/ScriptReference/Resources.Load.html
        /// </summary>
        public virtual string layoutStyle => string.Empty;

        /// <summary>
        /// Is the node created from a duplicate operation (either ctrl-D or copy/paste).
        /// </summary>
        public bool createdFromDuplication { get; internal set; } = false;
        
        /// <summary>
        /// Triggered after an edge was connected on the node
        /// </summary>
        public event Action<BaseEdge> onAfterEdgeConnected;

        /// <summary>
        /// Triggered after an edge was disconnected on the node
        /// </summary>
        public event Action<BaseEdge> onAfterEdgeDisconnected;
        
        /// <summary>
        /// Triggered after a single/list of port(s) is updated, the parameter is the field name
        /// </summary>
        public event Action< string >					onPortsUpdated;

        protected BaseNode() {
            inputPorts = new Dictionary<string, NodePort>();
            outputPorts = new Dictionary<string, NodePort>();

            InitNodePortAttributes();
        }

        public class NodeFieldInformation {
            public string name;
            public string fieldName;
            public FieldInfo info;
            public bool input;
            public bool isMultiple;
            public string tooltip;
            public bool vertical;
            public bool needAdd;

            public NodeFieldInformation(FieldInfo info, string name, bool input, bool isMultiple, string tooltip,
                bool vertical) {
                this.input = input;
                this.isMultiple = isMultiple;
                this.info = info;
                this.name = name;
                this.fieldName = info.Name;
                this.tooltip = tooltip;
                this.vertical = vertical;
                this.needAdd = true;
            }
        }
    }
}