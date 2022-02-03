using UnityEngine;
using ArcCore.Gameplay.Parsing;
using ArcCore.Gameplay.Parsing.Data;
using ArcCore.Utilities;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Behaviours
{
    public class ScenecontrolData
    {
        public IndexedList<ControlAxisKey> xAxisKeys;
        public IndexedList<ControlAxisKey> yAxisKeys;
        public IndexedList<ControlAxisKey> zAxisKeys;

        public IndexedList<ControlAxisKey> xRotAxisKeys;
        public IndexedList<ControlAxisKey> yRotAxisKeys;
        public IndexedList<ControlAxisKey> zRotAxisKeys;

        public IndexedList<ControlAxisKey> xScaleAxisKeys;
        public IndexedList<ControlAxisKey> yScaleAxisKeys;
        public IndexedList<ControlAxisKey> zScaleAxisKeys;

        public IndexedList<ControlAxisKey> redKeys;
        public IndexedList<ControlAxisKey> greenKeys;
        public IndexedList<ControlAxisKey> blueKeys;
        public IndexedList<ControlAxisKey> alphaKeys;

        public IndexedList<ControlValueKey<bool>> enableKeys;

        public ScenecontrolData()
        {
            xAxisKeys = new IndexedList<ControlAxisKey>();
            yAxisKeys = new IndexedList<ControlAxisKey>();
            zAxisKeys = new IndexedList<ControlAxisKey>();

            xRotAxisKeys = new IndexedList<ControlAxisKey>();
            yRotAxisKeys = new IndexedList<ControlAxisKey>();
            zRotAxisKeys = new IndexedList<ControlAxisKey>();

            xScaleAxisKeys = new IndexedList<ControlAxisKey>();
            yScaleAxisKeys = new IndexedList<ControlAxisKey>();
            zScaleAxisKeys = new IndexedList<ControlAxisKey>();

            redKeys = new IndexedList<ControlAxisKey>();
            greenKeys = new IndexedList<ControlAxisKey>();
            blueKeys = new IndexedList<ControlAxisKey>();
            alphaKeys = new IndexedList<ControlAxisKey>();

            enableKeys = new IndexedList<ControlValueKey<bool>>();
        }

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
    }

    public class ScenecontrolObject : MonoBehaviour
    {
        public ScenecontrolData data;

        public virtual void Reset()
        {
            data.Reset();
        }

        protected float GetAxis(IndexedList<ControlAxisKey> keys, int time, float defvalue = default)
        {
            while(keys.HasNext && time < keys.Current.timing)
            {
                keys.index++;
            }

            if (!keys.HasNext)
                return keys.Current.targetValue;

            if (keys.HasPrevious)
                return (keys.Current.targetValue - defvalue) * ((float)time / keys.Current.timing);

            return math.lerp(keys.Previous.targetValue, keys.Current.targetValue, Easing.Do(math.unlerp(keys.Previous.timing, keys.Current.timing, time), keys.Previous.easing));
        }

        protected Color GetColor(int time)
        {
            Color color;
            color.r = GetAxis(data.redKeys, time, 1);
            color.g = GetAxis(data.greenKeys, time, 1);
            color.b = GetAxis(data.blueKeys, time, 1);
            color.a = GetAxis(data.alphaKeys, time, 1);
            return color;
        }

        public virtual void Update()
        {
            var time = PlayManager.Conductor.receptorTime;

            float3 pos;
            pos.x = GetAxis(data.xAxisKeys, time);
            pos.y = GetAxis(data.yAxisKeys, time);
            pos.z = GetAxis(data.zAxisKeys, time);

            float3 rot;
            rot.x = GetAxis(data.xRotAxisKeys, time);
            rot.y = GetAxis(data.yRotAxisKeys, time);
            rot.z = GetAxis(data.zRotAxisKeys, time);

            float3 scl;
            scl.x = GetAxis(data.xScaleAxisKeys, time, 1);
            scl.y = GetAxis(data.yScaleAxisKeys, time, 1);
            scl.z = GetAxis(data.zScaleAxisKeys, time, 1);

            transform.position = pos;
            transform.rotation = Quaternion.Euler(rot);
            transform.localScale = scl;
        }
    }
}