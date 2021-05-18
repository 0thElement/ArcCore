using ArcCore.Components;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using ArcCore.Behaviours;

namespace ArcCore.Structs
{
    public struct ArcCompleteState
    {
        public ArcState state;
        public bool isRed;
        public bool finalized;

        public float redRoll;
        public float cutoff;

        public const float maxRedRoll = 1;
        public const float minRedRoll = 0;
        private const float redRange = maxRedRoll - minRedRoll;

        public ArcCompleteState(ArcState state)
        {
            this.state = state;
            isRed = false;
            redRoll = 0f;
            cutoff = 0f;
            finalized = false;
        }

        public ArcCompleteState(ArcCompleteState from)
        {
            state = from.state;
            isRed = from.isRed;
            finalized = from.finalized;
            redRoll = from.redRoll;
            cutoff = from.cutoff;
        }

        public ArcCompleteState Copy(ArcState? withState = null, bool? withRed = null)
            => new ArcCompleteState(this) { state = withState ?? state, };

        public ArcCompleteState Finalize()
            => new ArcCompleteState(this)
            {
                finalized = true
            };

        public void Update(float deltaTime = 0.02f)
        {
            const float dfac = 10;
            float timef = deltaTime * dfac;
            redRoll = math.min(redRoll + (isRed ? -1 : 1) * timef * redRange, maxRedRoll);
        }
    }
}