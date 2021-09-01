using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using PortView = UnityEditor.Experimental.GraphView.Port;

namespace SkillEngine {
    public class BasePortView : PortView {
       readonly string portStyle = "GraphProcessorStyles/PortView";
        string userPortStyleFile = "PortViewTypes";
        public BaseNodeView owner{ get; private set; }
        public PortData portData;
        protected FieldInfo fieldInfo;
        protected BaseEdgeConnectorListener listener;
        public string fieldName => fieldInfo.Name;
        public Type fieldType => fieldInfo.FieldType;
        public new Type portType;

        public event Action<PortView, Edge> OnConnected;
        public event Action<PortView, Edge> OnDisconnected;

        List<BaseEdgeView> edges = new List<BaseEdgeView>();
        public int connectionCount => edges.Count;



        protected BasePortView(Direction direction, FieldInfo fieldInfo, PortData portData,
            BaseEdgeConnectorListener edgeConnectorListener)
            : base(portData.vertical ? Orientation.Vertical : Orientation.Horizontal, direction, Capacity.Multi,
                portData.displayType ?? fieldInfo.FieldType){
            this.fieldInfo = fieldInfo;
            this.listener = edgeConnectorListener;
            this.portType = portData.displayType ?? fieldInfo.FieldType;
            this.portData = portData;
            this.portName = fieldName;

            styleSheets.Add(Resources.Load<StyleSheet>(portStyle));

            UpdatePortSize();

            var userPortStyle = Resources.Load<StyleSheet>(userPortStyleFile);
            if (userPortStyle != null)
                styleSheets.Add(userPortStyle);

            if (portData.vertical)
                AddToClassList("Vertical");

            this.tooltip = portData.tooltip;
        }

        public static BasePortView CreatePortView(Direction direction, FieldInfo fieldInfo, PortData portData,
            BaseEdgeConnectorListener edgeConnectorListener){
            var pv = new BasePortView(direction, fieldInfo, portData, edgeConnectorListener);
            pv.m_EdgeConnector = new BaseEdgeConnector(edgeConnectorListener);
            pv.AddManipulator(pv.m_EdgeConnector);

            // Force picking in the port label to enlarge the edge creation zone
            var portLabel = pv.Q("type");
            if (portLabel != null){
                portLabel.pickingMode = PickingMode.Position;
                portLabel.style.flexGrow = 1;
            }

            // hide label when the port is vertical
            if (portData.vertical && portLabel != null)
                portLabel.style.display = DisplayStyle.None;

            // Fixup picking mode for vertical top ports
            if (portData.vertical)
                pv.Q("connector").pickingMode = PickingMode.Position;

            return pv;
        }

        public virtual void Initialize(BaseNodeView nodeView, string name){
            this.owner = nodeView;
            AddToClassList(fieldName);

            // Correct port type if port accept multiple values (and so is a container)
            if (direction == Direction.Input && portData.acceptMultipleEdges && portType == fieldType
            ) // If the user haven't set a custom field type
            {
                if (fieldType.GetGenericArguments().Length > 0)
                    portType = fieldType.GetGenericArguments()[0];
            }

            if (name != null)
                portName = name;
            visualClass = "Port_" + portType.Name;
            tooltip = portData.tooltip;
        }


        /// <summary>
        /// Update the size of the port view (using the portData.sizeInPixel property)
        /// </summary>
        public void UpdatePortSize(){
            int size = portData.sizeInPixel == 0 ? 8 : portData.sizeInPixel;
            var connector = this.Q("connector");
            var cap = connector.Q("cap");
            connector.style.width = size;
            connector.style.height = size;
            cap.style.width = size - 4;
            cap.style.height = size - 4;

            // Update connected edge sizes:
            // edges.ForEach(e => e.UpdateEdgeSize());
        }

        public override void Connect(Edge edge){
            OnConnected?.Invoke(this, edge);

            base.Connect(edge);

            var inputNode = (edge.input as BasePortView).owner;
            var outputNode = (edge.output as BasePortView).owner;

            edges.Add(edge as BaseEdgeView);
            
            inputNode.OnPortConnected(edge.input as BasePortView);
            outputNode.OnPortConnected(edge.output as BasePortView);
        }
        

        public override void Disconnect(Edge edge){
            OnDisconnected?.Invoke(this, edge);

            base.Disconnect(edge);
            
            if (!(edge as BaseEdgeView).isConnected)
                return;
            
            var inputNode = (edge.input as BasePortView).owner;
            var outputNode = (edge.output as BasePortView).owner;
            
            inputNode.OnPortDisconnected(edge.input as BasePortView);
            outputNode.OnPortDisconnected(edge.output as BasePortView);

            edges.Remove(edge as BaseEdgeView);
        }

        public void UpdatePortView(PortData data){
            if (data.displayType != null){
                base.portType = data.displayType;
                portType = data.displayType;
                visualClass = "Port_" + portType.Name;
            }

            if (!String.IsNullOrEmpty(data.displayName))
                base.portName = data.displayName;

            portData = data;

            // Update the edge in case the port color have changed
            schedule.Execute(() => {
                foreach (var edge in edges){
                    edge.UpdateEdgeControl();
                    edge.MarkDirtyRepaint();
                }
            }).ExecuteLater(50); // Hummm

            UpdatePortSize();
        }

        public List<BaseEdgeView> GetEdges(){
            return edges;
        }
    }
}