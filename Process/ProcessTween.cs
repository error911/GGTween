﻿/* 
 * https://github.com/error911/GGTween
 * Free license: CC BY Murnik Roman
 */

using System;
using UnityEngine;

namespace GGTools.GGTween
{
    public abstract class ProcessTween
    {
        protected int m_TweenId = -1;
        public int tweenId
        {
            get { return m_TweenId; }
        }

        protected bool m_Active;
        protected bool m_WaitingForDelay;

        protected Action m_Callback;

        protected float m_StartTime;
        protected float m_DeltaTime;
        protected float m_Duration;
        protected float m_Delay;
        protected bool m_ScaledTime;

        private AnimationCurve[] m_AnimationCurves;
        private Keyframe[][] m_Keyframes;

        protected TweenType m_TweenType;
        protected AnimationCurve m_CustomCurve;

        protected abstract int ValueLength();
        protected abstract void OnUpdateValue();
        protected abstract void OnFinalUpdateValue();
        protected abstract float GetValue(bool isEnd, int valueIndex);

        public void Initialize(float duration, float delay, TweenType tweenType, Action callback, AnimationCurve animationCurve, bool scaledTime, int id)
        {
            m_Duration = duration;
            m_Delay = delay;
            m_TweenType = tweenType;
            m_Callback = callback;
            m_CustomCurve = animationCurve;
            m_ScaledTime = scaledTime;
            m_TweenId = id;

            if (m_Delay > 0)
            {
                m_WaitingForDelay = true;
            }
            else
            {
                m_WaitingForDelay = false;
                StartTween();
            }

            m_Active = true;
        }

        protected virtual void StartTween()
        {
            SetupCurves();

            m_StartTime = m_ScaledTime ? Time.time : Time.unscaledTime;
        }

        public virtual void UpdateTween()
        {
            if (!m_Active) return;

            if (m_WaitingForDelay)
            {
                m_Delay -= m_ScaledTime ? Time.deltaTime : Time.unscaledDeltaTime;

                if (m_Delay <= 0)
                {
                    StartTween();
                    m_WaitingForDelay = false;
                }
            }
            else
            {
                m_DeltaTime = m_ScaledTime ? Time.time : Time.unscaledTime - m_StartTime;

                if (m_DeltaTime < m_Duration)
                {
                    try
                    {
                        OnUpdateValue();
                    }
                    catch (Exception)
                    {
                        StopTween(false);
                    }
                }
                else
                {
                    try
                    {
                        OnFinalUpdateValue();
                    }
                    catch
                    {
                        //  ignored
                    }

                    m_Active = false;
                    StopTween(true);
                }
            }
        }

        public void StopTween(bool callback)
        {
            if (callback)
            {
                m_Callback.InvokeIfNotNull();
            }

            m_TweenId = -1;
            Tween.Release(this);
        }

        private void SetupCurves()
        {
            m_AnimationCurves = new AnimationCurve[ValueLength()];
            m_Keyframes = new Keyframe[ValueLength()][];

            if (m_TweenType == TweenType.Custom)
            {
                for (int i = 0; i < ValueLength(); i++)
                {
                    m_Keyframes[i] = m_CustomCurve.keys;
                }
            }
            else
            {
                GetKeys(BaseTween.GetAnimCurveKeys(m_TweenType));
            }

            for (int i = 0; i < ValueLength(); i++)
            {
                for (int j = 0; j < m_Keyframes[i].Length; j++)
                {
                    m_Keyframes[i][j].value *= GetValue(true, i) - GetValue(false, i);
                    m_Keyframes[i][j].value += GetValue(false, i);
                    m_Keyframes[i][j].time *= m_Duration;
                }

                m_AnimationCurves[i] = new AnimationCurve(m_Keyframes[i]);

                if (m_CustomCurve == null)
                {
                    for (int j = 0; j < m_Keyframes[i].Length; j++)
                    {
                        m_AnimationCurves[i].SmoothTangents(j, 0f);
                    }
                }
            }
        }

        private void GetKeys(float[][] source)
        {
            for (int i = 0; i < ValueLength(); i++)
            {
                m_Keyframes[i] = new Keyframe[source.Length];

                for (int j = 0; j < m_Keyframes[i].Length; j++)
                {
                    m_Keyframes[i][j].time = source[j][0];
                    m_Keyframes[i][j].value = source[j][1];
                }
            }
        }
    }

    public class ProcessTweenInt : ProcessTween
    {
        private Func<int> m_GetStartValue;
        private Func<int> m_GetTargetValue;
        private Action<int> m_UpdateValue;
        private int m_StartValue;
        private int m_TargetValue;

        protected override float GetValue(bool isEnd, int valueIndex)
        {
            return (isEnd ? m_GetTargetValue() : m_GetStartValue());
        }

        protected override int ValueLength()
        {
            return 1;
        }

        public void Initialize(Action<int> updateValue, Func<int> startValue, Func<int> targetValue, float duration, float delay, TweenType tweenType, Action callback, AnimationCurve animationCurve, bool scaledTime, int id)
        {
            m_GetStartValue = startValue;
            m_UpdateValue = updateValue;
            m_GetTargetValue = targetValue;

            base.Initialize(duration, delay, tweenType, callback, animationCurve, scaledTime, id);
        }

        protected override void StartTween()
        {
            if (m_UpdateValue == null)
            {
                StopTween(false);
                return;
            }

            base.StartTween();

            m_StartValue = m_GetStartValue();
            m_TargetValue = m_GetTargetValue();
        }

        protected override void OnUpdateValue()
        {
            if (m_UpdateValue == null)
            {
                StopTween(false);
                return;
            }

            m_UpdateValue(
                Mathf.RoundToInt(BaseTween.Estimate(m_TweenType, m_StartValue, m_TargetValue, m_DeltaTime, m_Duration,
                    m_CustomCurve)));
        }

        protected override void OnFinalUpdateValue()
        {
            if (m_UpdateValue == null)
            {
                return;
            }

            m_UpdateValue(m_TargetValue);
        }
    }

    public class ProcessTweenFloat : ProcessTween
    {
        private Func<float> m_GetStartValue;
        private Func<float> m_GetTargetValue;
        private Action<float> m_UpdateValue;
        private float m_StartValue;
        private float m_TargetValue;

        protected override float GetValue(bool isEnd, int valueIndex)
        {
            return (isEnd ? m_GetTargetValue() : m_GetStartValue());
        }

        protected override int ValueLength()
        {
            return 1;
        }

        public void Initialize(Action<float> updateValue, Func<float> startValue, Func<float> targetValue, float duration, float delay, TweenType tweenType, Action callback, AnimationCurve animationCurve, bool scaledTime, int id)
        {
            m_GetStartValue = startValue;
            m_UpdateValue = updateValue;
            m_GetTargetValue = targetValue;

            base.Initialize(duration, delay, tweenType, callback, animationCurve, scaledTime, id);
        }

        protected override void StartTween()
        {
            if (m_UpdateValue == null)
            {
                StopTween(false);
                return;
            }

            base.StartTween();

            try
            {
                m_StartValue = m_GetStartValue();
                m_TargetValue = m_GetTargetValue();
            }
            catch (Exception)
            {
                StopTween(false);
            }
        }

        protected override void OnUpdateValue()
        {
            if (m_UpdateValue == null)
            {
                StopTween(false);
                return;
            }

            m_UpdateValue(BaseTween.Estimate(m_TweenType, m_StartValue, m_TargetValue, m_DeltaTime, m_Duration,
                m_CustomCurve));
        }

        protected override void OnFinalUpdateValue()
        {
            if (m_UpdateValue == null)
            {
                return;
            }

            m_UpdateValue(m_TargetValue);
        }
    }

    public class ProcessTweenVector2 : ProcessTween
    {
        private Func<Vector2> m_GetStartValue;
        private Func<Vector2> m_GetTargetValue;
        private Action<Vector2> m_UpdateValue;
        private Vector2 m_StartValue;
        private Vector2 m_TargetValue;

        protected override float GetValue(bool isEnd, int valueIndex)
        {
            return (isEnd ? m_GetTargetValue()[valueIndex] : m_GetStartValue()[valueIndex]);
        }

        protected override int ValueLength()
        {
            return 2;
        }

        public void Initialize(Action<Vector2> updateValue, Func<Vector2> startValue, Func<Vector2> targetValue, float duration, float delay, TweenType tweenType, Action callback, AnimationCurve animationCurve, bool scaledTime, int id)
        {
            m_GetStartValue = startValue;
            m_UpdateValue = updateValue;
            m_GetTargetValue = targetValue;

            base.Initialize(duration, delay, tweenType, callback, animationCurve, scaledTime, id);
        }

        protected override void StartTween()
        {
            if (m_UpdateValue == null)
            {
                StopTween(false);
                return;
            }

            base.StartTween();

            try
            {
                m_StartValue = m_GetStartValue();
                m_TargetValue = m_GetTargetValue();
            }
            catch (Exception)
            {
                StopTween(false);
            }
        }

        protected override void OnUpdateValue()
        {
            if (m_UpdateValue == null)
            {
                StopTween(false);
                return;
            }

            Vector2 value = new Vector2
            {
                x = BaseTween.Estimate(m_TweenType, m_StartValue.x, m_TargetValue.x, m_DeltaTime, m_Duration, m_CustomCurve),
                y = BaseTween.Estimate(m_TweenType, m_StartValue.y, m_TargetValue.y, m_DeltaTime, m_Duration, m_CustomCurve)
            };
            m_UpdateValue(value);
        }

        protected override void OnFinalUpdateValue()
        {
            if (m_UpdateValue == null)
            {
                return;
            }

            m_UpdateValue(m_TargetValue);
        }
    }

    public class ProcessTweenVector3 : ProcessTween
    {
        private Func<Vector3> m_GetStartValue;
        private Func<Vector3> m_GetTargetValue;
        private Action<Vector3> m_UpdateValue;
        private Vector3 m_StartValue;
        private Vector3 m_TargetValue;

        protected override float GetValue(bool isEnd, int valueIndex)
        {
            return (isEnd ? m_GetTargetValue()[valueIndex] : m_GetStartValue()[valueIndex]);
        }

        protected override int ValueLength()
        {
            return 3;
        }

        public void Initialize(Action<Vector3> updateValue, Func<Vector3> startValue, Func<Vector3> targetValue, float duration, float delay, TweenType tweenType, Action callback, AnimationCurve animationCurve, bool scaledTime, int id)
        {
            m_GetStartValue = startValue;
            m_UpdateValue = updateValue;
            m_GetTargetValue = targetValue;

            base.Initialize(duration, delay, tweenType, callback, animationCurve, scaledTime, id);
        }

        protected override void StartTween()
        {
            if (m_UpdateValue == null)
            {
                StopTween(false);
                return;
            }

            base.StartTween();

            try
            {
                m_StartValue = m_GetStartValue();
                m_TargetValue = m_GetTargetValue();
            }
            catch (Exception)
            {
                StopTween(false);
            }
        }

        protected override void OnUpdateValue()
        {
            if (m_UpdateValue == null)
            {
                StopTween(false);
                return;
            }

            Vector3 value = new Vector3
            {
                x = BaseTween.Estimate(m_TweenType, m_StartValue.x, m_TargetValue.x, m_DeltaTime, m_Duration, m_CustomCurve),
                y = BaseTween.Estimate(m_TweenType, m_StartValue.y, m_TargetValue.y, m_DeltaTime, m_Duration, m_CustomCurve),
                z = BaseTween.Estimate(m_TweenType, m_StartValue.z, m_TargetValue.z, m_DeltaTime, m_Duration, m_CustomCurve)
            };
            m_UpdateValue(value);
        }

        protected override void OnFinalUpdateValue()
        {
            if (m_UpdateValue == null)
            {
                return;
            }

            m_UpdateValue(m_TargetValue);
        }
    }

    public class ProcessTweenVector4 : ProcessTween
    {
        private Func<Vector4> m_GetStartValue;
        private Func<Vector4> m_GetTargetValue;
        private Action<Vector4> m_UpdateValue;
        private Vector4 m_StartValue;
        private Vector4 m_TargetValue;

        protected override float GetValue(bool isEnd, int valueIndex)
        {
            return (isEnd ? m_GetTargetValue()[valueIndex] : m_GetStartValue()[valueIndex]);
        }

        protected override int ValueLength()
        {
            return 4;
        }

        public void Initialize(Action<Vector4> updateValue, Func<Vector4> startValue, Func<Vector4> targetValue, float duration, float delay, TweenType tweenType, Action callback, AnimationCurve animationCurve, bool scaledTime, int id)
        {
            m_GetStartValue = startValue;
            m_UpdateValue = updateValue;
            m_GetTargetValue = targetValue;

            base.Initialize(duration, delay, tweenType, callback, animationCurve, scaledTime, id);
        }

        protected override void StartTween()
        {
            if (m_UpdateValue == null)
            {
                StopTween(false);
                return;
            }

            base.StartTween();

            try
            {
                m_StartValue = m_GetStartValue();
                m_TargetValue = m_GetTargetValue();
            }
            catch (Exception)
            {
                StopTween(false);
            }
        }

        protected override void OnUpdateValue()
        {
            if (m_UpdateValue == null)
            {
                StopTween(false);
                return;
            }

            Vector4 value = new Vector4
            {
                x = BaseTween.Estimate(m_TweenType, m_StartValue.x, m_TargetValue.x, m_DeltaTime, m_Duration, m_CustomCurve),
                y = BaseTween.Estimate(m_TweenType, m_StartValue.y, m_TargetValue.y, m_DeltaTime, m_Duration, m_CustomCurve),
                z = BaseTween.Estimate(m_TweenType, m_StartValue.z, m_TargetValue.z, m_DeltaTime, m_Duration, m_CustomCurve),
                w = BaseTween.Estimate(m_TweenType, m_StartValue.w, m_TargetValue.w, m_DeltaTime, m_Duration, m_CustomCurve)
            };
            m_UpdateValue(value);
        }

        protected override void OnFinalUpdateValue()
        {
            if (m_UpdateValue == null)
            {
                return;
            }

            m_UpdateValue(m_TargetValue);
        }
    }

    public class ProcessTweenColor : ProcessTween
    {
        private Func<Color> m_GetStartValue;
        private Func<Color> m_GetTargetValue;
        private Action<Color> m_UpdateValue;
        private Color m_StartValue;
        private Color m_TargetValue;

        protected override float GetValue(bool isEnd, int valueIndex)
        {
            return (isEnd ? m_GetTargetValue()[valueIndex] : m_GetStartValue()[valueIndex]);
        }

        protected override int ValueLength()
        {
            return 4;
        }

        public void Initialize(Action<Color> updateValue, Func<Color> startValue, Func<Color> targetValue, float duration, float delay, TweenType tweenType, Action callback, AnimationCurve animationCurve, bool scaledTime, int id)
        {
            m_GetStartValue = startValue;
            m_UpdateValue = updateValue;
            m_GetTargetValue = targetValue;

            base.Initialize(duration, delay, tweenType, callback, animationCurve, scaledTime, id);
        }

        protected override void StartTween()
        {
            if (m_UpdateValue == null)
            {
                StopTween(false);
                return;
            }

            base.StartTween();

            try
            {
                m_StartValue = m_GetStartValue();
                m_TargetValue = m_GetTargetValue();
            }
            catch (Exception)
            {
                StopTween(false);
            }
        }

        protected override void OnUpdateValue()
        {
            if (m_UpdateValue == null)
            {
                StopTween(false);
                return;
            }

            Color value = new Color
            {
                r = BaseTween.Estimate(m_TweenType, m_StartValue.r, m_TargetValue.r, m_DeltaTime, m_Duration, m_CustomCurve),
                g = BaseTween.Estimate(m_TweenType, m_StartValue.g, m_TargetValue.g, m_DeltaTime, m_Duration, m_CustomCurve),
                b = BaseTween.Estimate(m_TweenType, m_StartValue.b, m_TargetValue.b, m_DeltaTime, m_Duration, m_CustomCurve),
                a = BaseTween.Estimate(m_TweenType, m_StartValue.a, m_TargetValue.a, m_DeltaTime, m_Duration, m_CustomCurve)
            };
            m_UpdateValue(value);
        }

        protected override void OnFinalUpdateValue()
        {
            if (m_UpdateValue == null)
            {
                return;
            }

            m_UpdateValue(m_TargetValue);
        }
    }

    public class ProcessTweenQuaternion : ProcessTween
    {
        private Func<Quaternion> m_GetStartValue;
        private Func<Quaternion> m_GetTargetValue;
        private Action<Quaternion> m_UpdateValue;
        private Quaternion m_StartValue;
        private Quaternion m_TargetValue;

        protected override float GetValue(bool isEnd, int valueIndex)
        {
            return (isEnd ? m_GetTargetValue()[valueIndex] : m_GetStartValue()[valueIndex]);
        }

        protected override int ValueLength()
        {
            return 1;
        }

        public void Initialize(Action<Quaternion> updateValue, Func<Quaternion> startValue, Func<Quaternion> targetValue, float duration, float delay, TweenType tweenType, Action callback, AnimationCurve animationCurve, bool scaledTime, int id)
        {
            m_GetStartValue = startValue;
            m_UpdateValue = updateValue;
            m_GetTargetValue = targetValue;

            base.Initialize(duration, delay, tweenType, callback, animationCurve, scaledTime, id);
        }

        protected override void StartTween()
        {
            if (m_UpdateValue == null)
            {
                StopTween(false);
                return;
            }

            base.StartTween();

            m_StartValue = m_GetStartValue();
            m_TargetValue = m_GetTargetValue();
        }

        protected override void OnUpdateValue()
        {
            if (m_UpdateValue == null)
            {
                StopTween(false);
                return;
            }


            Quaternion value = new Quaternion
            {
                x = BaseTween.Estimate(m_TweenType, m_StartValue.x, m_TargetValue.x, m_DeltaTime, m_Duration, m_CustomCurve),
                y = BaseTween.Estimate(m_TweenType, m_StartValue.y, m_TargetValue.y, m_DeltaTime, m_Duration, m_CustomCurve),
                z = BaseTween.Estimate(m_TweenType, m_StartValue.z, m_TargetValue.z, m_DeltaTime, m_Duration, m_CustomCurve),
                w = BaseTween.Estimate(m_TweenType, m_StartValue.w, m_TargetValue.w, m_DeltaTime, m_Duration, m_CustomCurve)
            };
            m_UpdateValue(value);

        }

        protected override void OnFinalUpdateValue()
        {
            if (m_UpdateValue == null)
            {
                return;
            }

            m_UpdateValue(m_TargetValue);
        }
    }
}