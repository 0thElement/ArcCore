#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ArcCore.Gameplay;

[CustomEditor(typeof(PlayManager))]
public class ConductorEditor : Editor
{
    private int timing;
    public override void OnInspectorGUI()
    {
        /////
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Pause/Resume"))
            if (PlayManager.IsUpdating)
                PlayManager.Pause();
            else
                PlayManager.Resume();

        if (GUILayout.Button("Restart"))
            PlayManager.PlayMusic();

        GUILayout.EndHorizontal();

        /////
        GUILayout.BeginHorizontal();
        GUILayout.Label("Jump to timing");
        timing = EditorGUILayout.IntField(timing);
        if (GUILayout.Button("Go")) PlayManager.PlayMusic(timing);
        GUILayout.EndHorizontal();
        
        DrawDefaultInspector();
    }
}
#endif