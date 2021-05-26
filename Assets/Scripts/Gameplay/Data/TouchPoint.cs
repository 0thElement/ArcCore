﻿using ArcCore.Math;
using Unity.Mathematics;
using System.Collections;
using System.Collections.Generic;

namespace ArcCore.Gameplay.Data
{
    public struct TouchPoint
    {
        public enum Status
        {
            Tapped,
            Sustained,
            Released
        }

        public float2? inputPosition;
        public Rect2D? inputPlane;
        public int track;

        public Rect2D InputPlane => inputPlane.GetValueOrDefault();
        public bool InputPlaneValid => inputPlane.HasValue;
        public bool TrackValid => track != -1;

        //public int time;
        public Status status;
        public int fingerId;

        public TouchPoint(float2? inputPosition, Rect2D? inputPlane, int track, /*int time,*/ Status status, int fingerId)
        {
            this.inputPosition = inputPosition;
            this.inputPlane = inputPlane;
            this.track = track;
            //this.time = time;
            this.status = status;
            this.fingerId = fingerId;
        }
    }
}