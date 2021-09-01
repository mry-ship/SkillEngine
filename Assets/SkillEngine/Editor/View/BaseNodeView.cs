using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using NodeView = UnityEditor.Experimental.GraphView.Node;

namespace SkillEngine {
    public class BaseNodeView : NodeView {
        readonly string baseNodeStyle = "GraphProcessorStyles/BaseNodeView";

        public BaseNode nodeTarget;

        #region 元素容器

        public BaseGraphView owner { private set; get; }

        public List<BasePortView> inputPortViews = new List<BasePortView>();
        public List<BasePortView> outputPortViews = new List<BasePortView>();

        public Dictionary<string, List<BasePortView>> portsPerFieldName = new Dictionary<string, List<BasePortView>>();

        public VisualElement controlsContainer;
        protected VisualElement debugContainer;
        protected VisualElement rightTitleContainer;
        protected VisualElement topPortContainer;
        protected VisualElement bottomPortContainer;
        private VisualElement inputContainerElement;

        protected virtual bool hasSettings { get; set; }
        VisualElement settings;

        // Todo: 带配置的节点
        // NodeSettingsView settingsContainer;
        Button settingButton;
        TextField titleTextField;

        Label computeOrderLabel = new Label();

        #endregion

        #region Events

        public event Action<BasePortView> onPortConnected;
        public event Action<BasePortView> onPortDisconnected;
        public bool initializing = false; //Used for applying SetPosition on locked node at init.

        #endregion

        Dictionary<string, List<(object value, VisualElement target)>> visibleConditions =
            new Dictionary<string, List<(object value, VisualElement target)>>();

        Dictionary<string, VisualElement> hideElementIfConnected = new Dictionary<string, VisualElement>();
        Dictionary<FieldInfo, List<VisualElement>> fieldControlsMap = new Dictionary<FieldInfo, List<VisualElement>>();

        #region Overrides

        public virtual void Enable(bool fromInspector = false) => DrawDefaultInspector(fromInspector);
        public virtual void Enable() => DrawDefaultInspector(false);

        public virtual void Disable() {
        }

        protected void AddInputContainer() {
            inputContainerElement = new VisualElement {name = "input-container"};
            mainContainer.parent.Add(inputContainerElement);
            inputContainerElement.SendToBack();
            inputContainerElement.pickingMode = PickingMode.Ignore;
        }

        protected virtual void DrawDefaultInspector(bool fromInspector = false) {
            var fields = nodeTarget.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                // Filter fields from the BaseNode type since we are only interested in user-defined fields
                // (better than BindingFlags.DeclaredOnly because we keep any inherited user-defined fields) 
                .Where(f => f.DeclaringType != typeof(BaseNode));

            // 排序，按继承顺序
            fields = nodeTarget.OverrideFieldOrder(fields);

            // LayOut的排版是通过添加空的元素来占位的
            foreach (var field in fields) {
                //skip if the field is a node setting
                if (field.GetCustomAttribute(typeof(ShowAsDrawer.SettingAttribute)) != null) {
                    hasSettings = true;
                    continue;
                }

                //skip if the field is not serializable
                if ((!field.IsPublic && field.GetCustomAttribute(typeof(SerializeField)) == null) ||
                    field.IsNotSerialized) {
                    AddEmptyField(field, fromInspector);
                    continue;
                }

                //skip if the field is an input/output and not marked as SerializedField
                bool hasInputAttribute = field.GetCustomAttribute(typeof(InputAttribute)) != null;
                bool hasInputOrOutputAttribute =
                    hasInputAttribute || field.GetCustomAttribute(typeof(OutputAttribute)) != null;
                bool showAsDrawer = !fromInspector && field.GetCustomAttribute(typeof(ShowAsDrawer)) != null;
                if (field.GetCustomAttribute(typeof(SerializeField)) == null && hasInputOrOutputAttribute &&
                    !showAsDrawer) {
                    AddEmptyField(field, fromInspector);
                    continue;
                }

                //skip if marked with NonSerialized or HideInInspector
                if (field.GetCustomAttribute(typeof(System.NonSerializedAttribute)) != null ||
                    field.GetCustomAttribute(typeof(HideInInspector)) != null) {
                    AddEmptyField(field, fromInspector);
                    continue;
                }

                // Hide the field if we want to display in in the inspector
                var showInInspector = field.GetCustomAttribute<ShowInView>();
                if (showInInspector != null && !showInInspector.showInNode && !fromInspector) {
                    AddEmptyField(field, fromInspector);
                    continue;
                }

                var showInputDrawer = field.GetCustomAttribute(typeof(InputAttribute)) != null &&
                                      field.GetCustomAttribute(typeof(SerializeField)) != null;
                showInputDrawer |= field.GetCustomAttribute(typeof(InputAttribute)) != null &&
                                   field.GetCustomAttribute(typeof(ShowAsDrawer)) != null;
                showInputDrawer &= !fromInspector; // We can't show a drawer in the inspector
                showInputDrawer &= !typeof(IList).IsAssignableFrom(field.FieldType);

                string displayName = ObjectNames.NicifyVariableName(field.Name);

                var inspectorNameAttribute = field.GetCustomAttribute<InspectorNameAttribute>();
                if (inspectorNameAttribute != null)
                    displayName = inspectorNameAttribute.displayName;

                var elem = AddControlField(field, displayName, showInputDrawer);
                if (hasInputAttribute) {
                    hideElementIfConnected[field.Name] = elem;

                    // Hide the field right away if there is already a connection:
                    if (portsPerFieldName.TryGetValue(field.Name, out var pvs))
                        if (pvs.Any(pv => pv.GetEdges().Count > 0))
                            elem.style.display = DisplayStyle.None;
                }
            }
        }

        protected VisualElement AddControlField(FieldInfo field, string label = null, bool showInputDrawer = false,
            Action valueChangedCallback = null) {
            if (field == null)
                return null;
            // 前面的是绑定的字段，后面的是Labl名字；是input的，不显示Label，OutPut的显示label；这个元素跟属性面板的一样，只是放到哪里显示的问题
            var element = new PropertyField(FindSerializedProperty(field.Name), showInputDrawer ? "" : label);
            element.Bind(owner.serializedGraph);

#if UNITY_2020_3 // In Unity 2020.3 the empty label on property field doesn't hide it, so we do it manually
			if ((showInputDrawer || String.IsNullOrEmpty(label)) && element != null)
				element.AddToClassList("DrawerField_2020_3");
#endif

            if (typeof(IList).IsAssignableFrom(field.FieldType))
                EnableSyncSelectionBorderHeight();

            element.RegisterValueChangeCallback(e => {
                UpdateFieldVisibility(field.Name, field.GetValue(nodeTarget));
                valueChangedCallback?.Invoke();
                NotifyNodeChanged();
            });

            // Todo: 使用场景中的GameObject
            // Disallow picking scene objects when the graph is not linked to a scene
            // if (element != null && !owner.graph.IsLinkedToScene())
            // {
            // 	var objectField = element.Q<ObjectField>();
            // 	if (objectField != null)
            // 		objectField.allowSceneObjects = false;
            // }

            if (!fieldControlsMap.TryGetValue(field, out var inputFieldList))
                inputFieldList = fieldControlsMap[field] = new List<VisualElement>();
            inputFieldList.Add(element);

            // input的放到inputContainer
            // outPut的放到controlsContainer
            if (element != null) {
                if (showInputDrawer) {
                    var box = new VisualElement {name = field.Name};
                    box.AddToClassList("port-input-element");
                    box.Add(element);
                    inputContainerElement.Add(box);
                }
                else {
                    controlsContainer.Add(element);
                }

                element.name = field.Name;
            }
            else {
                // Make sure we create an empty placeholder if FieldFactory can not provide a drawer
                if (showInputDrawer) AddEmptyField(field, false);
            }

            var visibleCondition = field.GetCustomAttribute(typeof(VisibleIf)) as VisibleIf;
            if (visibleCondition != null) {
                // Check if target field exists:
                var conditionField = nodeTarget.GetType().GetField(visibleCondition.fieldName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (conditionField == null)
                    Debug.LogError(
                        $"[VisibleIf] Field {visibleCondition.fieldName} does not exists in node {nodeTarget.GetType()}");
                else {
                    visibleConditions.TryGetValue(visibleCondition.fieldName, out var list);
                    if (list == null)
                        list = visibleConditions[visibleCondition.fieldName] =
                            new List<(object value, VisualElement target)>();
                    list.Add((visibleCondition.value, element));
                    UpdateFieldVisibility(visibleCondition.fieldName, conditionField.GetValue(nodeTarget));
                }
            }

            return element;
        }

        #endregion

        #region 读取SO文件时 Initialization

        public void InitOnOpenSO(BaseGraphView owner, BaseNode node) {
            this.owner = owner;
            nodeTarget = node;

            styleSheets.Add(Resources.Load<StyleSheet>(baseNodeStyle));

            InitializeView();
            InitializePorts();
            // InitializeDebug();
            // If the standard Enable method is still overwritten, we call it
            if (GetType().GetMethod(nameof(Enable), new Type[] { }).DeclaringType != typeof(BaseNodeView)) {
                ExceptionToLog.Call(() => Enable());
            }
            else {
                ExceptionToLog.Call(() => Enable(false));
            }

            RefreshExpandedState();
            this.RefreshPorts();
            // FindSerializedProperty("start");
        }

        /// <summary>
        /// 初始化Node的视图，主要是绑定各个container
        /// </summary>
        private void InitializeView() {
            controlsContainer = new VisualElement {name = "controls"};
            controlsContainer.AddToClassList("NodeControls");
            mainContainer.Add(controlsContainer);

            rightTitleContainer = new VisualElement {name = "RightTitleContainer"};
            titleContainer.Add(rightTitleContainer);

            topPortContainer = new VisualElement {name = "TopPortContainer"};
            this.Insert(0, topPortContainer);

            bottomPortContainer = new VisualElement {name = "BottomPortContainer"};
            this.Add(bottomPortContainer);

            debugContainer = new VisualElement {name = "debug"};


            debugContainer = new VisualElement {name = "debug"};

            initializing = true;

            // 设置title
            UpdateTitle();
            SetPosition(nodeTarget.position);

            AddInputContainer();
        }


        /// <summary>
        /// 根据BaseNode里的inputPorts来添加Port的视图
        /// </summary>
        private void InitializePorts() {
            // 这里将 GraphView的ConnectorListener赋值给Port
            var listener = owner.connectorListener;
            var inputPorts = nodeTarget.inputPorts.Values.ToList();
            foreach (var inputPort in inputPorts) {
                AddPort(inputPort.fieldInfo, Direction.Input, listener, inputPort.portData);
            }

            var outputPorts = nodeTarget.outputPorts.Values.ToList();
            foreach (var outputPort in outputPorts) {
                AddPort(outputPort.fieldInfo, Direction.Output, listener, outputPort.portData);
            }
        }

        void UpdateTitle() {
            title = (nodeTarget.GetCustomName() == null) ? nodeTarget.GetType().Name : nodeTarget.GetCustomName();
            // title = nodeTarget.GUID;
        }

        #endregion

        #region Port Api

        public BasePortView AddPort(FieldInfo fieldInfo, Direction direction, BaseEdgeConnectorListener listener,
            PortData portData) {
            BasePortView p = CreatePortView(direction, fieldInfo, portData, listener);

            if (p.direction == Direction.Input) {
                inputPortViews.Add(p);

                if (portData.vertical)
                    topPortContainer.Add(p);
                else
                    inputContainer.Add(p);
            }
            else {
                outputPortViews.Add(p);

                if (portData.vertical)
                    bottomPortContainer.Add(p);
                else
                    outputContainer.Add(p);
            }

            p.Initialize(this, portData?.displayName);

            List<BasePortView> ports;
            portsPerFieldName.TryGetValue(p.fieldName, out ports);
            if (ports == null) {
                ports = new List<BasePortView>();
                portsPerFieldName[p.fieldName] = ports;
            }

            ports.Add(p);

            return p;
        }

        protected virtual BasePortView CreatePortView(Direction direction, FieldInfo fieldInfo, PortData portData,
            BaseEdgeConnectorListener listener)
            => BasePortView.CreatePortView(direction, fieldInfo, portData, listener);


        public List<BasePortView> GetPortViewsFromFieldName(string fieldName) {
            List<BasePortView> ret;

            portsPerFieldName.TryGetValue(fieldName, out ret);

            return ret;
        }

        public BasePortView GetPortViewFromFieldName(string fieldName, string identifier) {
            return GetPortViewsFromFieldName(fieldName)?.FirstOrDefault(pv => {
                return (pv.portData.identifier == identifier) ||
                       (String.IsNullOrEmpty(pv.portData.identifier) && String.IsNullOrEmpty(identifier));
            });
        }

        public void RemovePort(BasePortView p) {
            // Remove all connected edges:
            var edgesCopy = p.GetEdges().ToList();
            foreach (var e in edgesCopy)
                owner.Disconnect(e, refreshPorts: false);

            if (p.direction == Direction.Input) {
                if (inputPortViews.Remove(p))
                    p.RemoveFromHierarchy();
            }
            else {
                if (outputPortViews.Remove(p))
                    p.RemoveFromHierarchy();
            }

            List<BasePortView> ports;
            portsPerFieldName.TryGetValue(p.fieldName, out ports);
            ports?.Remove(p);
        }

        #endregion

        #region 编辑器操作

        public override void SetPosition(Rect newPos) {
            if (initializing || !nodeTarget.isLocked) {
                base.SetPosition(newPos);

                if (!initializing) {
                    owner.RegisterCompleteObjectUndo("Moved graph node");
                }

                nodeTarget.position = newPos;

                initializing = false;
            }
        }

        public override bool expanded {
            get { return base.expanded; }
            set {
                base.expanded = value;
                nodeTarget.expanded = value;
            }
        }

        #endregion

        #region Events

        // 暂时没用
        public virtual void OnRemoved() {
        }

        public virtual void OnCreated() {
        }

        internal void OnPortConnected(BasePortView port) {
            Debug.Log("<color=green>[BaseNodeView.OnPortConnected]</color>node:" + nodeTarget.name + " port: " +
                      port.portData.displayName);
            if (port.direction == Direction.Input && inputContainerElement?.Q(port.fieldName) != null) {
                inputContainerElement.Q(port.fieldName).AddToClassList("empty");
            }

            if (hideElementIfConnected.TryGetValue(port.fieldName, out var elem))
                elem.style.display = DisplayStyle.None;

            onPortConnected?.Invoke(port);
        }


        internal void OnPortDisconnected(BasePortView port) {
            Debug.Log("<color=red>[BaseNodeView.OnPortDisconnected]</color>node:" + nodeTarget.name + " port: " +
                      port.portData.displayName);
            if (port.direction == Direction.Input && inputContainerElement?.Q(port.fieldName) != null) {
                inputContainerElement.Q(port.fieldName).RemoveFromClassList("empty");

                if (nodeTarget.nodeFields.TryGetValue(port.fieldName, out var fieldInfo)) {
                    var valueBeforeConnection = GetInputFieldValue(fieldInfo.info);

                    if (valueBeforeConnection != null) {
                        fieldInfo.info.SetValue(nodeTarget, valueBeforeConnection);
                    }
                }
            }


            onPortDisconnected?.Invoke(port);
        }

        #endregion

        #region 通用方法

        protected SerializedProperty FindSerializedProperty(string fieldName) {
            // Debug.Log("[BaseNodeView.FindSerializedProperty]fieldName:" + fieldName);
            int i = owner.graph.nodes.FindIndex(n => n == nodeTarget);
            return owner.serializedGraph.FindProperty("nodes").GetArrayElementAtIndex(i)
                .FindPropertyRelative(fieldName);
        }

        VisualElement selectionBorder, nodeBorder;

        internal void EnableSyncSelectionBorderHeight() {
            // Debug.Log("[BaseNodeView.EnableSyncSelectionBorderHeight]");
            if (selectionBorder == null || nodeBorder == null) {
                selectionBorder = this.Q("selection-border");
                nodeBorder = this.Q("node-border");

                schedule.Execute(() => { selectionBorder.style.height = nodeBorder.localBound.height; }).Every(17);
            }
        }

        void UpdateFieldVisibility(string fieldName, object newValue) {
            // Debug.Log("[BaseNodeView.UpdateFieldVisibility]");
            if (newValue == null)
                return;
            if (visibleConditions.TryGetValue(fieldName, out var list)) {
                foreach (var elem in list) {
                    if (newValue.Equals(elem.value))
                        elem.target.style.display = DisplayStyle.Flex;
                    else
                        elem.target.style.display = DisplayStyle.None;
                }
            }
        }


        void UpdateOtherFieldValueSpecific<T>(FieldInfo field, object newValue) {
            // Debug.Log("[BaseNodeView.UpdateOtherFieldValueSpecific]");
            foreach (var inputField in fieldControlsMap[field]) {
                var notify = inputField as INotifyValueChanged<T>;
                if (notify != null)
                    notify.SetValueWithoutNotify((T) newValue);
            }
        }

        object GetInputFieldValueSpecific<T>(FieldInfo field) {
            // Debug.Log("[BaseNodeView.GetInputFieldValueSpecific]");
            if (fieldControlsMap.TryGetValue(field, out var list)) {
                foreach (var inputField in list) {
                    if (inputField is INotifyValueChanged<T> notify)
                        return notify.value;
                }
            }

            return null;
        }

        static MethodInfo specificUpdateOtherFieldValue =
            typeof(BaseNodeView).GetMethod(nameof(UpdateOtherFieldValueSpecific),
                BindingFlags.NonPublic | BindingFlags.Instance);

        void UpdateOtherFieldValue(FieldInfo info, object newValue) {
            // Debug.Log("[BaseNodeView.UpdateOtherFieldValue]");
            // Warning: Keep in sync with FieldFactory CreateField
            var fieldType = info.FieldType.IsSubclassOf(typeof(UnityEngine.Object))
                ? typeof(UnityEngine.Object)
                : info.FieldType;
            var genericUpdate = specificUpdateOtherFieldValue.MakeGenericMethod(fieldType);

            genericUpdate.Invoke(this, new object[] {info, newValue});
        }

        void UpdateFieldValues() {
            // Debug.Log("[BaseNodeView.UpdateFieldValues]");
            foreach (var kp in fieldControlsMap)
                UpdateOtherFieldValue(kp.Key, kp.Key.GetValue(nodeTarget));
        }

        /// <summary>
        /// Send an event to the graph telling that the content of this node have changed
        /// </summary>
        public void NotifyNodeChanged() => owner.graph.NotifyNodeChanged(nodeTarget);

        static MethodInfo specificGetValue = typeof(BaseNodeView).GetMethod(nameof(GetInputFieldValueSpecific),
            BindingFlags.NonPublic | BindingFlags.Instance);

        object GetInputFieldValue(FieldInfo info) {
            // Debug.Log("[BaseNodeView.GetInputFieldValue]");
            // Warning: Keep in sync with FieldFactory CreateField
            var fieldType = info.FieldType.IsSubclassOf(typeof(UnityEngine.Object))
                ? typeof(UnityEngine.Object)
                : info.FieldType;
            var genericUpdate = specificGetValue.MakeGenericMethod(fieldType);

            return genericUpdate.Invoke(this, new object[] {info});
        }

        #endregion

        #region 样式

        private void AddEmptyField(FieldInfo field, bool fromInspector) {
            if (field.GetCustomAttribute(typeof(InputAttribute)) == null || fromInspector)
                return;

            if (field.GetCustomAttribute<VerticalAttribute>() != null)
                return;

            var box = new VisualElement {name = field.Name};
            box.AddToClassList("port-input-element");
            box.AddToClassList("empty");
            inputContainerElement.Add(box);
        }

        #endregion
    }
}