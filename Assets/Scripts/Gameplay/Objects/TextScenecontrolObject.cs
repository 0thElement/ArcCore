using UnityEngine;
using ArcCore.Parsing.Data;
using ArcCore.Utilities;

namespace ArcCore.Gameplay.Behaviours
{
    public class TextScenecontrolData
    {
        public string startValue;
        public IndexedList<ControlValueKey<string>> stringKeys;

        public TextScenecontrolData(string startValue)
        {
            this.startValue = startValue;
            stringKeys = new IndexedList<ControlValueKey<string>>();
        }

        public void Reset()
        {
            stringKeys.Reset();
        }
    }

    [RequireComponent(typeof(TextMesh))]
    public class TextScenecontrolObject : ScenecontrolObject
    {
        public TextMesh textMesh;

        public TextScenecontrolData textData;

        public new void Reset()
        {
            base.Reset();
            textData.Reset();
            textMesh.text = textData.startValue;
        }

        public new void Update()
        {
            base.Update();
            var time = PlayManager.Conductor.receptorTime;

            string value = null;
            while(textData.stringKeys.Unfinished && time < textData.stringKeys.Current.timing)
            {
                value = textData.stringKeys.Current.value;
            }
            if (value != null)
                textMesh.text = value;

            textMesh.color = GetColor(time);
            enabled = true; //fix alpha bugs
        }
    }
}