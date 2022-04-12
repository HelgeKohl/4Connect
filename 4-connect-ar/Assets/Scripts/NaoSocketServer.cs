using OpenCvSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class NaoSocketServer : MonoBehaviour
{
    System.Threading.Thread SocketThread;
    volatile bool keepReading = false;
    internal static byte[] ImageBytes = null;
    public static WinState WinState { get; private set; }
    public static int SuggestedIndex { get; private set; }
    public static bool NaoRequestActive { get; internal set; }

    public static string PythonNaoPath { 
        get
        {
            return Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName, "python-nao");
        }
    }

    public static Texture2D CurrentTexture2D { get; internal set; }


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
                Debug.Log("Waiting for Connection");
                handler = listener.Accept();
                Debug.Log("Client Connected");
                data = null;

                // An incoming connection needs to be processed.
                while (keepReading)
                {
                    bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);

                    if (bytesRec <= 0)
                    {
                        keepReading = false;
                        handler.Disconnect(true);
                        break;
                    }
                    
                    AppendAllBytes(Path.Combine(PythonNaoPath, "tmp", "unity.png"), bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    if (data.IndexOf("<EOF>") > -1)
                    {
                        Debug.Log("EOF");
                        string file = Path.Combine(PythonNaoPath, "tmp", "unity.png");
                        string file_current = Path.Combine(PythonNaoPath, "tmp", "unity_current.png");

                        // Delete old unity_current.png file, if it exists
                        if (File.Exists(file_current))
                        {
                            File.Delete(file_current);
                        }
                        File.Move(file, file_current);
                        // Delete tmp unity.png file
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                        }

                        // Neues Bild übertragen und erhalten
                        // file_current
                        //Texture2D image = new Texture2D(640, 480);
                        //NaoSocketServer.Image = new Texture2D(640, 480);
                        NaoSocketServer.ImageBytes = File.ReadAllBytes(file_current);

                        // Zurücksetzen
                        NaoSocketServer.CurrentTexture2D = null;

                        NaoSocketServer.NaoRequestActive = true;
                        //NaoSocketServer.Image.LoadImage(imgBytes);

                        //AppendAllBytes(file, Encoding.ASCII.GetBytes(data));

                        while(NaoSocketServer.NaoRequestActive)
                        {
                            System.Threading.Thread.Sleep(1);
                        }


                        data = null;

                        

                        //struct.pack('!ii', state, column)
                        object[] items = new object[2];
                        //items[0] = (byte)2;
                        //items[1] = (byte)2;
                        //items[0] = 1;
                        //items[1] = 2;
                        items[0] = new System.Random().Next(4);
                        items[1] = new System.Random().Next(7);

                        items[0] = (int) NaoSocketServer.WinState;
                        items[1] = NaoSocketServer.SuggestedIndex;
                        Debug.Log(items[0] + " ... " + items[1]);
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

    internal static void SetState(WinState winState, int suggestedIndex)
    {
        WinState = winState;
        SuggestedIndex = suggestedIndex;
        Debug.Log("" + winState);
        Debug.Log("Winstate: " + (int)WinState + " SuggestedIndex: " + suggestedIndex);
    }
}

