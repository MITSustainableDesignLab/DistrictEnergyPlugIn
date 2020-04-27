using System;
using System.Collections.Generic;
using System.Linq;
using LiveCharts.Defaults;

namespace DistrictEnergy.Helpers
{
    public static class LinqExtension
    {
        public static List<DateTimePoint> AggregateByPeriod(this IEnumerable<double> d, int period)
        {
            return AggregateByPeriod(d.ToList(), period);
        }

        public static List<DateTimePoint> AggregateByPeriod(this List<double> d, int period)
        {
            // Using a startdate of "2018-01-01" because it starts on a Monday
            var startDate = new DateTime(2018, 01, 01, 0, 0, 0);

            // Create SeriesBuilder
            var seriesBuilder = new List<DateTimePoint>();

            // Iterate over each element of the array of results & create datetime index incrementally
            for (int t = 0; t < d.Count; t++)
            {
                seriesBuilder.Add(new DateTimePoint(startDate, d[t]));
                startDate = startDate.AddHours(period);
            }

            return seriesBuilder;
        }
        public static double Sum(this IEnumerable<DateTimePoint> source)
        {
            return source.Select(o => o.Value).Sum();
        }

        public static double Max(this IEnumerable<DateTimePoint> source)
        {
            return source.Select(o => o.Value).Max();
        }

        public static double Average(this IEnumerable<DateTimePoint> source)
        {
            return source.Select(o => o.Value).Average();
        }

        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunksize)
        {
            while (source.Any())
            {
                yield return source.Take(chunksize);
                source = source.Skip(chunksize);
            }
        }
    }
}