using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror.Profiler.Chart
{
    public readonly struct DataPoint
    {
        public readonly int id;
        public readonly double time;
        public readonly float value;

        public DataPoint(int id, double time, float value)
        {
            this.id = id;
            this.time = time;
            this.value = value;
        }
    }

    public interface ISeries
    {
        IEnumerable<DataPoint> Data { get; }

        string Legend { get; }

        float Average();
    }
}
