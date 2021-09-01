using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SkillEngine {
    public class GraphChanges {
        public BaseEdge removedEdge;
        public BaseEdge addedEdge;
        public BaseNode removedNode;
        public BaseNode addedNode;
        public BaseNode nodeChanged;
    }

    public partial class BaseGraph : SerializedScriptableObject {
        [SerializeReference, ListDrawerSettings(NumberOfItemsPerPage = 1), EnableGUI]
        public List<BaseNode> nodes = new List<BaseNode>();

        [SerializeField, ListDrawerSettings(NumberOfItemsPerPage = 1)]
        public List<BaseEdge> edges = new List<BaseEdge>();


        [NonSerialized]
        public Dictionary<string, BaseNode> nodesPerGUID = new Dictionary<string, BaseNode>();

        [NonSerialized]
        public Dictionary<string, BaseEdge> edgesPerGUID = new Dictionary<string, BaseEdge>();


        //graph visual properties
        public Vector3 position = Vector3.zero;
        public Vector3 scale = Vector3.one;

        /// <summary>
        /// All pinned elements in the graph
        /// </summary>
        /// <typeparam name="PinnedElement"></typeparam>
        /// <returns></returns>
        [SerializeField]
        public List<PinnedElement> pinnedElements = new List<PinnedElement>();

        [SerializeField, SerializeReference]
        public List<ExposedParameter> exposedParameters = new List<ExposedParameter>();

        #region Events

        /// <summary>
        /// Triggered when the graph is changed
        /// </summary>
        public event Action<GraphChanges> onGraphChanges;

        /// <summary>
        /// Triggered when something is changed in the list of exposed parameters
        /// </summary>
        public event Action onExposedParameterListChanged;

        public event Action<ExposedParameter> onExposedParameterModified;
        public event Action<ExposedParameter> onExposedParameterValueChanged;

        #endregion

        bool mIsEnabled = false;

        public bool isEnabled {
            get => mIsEnabled;
            private set => mIsEnabled = value;
        }

        protected virtual void OnEnable() {
            Debug.Log("<color=green>" + this.name + " is enable" + "</color>");
            InitializeGraphElements();
        }
    }
}