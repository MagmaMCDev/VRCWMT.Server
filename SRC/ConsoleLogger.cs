// File: Services/ConsoleLogger.cs

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace VRCWMT.Services
{
    public class ConsoleLogger : TextWriter
    {
        private readonly ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();
        private readonly TextWriter _originalOut;

        public ConsoleLogger(TextWriter originalOut)
        {
            _originalOut = originalOut;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            _originalOut.Write(value);
            _logQueue.Enqueue(value.ToString());
        }

        public override void Write(string? value)
        {
            _originalOut.Write(value);
            if (value != null)
            {
                _logQueue.Enqueue(value);
            }
        }

        public override void WriteLine(string? value)
        {
            _originalOut.WriteLine(value);
            if (value != null)
            {
                _logQueue.Enqueue(value + Environment.NewLine);
            }
        }

        public List<string> GetLogs()
        {
            var logs = new List<string>();
            while (_logQueue.TryDequeue(out var log))
            {
                logs.Add(log);
            }
            return logs;
        }
    }
}
