using System;
using System.Diagnostics;
using System.Net.Sockets;

namespace Forwarder
{
    public class TCPForwarder : Forwarder
    {
        private TcpClient client;
        private NetworkStream stream;
        private string hostname;
        private int port;

        // WARN WARN WARN
        public int CallCount { get; set; } = 0;
        public int HandleCount { get; set; } = 0;
        public int ExceptionCount { get; set; } = 0;
        private int missCount = 0;

        private void HandleWriteError(Exception cause, byte[] msg)
        {
            // WARN WARN WARN
            HandleCount++;

            if (client == null || !client.Connected)
            {
                client?.Close();
                client = null;
                client = new TcpClient(hostname, port)
                {
                    SendTimeout = 2000 // 2 seconds
                };
                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }
                stream = client.GetStream();
                stream.Write(msg, 0, msg.Length);
            }
        }

        public void Write(EventLogEntry entry)
        {
            CallCount++;

            byte[] msg = FormatMessage(Priority.LOG_DEBUG, entry.Source, entry.Message);

            try
            {
                if (stream == null)
                {
                    stream = client.GetStream();
                }
                stream.Write(msg, 0, msg.Length);
            }
            catch (System.IO.IOException ex) { HandleWriteError(ex, msg); }
            catch (InvalidOperationException ex) { HandleWriteError(ex, msg); }
            catch (Exception ex)
            {
                ExceptionCount++;
            }
        }

        // WARN (CEV): Make this class disposable!
        public void Close()
        {
            if (client != null)
            {
                client.Close();
                client = null;
            }
            if (stream != null)
            {
                stream.Close();
                stream = null;
            }
        }

        public TCPForwarder(string hostname, int port)
        {
            this.port = port;
            this.hostname = hostname;
            client = new TcpClient(hostname, port)
            {

                // WARN: Are there any other timeouts that
                // we want to set - are there defaults???
                SendTimeout = 2000 // 2 Seconds
            };
        }
    }
}
