using UnityEngine;
using ArcCore.Parsing.Data;
using ArcCore.Utilities;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Behaviours
{
    public class BaseSCObject : MonoBehaviour
    {
        public IndexedArray<ControlAxisKey> xAxisKeys;
        public IndexedArray<ControlAxisKey> yAxisKeys;
        public IndexedArray<ControlAxisKey> zAxisKeys;

        public IndexedArray<ControlAxisKey> xRotAxisKeys;
        public IndexedArray<ControlAxisKey> yRotAxisKeys;
        public IndexedArray<ControlAxisKey> zRotAxisKeys;

        public IndexedArray<ControlAxisKey> xScaleAxisKeys;
        public IndexedArray<ControlAxisKey> yScaleAxisKeys;
        public IndexedArray<ControlAxisKey> zScaleAxisKeys;

        public IndexedArray<ControlAxisKey> redKeys;
        public IndexedArray<ControlAxisKey> greenKeys;
        public IndexedArray<ControlAxisKey> blueKeys;
        public IndexedArray<ControlAxisKey> alphaKeys;

        public IndexedArray<ControlValueKey<bool>> enableKeys;

        public void Reset()
        {
            xAxisKeys.Reset();
            yAxisKeys.Reset();
            zAxisKeys.Reset();

            xRotAxisKeys.Reset();
            yRotAxisKeys.Reset();
            zRotAxisKeys.Reset();

            xScaleAxisKeys.Reset();
            yScaleAxisKeys.Reset();
            zScaleAxisKeys.Reset();

            redKeys.Reset();
            greenKeys.Reset();
            blueKeys.Reset();
            alphaKeys.Reset();

            enableKeys.Reset();
        }

        protected float GetAxis(IndexedArray<ControlAxisKey> keys, int time, float defvalue = default)
        {
            while(keys.HasNext && time < keys.Current.timing)
            {
                keys.index++;
            }

            if (!keys.HasNext)
                return keys.Current.targetValue;

            if (keys.HasPrevious)
                return (keys.Current.targetValue - defvalue) * ((float)time / keys.Current.timing);

            return math.lerp(keys.Previous.targetValue, keys.Current.targetValue, Ease.Do(math.unlerp(keys.Previous.timing, keys.Current.timing, time), keys.Previous.easing));
        }

        protected Color GetColor(int time)
        {
            Color color;
            color.r = GetAxis(redKeys, time, 1);
            color.g = GetAxis(greenKeys, time, 1);
            color.b = GetAxis(blueKeys, time, 1);
            color.a = GetAxis(alphaKeys, time, 1);
            return color;
        }

        public void Update()
        {
            var time = Conductor.Instance.receptorTime;

            float3 pos;
            pos.x = GetAxis(xAxisKeys, time);
            pos.y = GetAxis(yAxisKeys, time);
            pos.z = GetAxis(zAxisKeys, time);

            float3 rot;
            rot.x = GetAxis(xRotAxisKeys, time);
            rot.y = GetAxis(yRotAxisKeys, time);
            rot.z = GetAxis(zRotAxisKeys, time);

            float3 scl;
            scl.x = GetAxis(xScaleAxisKeys, time, 1);
            scl.y = GetAxis(yScaleAxisKeys, time, 1);
            scl.z = GetAxis(zScaleAxisKeys, time, 1);

            transform.position = pos;
            transform.rotation = Quaternion.Euler(rot);
            transform.localScale = scl;
        }
    }
}