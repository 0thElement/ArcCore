using System;

namespace ArcCore
{
    public class GameSettings
    {
        private static bool _instanceInvalid = true;
        private static GameSettings _instance;

        /// <summary>
        /// The instance of settings. Do <b>not</b> use this item's setter unless it known to be is uninitialized (<see langword="null"/>) or that <see cref="FinalizeInstance"/> has not been called.
        /// After initialization, code must call <see cref="FinalizeInstance"/>, which will then cause the setter to throw an error on call.
        /// </summary>
        public static GameSettings Instance
        {
            get => _instance;
            set
            {
                if(_instanceInvalid)
                {
                    _instance = value;
                }
                else
                {
                    throw new Exception("You cannot set Instance directely after it has been created. Please modify its fields instead.");
                }
            }
        }

        public static void FinalizeInstance()
            => _instanceInvalid = false;

        public static GameSettings Default 
            => new GameSettings
            {
                maxLevelId = 0,
                maxPackId = 0
            };

        public ulong maxLevelId;
        public ulong maxPackId;
    }
}
