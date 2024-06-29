using FishNet;
using UnityEngine;

namespace Runtime.Utility
{
    public class TimeUtil
    {
        public static float tickTime
        {
            get
            {
                var timeManager = InstanceFinder.TimeManager;
                return timeManager ? (float)timeManager.TicksToTime(timeManager.Tick) : 0f;
            }
        }
    }
}