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

namespace Amqp
{
    using System.Diagnostics;

    /// <summary>
    /// Defines the traces levels. Except Frame, levels are forward inclusive.
    /// For example, Information level includes the Error and Warning levels.
    /// </summary>
    public enum TraceLevel
    {
        /// <summary>
        /// Specifies that error events should be traced.
        /// </summary>
        Error = 0x01,

        /// <summary>
        /// Specifies that warning events should be traced.
        /// </summary>
        Warning = 0x03,

        /// <summary>
        /// Specifies that informational events should be traced.
        /// </summary>
        Information = 0x07,

        /// <summary>
        /// Specifies that verbose events should be traced.
        /// </summary>
        Verbose = 0x0F,

        /// <summary>
        /// Specifies that AMQP frames should be traced (<see cref="Trace.WriteFrameNullFields"/>
        /// controls how null fields should be handled).
        /// </summary>
        Frame = 0x10,

        /// <summary>
        /// Specifies that frame buffers should be traced. Cannot be combined with other levels.
        /// </summary>
        Buffer = 0x20,

        /// <summary>
        /// Specifies that application output should be traced.
        /// </summary>
        Output = 0x80
    }

    /// <summary>
    /// The callback to invoke to write traces.
    /// </summary>
    /// <param name="level">The trace level at which the trace event is raised.</param>
    /// <param name="format">The format string for the arguments.</param>
    /// <param name="args">The arguments attached to the trace event.</param>
    public delegate void WriteTrace(TraceLevel level, string format, params object[] args);

    /// <summary>
    /// The Trace class for writing traces.
    /// </summary>
    public static class Trace
    {
        /// <summary>
        /// Gets or sets the trace level.
        /// </summary>
        public static TraceLevel TraceLevel;

        /// <summary>
        /// Gets or sets the trace callback.
        /// </summary>
        public static WriteTrace TraceListener;

        /// <summary>
        /// Gets or sets the value that controls if null fields should be written in a frame trace.
        /// </summary>
        public static bool WriteFrameNullFields;

        /// <summary>
        /// Writes a debug trace.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The argument list.</param>
        [Conditional("DEBUG")]
        public static void Debug(string format, params object[] args)
        {
            if (TraceListener != null)
            {
                TraceListener(TraceLevel.Verbose, format, args);
            }
        }

        /// <summary>
        /// Writes a trace if the specified level is enabled.
        /// </summary>
        /// <param name="level">The trace level.</param>
        /// <param name="format">The content to trace.</param>
        [Conditional("TRACE")]
        public static void WriteLine(TraceLevel level, string format)
        {
            if (TraceListener != null && (level & TraceLevel) == level)
            {
                TraceListener(level, format);
            }
        }

        /// <summary>
        /// Writes a trace if the specified level is enabled.
        /// </summary>
        /// <param name="level">The trace level.</param>
        /// <param name="format">The format string.</param>
        /// <param name="arg1">The first argument.</param>
        [Conditional("TRACE")]
        public static void WriteLine(TraceLevel level, string format, object arg1)
        {
            if (TraceListener != null && (level & TraceLevel) == level)
            {
                TraceListener(level, format, arg1);
            }
        }

        /// <summary>
        /// Writes a trace if the specified level is enabled.
        /// </summary>
        /// <param name="level">The trace level.</param>
        /// <param name="format">The format string.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        [Conditional("TRACE")]
        public static void WriteLine(TraceLevel level, string format, object arg1, object arg2)
        {
            if (TraceListener != null && (level & TraceLevel) == level)
            {
                TraceListener(level, format, arg1, arg2);
            }
        }

        /// <summary>
        /// Writes a trace if the specified level is enabled.
        /// </summary>
        /// <param name="level">The trace level.</param>
        /// <param name="format">The format string.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        [Conditional("TRACE")]
        public static void WriteLine(TraceLevel level, string format, object arg1, object arg2, object arg3)
        {
            if (TraceListener != null && (level & TraceLevel) == level)
            {
                TraceListener(level, format, arg1, arg2, arg3);
            }
        }

        [Conditional("TRACE")]
        internal static void WriteBuffer(string format, byte[] buffer, int offset, int count)
        {
            if (TraceListener != null && (TraceLevel.Buffer & TraceLevel) > 0)
            {
                TraceListener(TraceLevel.Buffer, format, GetBinaryString(buffer, offset, count));
            }
        }

#if TRACE
        internal static string GetBinaryString(byte[] buffer, int offset, int count)
        {
            const string hexChars = "0123456789ABCDEF";
            System.Text.StringBuilder sb = new System.Text.StringBuilder(count * 2);
            for (int i = offset; i < offset + count; ++i)
            {
                sb.Append(hexChars[buffer[i] >> 4]);
                sb.Append(hexChars[buffer[i] & 0x0F]);
            }

            return sb.ToString();
        }

        internal static object GetTraceObject(object value)
        {
            if (value == null)
            {
                return "null";
            }

            byte[] binary = value as byte[];
            if (binary != null)
            {
                return GetBinaryString(binary, 0, binary.Length);
            }

            var list = value as System.Collections.IList;
            if (list != null)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append('[');
                for (int i = 0; i < list.Count; ++i)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(list[i]);
                }
                sb.Append(']');

                return sb.ToString();
            }

            return value;
        }
#endif
    }
}