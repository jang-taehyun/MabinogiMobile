using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;


namespace MabinogiMobileServer
{
    internal class ServerMain
    {
        static void Main(string[] args)
        {
            Dictionary<int, Socket> ClientList = new Dictionary<int, Socket>();
            int cnt = 0;
            Socket? Listener = null;

            try
            {
                IPEndPoint LocalAddress = new IPEndPoint(address: IPAddress.Any, port: 33355);
                if (LocalAddress is null)
                    throw new Exception("Address is null");

                Listener = new Socket(addressFamily: AddressFamily.InterNetwork, socketType: SocketType.Stream, protocolType: ProtocolType.Tcp);
                if (Listener is null)
                    throw new Exception("Listener is null");

                Listener.Bind(LocalAddress);
                Listener.Listen(100);

                while(true)
                {
                    // client connected
                    if(Listener.Poll(0, SelectMode.SelectRead) is true)
                    {
                        Socket Client = Listener.Accept();
                        ClientList[cnt++] = Client;
                        Console.WriteLine($"client connected!");
                    }
                    
                    // read/write data
                    foreach(KeyValuePair<int, Socket> p in ClientList)
                    {
                        if(p.Value.Available > 0)
                        {
                            // read data
                            float[] ts = new float[10];
                            byte[][] buffer = new byte[10][];
                            using (NetworkStream ns = new NetworkStream(p.Value))
                            {
                                for (int i = 0; i < 10; ++i)
                                {
                                    buffer[i] = new byte[4];
                                    ns.Read(buffer[i], 0, 4);
                                }
                                    

                                for (int i = 0; i < 10; ++i)
                                    ts[i] = BitConverter.ToSingle(buffer[i]);

                                // send data all client
                                foreach (KeyValuePair<int, Socket> p2 in ClientList)
                                {
                                    if (p2.Key != p.Key)
                                    {
                                        foreach (byte[] data in buffer)
                                            ns.Write(data, 0, data.Length);
                                    }
                                }
                            }

                            
                        }
                    }
                }
            }
            catch (Exception e)
            {
                e.OutputExceptionLog();
            }
            finally
            {
                // end
                foreach (KeyValuePair<int, Socket> p in ClientList)
                    p.Value.Close();
                Listener?.Close();
            }
        }
    }
}
