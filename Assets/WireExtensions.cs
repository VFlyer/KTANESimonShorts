using System;
using System.Collections.Generic;
using System.Linq;

namespace WireGenerator
{
    public static class WireExtensions
    {
        public static double sin(double x) { return Math.Sin(x * Math.PI / 180); }
        public static double cos(double x) { return Math.Cos(x * Math.PI / 180); }
        public static Point RotateY(this Point p, double angle) { return new Point(p.X * cos(angle) - p.Z * sin(angle), p.Y, p.X * sin(angle) + p.Z * cos(angle)); }

        public static IEnumerable<TResult> SelectConsecutivePairs<T, TResult>(this IEnumerable<T> source, bool closed, Func<T, T, TResult> selector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");
            return selectConsecutivePairsIterator(source, closed, selector);
        }
        private static IEnumerable<TResult> selectConsecutivePairsIterator<T, TResult>(IEnumerable<T> source, bool closed, Func<T, T, TResult> selector)
        {
            using (var enumer = source.GetEnumerator())
            {
                bool any = enumer.MoveNext();
                if (!any)
                    yield break;
                T first = enumer.Current;
                T last = enumer.Current;
                while (enumer.MoveNext())
                {
                    yield return selector(last, enumer.Current);
                    last = enumer.Current;
                }
                if (closed)
                    yield return selector(last, first);
            }
        }

        public static IEnumerable<TResult> SelectManyConsecutivePairs<T, TResult>(this IEnumerable<T> source, bool closed, Func<T, T, IEnumerable<TResult>> selector)
        {
            return source.SelectConsecutivePairs(closed, selector).SelectMany(x => x);
        }

        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int count, bool throwIfNotEnough = false)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "count cannot be negative.");
            if (count == 0)
                return source;

            var collection = source as ICollection<T>;
            if (collection != null)
            {
                if (throwIfNotEnough && collection.Count < count)
                    throw new InvalidOperationException("The collection does not contain enough elements.");
                return collection.Take(Math.Max(0, collection.Count - count));
            }

            return skipLastIterator(source, count, throwIfNotEnough);
        }

        private static IEnumerable<T> skipLastIterator<T>(IEnumerable<T> source, int count, bool throwIfNotEnough)
        {
            var queue = new T[count];
            int headtail = 0; // tail while we're still collecting, both head & tail afterwards because the queue becomes completely full
            int collected = 0;

            foreach (var item in source)
            {
                if (collected < count)
                {
                    queue[headtail] = item;
                    headtail++;
                    collected++;
                }
                else
                {
                    if (headtail == count)
                        headtail = 0;
                    yield return queue[headtail];
                    queue[headtail] = item;
                    headtail++;
                }
            }

            if (throwIfNotEnough && collected < count)
                throw new InvalidOperationException("The collection does not contain enough elements.");
        }

        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "count cannot be negative.");
            if (count == 0)
                return new T[0];

            var collection = source as ICollection<T>;
            if (collection != null)
                return collection.Skip(Math.Max(0, collection.Count - count));

            var queue = new Queue<T>(count + 1);
            foreach (var item in source)
            {
                if (queue.Count == count)
                    queue.Dequeue();
                queue.Enqueue(item);
            }
            return queue.AsEnumerable();
        }

        public static T[] NewArray<T>(params T[] array) { return array; }
    }
}