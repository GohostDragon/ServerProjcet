using System;
using System.Collections.Generic;
using System.Text;

namespace PacketGenerator
{
    class PacketFormat
    {
        // {0} 패킷 이름
        public static string managerRegisterFormat =
@"       _makeFunc.Add((ushort)PacketID.{0}, MakePacket<{0}>);
       _handler.Add((ushort)PacketID.{0}, PacketHandler.{0}Handler);";

        // {0} 패킷 등록
        public static string managerFormat =
@"using ServerCore;
using System;
using System.Collections.Generic;

public class PacketManager
{{
    #region Singleton
    static PacketManager _instance = new PacketManager();
    public static PacketManager Instance {{ get {{ return _instance; }} }}
    #endregion

    private PacketManager()
    {{
        Register();
    }}

    Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>> _makeFunc = new Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>>();
    Dictionary<ushort, Action<PacketSession, IPacket>> _handler = new Dictionary<ushort, Action<PacketSession, IPacket>>();

    public void Register()
    {{
{0}
    }}

    public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer, Action<PacketSession, IPacket> onRecvCallback = null)
    {{
        ushort count = 0;

        ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
        count += 2;
        ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
        count += 2;

        Func<PacketSession, ArraySegment<byte>, IPacket> func = null;
        if (_makeFunc.TryGetValue(id, out func)) {{
            IPacket packet = func.Invoke(session, buffer);

            if(onRecvCallback != null) {{
                onRecvCallback.Invoke(session, packet);
            }} else {{
                HandlePacket(session, packet);
            }}
        }}
    }}

    private T MakePacket<T>(PacketSession session, ArraySegment<byte> buffer) where T : IPacket, new()
    {{
        T packet = new T();
        packet.Read(buffer);
        return packet;
    }}

    public void HandlePacket(PacketSession session, IPacket packet) {{
        Action<PacketSession, IPacket> action = null;
        if (_handler.TryGetValue(packet.Protocol, out action)) {{
            action.Invoke(session, packet);
        }}
    }}
}}";




        // 1. 아래는 패킷에 대한 포맷이고
        // 2. 이번엔 파일 자체에 대한 포맷
        // 3. 패킷 목록도 추가

        // {0} 패킷 이름 / 번호 목록
        // {1} 패킷 목록
        public static string fileFormat =
@"using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using ServerCore;

public enum PacketID
{{
    {0}
}}

public interface IPacket
{{
	ushort Protocol {{ get; }}
	void Read(ArraySegment<byte> segment);
	ArraySegment<byte> Write();
}}

{1}
";
        // 4. 패킷 ID에 대응되는 패킷 Enum
        // 5. 다 추가되면 패킷 제네레이터 Program으로 이동

        // {0} 패킷 이름
        // {1} 패킷 번호
        public static string packetEnumFormat =
@"{0} = {1},";


        // {0} 패킷 이름
        // {1} 멤버 변수 이름
        // {2} 멤버 변수 Read
        // {3} 멤버 변수 Write
        public static string packetFormat =
@"
public class {0} : IPacket
{{
    {1}

    public ushort Protocol {{ get {{ return (ushort)PacketID.{0}; }} }}

    public void Read(ArraySegment<byte> segment)
    {{
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

        // size / packetId
        count += sizeof(ushort);
        count += sizeof(ushort);

        {2}
    }}

    public ArraySegment<byte> Write()
    {{
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.{0});
        count += sizeof(ushort);

        {3} 

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false) {{
            return null;
        }}

        return SendBufferHelper.Close(count);
    }}
}}
";

        // {0} 변수 형식
        // {1} 변수 이름
        public static string memberFormat =
@"public {0} {1};";

        // {0} 리스트 이름 [대문자]
        // {1} 리스트 이름 [소문자]
        // {2} 멤버 변수들
        // {3} 멤버 변수들 Read
        // {4} 멤버 변수들 Write
        public static string memberListFormat =
@"
public class {0}
{{
    {2}

    public void Read(ReadOnlySpan<byte> s, ref ushort count)
    {{
        {3}
    }}

    public bool Write(Span<byte> s, ref ushort count)
    {{
        bool success = true;
       {4}
        return success;
    }}
}}

public List<{0}> {1}s = new List<{0}>();
";


        // {0} 변수 이름
        // {1} To - 변수 형식
        // {2} 변수 형식
        public static string readFormat =
@"this.{0} = BitConverter.{1}(s.Slice(count, s.Length - count));
                count += sizeof({2});";


        // {0} 변수 이름
        // {1} 변수 형식
        public static string readByteFormat =
@"this.{0} = ({1})segment.Array[segment.Offset + count];
count += sizeof({1});";


        // {0} 변수 이름
        public static string readStringFormat =
@"ushort {0}Length = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
count += sizeof(ushort);
this.{0} = Encoding.Unicode.GetString(s.Slice(count, {0}Length));
count += {0}Length;
";

        // {0} 리스트 이름 [대문자]
        // {1} 리스트 이름 [소문자]
        public static string readListFormat =
@"{1}s.Clear();
ushort {1}Length = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
count += sizeof(ushort);
for (int i = 0; i < {1}Length; i++) {{
    {0} {1} = new {0}();
    {1}.Read(s, ref count);
    {1}s.Add({1});
}}";

        // {0} 변수 이름
        // {1} 변수 형식
        public static string writeFormat =
@"success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.{0});
count += sizeof({1});
";

        // {0} 변수 이름
        // {1} 변수 형식
        public static string writeByteFormat =
@"segment.Array[segment.Offset + count] = (byte)this.{0};
count += sizeof({1});";


        // {0} 변수 이름
        public static string writeStringFormat =
@"ushort {0}Length = (ushort)Encoding.Unicode.GetBytes(this.{0}, 0, this.{0}.Length, segment.Array, segment.Offset + count + sizeof(ushort));
success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), {0}Length);
count += sizeof(ushort);
count += {0}Length;
";

        // {0} 리스트 이름 [대문자]
        // {1} 리스트 이름 [소문자]
        public static string writeListFormat =
@"success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)this.{1}s.Count);
count += sizeof(ushort);
foreach ({0} {1} in this.{1}s) {{
    success &= {1}.Write(s, ref count);
}}";
    }
}



//// <패킷 제네레이터 2#> 22.02.28 - 리스트 추가
//namespace PacketGenerator
//{
//    class PacketFormat
//    {
//        // {0} 패킷 이름
//        // {1} 멤버 변수 이름
//        // {2} 멤버 변수 Read
//        // {3} 멤버 변수 Write
//        public static string packetFormat =
//@"
//class {0}
//{{
//    {1}

//    public void Read(ArraySegment<byte> segment)
//    {{
//        ushort count = 0;

//        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

//        // size / packetId
//        count += sizeof(ushort);
//        count += sizeof(ushort);

//        {2}
//    }}

//    public ArraySegment<byte> Write()
//    {{
//        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

//        ushort count = 0;
//        bool success = true;

//        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
//        count += sizeof(ushort);
//        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.{0});
//        count += sizeof(ushort);

//        {3} 

//        success &= BitConverter.TryWriteBytes(s, count);

//        if (success == false) {{
//            return null;
//        }}

//        return SendBufferHelper.Close(count);
//    }}
//}}
//";

//        // {0} 변수 형식
//        // {1} 변수 이름
//        public static string memberFormat =
//@"public {0} {1};";

//        // 1. 리스트 처리

//        // {0} 리스트 이름 [대문자]
//        // {1} 리스트 이름 [소문자]
//        // {2} 멤버 변수들
//        // {3} 멤버 변수들 Read
//        // {4} 멤버 변수들 Write
//        public static string memberListFormat =
//@"
//public struct {0}
//{{
//    {2}

//    public void Read(ReadOnlySpan<byte> s, ref ushort count)
//    {{
//        {3}
//    }}

//    public bool Write(Span<byte> s, ref ushort count)
//    {{
//        bool success = true;
//       {4}
//        return success;
//    }}
//}}

//public List<{0}> {1}s = new List<{0}>();
//";


//        // {0} 변수 이름
//        // {1} To - 변수 형식
//        // {2} 변수 형식
//        public static string readFormat =
//@"this.{0} = BitConverter.{1}(s.Slice(count, s.Length - count));
//                count += sizeof({2});";

//        // {0} 변수 이름
//        public static string readStringFormat =
//@"ushort {0}Length = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
//count += sizeof(ushort);
//this.name = Encoding.Unicode.GetString(s.Slice(count, nameLength));
//count += {0}Length;
//";

//        // 2.
//        // {0} 리스트 이름 [대문자]
//        // {1} 리스트 이름 [소문자]
//        public static string readListFormat =
//@"{1}s.Clear();
//ushort {1}Length = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
//count += sizeof(ushort);
//for (int i = 0; i < {1}Length; i++) {{
//    {0} {1} = new {0}();
//    {1}.Read(s, ref count);
//    {1}s.Add({1});
//}}";

//        // {0} 변수 이름
//        // {1} 변수 형식
//        public static string writeFormat =
//@"success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.{0});
//count += sizeof({1});
//";

//        // {0} 변수 이름
//        public static string writeStringFormat =
//@"ushort {0}Length = (ushort)Encoding.Unicode.GetBytes(this.{0}, 0, this.{0}.Length, segment.Array, segment.Offset + count + sizeof(ushort));
//success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), {0}Length);
//count += sizeof(ushort);
//count += {0}Length;
//";

//        // {0} 리스트 이름 [대문자]
//        // {1} 리스트 이름 [소문자]
//        public static string writeListFormat =
//@"success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)this.{1}s.Count);
//count += sizeof(ushort);
//foreach ({0} {1} in this.{1}s) {{
//    success &= {1}.Write(s, ref count);
//}}";
//    }
//}



//// <패킷 제네레이터 1#> 22.02.28 - 기초 작업
//// 1. 패킷 포맷 추가
//namespace PacketGenerator
//{
//    class PacketFormat
//    {
//        // 1. ServerSession의 부분을 복사
//        // 2. 고정적인 부분과 바꿔치기할 부분을 구분
//        // -> 바꿔치기할 부분들은 {0} 같은 식으로 바꿔치기
//        // -> { 코드의 소괄호는 문자열에선 {{ 두개씩 써야함

//        // {0} 패킷 이름
//        // {1} 멤버 변수 이름
//        // {2} 멤버 변수 Read
//        // {3} 멤버 변수 Write
//        public static string packetFormat =
//@"
//class {0}
//{{
//    {1}

//    public void Read(ArraySegment<byte> segment)
//    {{
//        ushort count = 0;

//        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

//        // size / packetId
//        count += sizeof(ushort);
//        count += sizeof(ushort);

//        {2}
//    }}

//    public ArraySegment<byte> Write()
//    {{
//        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

//        ushort count = 0;
//        bool success = true;

//        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
//        count += sizeof(ushort);
//        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.{0});
//        count += sizeof(ushort);

//        {3} 

//        success &= BitConverter.TryWriteBytes(s, count);

//        if (success == false) {{
//            return null;
//        }}

//        return SendBufferHelper.Close(count);
//    }}
//}}
//";

//        // {0} 변수 형식
//        // {1} 변수 이름
//        public static string memberFormat =
//@"public {0} {1}";

//        // {0} 변수 이름
//        // {1} To - 변수 형식
//        // {2} 변수 형식
//        public static string readFormat =
//@"this.{0} = BitConverter.{1}(s.Slice(count, s.Length - count));
//                count += sizeof({2});";

//        // {0} 변수 이름
//        public static string readStringFormat =
//@"ushort {0}Length = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
//count += sizeof(ushort);
//this.name = Encoding.Unicode.GetString(s.Slice(count, nameLength));
//count += {0}Length;
//";

//        // {0} 변수 이름
//        // {1} 변수 형식
//        public static string writeFormat =
//@"success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.{0});
//count += sizeof({1});
//";

//        // {0} 변수 이름
//        public static string writeStringFormat =
//@"ushort {0}Length = (ushort)Encoding.Unicode.GetBytes(this.{0}, 0, this.{0}.Length, segment.Array, segment.Offset + count + sizeof(ushort));
//success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), {0}Length);
//count += sizeof(ushort);
//count += {0}Length;
//";
//    }
//}
