﻿using System;
using System.Collections.Generic;
using System.Linq;
using LiveCharts.Defaults;

namespace DistrictEnergy.Helpers
{
    public static class LinqExtension
    {
        public static List<DateTimePoint> AggregateByPeriod(this IEnumerable<double> d, int period)
        {
            // Using a startdate of "2018-01-01" because it starts on a Monday
            var startDate = new DateTime(2018, 01, 01, 0, 0, 0);
            // Create SeriesBuilder
            var seriesBuilder = new List<DateTimePoint>();
            var i = 0;
            // Iterate over each element of the array of results & create datetime index incrementally
            foreach (var x1 in d.Batch(period))
            {
                seriesBuilder.Add(new DateTimePoint(startDate, x1.Sum()));
                startDate = startDate.AddHours(period);
                i += 1;
            }

            return seriesBuilder;
        }

        public static List<DateTimePoint> ToDateTimePoint(this IEnumerable<double> d)
        {
            // Using a startdate of "2018-01-01" because it starts on a Monday
            var startDate = new DateTime(2018, 01, 01, 0, 0, 0);
            // Create SeriesBuilder
            var seriesBuilder = new List<DateTimePoint>();
            var len = d.Count();
            var hours = 8760 / len;
            // Iterate over each element of the array of results & create datetime index incrementally
            foreach (var x1 in d)
            {
                seriesBuilder.Add(new DateTimePoint(startDate, x1));
                startDate = startDate.AddHours(hours);
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

        public static double Min(this IEnumerable<DateTimePoint> source)
        {
            return source.Select(o => o.Value).Min();
        }

        public static double Average(this IEnumerable<DateTimePoint> source)
        {
            return source.Select(o => o.Value).Average();
        }

        public static double Variance(this IEnumerable<DateTimePoint> values)
        {
            return values.Select(x => x.Value).ToList()
                .Variance(values.Select(x => x.Value).ToList().Mean(), 0, values.Count());
        }

        public static IEnumerable<IEnumerable<T>> Batch<T>(
            this IEnumerable<T> source, int batchSize)
        {
            using (var enumerator = source.GetEnumerator())
                while (enumerator.MoveNext())
                    yield return YieldBatchElements(enumerator, batchSize - 1);
        }

        private static IEnumerable<T> YieldBatchElements<T>(
            IEnumerator<T> source, int batchSize)
        {
            yield return source.Current;
            for (int i = 0; i < batchSize && source.MoveNext(); i++)
                yield return source.Current;
        }

        /// <summary>
        /// Splits an array into n smaller arrays.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="source">The IEnumerable to split.</param>
        /// <param name="n">The size of the smaller arrays.</param>
        /// <returns>An array containing smaller arrays.</returns>
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> source, int n)
        {
            var count = source.Count();
            var thisMany = count / n;
            for (var i = 0; i < (float) n; i++)
            {
                yield return source.Skip(i * thisMany).Take(thisMany);
            }
        }

        public static double Mean(this List<double> values)
        {
            return values.Count == 0 ? 0 : values.Mean(0, values.Count);
        }

        public static double Mean(this List<double> values, int start, int end)
        {
            double s = 0;

            for (int i = start; i < end; i++)
            {
                s += values[i];
            }

            return s / (end - start);
        }

        public static double Variance(this List<double> values)
        {
            return values.Variance(values.Mean(), 0, values.Count);
        }

        public static double Variance(this List<double> values, double mean)
        {
            return values.Variance(mean, 0, values.Count);
        }

        public static double Variance(this List<double> values, double mean, int start, int end)
        {
            double variance = 0;

            for (int i = start; i < end; i++)
            {
                variance += Math.Pow((values[i] - mean), 2);
            }

            int n = end - start;
            if (start > 0) n -= 1;

            return variance / (n);
        }

        public static double StandardDeviation(this List<double> values)
        {
            return values.Count == 0 ? 0 : values.StandardDeviation(0, values.Count);
        }

        public static double StandardDeviation(this List<double> values, int start, int end)
        {
            double mean = values.Mean(start, end);
            double variance = values.Variance(mean, start, end);

            return Math.Sqrt(variance);
        }
    }
}