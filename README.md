# IOCP 채팅 클라이언트

> Unity 2021.3.45f2 기반 TCP 실시간 채팅 클라이언트 — C++ IOCP 서버와 멀티스레드 소켓 통신

**[▶ 시연 영상](https://www.youtube.com/watch?v=_mLpdSNiVVQ)**

---

## 프로젝트 개요

|             |                                                                                           |
| ----------- | ----------------------------------------------------------------------------------------- |
| **기간**      | 2026.08.12 ~ 2026.08.20 (9일)(기존 개인 프로젝트에서 채팅 클라이언트만 추출해 리팩토링)                       |
| **팀 구성**    | 개인                                                                                        |
| **본인 역할**   | 클라이언트 전체 구현                                                                               |
| **엔진 / 언어** | Unity 2021.3.45f2 · C# (.NET 4.7.1)                                                       |
| **주요 기술**   | TCP 소켓, 멀티스레딩, ConcurrentQueue, Length-Prefix 프레이밍, TextMeshPro                           |
| **연동 서버**   | [khs081215/IOCPUnityChattingServer](https://github.com/khs081215/IOCPUnityChattingServer) |

---

## 핵심 구현

### 1. 멀티스레드 수신 구조 — UI 블로킹 없는 실시간 메시지 수신

**문제**
Unity의 `Update()`에서 `Socket.Receive()`를 직접 호출하면 데이터가 도착할 때까지 프레임 전체가 멈춘다.

**해결**
전용 백그라운드 스레드(`RecvThreadMain`)가 소켓을 블로킹 방식으로 감시하고, 수신된 메시지를 `ConcurrentQueue<string>`에 enqueue한다. 메인 스레드의 `Update()`에서 `while(TryDequeue)` 루프로 꺼내 UI에 반영한다.

**설계 판단**
Unity는 UI 접근을 메인 스레드로 제한하므로 스레드 간 공유 자료구조가 필요했다. `lock`을 직접 구현하는 대신 `ConcurrentQueue<string>`를 선택해 동기화 코드를 직접 작성하는 데서 오는 실수를 줄였다.

큐를 꺼낼 때 `if`가 아니라 `while`을 쓴 이유는, 한 프레임에 여러 메시지가 도착했을 때 나머지가 다음 프레임으로 밀리지 않게 하기 위해서다.

**결과**
수신 대기 중에도 UI가 멈추지 않으며, 한 프레임에 여러 메시지가 도착해도 그 프레임 안에서 모두 처리된다.

---

### 2. Length-Prefix 프레이밍 — TCP 스트림의 메시지 경계 처리

**문제**
초기에는 교재의 에코 서버 구조를 그대로 가져와 연결했다. 긴 메시지를 보낸 뒤 짧은 메시지를 보내면 이전 데이터가 뒤에 붙는 현상이 있었다.

원인을 보니 수신 버퍼 전체를 문자열로 변환하고 있었다. 실제 수신 바이트 수만 변환하도록 고친 뒤에도, TCP가 스트림 프로토콜이라 단일 `Receive()` 호출이 전송된 메시지 경계와 일치하지 않는다는 점은 그대로 남아 있었다.

**해결**
`MakePacket()`에서 UTF-8 인코딩된 메시지 앞에 **4바이트 Big-Endian 길이 필드**를 붙여 전송한다.

```csharp
// MakePacket: 길이 프리픽스(Big-Endian) + 메시지 본문
byte[] length = BitConverter.GetBytes(message.Length);
if (BitConverter.IsLittleEndian) Array.Reverse(length);
```

또한 `Send()`가 요청한 바이트를 한 번에 모두 보내지 못할 수 있으므로, 전송이 완료될 때까지 반복한다.

```csharp
int totalSent = 0;
while (totalSent < packet.Length)
{
    totalSent += hSocket.Send(packet, totalSent,
        packet.Length - totalSent, SocketFlags.None);
}
```

**설계 판단**
Big-Endian을 선택한 이유는 네트워크 바이트 오더 관례를 따르기 위해서이며, 서버는 `ntohl()`로 그대로 변환한다. `BitConverter.IsLittleEndian` 검사를 넣어 실행 환경의 엔디안에 관계없이 동작하도록 했다.

**결과**
서버가 병합·분할 수신 상황 모두에서 메시지 경계를 복원함을 확인했다. 검증 내용은 [서버 레포](https://github.com/khs081215/IOCPUnityChattingServer)에 정리했다.

---

### 3. 안전한 소켓 종료 — 수신 스레드 누수 방지

**문제**
Unity 에디터에서 Play Mode를 중지하는 것은 프로세스 종료가 아니다. 수신 스레드가 `Socket.Receive()` 블로킹 상태에 머물러 있으면 그대로 남고, 다음 Play에서 하나가 더 생긴다.

**해결**
두 단계로 처리한다.

1. `bIsRunning`(`volatile bool`)을 `false`로 내려 다음 루프 진입을 막는다.
2. `hSocket.Shutdown(SocketShutdown.Both)`로 소켓의 송수신 경로를 닫아, 이미 블로킹 중인 `Receive()`가 반환되도록 한다.

**설계 판단**
플래그만 내려서는 부족하다. 이미 `Receive()`에서 대기 중인 스레드는 루프 조건을 검사할 기회 자체가 없기 때문이다. 소켓을 닫아야 대기가 풀린다.

`Thread.Abort()`는 .NET에서 비권장이며 정리 시점을 예측할 수 없어 사용하지 않았다.

`bIsRunning`에 `volatile`을 붙인 이유는, 컴파일러가 루프 안에서 값이 바뀌지 않는다고 판단해 레지스터에 캐싱하면 메인 스레드의 변경이 반영되지 않을 수 있기 때문이다.

소켓이 닫히는 과정에서 `SocketException`과 `ObjectDisposedException`이 발생할 수 있어, 종료 흐름의 일부로 보고 처리했다.

**결과**
에디터 Play 중지와 씬 전환 시 스레드가 정상 종료된다.

---

## 알려진 한계

- **수신 측 프레이밍 미구현** — 송신에는 길이 prefix를 붙이지만, 수신은 받은 바이트를 그대로 문자열로 변환한다. 현재 서버가 본문만 브로드캐스트하므로 동작하지만, 메시지가 이어붙어 도착하면 한 줄로 표시된다. 서버와 동일한 링 버퍼 방식의 파싱이 다음 작업이다.
- **채팅 로그 무한 누적** — 수신 메시지를 `Text`에 계속 이어붙이므로 장시간 실행 시 문자열이 계속 커진다. 표시 줄 수 상한이 필요하다.
- **재연결 미지원** — 연결이 끊기면 씬을 다시 시작해야 한다.

---

## 빌드 및 실행

```
엔진 버전 : Unity 2021.3.45f2 (LTS)
서버      : https://github.com/khs081215/IOCPUnityChattingServer (별도 실행 필요)
```

1. 레포를 클론합니다.
2. Unity Hub에서 Unity 2021.3.45f2로 프로젝트를 엽니다.
3. C++ IOCP 서버를 먼저 실행합니다 (기본 포트: `9190`).
4. Unity Editor에서 `titlescene`을 열고 Play Mode를 실행합니다.
5. 이름을 입력하면 채팅 씬으로 전환되고 서버에 자동 연결됩니다.

접속 주소와 포트는 `IOCPchatClnt` 컴포넌트의 인스펙터에서 변경할 수 있습니다.

**TextMeshPro와 같은 플러그인은 해당 레포에 포함되어있지 않습니다.**

---

## 참고

- [IOCP 채팅 서버 (C++)](https://github.com/khs081215/IOCPUnityChattingServer)
