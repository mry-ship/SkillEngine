using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SkillEngine
{
    public partial class NodePort
    {
        /// <summary>
        /// The actual name of the property behind the port (must be exact, it is used for Reflection)
        /// </summary>
        [HideInInspector]
        public string fieldName;

        public string portType;
        
        /// <summary>
        /// Data of the port
        /// </summary>
        [NonSerialized, ShowInInspector]
        public PortData portData;
        
        /// <summary>
        /// The node on which the port is
        /// </summary>
        [NonSerialized]
        public BaseNode owner;
        
        /// <summary>
        /// The fieldInfo from the fieldName
        /// </summary>
        [NonSerialized]
        public FieldInfo fieldInfo;
        
        // Todo: 这些不序列化字段的构造
        [NonSerialized,ShowInInspector]
        private List<BaseEdge> edges = new List<BaseEdge>();
        
        public NodePort(BaseNode owner, string fieldName, PortData portData){
            this.fieldName = fieldName;
            this.owner = owner;
            this.portData = portData;
            this.portType = portData.displayType.ToString();
            fieldInfo = owner.GetType().GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
    
    /// <summary>
    /// Class that describe port attributes for it's creation
    /// </summary>
    public class PortData : IEquatable<PortData>{
        /// <summary>
        /// Unique identifier for the port
        /// Todo：一个预留使用的字段，暂时不知道有啥用
        /// </summary>
        public string identifier;

        /// <summary>
        /// Display name on the node
        /// </summary>
        public string displayName;

        /// <summary>
        /// The type that will be used for coloring with the type stylesheet
        /// </summary>
        public Type displayType;
        /// <summary>
        /// If the port accept multiple connection
        /// </summary>
        public bool acceptMultipleEdges;

        /// <summary>
        /// Port size, will also affect the size of the connected edge
        /// </summary>
        public int sizeInPixel;

        /// <summary>
        /// Tooltip of the port
        /// </summary>
        public string tooltip;

        /// <summary>
        /// Is the port vertical
        /// </summary>
        public bool vertical;

        public bool Equals(PortData other){
            return identifier == other.identifier
                   && displayName == other.displayName
                   && displayType == other.displayType
                   && acceptMultipleEdges == other.acceptMultipleEdges
                   && sizeInPixel == other.sizeInPixel
                   && tooltip == other.tooltip
                   && vertical == other.vertical;
        }

        public void CopyFrom(PortData other){
            identifier = other.identifier;
            displayName = other.displayName;
            displayType = other.displayType;
            acceptMultipleEdges = other.acceptMultipleEdges;
            sizeInPixel = other.sizeInPixel;
            tooltip = other.tooltip;
            vertical = other.vertical;
        }
    }

}