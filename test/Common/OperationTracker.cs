//  ------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation
//  All rights reserved. 
//  
//  Licensed under the Apache License, Version 2.0 (the ""License""); you may not use this 
//  file except in compliance with the License. You may obtain a copy of the License at 
//  http://www.apache.org/licenses/LICENSE-2.0  
//  
//  THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
//  EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR 
//  CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR 
//  NON-INFRINGEMENT. 
// 
//  See the Apache Version 2.0 License for specific language governing permissions and 
//  limitations under the License.
//  ------------------------------------------------------------------------------------

namespace Test.Common
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    sealed class OperationTracker
    {
        // Index       Interval (ms)   Total
        //   0 - 99:   1               [  0 - 100)
        // 100 - 199:  2               [100 - 300)
        // 200 - 299:  4               [300 - 700)
        // 300 - 399:  8               [700 - 1500)
        // 400 - 499:  16              [1500 - 3100)
        // 500 - 599:  32              [3100 - 6300)
        // 600 - 699:  64              [6300 - 12700)
        // 700 - 799:  128             [12700 - 25800)
        struct Bucket
        {
            public readonly int Id;
            public readonly int MinMsInclusive;
            public readonly int MaxMsExclusive;

            public Bucket(int id, int minMsInclusive, int maxMsInclusive)
            {
                this.Id = id;
                this.MinMsInclusive = minMsInclusive;
                this.MaxMsExclusive = maxMsInclusive;
            }

            public int Compare(int latencyMs)
            {
                if (latencyMs < this.MinMsInclusive)
                {
                    return 1;
                }

                if (latencyMs >= this.MaxMsExclusive)
                {
                    return -1;
                }

                return 0;
            }
        }

        static Bucket[] buckets;

        static OperationTracker()
        {
            buckets = new Bucket[16];
            int totalMs = 0;
            for (int i = 0; i < buckets.Length; i++)
            {
                int intervalMs = 1 << i;
                int max = totalMs + 100 * intervalMs;
                buckets[i] = new Bucket(i, totalMs, max);
                totalMs = max;
            }
        }

        static int GetIndex(int latencyMs)
        {
            int min = 0;
            int max = buckets.Length - 1;
            while (min <= max)
            {
                int middle = (min + max) / 2;
                int comparison = buckets[middle].Compare(latencyMs);
                if (comparison == 0)
                {
                    int start = buckets[middle].Id * 100;
                    int interval = 1 << buckets[middle].Id;
                    int minMs = buckets[middle].MinMsInclusive;
                    return buckets[middle].Id * 100 + (latencyMs - minMs) / interval;
                }

                if (comparison < 0)
                {
                    min = middle + 1;
                }
                else
                {
                    max = middle - 1;
                }
            }

            return -1;
        }

        static int GetInterval(int idx)
        {
            return 1 << (idx / 100);
        }

        readonly long[] samples;
        long startTicks;
        long totalCount;
        long totalLatencyMs;

        public OperationTracker(int maxLatencyMs)
        {
            int size = GetIndex(maxLatencyMs);
            if (size <= 0)
            {
                throw new ArgumentException($"maxLatencyMs too large");
            }

            this.samples = new long[size];
            this.startTicks = Stopwatch.GetTimestamp();
        }

        public void Track(int latencyMs)
        {
            Interlocked.Add(ref this.totalCount, 1);
            Interlocked.Add(ref this.totalLatencyMs, latencyMs);

            int index = GetIndex(latencyMs);
            if (index >= 0 && index < this.samples.Length)
            {
                Interlocked.Increment(ref this.samples[index]);
            }
        }

        public string Report(bool reset)
        {
            double throughput = 0;
            double avgMs = 0;
            long p90 = 0;
            long p95 = 0;
            long p99 = 0;
            long p999 = 0;
            long total = this.totalCount;
            long totalMs = this.totalLatencyMs;

            long durationTicks = Stopwatch.GetTimestamp() - this.startTicks;
            if (durationTicks > 0)
            {
                throughput = (double)total * TimeSpan.TicksPerSecond / durationTicks;
            }

            if (total > 0)
            {
                avgMs = (double)totalMs / total;
            }

            long count = 0;
            for (int i = 0; i < this.samples.Length; i++)
            {
                count += this.samples[i];
                int intervalMs = GetInterval(i);
                if (p90 == 0 && count >= total * 0.9)
                {
                    p90 = (i + 1) * intervalMs;
                }

                if (p95 == 0 && count >= total * 0.95)
                {
                    p95 = (i + 1) * intervalMs;
                }

                if (p99 == 0 && count >= total * 0.99)
                {
                    p99 = (i + 1) * intervalMs;
                }

                if (p999 == 0 && count >= total * 0.999)
                {
                    p999 = (i + 1) * intervalMs;
                }
            }

            if (reset)
            {
                this.startTicks = Stopwatch.GetTimestamp();
                this.totalCount = 0;
                this.totalLatencyMs = 0;
                for (int i = 0; i < this.samples.Length; i++)
                {
                    this.samples[i] = 0;
                }
            }

            return $"Throughput:{throughput:F2} Latency:Avg={avgMs:F2} P90={p90} P95={p95} P99={p99} P99.9={p999}";
        }
    }
}
