using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


namespace SkillEngine {
    public abstract class BaseGraphWindow : EditorWindow{
        protected VisualElement rootView;
        protected BaseGraphView graphView;

        [SerializeField]
        protected BaseGraph graph;

        readonly string graphWindowStyle = "GraphProcessorStyles/BaseGraphView";

        public event Action<BaseGraph> graphLoaded;
        public event Action<BaseGraph> graphUnloaded;

        private void OnEnable(){
            InitializeRootView();
        }
        
        protected virtual void OnDisable()
        {
            // Debug.Log("WindowOnDisable");
            if (graph != null && graphView != null)
                graphView.SaveGraphToDisk();
            Undo.ClearAll();
        }
        
        protected virtual void OnDestroy(){
        }

        void InitializeRootView(){
            rootView = base.rootVisualElement;

            rootView.name = "graphRootView";

            rootView.styleSheets.Add(Resources.Load<StyleSheet>(graphWindowStyle));
        }

        /// <summary>
        /// 调用该方法会生成graphView，打开Window
        /// 先调用InitializeWindow方法，创建Window，再通过Graph生成GraphView实例，将GraphView放入Window
        /// 最后调用InitializeGraphView，决定当前页面打开时GraphView上打开哪些元素
        /// </summary>
        /// <param name="graph"></param>
        public void InitOnOpenSO(BaseGraph graph){
            // 看不懂
            // 当没关闭当前图，打开另一张图时，会触发
            if (this.graph != null && graph != this.graph){
                // 存盘
                EditorUtility.SetDirty(this.graph);
                AssetDatabase.SaveAssets();
                // 卸载graph
                graphUnloaded?.Invoke(this.graph);
            }

            graphLoaded?.Invoke(graph);
           
            this.graph = graph;

            if(graphView != null)
                rootView.Remove(graphView);

            InitializeWindow(graph);

            graphView = rootView.Children().FirstOrDefault(e => e is BaseGraphView) as BaseGraphView;

            if (graphView == null){
                Debug.LogError("[BaseGraphWindow.InitializeGraph] graphView is null");
                return;
            }
            
            // 在这里又创建了一次BaseNode，是否合理呐
            graphView.InitOnOpenSO(graph);

            InitializeGraphView(graphView);
        }

        /// <summary>
        /// 子类实现该方法，确定Window的样式
        /// </summary>
        /// <param name="graph"></param>
        protected abstract void InitializeWindow(BaseGraph graph);

        /// <summary>
        /// 非必须实现，用来确定打开Window时，GraphView上的内容
        /// </summary>
        /// <param name="view"></param>
        protected virtual void InitializeGraphView(BaseGraphView view){
        }


    }
}