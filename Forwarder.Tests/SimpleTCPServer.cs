using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Forwarder.Tests
{
    public class SimpleTCPServer : IDisposable
    {
        public delegate void DatagramReceiveHandler(string data);
        public bool Crashy { get; set; }
        public int CrashCount { get; set; } = 0;
        public int HitCount { get; set; } = 0;

        private bool running = false;
        private bool disposed = false;
        private readonly DatagramReceiveHandler handler;
        private readonly Encoding encoding;
        private Thread serverThread;
        private TcpListener server;

        public SimpleTCPServer(int port, DatagramReceiveHandler handler)
            : this(port, handler, Encoding.Unicode)
        {
        }

        public SimpleTCPServer(int port, DatagramReceiveHandler handler, Encoding encoding)
        {
            var localIPEndPoint = new IPEndPoint(IPAddress.Loopback, port);
            server = new TcpListener(localIPEndPoint);

            this.handler = handler;
            this.encoding = encoding;
        }

        public void Start()
        {
            if (running)
            {
                throw new Exception("SimpleUDPServer: multiple calls to start");
            }
            running = true;
            serverThread = new Thread(startServer);
            serverThread.Start();

            while (!serverThread.IsAlive) ;
        }

        public void Stop()
        {
            if (running)
            {
                running = false;
                if (server != null)
                {
                    server.Stop();
                }
                if (serverThread != null)
                {
                    serverThread.Join();
                    serverThread = null;
                    server = null;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                Stop();
            }
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void StreamMessages()
        {
            int crashInterval = 1;
            bool crashed = false;

            TcpClient client = server.AcceptTcpClient();
            NetworkStream stream = client.GetStream();

            try
            {
                using (StreamReader sr = new StreamReader(stream, encoding))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (Crashy && crashInterval++ % 7 == 0)
                        {
                            crashed = true;
                            CrashCount++;
                            stream.Close();
                            var xxx = sr.ReadToEnd();
                            var yyy = xxx; // DEBUG THIS THING
                            //break;
                        }
                        HitCount++;
                        this.handler(line);
                    }
                }
            }
            catch (ObjectDisposedException ex)
            {
                if (!crashed)
                {
                    throw ex;
                }
                return;
            }
            catch (Exception ex)
            {
                throw ex; // here for debugging
            }
            finally
            {
                if (!crashed)
                {
                    client.Close();
                }
            }
        }

        private void startServer()
        {
            try
            {
                server.Start();
                while (running)
                {
                    StreamMessages();
                }
            }
            catch (SocketException ex)
            {
                // ignore - we closed the connection
                return;
            }
            catch (Exception ex)
            {
                // Great...
            }
        }
    }
}
