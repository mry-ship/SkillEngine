using SkillEngine;
using UnityEditor;
using UnityEngine;

public class TestWindow :BaseGraphWindow {
    BaseGraph tmpGraph;

    // CustomToolbarView	toolbarView;
    [MenuItem("SkillEngine/测试")]
    public static BaseGraphWindow OpenWithTmpGraph(){
        var graphWindow = CreateWindow<TestWindow>();

        // When the graph is opened from the window, we don't save the graph to disk
        graphWindow.tmpGraph = ScriptableObject.CreateInstance<BaseGraph>();
        graphWindow.tmpGraph.hideFlags = HideFlags.HideAndDontSave;
        graphWindow.InitOnOpenSO(graphWindow.tmpGraph);

        graphWindow.Show();

        return graphWindow;
    }

    protected override void InitializeWindow(BaseGraph graph){
        titleContent = new GUIContent("测试");

        if (graphView == null){
            graphView = new TestGraphView(this, true);
            // graphView.Add(toolbarView);
        }

        rootView.Add(graphView);
    }

    protected override void InitializeGraphView(BaseGraphView view){

    }
}