using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Status = UnityEngine.UIElements.DropdownMenuAction.Status;
using Object = UnityEngine.Object;

namespace SkillEngine {
    public class BaseGraphView : GraphView, IDisposable {
        // 数据源
        public BaseGraph graph;

        public SerializedObject serializedGraph;
        void UpdateSerializedProperties()
        {
            serializedGraph = new SerializedObject(graph);
        }

        
        public BaseEdgeConnectorListener connectorListener;

        #region 界面元素容器

        public List<BaseNodeView> nodeViews = new List<BaseNodeView>();

        /// <summary>
        /// Dictionary of the node views accessed view the node instance, faster than a Find in the node view list
        /// </summary>
        /// <typeparam name="BaseNode"></typeparam>
        /// <typeparam name="BaseNodeView"></typeparam>
        /// <returns></returns>
        public Dictionary<BaseNode, BaseNodeView> nodeViewsPerNode = new Dictionary<BaseNode, BaseNodeView>();

        /// <summary>
        /// Node的MC和View的对应字典
        /// </summary>
        public Dictionary<BaseNode, BaseNodeView> nodeMCAndViewsDic = new Dictionary<BaseNode, BaseNodeView>();

        public List<BaseEdgeView> edgeViews = new List<BaseEdgeView>();

        public DefaultGridBackGround backGround;

        /// <summary>
        /// 右键菜单
        /// </summary>
        CreateNodeMenu createNodeMenu;
        
        /// <summary>
        /// 固定窗口
        /// </summary>
        Dictionary< Type, PinnedElementView >		pinnedElements = new Dictionary< Type, PinnedElementView >();


        #endregion

        #region events
        public delegate void NodeDuplicatedDelegate(BaseNode duplicatedNode, BaseNode newNode);

        public event Action initialized;

        public event Action onExposedParameterListChanged;
        
        // Todo:全局变量相关事件
        /// <summary>
        /// Same event than BaseGraph.onExposedParameterModified
        /// Safe event (not triggered in case the graph is null).
        /// </summary>
        public event Action<ExposedParameter> onExposedParameterModified;
        public event NodeDuplicatedDelegate nodeDuplicated;

        #endregion

        Dictionary<Type, (Type nodeType, MethodInfo initalizeNodeFromObject)> nodeTypePerCreateAssetType =
            new Dictionary<Type, (Type, MethodInfo)>();

        public BaseGraphView(EditorWindow window, bool hasGridBackground) {
            // Todo:定义界面操作相关的回调
            graphViewChanged = GraphViewChangedCallback; //处理删除操作
            // 赋值粘贴操作
            serializeGraphElements = SerializeGraphElementsCallback;
            canPasteSerializedData = CanPasteSerializedDataCallback;
            unserializeAndPaste = UnserializeAndPasteCallback;

            RegisterCallback<KeyDownEvent>(KeyDownCallback);
            RegisterCallback<DragPerformEvent>(DragPerformedCallback);
            RegisterCallback<DragUpdatedEvent>(DragUpdatedCallback);
            RegisterCallback<MouseDownEvent>(MouseDownCallback);
            RegisterCallback<MouseUpEvent>(MouseUpCallback);

            if (hasGridBackground) {
                backGround = new DefaultGridBackGround();
                Insert(0, backGround);
            }

            // 添加操作功能
            InitializeManipulators();

            SetupZoom(0.05f, 2f);

            // 执行撤销操作需要重新载入
            Undo.undoRedoPerformed += ReloadView;

            createNodeMenu = ScriptableObject.CreateInstance<CreateNodeMenu>();
            createNodeMenu.Initialize(this, window);

            this.StretchToParentSize();
        }

        protected virtual void InitializeManipulators() {
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
        }

        #region 读取SO文件时 Initialization

        // 根据M和C生成V
        // 会在打开窗口时调用
        public void InitOnOpenSO(BaseGraph graph) {
            this.graph = graph;
            // 设置SerializedObject，为了绑定SO文件的字段
            UpdateSerializedProperties();
            
            // 全局变量工厂
            exposedParameterFactory = new ExposedParameterFieldFactory(graph);
            
            connectorListener = CreateEdgeConnectorListener();
            // When pressing ctrl-s, we save the graph
            EditorSceneManager.sceneSaved += _ => SaveGraphToDisk();
            InitializeGraphView();

            InitializeNodeViews();
            InitializeEdgeViews();

            initialized?.Invoke();
            InitializeView();

            // 提前加载graph的节点
            NodeProvider.LoadGraph(graph);

            // Register the nodes that can be created from assets
            foreach (var nodeInfo in NodeProvider.GetNodeMenuEntries(graph)) {
                var interfaces = nodeInfo.type.GetInterfaces();
                var exceptInheritedInterfaces = interfaces.Except(interfaces.SelectMany(t => t.GetInterfaces()));
                foreach (var i in interfaces) {
                    if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICreateNodeFrom<>)) {
                        var genericArgumentType = i.GetGenericArguments()[0];
                        var initializeFunction = nodeInfo.type.GetMethod(
                            nameof(ICreateNodeFrom<Object>.InitializeNodeFromObject),
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                            null, new Type[] {genericArgumentType}, null
                        );

                        // We only add the type that implements the interface, not it's children
                        if (initializeFunction.DeclaringType == nodeInfo.type)
                            nodeTypePerCreateAssetType[genericArgumentType] = (nodeInfo.type, initializeFunction);
                    }
                }
            }
        }

        protected virtual BaseEdgeConnectorListener CreateEdgeConnectorListener()
            => new BaseEdgeConnectorListener(this);

        void InitializeGraphView() {
            graph.onExposedParameterListChanged += OnExposedParameterListChanged;
            graph.onExposedParameterModified += (s) => onExposedParameterModified?.Invoke(s);
            graph.onGraphChanges += GraphChangesCallback;
            viewTransform.position = graph.position;
            viewTransform.scale = graph.scale;
            nodeCreationRequest = (c) =>
                SearchWindow.Open(new SearchWindowContext(c.screenMousePosition), createNodeMenu);
        }

        void InitializeNodeViews() {
            foreach (var nodePair in graph.nodesPerGUID) {
                var v = AddNodeView(nodePair.Value);
            }
        }

        void InitializeEdgeViews() {
            // Sanitize edges in case a node broke something while loading
            graph.edges.RemoveAll(edge => edge == null || edge.inputNode == null || edge.outputNode == null);

            foreach (var edge in graph.edges) {
                nodeViewsPerNode.TryGetValue(edge.inputNode, out var inputNodeView);
                nodeViewsPerNode.TryGetValue(edge.outputNode, out var outputNodeView);
                if (inputNodeView == null || outputNodeView == null)
                    continue;

                var edgeView = CreateEdgeView();
                // Edge的数据
                edgeView.userData = edge;
                edgeView.input = inputNodeView.GetPortViewFromFieldName(edge.inputFieldName, edge.inputPortIdentifier);
                edgeView.output =
                    outputNodeView.GetPortViewFromFieldName(edge.outputFieldName, edge.outputPortIdentifier);


                ConnectView(edgeView);
            }
        }

        /// <summary>
        /// 给子类重写，补充界面元素
        /// </summary>
        protected virtual void InitializeView() {
        }

        #endregion

        #region Node api

        public BaseNodeView AddNode(BaseNode node) {
            // This will initialize the node using the graph instance
            graph.AddNode(node);

            UpdateSerializedProperties();
            
            var view = AddNodeView(node);

            // Call create after the node have been initialized
            ExceptionToLog.Call(() => view.OnCreated());

            // UpdateComputeOrder();

            return view;
        }
        
        public BaseNodeView AddNodeView(BaseNode node) {
            // 用来自定义View的，虽然会因为NodeProvider重新构造一次BaseNode，但不影响已经传入的node
            var viewType = NodeProvider.GetNodeViewTypeFromType(node.GetType());

            if (viewType == null)
                viewType = typeof(BaseNodeView);

            var baseNodeView = Activator.CreateInstance(viewType) as BaseNodeView;
            baseNodeView.InitOnOpenSO(this, node);
            AddElement(baseNodeView);

            nodeViews.Add(baseNodeView);
            nodeViewsPerNode[node] = baseNodeView;

            return baseNodeView;
        }

        #endregion

        public virtual BaseEdgeView CreateEdgeView() {
            return new BaseEdgeView();
        }

        #region Edge Api

        public bool Connect(BaseEdgeView e, bool autoDisconnectInputs = true) {
            if (!CanConnectEdge(e, autoDisconnectInputs))
                return false;

            var inputPortView = e.input as BasePortView;
            var outputPortView = e.output as BasePortView;
            var inputNodeView = inputPortView.node as BaseNodeView;
            var outputNodeView = outputPortView.node as BaseNodeView;
            var inputPort =
                inputNodeView.nodeTarget.GetPort(inputPortView.fieldName, inputPortView.portData.identifier);
            var outputPort =
                outputNodeView.nodeTarget.GetPort(outputPortView.fieldName, outputPortView.portData.identifier);

            e.userData = graph.Connect(inputPort, outputPort, autoDisconnectInputs);

            ConnectView(e, autoDisconnectInputs);

            return true;
        }

        public bool ConnectView(BaseEdgeView e, bool autoDisconnectInputs = true) {
            if (!CanConnectEdge(e, autoDisconnectInputs))
                return false;

            var inputPortView = e.input as BasePortView;
            var outputPortView = e.output as BasePortView;
            var inputNodeView = inputPortView.node as BaseNodeView;
            var outputNodeView = outputPortView.node as BaseNodeView;

            //If the input port does not support multi-connection, we remove them
            if (autoDisconnectInputs && !(e.input as BasePortView).portData.acceptMultipleEdges) {
                foreach (var edge in edgeViews.Where(ev => ev.input == e.input).ToList()) {
                    // TODO: do not disconnect them if the connected port is the same than the old connected
                    DisconnectView(edge);
                }
            }

            // same for the output port:
            if (autoDisconnectInputs && !(e.output as BasePortView).portData.acceptMultipleEdges) {
                foreach (var edge in edgeViews.Where(ev => ev.output == e.output).ToList()) {
                    // TODO: do not disconnect them if the connected port is the same than the old connected
                    DisconnectView(edge);
                }
            }

            AddElement(e);

            e.input.Connect(e);
            e.output.Connect(e);

            // If the input port have been removed by the custom port behavior
            // we try to find if it's still here
            if (e.input == null)
                e.input = inputNodeView.GetPortViewFromFieldName(inputPortView.fieldName,
                    inputPortView.portData.identifier);
            if (e.output == null)
                e.output = inputNodeView.GetPortViewFromFieldName(outputPortView.fieldName,
                    outputPortView.portData.identifier);

            edgeViews.Add(e);

            inputNodeView.RefreshPorts();
            outputNodeView.RefreshPorts();

            // In certain cases the edge color is wrong so we patch it
            schedule.Execute(() => { e.UpdateEdgeControl(); }).ExecuteLater(1);

            e.isConnected = true;

            return true;
        }

        public void Disconnect(BaseEdgeView e, bool refreshPorts = true) {
            // Remove the serialized edge if there is one
            if (e.userData is BaseEdge baseEdge)
                graph.Disconnect(baseEdge.GUID);

            DisconnectView(e, refreshPorts);
        }

        public void DisconnectView(BaseEdgeView e, bool refreshPorts = true) {
            if (e == null)
                return;

            RemoveElement(e);

            if (e?.input?.node is BaseNodeView inputNodeView) {
                e.input.Disconnect(e);
                if (refreshPorts)
                    inputNodeView.RefreshPorts();
            }

            if (e?.output?.node is BaseNodeView outputNodeView) {
                e.output.Disconnect(e);
                if (refreshPorts)
                    outputNodeView.RefreshPorts();
            }

            edgeViews.Remove(e);
        }

        /// <summary>
        /// 判断能不能连
        /// </summary>
        /// <param name="e"></param>
        /// <param name="autoDisconnectInputs"></param>
        /// <returns></returns>
        public bool CanConnectEdge(BaseEdgeView e, bool autoDisconnectInputs = true) {
            if (e.input == null || e.output == null)
                return false;

            var inputPortView = e.input as BasePortView;
            var outputPortView = e.output as BasePortView;
            var inputNodeView = inputPortView.node as BaseNodeView;
            var outputNodeView = outputPortView.node as BaseNodeView;

            if (inputNodeView == null || outputNodeView == null) {
                Debug.LogError("Connect aborted !");
                return false;
            }

            return true;
        }

        public void RemoveEdges() {
            foreach (var edge in edgeViews)
                RemoveElement(edge);
            edgeViews.Clear();
        }

        #endregion

        #region Menu

        public virtual IEnumerable<(string path, Type type)> FilterCreateNodeMenuEntries() {
            // By default we don't filter anything
            foreach (var nodeMenuItem in NodeProvider.GetNodeMenuEntries(graph))
                yield return nodeMenuItem;

            // TODO: add exposed properties to this list
        }

        #endregion

        #region 编辑时操作

        public void RegisterCompleteObjectUndo(string name) {
            Undo.RegisterCompleteObjectUndo(graph, name);
        }

        public void SaveGraphToDisk() {
            if (graph == null)
                return;

            EditorUtility.SetDirty(graph);
        }

        GraphViewChange GraphViewChangedCallback(GraphViewChange changes) {
            if (changes.elementsToRemove != null) {
                Debug.Log("Solve Delete");
                RegisterCompleteObjectUndo("Remove Graph Elements");
                // Destroy priority of objects
                // We need nodes to be destroyed first because we can have a destroy operation that uses node connections
                // 先删除Node捏
                changes.elementsToRemove.Sort((e1, e2) => {
                    int GetPriority(GraphElement e) {
                        if (e is BaseNodeView)
                            return 0;
                        else
                            return 1;
                    }

                    return GetPriority(e1).CompareTo(GetPriority(e2));
                });


                //Handle ourselves the edge and node remove
                changes.elementsToRemove.RemoveAll(e => {
                    switch (e) {
                        case BaseEdgeView edge:
                            Disconnect(edge);
                            return true;
                        case BaseNodeView nodeView:
                            // For vertical nodes, we need to delete them ourselves as it's not handled by GraphView
                            Debug.Log("Remove Node");
                            foreach (var pv in nodeView.inputPortViews.Concat(nodeView.outputPortViews))
                                if (pv.orientation == Orientation.Vertical)
                                    foreach (var edge in pv.GetEdges().ToList())
                                        Disconnect(edge);

                            ExceptionToLog.Call(() => nodeView.OnRemoved());
                            graph.RemoveNode(nodeView.nodeTarget);
                            UpdateSerializedProperties();
                            RemoveElement(nodeView);
                            return true;
                    }

                    return false;
                });
            }

            return changes;
        }

        void ReloadView() {
            // Force the graph to reload his data (Undo have updated the serialized properties of the graph
            // so the one that are not serialized need to be synchronized)
            // 先清除Port数据，node的Dic是根据Key赋值的，所以不需要清除
            graph.ClearPortsAndNodePerGUID();
            graph.RegenerateData();

            // Get selected nodes
            var selectedNodeGUIDs = new List<string>();
            foreach (var e in selection) {
                if (e is BaseNodeView v && this.Contains(v))
                    selectedNodeGUIDs.Add(v.nodeTarget.GUID);
            }

            // Remove everything
            RemoveNodeViews();
            RemoveEdges();
            // RemoveGroups();
#if UNITY_2020_1_OR_NEWER
            // RemoveStrickyNotes();
#endif
            // RemoveStackNodeViews();
            
            UpdateSerializedProperties();
            
            // And re-add with new up to date datas
            InitializeNodeViews();
            InitializeEdgeViews();
            // InitializeGroups();
            // InitializeStickyNotes();
            // InitializeStackNodes();

            Reload();

            // UpdateComputeOrder();

            // Restore selection after re-creating all views
            // selection = nodeViews.Where(v => selectedNodeGUIDs.Contains(v.nodeTarget.GUID)).Select(v => v as ISelectable).ToList();
            foreach (var guid in selectedNodeGUIDs) {
                AddToSelection(nodeViews.FirstOrDefault(n => n.nodeTarget.GUID == guid));
            }

            // UpdateNodeInspectorSelection();
        }

        string SerializeGraphElementsCallback(IEnumerable<GraphElement> elements) {
            var data = new CopyPasteHelper();

            data.sourceGraphName = this.graph.name;
            
            foreach (BaseNodeView nodeView in elements.Where(e => e is BaseNodeView))
            {
                data.copiedNodes.Add(JsonSerializer.SerializeNode(nodeView.nodeTarget));
                foreach (var port in nodeView.nodeTarget.GetAllPorts())
                {
                    if (port.portData.vertical)
                    {
                        foreach (var edge in port.GetEdges()) {
                            Debug.Log("[BaseGraphView.SerializeGraphElementsCallback] portData Vertical GetEdge");
                            data.copiedEdges.Add(JsonSerializer.Serialize(edge));
                        }
                            
                    }
                }
            }

            foreach (BaseEdgeView edgeView in elements.Where(e => e is BaseEdgeView)) {
                data.copiedEdges.Add(JsonSerializer.Serialize(edgeView.edgeData));
            }
                
            
            ClearSelection();
            Debug.Log(JsonUtility.ToJson(data, true));
            return JsonUtility.ToJson(data, true);
        }
        
        bool CanPasteSerializedDataCallback(string serializedData)
        {
            try {
                return JsonUtility.FromJson(serializedData, typeof(CopyPasteHelper)) != null;
            } catch {
                return false;
            }
        }

        void UnserializeAndPasteCallback(string operationName, string serializedData) {
            Debug.Log("UnserializeAndPaste");
            var data = JsonUtility.FromJson< CopyPasteHelper >(serializedData);
            RegisterCompleteObjectUndo(operationName);
            
            Dictionary<string, BaseNode> copiedNodesMap = new Dictionary<string, BaseNode>();

            foreach (var serializedNode in data.copiedNodes)
            {
                var node = JsonSerializer.DeserializeNode(serializedNode);

                if (node == null)
                    continue ;

                string sourceGUID = node.GUID;
                graph.nodesPerGUID.TryGetValue(sourceGUID, out var sourceNode);
                //Call OnNodeCreated on the new fresh copied node
                node.createdFromDuplication = true;
                node.GUID = Guid.NewGuid().ToString();
                //And move a bit the new node
                node.position.position += new Vector2(20, 20);

                var newNodeView = AddNode(node);

                // If the nodes were copied from another graph, then the source is null
                if (sourceNode != null)
                    nodeDuplicated?.Invoke(sourceNode, node);
                copiedNodesMap[sourceGUID] = node;
                
                foreach (var baseNode in copiedNodesMap) {
                    Debug.Log("key:"+baseNode.Key);
                }
                //Select the new node
                AddToSelection(nodeViewsPerNode[node]);
            }
            
            foreach (var serializedEdge in data.copiedEdges)
            {
                var edge = JsonSerializer.Deserialize<BaseEdge>(serializedEdge);

                edge.owner = this.graph;
                edge.Deserialize();

                if (data.sourceGraphName == this.graph.name) {
                    
                }
                // Find port of new nodes:
                copiedNodesMap.TryGetValue(edge.inputNodeGUID, out var oldInputNode);
                copiedNodesMap.TryGetValue(edge.outputNodeGUID, out var oldOutputNode);
                
                // We avoid to break the graph by replacing unique connections:
                if (oldOutputNode == null || oldInputNode == null)
                    continue;
              
                oldInputNode = oldInputNode ?? edge.inputNode;
                oldOutputNode = oldOutputNode ?? edge.outputNode;

                var inputPort = oldInputNode.GetPort(edge.inputFieldName, edge.inputPortIdentifier);
                var outputPort = oldOutputNode.GetPort(edge.outputFieldName, edge.outputPortIdentifier);
                
                Debug.LogWarning(inputPort);
                Debug.LogWarning(outputPort);
                var newEdge = BaseEdge.CreateNewEdge(graph, inputPort, outputPort);

                if (nodeViewsPerNode.ContainsKey(oldInputNode) && nodeViewsPerNode.ContainsKey(oldOutputNode))
                {
                    var edgeView = CreateEdgeView();
                    edgeView.userData = newEdge;
                    edgeView.input = nodeViewsPerNode[oldInputNode].GetPortViewFromFieldName(newEdge.inputFieldName, newEdge.inputPortIdentifier);
                    edgeView.output = nodeViewsPerNode[oldOutputNode].GetPortViewFromFieldName(newEdge.outputFieldName, newEdge.outputPortIdentifier);

                    Connect(edgeView);
                }
            }
        }

        #endregion

        #region UiElements Event

        void DragPerformedCallback(DragPerformEvent e) {
            var mousePos =
                (e.currentTarget as VisualElement).ChangeCoordinatesTo(contentViewContainer, e.localMousePosition);
            var dragData = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;

            Debug.Log(":DRAG");
            // Drag and Drop for elements inside the graph
            if (dragData != null)
            {
                var exposedParameterFieldViews = dragData.OfType<ExposedParameterFieldView>();
                if (exposedParameterFieldViews.Any())
                {
                    foreach (var paramFieldView in exposedParameterFieldViews)
                    {
                        RegisterCompleteObjectUndo("Create Parameter Node");
                        var paramNode = BaseNode.CreateFromType< ParameterNode >(mousePos);
                        paramNode.parameterGUID = paramFieldView.parameter.guid;
                        AddNode(paramNode);
                    }
                }
            }

            // External objects drag and drop
            if (DragAndDrop.objectReferences.Length > 0) {
                RegisterCompleteObjectUndo("Create Node From Object(s)");
                foreach (var obj in DragAndDrop.objectReferences) {
                    var objectType = obj.GetType();

                    foreach (var kp in nodeTypePerCreateAssetType) {
                        if (kp.Key.IsAssignableFrom(objectType)) {
                            try {
                                var node = BaseNode.CreateFromType(kp.Value.nodeType, mousePos);
                                if ((bool)kp.Value.initalizeNodeFromObject.Invoke(node, new []{obj}))
                                    AddNode(node);
                                else
                                    break;	
                            }
                            catch (Exception exception) {
                                Debug.LogException(exception);
                            }
                        }
                    }
                }
            }
        }

        protected virtual void KeyDownCallback(KeyDownEvent e) {
            // if (e.keyCode == KeyCode.S && e.commandKey) {
            //     SaveGraphToDisk();
            //     e.StopPropagation();
            // }
            // else if (nodeViews.Count > 0 && e.commandKey && e.altKey) {
            //     //	Node Aligning shortcuts
            //     switch (e.keyCode) {
            //         case KeyCode.LeftArrow:
            //             nodeViews[0].AlignToLeft();
            //             e.StopPropagation();
            //             break;
            //         case KeyCode.RightArrow:
            //             nodeViews[0].AlignToRight();
            //             e.StopPropagation();
            //             break;
            //         case KeyCode.UpArrow:
            //             nodeViews[0].AlignToTop();
            //             e.StopPropagation();
            //             break;
            //         case KeyCode.DownArrow:
            //             nodeViews[0].AlignToBottom();
            //             e.StopPropagation();
            //             break;
            //         case KeyCode.C:
            //             nodeViews[0].AlignToCenter();
            //             e.StopPropagation();
            //             break;
            //         case KeyCode.M:
            //             nodeViews[0].AlignToMiddle();
            //             e.StopPropagation();
            //             break;
            //     }
            // }
        }

        void MouseUpCallback(MouseUpEvent e) {
            // schedule.Execute(() => {
            //     if (DoesSelectionContainsInspectorNodes())
            //         UpdateNodeInspectorSelection();
            // }).ExecuteLater(1);
        }

        void MouseDownCallback(MouseDownEvent e) {
            // When left clicking on the graph (not a node or something else)
            // if (e.button == 0) {
            //     // Close all settings windows:
            //     nodeViews.ForEach(v => v.CloseSettings());
            // }
        }

        void DragUpdatedCallback(DragUpdatedEvent e) {
            var dragData = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;
            var dragObjects = DragAndDrop.objectReferences;
            bool dragging = false;

            if (dragData != null) {
                // Handle drag from exposed parameter view
                if (dragData.OfType<ExposedParameterFieldView>().Any())
                {
                    dragging = true;
                }
            }

            if (dragObjects.Length > 0)
                dragging = true;

            if (dragging)
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
        }
        
        void GraphChangesCallback(GraphChanges changes)
        {
            if (changes.removedEdge != null)
            {
                var edge = edgeViews.FirstOrDefault(e => e.edgeData == changes.removedEdge);

                DisconnectView(edge);
            }
        }

        #endregion

        void RemoveNodeViews() {
            foreach (var nodeView in nodeViews)
                RemoveElement(nodeView);
            nodeViews.Clear();
            nodeViewsPerNode.Clear();
        }

        protected virtual void Reload() {
        }

        #region Override

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
            var compatiblePorts = new List<Port>();

            compatiblePorts.AddRange(ports.ToList().Where(p => {
                var portView = p as BasePortView;

                // 同一个Node的节点不能连
                if (portView.owner == (startPort as BasePortView).owner)
                    return false;

                // 相同方向的不能连
                if (p.direction == startPort.direction)
                    return false;

                // 判断两个点之间是不是已经连了
                if (portView.GetEdges().Any(e => e.input == startPort || e.output == startPort))
                    return false;

                // 判断两个节点的类型能不能连
                if (!BaseGraph.TypesAreConnectable(startPort.portType, p.portType))
                    return false;

                return true;
            }));

            return compatiblePorts;
        }

        #endregion

        #region 编辑器操作

        public void ResetPositionAndZoom()
        {
            graph.position = Vector3.zero;
            graph.scale = Vector3.one;

            UpdateViewTransform(graph.position, graph.scale);
        }
        
        public Status GetPinnedElementStatus< T >() where T : PinnedElementView
        {
            return GetPinnedElementStatus(typeof(T));
        }
        
        public Status GetPinnedElementStatus(Type type)
        {
            var pinned = graph.pinnedElements.Find(p => p.editorType.type == type);

            if (pinned != null && pinned.opened)
                return Status.Normal;
            else
                return Status.Hidden;
        }
        
        public void ToggleView< T >() where T : PinnedElementView
        {
            ToggleView(typeof(T));
        }

        public void ToggleView(Type type)
        {
            PinnedElementView view;
            pinnedElements.TryGetValue(type, out view);

            if (view == null)
                OpenPinned(type);
            else
                ClosePinned(type, view);
        }
        
        public void OpenPinned< T >() where T : PinnedElementView
        {
            OpenPinned(typeof(T));
        }

        public void OpenPinned(Type type)
        {
            PinnedElementView view;

            if (type == null)
                return ;

            PinnedElement elem = graph.OpenPinned(type);

            if (!pinnedElements.ContainsKey(type))
            {
                view = Activator.CreateInstance(type) as PinnedElementView;
                if (view == null)
                    return ;
                pinnedElements[type] = view;
                view.InitializeGraphView(elem, this);
            }
            view = pinnedElements[type];

            if (!Contains(view))
                Add(view);
        }

        public void ClosePinned< T >(PinnedElementView view) where T : PinnedElementView
        {
            ClosePinned(typeof(T), view);
        }

        public void ClosePinned(Type type, PinnedElementView elem)
        {
            pinnedElements.Remove(type);
            Remove(elem);
            graph.ClosePinned(type);
        }
        
        void RemovePinnedElementViews()
        {
            foreach (var pinnedView in pinnedElements.Values)
            {
                if (Contains(pinnedView))
                    Remove(pinnedView);
            }
            pinnedElements.Clear();
        }

        #endregion

        #region ExposedParams

        /// <summary>
        /// Workaround object for creating exposed parameter property fields.
        /// </summary>
        public ExposedParameterFieldFactory exposedParameterFactory { get; private set; }
        
        void OnExposedParameterListChanged()
        {
            UpdateSerializedProperties();
            onExposedParameterListChanged?.Invoke();
        }

        #endregion
        
        public void ClearGraphElements()
        {
    
            RemoveNodeViews();
            RemoveEdges();
          
            RemovePinnedElementViews();
        }
        
        public void Dispose() {
            ClearGraphElements();
            RemoveFromHierarchy();
            Undo.undoRedoPerformed -= ReloadView;
            // Object.DestroyImmediate(nodeInspector);
            NodeProvider.UnloadGraph(graph);
            exposedParameterFactory.Dispose();
            exposedParameterFactory = null;

            graph.onExposedParameterListChanged -= OnExposedParameterListChanged;
            graph.onExposedParameterModified += (s) => onExposedParameterModified?.Invoke(s);
            graph.onGraphChanges -= GraphChangesCallback;
        }
    }
}