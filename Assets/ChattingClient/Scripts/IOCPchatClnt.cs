using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using UnityEngine.Serialization;

/**
 * C++ IOCP 서버와 통신을 하기 위한 IOCP 클라이언트 입니다.
 */
public class IOCPchatClnt : MonoBehaviour
{
    public static Socket hSocket;

    private string chattingText;
    private Thread recvThread;
    private int byteMaxNum = 100;

    //thread-safe 보장을 위한 ConcurrentQueue
    private static ConcurrentQueue<string> receiveQueue = new ConcurrentQueue<string>();
    [SerializeField] private Text chatMessage;
    [SerializeField] private TMP_InputField chattingInputField;
    [SerializeField] private string ipString = "127.0.0.1";
    [SerializeField] private int portInt = 9190;

    //수신 스레드 동작 여부
    private volatile bool bIsRunning = true;


    ///서버에 연결되면, 수신 전용 스레드를 동작합니다.
    private async void Start()
    {
        try
        {
            await ConnectToServer();
        }
        catch (SocketException e)
        {
            Debug.LogError($"Socket Error: {e.SocketErrorCode}");
            Debug.LogError($"Message: {e.Message}");
            return;
        }

        //소켓에 정상적으로 연결되었을 경우    
        recvThread = new Thread(() => RecvThreadMain(hSocket));
        recvThread.IsBackground = true;
        recvThread.Start();
    }

    /**
     * 사용자의 이름과 채팅내용을 [이름] : 채팅내용 의 형태로 담아 UTF8로 인코딩해서 송신합니다.
     * 또한 수신 큐를 검사해서 비어있지않다면, 그 문자열을 채팅창에 추가합니다.
     */
    void Update()
    {
        if (chattingInputField.text.Length > 0 && Input.GetKeyDown(KeyCode.Return))
        {
            chattingText = "[" + UserName.name + "] : ";
            chattingText += chattingInputField.text;
            chattingInputField.text = null;

            byte[] message = System.Text.Encoding.UTF8.GetBytes(chattingText);

            hSocket.Send(message, message.Length, SocketFlags.None);
            chattingText = null;
        }

        string strbuf;
        //여러 메시지를 동시 수신 처리 위해 while 사용
        while (receiveQueue.TryDequeue(out strbuf))
        {
            Debug.Log(strbuf);
            Debug.Log(strbuf.Length);
            chatMessage.text += strbuf;
            chatMessage.text += "\n";
        }
    }

    /// 로컬 PC 서버에 TCP로 접속합니다.
    private async Task ConnectToServer()
    {
        hSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await hSocket.ConnectAsync(IPAddress.Parse(ipString), portInt);
    }

    /// 소켓에서 데이터를 받아와서 수신 큐에 저장합니다.
    private void RecvThreadMain(Socket socket)
    {
        try
        {
            byte[] ret = new byte[byteMaxNum];
            while (bIsRunning)
            {
                int recvFlag = socket.Receive(ret, ret.Length, SocketFlags.None);
                if (recvFlag <= 0)
                {
                    break;
                }

                string str = Encoding.UTF8.GetString(ret, 0, recvFlag);

                receiveQueue.Enqueue(str);
            }
        }
        catch (SocketException)
        {
            Debug.LogError("SocketException");
            return;
        }
        catch (ObjectDisposedException)
        {
            Debug.LogError("ObjectDisposedException");
            return;
        }
    }

    /**
     * 유니티 에디터 클라이언트 종료시 명시적 연결 종료
     */
    private void OnDestroy()
    {
        bIsRunning = false;

        // 소켓의 송수신 경로를 닫아 Receive를 리턴시킴
        hSocket?.Shutdown(SocketShutdown.Both);
        hSocket?.Close();
    }
}