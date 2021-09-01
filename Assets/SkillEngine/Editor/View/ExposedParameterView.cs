using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEngine {
    public class ExposedParameterView : PinnedElementView {
        protected BaseGraphView graphView;

        new const string title = "全局变量";

        readonly string exposedParameterViewStyle = "GraphProcessorStyles/ExposedParameterView";

        List<Rect> blackboardLayouts = new List<Rect>();

        public ExposedParameterView() {
            var style = Resources.Load<StyleSheet>(exposedParameterViewStyle);
            if (style != null)
                styleSheets.Add(style);
        }

        protected override void Initialize(BaseGraphView graphView) {
            this.graphView = graphView;
            base.title = title;
            scrollable = true;

            graphView.onExposedParameterListChanged += UpdateParameterList;
            graphView.initialized += UpdateParameterList;
            Undo.undoRedoPerformed += UpdateParameterList;

            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            RegisterCallback<MouseDownEvent>(OnMouseDownEvent, TrickleDown.TrickleDown);
            RegisterCallback<DetachFromPanelEvent>(OnViewClosed);

            UpdateParameterList();

            // Add exposed parameter button
            header.Add(new Button(OnAddClicked) {
                text = "+"
            });
        }


        #region Api

        protected virtual IEnumerable<Type> GetExposedParameterTypes() {
            foreach (var type in TypeCache.GetTypesDerivedFrom<ExposedParameter>()) {
                if (type.IsGenericType)
                    continue;

                yield return type;
            }
        }

        protected string GetNiceNameFromType(Type type) {
            string name = type.Name;

            // Remove parameter in the name of the type if it exists
            name = name.Replace("Parameter", "");

            return ObjectNames.NicifyVariableName(name);
        }

        protected string GetUniqueExposedPropertyName(string name) {
            // Generate unique name
            string uniqueName = name;
            int i = 0;
            while (graphView.graph.exposedParameters.Any(e => e.name == name))
                name = uniqueName + " " + i++;
            return name;
        }

        protected virtual void UpdateParameterList() {
            content.Clear();

            foreach (var param in graphView.graph.exposedParameters) {
                var row = new BlackboardRow(new ExposedParameterFieldView(graphView, param),
                    new ExposedParameterPropertyView(graphView, param));
                row.expanded = param.settings.expanded;
                row.RegisterCallback<GeometryChangedEvent>(e => { param.settings.expanded = row.expanded; });
                
                content.Add(row);
                
                var valueElement = graphView.exposedParameterFactory.GetParameterOnlyValField(param, (newValue) => {
                    param.value = newValue;
                });
                valueElement.AddToClassList("test");
                content.Add(valueElement);
                
               
            }
        }
        
        int GetInsertIndexFromMousePosition(Vector2 pos)
        {
            pos = content.WorldToLocal(pos);
            // We only need to look for y axis;
            float mousePos = pos.y;

            if (mousePos < 0)
                return 0;

            int index = 0;
            foreach (var layout in blackboardLayouts)
            {
                if (mousePos > layout.yMin && mousePos < layout.yMax)
                    return index + 1;
                index++;
            }

            return content.childCount;
        }

        #endregion

        #region Events

        protected virtual void OnAddClicked() {
            var parameterType = new GenericMenu();

            foreach (var paramType in GetExposedParameterTypes())
                parameterType.AddItem(new GUIContent(GetNiceNameFromType(paramType)), false, () => {
                    string uniqueName = "New " + GetNiceNameFromType(paramType);

                    uniqueName = GetUniqueExposedPropertyName(uniqueName);
                    graphView.graph.AddExposedParameter(uniqueName, paramType);
                });

            parameterType.ShowAsContext();
        }
        
        void OnViewClosed(DetachFromPanelEvent evt)
            => Undo.undoRedoPerformed -= UpdateParameterList;

        void OnMouseDownEvent(MouseDownEvent evt)
        {
            blackboardLayouts = content.Children().Select(c => c.layout).ToList();
        }
        
        void OnDragUpdatedEvent(DragUpdatedEvent evt) {
            // DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            int newIndex = GetInsertIndexFromMousePosition(evt.mousePosition);
            var graphSelectionDragData = DragAndDrop.GetGenericData("DragSelection");

            if (graphSelectionDragData == null)
                return;
            else {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            }

            // foreach (var obj in graphSelectionDragData as List<ISelectable>) {
            //     if (obj is ExposedParameterFieldView view) {
            //         var blackBoardRow = view.parent.parent.parent.parent.parent.parent;
            //         int oldIndex = content.Children().ToList().FindIndex(c => c == blackBoardRow);
            //         // Try to find the blackboard row
            //         content.Remove(blackBoardRow);
            //
            //         if (newIndex > oldIndex)
            //             newIndex--;
            //
            //         content.Insert(newIndex, blackBoardRow);
            //     }
            // }
        }
        
        /// <summary>
        /// 拖拽换位功能，不太需要
        /// </summary>
        /// <param name="evt"></param>
        void OnDragPerformEvent(DragPerformEvent evt) {
            // bool updateList = false;
            //
            // int newIndex = GetInsertIndexFromMousePosition(evt.mousePosition);
            // foreach (var obj in DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>) {
            //     if (obj is ExposedParameterFieldView view) {
            //         if (!updateList)
            //             graphView.RegisterCompleteObjectUndo("Moved parameters");
            //
            //         int oldIndex = graphView.graph.exposedParameters.FindIndex(e => e == view.parameter);
            //         var parameter = graphView.graph.exposedParameters[oldIndex];
            //         graphView.graph.exposedParameters.RemoveAt(oldIndex);
            //
            //         // Patch new index after the remove operation:
            //         if (newIndex > oldIndex)
            //             newIndex--;
            //
            //         graphView.graph.exposedParameters.Insert(newIndex, parameter);
            //
            //         updateList = true;
            //     }
            // }
            //
            // if (updateList) {
            //     evt.StopImmediatePropagation();
            //     UpdateParameterList();
            // }
        }

        #endregion
    }
}