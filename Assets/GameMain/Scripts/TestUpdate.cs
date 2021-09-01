using System;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Lomo {
    public class TestUpdate : MonoBehaviour {
        public TestOrder testOrder;
        public static TestUpdate I;
        private int frame;
        
        
        public void StartOrder() {
            testOrder = new TestOrder();
            testOrder.OnFinish = OnOrderFinish;
            testOrder.Start();
        }

        public void OnOrderFinish() {
            Log.Debug("[TestUpdate.OnOrderFinish]");
            testOrder = null;
        }

        private void Awake() {
            I = this;
            frame = 0;
        }

        private void Update() {
            frame += 1;
            
            if (testOrder != null) {
                Log.Debug("[TestUpdate.Update]frame:{0}",frame);
                testOrder.Update();
            }
        }

        private void LateUpdate() {
            
            if (testOrder != null) {
                Log.Debug("[TestUpdate.LateUpdate]frame:{0}",frame);
                testOrder.LateUpdate();
            }
        }
    }

    public class TestOrder {
        public Action OnFinish;

        public int times;
        public void Start() {
            times = 0;
            Log.Error("[TestOrder.Start]");
        }

        public void Update() {
            Log.Error("[TestOrder.Update]times:{0}",times);
            if (times > 1) {
                OnFinish?.Invoke();
            }

            times++;

        }

        public void LateUpdate() {
            Log.Error("[TestOrder.LateUpdate]times:{0}",times);
            if (times > 1) {
                OnFinish?.Invoke();
            }
        }
    }
}