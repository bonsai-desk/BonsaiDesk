using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mirror.Profiler.Chart
{
    public class Series : ISeries
    {

        public string Legend { get; }
        public IEnumerable<DataPoint> Data { get; }

        public Series(string name, IEnumerable<DataPoint> data)
        {
            Legend = name;
            Data = data;
        }

        public Rect Bounds
        {
            get
            {
                int minx = int.MaxValue;
                float miny = int.MaxValue;
                int maxx = int.MinValue;
                float maxy = int.MinValue;

                foreach (var point in Data)
                {
                    minx = Mathf.Min(minx, point.id);
                    miny = Mathf.Min(miny, point.value);
                    maxx = Mathf.Max(maxx, point.id);
                    maxy = Mathf.Max(maxy, point.value);
                }

                if (minx > maxx)
                {
                    minx = maxx = 0;
                    miny = maxy = 0;
                }

                return new Rect(minx, miny, maxx - minx, maxy - miny);
            }
        }

        /// <summary>
        /// Calculate the average value in the series for the last 5 seconds
        /// </summary>
        /// <returns></returns>
        public float Average()
        {
            float time = Time.time - 5;

            var last5Sec = Data.Where(point => point.time > time).Sum(point => point.value);

            return last5Sec / 5;
        }
    }

}