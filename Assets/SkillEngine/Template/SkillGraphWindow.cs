using SkillEngine;
using UnityEditor;
using UnityEngine;

public class SkillGraphWindow : BaseGraphWindow {
    BaseGraph tmpGraph;
    CustomToolbarView	toolbarView;

    // CustomToolbarView	toolbarView;
    [MenuItem("SkillEngine/SkillWindow")]
    public static BaseGraphWindow OpenWithTmpGraph() {
        var graphWindow = CreateWindow<SkillGraphWindow>();

        // When the graph is opened from the window, we don't save the graph to disk
        graphWindow.tmpGraph = ScriptableObject.CreateInstance<BaseGraph>();
        graphWindow.tmpGraph.hideFlags = HideFlags.HideAndDontSave;
        graphWindow.InitOnOpenSO(graphWindow.tmpGraph);

        graphWindow.Show();

        return graphWindow;
    }
    protected override void OnDestroy()
    {
        graphView?.Dispose();
        DestroyImmediate(tmpGraph);
    }

    
    protected override void InitializeWindow(BaseGraph graph) {
        titleContent = new GUIContent("技能流图");

        if (graphView == null) {
            graphView = new TestGraphView(this, true);
            toolbarView = new CustomToolbarView(graphView);
            graphView.Add(toolbarView);
        }

        rootView.Add(graphView);
    }

    protected override void InitializeGraphView(BaseGraphView view) {
    }
}