using UnityEngine;
using UnityEngine.UIElements;
using EdgeView = UnityEditor.Experimental.GraphView.Edge;

namespace SkillEngine {
    public class BaseEdgeView : EdgeView {
        public bool isConnected = false;

        public BaseEdge edgeData { get { return userData as BaseEdge; } }

        readonly string edgeStyle = "GraphProcessorStyles/EdgeView";
        
        protected BaseGraphView		owner => ((input ?? output) as BasePortView).owner.owner;
        
        public BaseEdgeView() : base()
        {
            styleSheets.Add(Resources.Load<StyleSheet>(edgeStyle));
            RegisterCallback<MouseDownEvent>(OnMouseDown);
        }
        
        
        public void UpdateEdgeSize()
        {
            if (input == null && output == null)
                return;

            PortData inputPortData = (input as BasePortView)?.portData;
            PortData outputPortData = (output as BasePortView)?.portData;

            for (int i = 1; i < 20; i++)
                RemoveFromClassList($"edge_{i}");
            int maxPortSize = Mathf.Max(inputPortData?.sizeInPixel ?? 0, outputPortData?.sizeInPixel ?? 0);
            if (maxPortSize > 0)
                AddToClassList($"edge_{Mathf.Max(1, maxPortSize - 6)}");
        }

        
        #region Events
        
        public override void OnPortChanged(bool isInput)
        {
            base.OnPortChanged(isInput);
            UpdateEdgeSize();
        }
        void OnMouseDown(MouseDownEvent e)
        {
            //Todo:双击添加RelayNode
            
            // if (e.clickCount == 2)
            // {
            //     // Empirical offset:
            //     var position = e.mousePosition;
            //     position += new Vector2(-10f, -28);
            //     Vector2 mousePos = owner.ChangeCoordinatesTo(owner.contentViewContainer, position);
            //
            //     owner.AddRelayNode(input as BasePortView, output as BasePortView, mousePos);
            // }
        }
        
        protected override void OnCustomStyleResolved(ICustomStyle styles)
        {
            base.OnCustomStyleResolved(styles);

            UpdateEdgeControl();
        }


        #endregion
    }
}