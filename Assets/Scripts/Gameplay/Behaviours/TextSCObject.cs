using UnityEngine;
using ArcCore.Parsing.Data;
using ArcCore.Utilities;

namespace ArcCore.Gameplay.Behaviours
{
    public class TextSCObject : BaseSCObject
    {
        public TextMesh textMesh;

        public string startValue;

        public IndexedArray<ControlStringKey> stringKeys;

        public new void Reset()
        {
            base.Reset();
            textMesh.text = startValue;
        }

        public void Awake()
        {
            textMesh = GetComponent<TextMesh>();
        }

        public new void Update()
        {
            base.Update();
            var time = Conductor.Instance.receptorTime;

            string value = null;
            while(stringKeys.Unfinished && time < stringKeys.Current.timing)
            {
                value = stringKeys.Current.newValue;
            }
            if (value != null)
                textMesh.text = value;

            textMesh.color = GetColor(time);
            enabled = true; //fix alpha bugs
        }
    }
}