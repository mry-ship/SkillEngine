using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SkillEngine {
    public partial class NodePort {
        #region Init

        public void InitSelf() {
        }

        #endregion

        /// <summary>
        /// Get all the edges connected to this port
        /// </summary>
        /// <returns></returns>
        public List<BaseEdge> GetEdges() => edges;
        
        /// <summary>
        /// Connect an edge to this port
        /// </summary>
        /// <param name="edge"></param>
        public void Add(BaseEdge edge)
        {
            if (!edges.Contains(edge))
                edges.Add(edge);
        }
        /// <summary>
        /// Disconnect an Edge from this port
        /// </summary>
        /// <param name="edge"></param>
        public void Remove(BaseEdge edge)
        {
            if (!edges.Contains(edge))
                return;
            
            edges.Remove(edge);
        }
        
        /// <summary>
        /// Reset the value of the field to default if possible
        /// </summary>
        public void ResetToDefault()
        {
            // Clear lists, set classes to null and struct to default value.
            if (typeof(IList).IsAssignableFrom(fieldInfo.FieldType))
                (fieldInfo.GetValue(owner) as IList)?.Clear();
            else if (fieldInfo.FieldType.GetTypeInfo().IsClass)
                fieldInfo.SetValue(owner, null);
            else
            {
                try
                {
                    fieldInfo.SetValue(owner, Activator.CreateInstance(fieldInfo.FieldType));
                } catch {} // Catch types that don't have any constructors
            }
        }
    }
}