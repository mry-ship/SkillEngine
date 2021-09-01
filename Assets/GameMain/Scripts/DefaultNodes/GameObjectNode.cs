using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using SkillEngine;
using UnityEngine;

[NodeMenuItem("Custom/Game Object"), Serializable]
public class GameObjectNode : BaseNode, ICreateNodeFrom<GameObject> {
    [Output(name = "Out"), SerializeField, ShowInInspector]
    public GameObject output;

    public override string name => "Game Object";


    public bool InitializeNodeFromObject(GameObject value)
    {
        output = value;
        return true;
    }
}