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
using UnityEngine.Serialization;

/**
 * C++ IOCP 서버와 통신을 하기 위한 IOCP 클라이언트 입니다.
 */
public class IOCPchatClnt : MonoBehaviour
{
    public static Socket hSocket;

    private string chattingText;
    private bool flag = false;
    private Thread recvThread;
    private static Queue<string> receiveQueue = new Queue<string>();
    private static Queue<string> sendQueue = new Queue<string>();
    [SerializeField] private Text ChatMessage;
    [SerializeField] private TMP_InputField chattingInputField;

    ///서버에 연결되면, 수신 전용 스레드를 동작합니다.
    private async void Start()
    {
        await ConnectToServer();
        recvThread = new Thread(() => RecvThreadMain(hSocket));
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

        if (receiveQueue.Count > 0)
        {
            //강제로 100바이트 크기이므로, 문자열의 원래 크기에 맞게 '\0'을 없애줍니다. 
            string strbuf = receiveQueue.Dequeue().TrimEnd('\0');
            Debug.Log(strbuf);
            Debug.Log(strbuf.Length);
            ChatMessage.text += strbuf;
            ChatMessage.text += "\n";
        }
    }

    /// 로컬 PC 서버에 TCP로 접속합니다.
    private async Task ConnectToServer()
    {
        hSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await hSocket.ConnectAsync(IPAddress.Parse("127.0.0.1"), 9190);
    }

    /// 소켓에서 데이터를 받아와서 수신 큐에 저장합니다.
    private void RecvThreadMain(Socket socket)
    {
        while (true)
        {
            byte[] ret = new byte[100];
            socket.Receive(ret, ret.Length, SocketFlags.None);
            string str = Encoding.UTF8.GetString(ret);

            receiveQueue.Enqueue(str);
        }
    }
}