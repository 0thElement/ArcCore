using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ArcCore.Gameplay.Data;

namespace Tests.GameplayTest
{
    public class ArcStateTest
    {
        [Test]
        public void InitialStateAcceptAllFingers()
        {
            ArcColorState state = new ArcColorState(0);

            state.Hit(1, 0);

            Assert.AreEqual(1, state.FingerId);
        }

        [Test]
        public void AfterInitialStateDoNotAcceptOtherFingers()
        {
            ArcColorState state = new ArcColorState(0);
            state.Hit(1, 0);

            state.Hit(2, 0);

            Assert.AreEqual(1, state.FingerId);
        }

        [Test]
        public void LiftFingerFreezesInput()
        {
            ArcColorState state = new ArcColorState(0);
            state.Hit(1, 0);

            state.Lift(0);

            Assert.False(state.AcceptsInput(0));
        }

        [Test]
        public void WrongFingerCausesRedArc()
        {
            ArcColorState state = new ArcColorState(0);
            state.Hit(1, 0);

            state.Hit(2, 0);

            Assert.AreNotEqual(0, state.Redmix(0));
        }
        
        [Test]
        public void WrongFingerThenCollideStopsRedArc()
        {
            ArcColorState state = new ArcColorState(0);
            state.Hit(1, 0);
            state.Hit(2, 0);

            state.Hit(1, 0);

            Assert.AreEqual(0, state.Redmix(0));
        }

        [Test]
        public void GracePeriodUpdatesArcFingers()
        {
            ArcColorState state = new ArcColorState(0);
            state.Hit(1, 0);

            state.Grace(0);
            state.Hit(2, 0);

            Assert.AreEqual(2, state.FingerId);
        }

        [Test]
        public void GracePeriodUnfreezesInput()
        {
            ArcColorState state = new ArcColorState(0);
            state.Hit(1, 0);
            state.Lift(0);
            if (state.AcceptsInput(0)) Assert.Fail("Input was never frozen");

            state.Grace(0);

            Assert.True(state.AcceptsInput(0));
        }
        
        [Test]
        public void GracePeriodCancelsRedArcs()
        {
            ArcColorState state = new ArcColorState(0);
            state.Hit(1, 0);

            state.Hit(2, 0);
            if (state.Redmix(0) == 0) Assert.Fail("Red arc never occurred");

            state.Grace(0);

            Assert.AreEqual(0, state.Redmix(0));
        }

        [Test]
        public void RestPeriodUnfreezesInput()
        {
            ArcColorState state = new ArcColorState(0);
            state.Hit(1, 0);

            state.Lift(0);
            if (state.AcceptsInput(0)) Assert.Fail("Input was never frozen");

            state.Rest(0);

            Assert.True(state.AcceptsInput(0));
        }

        [Test]
        public void RestPeriodCancelRedArcs()
        {
            ArcColorState state = new ArcColorState(0);
            state.Hit(1, 0);

            state.Hit(2, 0);
            if (state.Redmix(0) == 0) Assert.Fail("Red arc never occured");

            state.Rest(0);

            Assert.AreEqual(0, state.Redmix(0));
        }
        
        [Test]
        public void RestPeriodThenLiftResetsToInitial()
        {
            ArcColorState state = new ArcColorState(0);
            state.Hit(1, 0);
            state.Rest(0);

            state.Lift(0);
            state.Hit(2, 0);

            Assert.AreEqual(2, state.FingerId);
        }

        [Test]
        public void LiftFingerThenRestPeriodResetsToInitial()
        {
            ArcColorState state = new ArcColorState(0);
            state.Hit(1, 0);
            state.Lift(0);

            state.Rest(0);
            state.Hit(2, 0);

            Assert.AreEqual(2, state.FingerId);
        }
    }
}