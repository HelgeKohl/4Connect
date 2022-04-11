using OpenCvSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class NaoSocketServer : MonoBehaviour
{
    System.Threading.Thread SocketThread;
    volatile bool keepReading = false;

    // Use this for initialization
    void Start()
    {
        Debug.Log("Start NaoSocketServer");
        Application.runInBackground = true;
        startServer();
    }

    void startServer()
    {
        SocketThread = new System.Threading.Thread(networkCode);
        SocketThread.IsBackground = true;
        SocketThread.Start();
    }



    private string getIPAddress()
    {
        IPHostEntry host;
        string localIP = "";
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
            }

        }
        return localIP;
    }


    Socket listener;
    Socket handler;

    public static void AppendAllBytes(string path, byte[] bytes)
    {
        //argument-checking here.

        using (var stream = new FileStream(path, FileMode.Append))
        {
            stream.Write(bytes, 0, bytes.Length);
        }
    }

    void networkCode()
    {
        string data;

        // Data buffer for incoming data.
        byte[] bytes = new Byte[1024];

        // host running the application.
        Debug.Log("Ip " + getIPAddress().ToString());
        IPAddress[] ipArray = Dns.GetHostAddresses(getIPAddress());
        IPEndPoint localEndPoint = new IPEndPoint(ipArray[0], 9001);

        // Create a TCP/IP socket.
        listener = new Socket(ipArray[0].AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the local endpoint and 
        // listen for incoming connections.

        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(10);

            // Start listening for connections.
            while (true)
            {
                keepReading = true;

                // Program is suspended while waiting for an incoming connection.
                Debug.Log("Waiting for Connection");     //It works

                handler = listener.Accept();
                Debug.Log("Client Connected");     //It doesn't work
                data = null;


                // An incoming connection needs to be processed.
                while (keepReading)
                {
                    bytes = new byte[1024];
                    //bytes = new byte[4];
                    //bytes = new byte[361731];
                    int bytesRec = handler.Receive(bytes);
                    //Debug.Log("Received from Server bytes: - " + bytes);
                    //Debug.Log("Received from Server - bytesRec: " + bytesRec);

                    if (bytesRec <= 0)
                    {
                        keepReading = false;
                        handler.Disconnect(true);
                        break;
                    }

                    //Debug.Log("Unpack: " + StructConverter.Unpack(">i", bytes));

                    //data += Encoding.ASCII.GetString(bytes, 0, bytesRec);

                    //AppendAllBytes(@"C:\Users\Stephan\Desktop\nao\client_server_sockets_send_img_py\unity.png", bytes);
                    AppendAllBytes(@"C:\Development\nao\nao\client_server_sockets_send_img_py\unity.png", bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    if (data.IndexOf("<EOF>") > -1)
                    {
                        //data = data.Replace("<EOF>", "");
                        Debug.Log("EOF");
                        string file = @"C:\Development\nao\nao\client_server_sockets_send_img_py\unity.png";
                        string file_current = @"C:\Development\nao\nao\client_server_sockets_send_img_py\unity_current.png";
                        if (File.Exists(file_current))
                        {
                            File.Delete(file_current);
                        }
                        File.Move(file, file_current);
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                        }
                        //AppendAllBytes(file, Encoding.ASCII.GetBytes(data));
                        
                        data = null;

                        //struct.pack('!ii', state, column)
                        object[] items = new object[2];
                        //items[0] = (byte)2;
                        //items[1] = (byte)2;
                        //items[0] = 1;
                        //items[1] = 2;
                        items[0] = new System.Random().Next(4);
                        items[1] = new System.Random().Next(7);
                        byte[] packed = StructConverter.Pack(items);

                        handler.Send(packed);
                        
                        //handler.Send()
                        break;
                    }

                    System.Threading.Thread.Sleep(1);
                }

                System.Threading.Thread.Sleep(1);    
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    void stopServer()
    {
        keepReading = false;

        //stop thread
        if (SocketThread != null)
        {
            SocketThread.Abort();
        }

        if (handler != null && handler.Connected)
        {
            handler.Disconnect(false);
            Debug.Log("Disconnected!");
        }
    }

    void OnDisable()
    {
        stopServer();
    }
}

