using System;
using System.Linq;
using SkillEngine;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[NodeCustomEditor(typeof(ParameterNode))]
public class ParameterNodeView : BaseNodeView
{
    ParameterNode parameterNode;

    public override void Enable(bool fromInspector = false)
    {
        parameterNode = nodeTarget as ParameterNode;

        EnumField accessorSelector = new EnumField(parameterNode.accessor);
        accessorSelector.SetValueWithoutNotify(parameterNode.accessor);
        accessorSelector.RegisterValueChangedCallback(evt =>
        {
            parameterNode.accessor = (ParameterAccessor)evt.newValue;
            UpdatePort();
            controlsContainer.MarkDirtyRepaint();
            ForceUpdatePorts();
        });
        
        UpdatePort();
        controlsContainer.Add(accessorSelector);
        
        //    Find and remove expand/collapse button
        titleContainer.Remove(titleContainer.Q("title-button-container"));
        //    Remove Port from the #content
        topContainer.parent.Remove(topContainer);
        //    Add Port to the #title
        titleContainer.Add(topContainer);

        parameterNode.onParameterChanged += UpdateView;
        UpdateView();
    }

    void UpdateView()
    {
        title = parameterNode.parameter?.name;
    }
    
    void UpdatePort()
    {
        if(parameterNode.accessor == ParameterAccessor.Set)
        {
            titleContainer.AddToClassList("input");
        }
        else
        {
            titleContainer.RemoveFromClassList("input");
        }
    }
    
    #region 更新NodeView

    public void ForceUpdatePorts() {
        var node = nodeTarget as ParameterNode;
        node.UpdateAllPorts();
        var listener = owner.connectorListener;
        switch (node.accessor) {
            case ParameterAccessor.Get:
                RemovePort(inputPortViews[0]);
                var outputPort = nodeTarget.outputPorts.Values.ToList()[0];
                AddPort(outputPort.fieldInfo, Direction.Output, listener, outputPort.portData);
                break;
            
            case ParameterAccessor.Set:
                RemovePort(outputPortViews[0]);
                var inputPort = nodeTarget.inputPorts.Values.ToList()[0];
                AddPort(inputPort.fieldInfo, Direction.Input, listener, inputPort.portData);
                break;
        }
       
        RefreshPorts();
    }

    #endregion
}


















































