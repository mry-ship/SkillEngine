using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using BaseGraph = SkillEngine.BaseGraph;


public static class CreateSkillAssets{
    [MenuItem("Assets/Create/Skill", false, 10)]
    public static void CreateSkillAsset(){
        var skill = ScriptableObject.CreateInstance<SkillBaseGraph>();
        ProjectWindowUtil.CreateAsset(skill, "Skill.asset");
    }

    [OnOpenAsset(0)]
    public static bool OnBaseGraphOpened(int instanceID, int line){
        var asset = EditorUtility.InstanceIDToObject(instanceID) as BaseGraph;
        
        // 在Open之前就把graph的全部数据准备好，打开后不在操作graph数据，除非增加或者删除
        if (asset != null  && AssetDatabase.GetAssetPath(asset).Contains("Skills")){
            // 用Get会在当前界面打开，用Create会创建一个新的，可以打开多个
            EditorWindow.CreateWindow<SkillGraphWindow>().InitOnOpenSO(asset as BaseGraph);
            return true;
        }
        return false;
    }
}