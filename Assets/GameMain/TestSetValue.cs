using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace GameMain {
    public class TestSetValue : MonoBehaviour {
        public class MyClass {
            public string test;
        }

        private MyClass m_Class;

        private Dictionary<string, object> dic;
        private void Start() {
            m_Class = new MyClass();
            dic = new Dictionary<string, object>();
            dic.Add("test","321");
            var test_fanshe = m_Class.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var int_fanshe = test_fanshe[0];
            Log.Error((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000);
            for (int i = 0; i < 1000; i++) {
                int_fanshe.SetValue(m_Class,"123");
            }
            Log.Error((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000);
            for (int i = 0; i < 10000; i++) {
                m_Class.test = GetValue("test") as string; 
            }
            Log.Error((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000);
            Log.Error(m_Class.test);
            
        }

        private object GetValue(string str) {
            return dic[str];
        }
    }
}