/*
 * Copyright 2012 LBi Netherlands B.V.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. 
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using System.Xml.XPath;

namespace LBi.LostDoc.ConsoleApplication
{
    internal class ConsolidatedConsoleTraceListener : TraceListener
    {
        private Dictionary<string, string> _sourceMap;
        private ConcurrentDictionary<string, long> _startedTasks;

        public ConsolidatedConsoleTraceListener(Dictionary<string, string> sourceMap)
        {
            this._sourceMap = sourceMap;
            this._startedTasks = new ConcurrentDictionary<string, long>();
        }

        public override void Write(string message)
        {
            Console.Write(message);
        }

        public override void WriteLine(string message)
        {
            Console.WriteLine(message);
        }


        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
                                       object data)
        {
            this.TraceData(eventCache, source, eventType, id, new[] { data });
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
                                       params object[] data)
        {
            if (this.Filter != null &&
                !this.Filter.ShouldTrace(eventCache, source, eventType, id, string.Empty, null, null, null))
                return;

            foreach (object obj in data)
            {
                if (obj is XPathNavigator)
                {
                    using (var reader = ((XPathNavigator)obj).ReadSubtree())
                    {
                        reader.MoveToContent();
                        if (reader.IsStartElement())
                            Console.WriteLine(XElement.ReadFrom(reader).ToString(SaveOptions.OmitDuplicateNamespaces));
                        else
                        {
                            Console.WriteLine("Unable to write Xml data.");
                        }
                    }
                }
                else
                {
                    Console.WriteLine(obj.ToString());
                }
            }
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            if (this.Filter != null &&
                !this.Filter.ShouldTrace(eventCache, source, eventType, id, string.Empty, null, null, null))
                return;

            this.WriteLine(source, eventType, string.Empty);
        }  

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
                                        string format, params object[] args)
        {
            if (this.Filter != null &&
                !this.Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
                return;

            this.WriteLine(source, eventType, format, args);
        }

        

        private void WriteLine(string source, TraceEventType eventType, string format, object[] args = null)
        {
            string msg;
            if (args == null || args.Length == 0)
                msg = format;
            else
                msg = string.Format(format, args);

            string newSrc;
            if (!this._sourceMap.TryGetValue(source, out newSrc))
                newSrc = source;

            ConsoleColor oldColor = Console.ForegroundColor;

            switch (eventType)
            {
                case TraceEventType.Critical:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case TraceEventType.Error:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                case TraceEventType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case TraceEventType.Information:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case TraceEventType.Verbose:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case TraceEventType.Start:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    this._startedTasks.TryAdd(msg, Stopwatch.GetTimestamp());
                    msg = "Starting: " + msg;
                    break;
                case TraceEventType.Stop:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    long ts;
                    if (this._startedTasks.TryRemove(msg, out ts))
                    {
                        double seconds = (Stopwatch.GetTimestamp() - ts)/(double) Stopwatch.Frequency;
                        msg += string.Format(" [{0:N1} seconds]", seconds);
                    }
                    msg = "Finished: " + msg;
                    break;
                case TraceEventType.Suspend:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    break;
                case TraceEventType.Resume:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    break;
                case TraceEventType.Transfer:
                    return;
                default:
                    throw new ArgumentOutOfRangeException("eventType");
            }

            Console.WriteLine("[{0}] {1}", newSrc, msg);

            // restore color
            Console.ForegroundColor = oldColor;
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
                                        string message)
        {
            if (this.Filter != null &&
                !this.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
                return;

            this.WriteLine(source, eventType, message);
        }
    }
}
