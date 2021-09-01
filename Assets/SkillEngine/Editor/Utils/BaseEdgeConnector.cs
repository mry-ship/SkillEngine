using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEngine{
    public class BaseEdgeConnector : EdgeConnector{
        protected BaseEdgeDragHelper dragHelper;
        public override EdgeDragHelper edgeDragHelper => dragHelper;

        Edge edgeCandidate;
        protected bool active;
        Vector2 mouseDownPosition;
        protected BaseGraphView graphView;

        internal const float k_ConnectionDistanceTreshold = 10f;
        public BaseEdgeConnector(IEdgeConnectorListener listener) : base()
        {
            graphView = (listener as BaseEdgeConnectorListener)?.graphView;
            active = false;
            InitEdgeConnector(listener);
        }

        protected virtual void InitEdgeConnector(IEdgeConnectorListener listener)
        {
            dragHelper = new BaseEdgeDragHelper(listener);
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        protected override void RegisterCallbacksOnTarget(){
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        protected override void UnregisterCallbacksFromTarget(){
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        }
        protected virtual void OnMouseUp(MouseUpEvent e) {
            // Debug.Log("<color=red>--- BaseEdgeConnector.OnMouseUp ---</color>");
            if (!active || !CanStopManipulation(e))
                return;

            if (CanPerformConnection(e.localMousePosition))
                edgeDragHelper.HandleMouseUp(e);
            else
                Abort();

            active = false;
            edgeCandidate = null;
            target.ReleaseMouse();
            e.StopPropagation();
        }

        protected virtual void OnMouseMove(MouseMoveEvent e) {
            if (!active) return;

            edgeDragHelper.HandleMouseMove(e);
            edgeCandidate.candidatePosition = e.mousePosition;
            edgeCandidate.UpdateEdgeControl();
            e.StopPropagation();
        }

        protected virtual void OnMouseDown(MouseDownEvent e) {
            // Debug.Log("<color=red>--- BaseEdgeConnector.OnMouseDown ---</color>");
            if (active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(e))
            {
                return;
            }

            var graphElement = target as Port;
            if (graphElement == null)
            {
                return;
            }

            mouseDownPosition = e.localMousePosition;

            edgeCandidate = graphView != null ? graphView.CreateEdgeView() : new BaseEdgeView();
            edgeDragHelper.draggedPort = graphElement;
            edgeDragHelper.edgeCandidate = edgeCandidate;

            if (edgeDragHelper.HandleMouseDown(e))
            {
                active = true;
                target.CaptureMouse();

                e.StopPropagation();
            }
            else
            {
                edgeDragHelper.Reset();
                edgeCandidate = null;
            }
        }

        void Abort()
        {
            var graphView = target?.GetFirstAncestorOfType<GraphView>();
            graphView?.RemoveElement(edgeCandidate);

            edgeCandidate.input = null;
            edgeCandidate.output = null;
            edgeCandidate = null;

            edgeDragHelper.Reset();
        }
        bool CanPerformConnection(Vector2 mousePosition)
        {
            return Vector2.Distance(mouseDownPosition, mousePosition) > k_ConnectionDistanceTreshold;
        }
    }
}