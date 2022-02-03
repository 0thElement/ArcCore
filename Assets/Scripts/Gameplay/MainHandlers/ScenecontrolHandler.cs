using ArcCore.Gameplay.Parsing;
using System.Collections.Generic;
using UnityEngine;

namespace ArcCore.Gameplay.Behaviours
{
    public class ScenecontrolHandler : MonoBehaviour
    {
        [HideInInspector]
        public ScenecontrolObject[] objs;

        [SerializeField]
        private GameObject spriteObjectPrefab;
        [SerializeField]
        private GameObject textObjectPrefab;

        public void CreateObjects(IChartParser parser)
        {
            List<ScenecontrolObject> objsList = new List<ScenecontrolObject>();

            foreach(var spr in parser.SpriteScenecontrolData)
            {
                var newGameObject = Instantiate(spriteObjectPrefab);
                var scenecontrolObject = newGameObject.GetComponent<SpriteScenecontrolObject>();

                scenecontrolObject.data = spr.Item1;
                scenecontrolObject.spriteData = spr.Item2;

                objsList.Add(scenecontrolObject);
            }

            foreach (var txt in parser.TextScenecontrolData)
            {
                var newGameObject = Instantiate(textObjectPrefab);
                var scenecontrolObject = newGameObject.GetComponent<TextScenecontrolObject>();

                scenecontrolObject.data = txt.Item1;
                scenecontrolObject.textData = txt.Item2;

                objsList.Add(scenecontrolObject);
            }

            objs = objsList.ToArray();
        }

        public void Reset()
        {
            foreach(var obj in objs)
            {
                obj.Reset();
                obj.enabled = false;
            }
        }

        public void Update()
        {
            var time = PlayManager.Conductor.receptorTime;

            for(int i = 0; i < objs.Length; i++)
            {
                var obj = objs[i];

                bool nval = obj.enabled;
                while(obj.data.enableKeys.Unfinished && time < obj.data.enableKeys.Current.timing)
                {
                    nval = obj.data.enableKeys.Current.value;
                }
                obj.enabled = nval;
            }
        }
    }
}