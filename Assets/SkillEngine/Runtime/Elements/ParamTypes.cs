using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEngine;

namespace SkillEngine {
    [System.Serializable]
    public class ColorParameter : ExposedParameter {
        public enum ColorMode {
            Default,
            HDR
        }

        [Serializable]
        public class ColorSettings : Settings {
            public ColorMode mode;

            public override bool Equals(Settings param)
                => base.Equals(param) && mode == ((ColorSettings) param).mode;
        }

        [SerializeField]
        Color val;

        public override object value {
            get => val;
            set => val = (Color) value;
        }

        protected override Settings CreateSettings() => new ColorSettings();
    }

    [System.Serializable]
    public class FloatParameter : ExposedParameter {
        public enum FloatMode {
            Default,
            Slider,
        }

        [Serializable]
        public class FloatSettings : Settings {
            public FloatMode mode;
            public float min = 0;
            public float max = 1;

            public override bool Equals(Settings param)
                => base.Equals(param) && mode == ((FloatSettings) param).mode && min == ((FloatSettings) param).min &&
                   max == ((FloatSettings) param).max;
            
        }

        [SerializeField]
        float val;
        
        public override object value {
            get => val;
            set => val = (float) value;
        }

        protected override Settings CreateSettings() => new FloatSettings();
    }
    
    [System.Serializable]
    public class Vector2Parameter : ExposedParameter {
        public enum Vector2Mode {
            Default,
            MinMaxSlider,
        }

        [Serializable]
        public class Vector2Settings : Settings {
            public Vector2Mode mode;
            public float min = 0;
            public float max = 1;

            public override bool Equals(Settings param)
                => base.Equals(param) && mode == ((Vector2Settings) param).mode &&
                   min == ((Vector2Settings) param).min && max == ((Vector2Settings) param).max;
        }

        [SerializeField]
        Vector2 val;

        public override object value {
            get => val;
            set => val = (Vector2) value;
        }

        protected override Settings CreateSettings() => new Vector2Settings();
    }

    [System.Serializable]
    public class Vector3Parameter : ExposedParameter {
        [SerializeField]
        Vector3 val;

        public override object value {
            get => val;
            set => val = (Vector3) value;
        }
    }

    [System.Serializable]
    public class Vector4Parameter : ExposedParameter {
        [SerializeField]
        Vector4 val;

        public override object value {
            get => val;
            set => val = (Vector4) value;
        }
    }

    [System.Serializable]
    public class IntParameter : ExposedParameter {
        public enum IntMode {
            Default,
            Slider,
        }

        [Serializable]
        public class IntSettings : Settings {
            public IntMode mode;
            public int min = 0;
            public int max = 10;

            public override bool Equals(Settings param)
                => base.Equals(param) && mode == ((IntSettings) param).mode && min == ((IntSettings) param).min &&
                   max == ((IntSettings) param).max;
        }

        [SerializeField]
        int val;

        public override object value {
            get => val;
            set => val = (int) value;
        }

        protected override Settings CreateSettings() => new IntSettings();
    }

    [System.Serializable]
    public class Vector2IntParameter : ExposedParameter {
        [SerializeField]
        Vector2Int val;

        public override object value {
            get => val;
            set => val = (Vector2Int) value;
        }
    }

    [System.Serializable]
    public class Vector3IntParameter : ExposedParameter {
        [SerializeField]
        Vector3Int val;

        public override object value {
            get => val;
            set => val = (Vector3Int) value;
        }
    }

    [System.Serializable]
    public class DoubleParameter : ExposedParameter {
        [SerializeField]
        Double val;

        public override object value {
            get => val;
            set => val = (Double) value;
        }
    }

    [System.Serializable]
    public class LongParameter : ExposedParameter {
        [SerializeField]
        long val;

        public override object value {
            get => val;
            set => val = (long) value;
        }
    }

    [System.Serializable]
    public class StringParameter : ExposedParameter {
        [SerializeField]
        string val;

        public override object value {
            get => val;
            set => val = (string) value;
        }

        public override Type GetValueType() => typeof(String);
    }

    [System.Serializable]
    public class RectParameter : ExposedParameter {
        [SerializeField]
        Rect val;

        public override object value {
            get => val;
            set => val = (Rect) value;
        }
    }

    [System.Serializable]
    public class RectIntParameter : ExposedParameter {
        [SerializeField]
        RectInt val;

        public override object value {
            get => val;
            set => val = (RectInt) value;
        }
    }

    [System.Serializable]
    public class BoundsParameter : ExposedParameter {
        [SerializeField]
        Bounds val;

        public override object value {
            get => val;
            set => val = (Bounds) value;
        }
    }

    [System.Serializable]
    public class BoundsIntParameter : ExposedParameter {
        [SerializeField]
        BoundsInt val;

        public override object value {
            get => val;
            set => val = (BoundsInt) value;
        }
    }

    [System.Serializable]
    public class AnimationCurveParameter : ExposedParameter {
        [SerializeField]
        AnimationCurve val;

        public override object value {
            get => val;
            set => val = (AnimationCurve) value;
        }

        public override Type GetValueType() => typeof(AnimationCurve);
    }

    [System.Serializable]
    public class GradientParameter : ExposedParameter {
        public enum GradientColorMode {
            Default,
            HDR,
        }

        [Serializable]
        public class GradientSettings : Settings {
            public GradientColorMode mode;

            public override bool Equals(Settings param)
                => base.Equals(param) && mode == ((GradientSettings) param).mode;
        }

        [SerializeField]
        Gradient val;

        [SerializeField, GradientUsage(true)]
        Gradient hdrVal;

        public override object value {
            get => val;
            set => val = (Gradient) value;
        }

        public override Type GetValueType() => typeof(Gradient);
        protected override Settings CreateSettings() => new GradientSettings();
    }

    [System.Serializable]
    public class GameObjectParameter : ExposedParameter {
        [SerializeField]
        GameObject val;

        public override object value {
            get => val;
            set => val = (GameObject) value;
        }

        public override Type GetValueType() => typeof(GameObject);
    }

    [System.Serializable]
    public class BoolParameter : ExposedParameter {
        [SerializeField]
        bool val;

        public override object value {
            get => val;
            set => val = (bool) value;
        }
    }

    [System.Serializable]
    public class Texture2DParameter : ExposedParameter {
        [SerializeField]
        Texture2D val;

        public override object value {
            get => val;
            set => val = (Texture2D) value;
        }

        public override Type GetValueType() => typeof(Texture2D);
    }

    [System.Serializable]
    public class RenderTextureParameter : ExposedParameter {
        [SerializeField]
        RenderTexture val;

        public override object value {
            get => val;
            set => val = (RenderTexture) value;
        }

        public override Type GetValueType() => typeof(RenderTexture);
    }

    [System.Serializable]
    public class MeshParameter : ExposedParameter {
        [SerializeField]
        Mesh val;

        public override object value {
            get => val;
            set => val = (Mesh) value;
        }

        public override Type GetValueType() => typeof(Mesh);
    }

    [System.Serializable]
    public class MaterialParameter : ExposedParameter {
        [SerializeField]
        Material val;

        public override object value {
            get => val;
            set => val = (Material) value;
        }

        public override Type GetValueType() => typeof(Material);
    }
}