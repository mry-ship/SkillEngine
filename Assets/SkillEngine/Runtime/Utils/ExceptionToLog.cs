using UnityEngine;
using System;

namespace SkillEngine {
    // 会抛出错误的调用
    public static class ExceptionToLog
    {
        public static void Call(Action a)
        {
#if UNITY_EDITOR
            try
            {
#endif
                a?.Invoke();
#if UNITY_EDITOR
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
#endif
        }
    }
}