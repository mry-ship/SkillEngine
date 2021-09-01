using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SkillEngine {
    [Serializable]
    public class ExposedParameter{
        [HideInInspector]
        public string guid; // unique id to keep track of the parameter
        public string name;
        public bool input = true;
       
        [Serializable]
        public class Settings
        {
            [HideInInspector]
            public bool isHidden = false;
       
            public bool expanded = false;

            [SerializeField,HideInInspector]
            internal string guid = null;

            public override bool Equals(object obj)
            {
                if (obj is Settings s && s != null)
                    return Equals(s);
                else
                    return false;
            }

            public virtual bool Equals(Settings param)
                => isHidden == param.isHidden && expanded == param.expanded;

            public override int GetHashCode() => base.GetHashCode();
        }
        [SerializeReference]
        public Settings settings;

        public string shortType => GetValueType()?.Name;
        public virtual object value { get; set; }
        public virtual Type GetValueType() => value.GetType();
        
        protected virtual Settings CreateSettings() => new Settings();
        
        public void Initialize(string name, object value)
        {
            guid = Guid.NewGuid().ToString(); // Generated once and unique per parameter
            settings = CreateSettings();
            settings.guid = guid;
            this.name = name;
            this.value = value;
        }

        public static bool operator ==(ExposedParameter param1, ExposedParameter param2)
        {
            if (ReferenceEquals(param1, null) && ReferenceEquals(param2, null))
                return true;
            if (ReferenceEquals(param1, param2))
                return true;
            if (ReferenceEquals(param1, null))
                return false;
            if (ReferenceEquals(param2, null))
                return false;

            return param1.Equals(param2);
        }

        public static bool operator !=(ExposedParameter param1, ExposedParameter param2) => !(param1 == param2);

        public bool Equals(ExposedParameter parameter) => guid == parameter.guid;

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
                return false;
            else
                return Equals((ExposedParameter)obj);
        }

        public override int GetHashCode() => guid.GetHashCode();

        public ExposedParameter Clone()
        {
            var clonedParam = Activator.CreateInstance(GetType()) as ExposedParameter;

            clonedParam.guid = guid;
            clonedParam.name = name;
            clonedParam.input = input;
            clonedParam.settings = settings;
            clonedParam.value = value;

            return clonedParam;
        }

        public virtual void OnSettingChanged() {
            
        }
    }
}