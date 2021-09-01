using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace SkillEngine{
    public class BaseEdgeConnectorListener : IEdgeConnectorListener{
        public readonly BaseGraphView graphView;
        Dictionary<Edge, BasePortView> edgeInputPorts = new Dictionary<Edge, BasePortView>();
        Dictionary<Edge, BasePortView> edgeOutputPorts = new Dictionary<Edge, BasePortView>();

        public BaseEdgeConnectorListener(BaseGraphView graphView){
            this.graphView = graphView;
        }


        public void OnDropOutsidePort(Edge edge, Vector2 position){
            // Debug.Log("<color=green>--- EdgeConnectorListener.OnDropOutsidePort ---</color>");
            this.graphView.RegisterCompleteObjectUndo("Disconnect edge");
            
            //If the edge was already existing, remove it
            if (!edge.isGhostEdge)
                graphView.Disconnect(edge as BaseEdgeView);
            
            // // when on of the port is null, then the edge was created and dropped outside of a port
            // if (edge.input == null || edge.output == null)
            //     ShowNodeCreationMenuFromEdge(edge as EdgeView, position);
        }

        public void OnDrop(GraphView graphView, Edge edge){
            // Debug.Log("<color=green>--- EdgeConnectorListener.OnDrop ---</color>");
            var edgeView = edge as BaseEdgeView;
            bool wasOnTheSamePort = false;

            if (edgeView?.input == null || edgeView?.output == null)
                return ;

            //If the edge was moved to another port
            if (edgeView.isConnected)
            {
                if (edgeInputPorts.ContainsKey(edge) && edgeOutputPorts.ContainsKey(edge))
                    if (edgeInputPorts[edge] == edge.input && edgeOutputPorts[edge] == edge.output)
                        wasOnTheSamePort = true;
            
                if (!wasOnTheSamePort)
                    this.graphView.Disconnect(edgeView);
            }
            
            if (edgeView.input.node == null || edgeView.output.node == null)
                return;

            edgeInputPorts[edge] = edge.input as BasePortView;
            edgeOutputPorts[edge] = edge.output as BasePortView;
            try
            {
                this.graphView.RegisterCompleteObjectUndo("Connected " + edgeView.input.node.name + " and " + edgeView.output.node.name);
                if (!this.graphView.Connect(edge as BaseEdgeView, autoDisconnectInputs: !wasOnTheSamePort))
                    this.graphView.Disconnect(edge as BaseEdgeView);
            } catch (System.Exception e)
            {
                Debug.Log(e);
                this.graphView.Disconnect(edge as BaseEdgeView);
            }
        }
    }
}