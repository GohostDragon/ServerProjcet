using System;
using System.IO;
using System.Xml;

// <패킷 제네레이터 6#> 22.03.04 - 클라 / 서버 패킷 레지스터 분리
namespace PacketGenerator
{
    class Program
    {
        static string genPackets;
        static ushort packetId;
        static string packetEnums;

        static string clientRegister;
        static string serverRegister;
		static string dbRegister;

		static void Main(string[] args)
        {
            string pdlPath = "../PDL.xml";

            XmlReaderSettings settings = new XmlReaderSettings() {
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

            if (args.Length >= 1) {
                pdlPath = args[0];
            }

            using (XmlReader reader = XmlReader.Create(pdlPath, settings)) {
                reader.MoveToContent();

                while (reader.Read()) {
                    if (reader.Depth == 1 && reader.NodeType == XmlNodeType.Element) {
                        ParsePacket(reader);
                    }
                }

                string fileText = string.Format(PacketFormat.fileFormat, packetEnums, genPackets);
                File.WriteAllText("GenPackets.cs", fileText);

                string clientManagerText = string.Format(PacketFormat.managerFormat, clientRegister);
                File.WriteAllText("ClientPacketManager.cs", clientManagerText);

                string serverManagerText = string.Format(PacketFormat.managerFormat, serverRegister);
                File.WriteAllText("ServerPacketManager.cs", serverManagerText);

				string dbManagerText = string.Format(PacketFormat.managerFormat, dbRegister);
				File.WriteAllText("DBPacketManager.cs", dbManagerText);
			}
        }

        public static void ParsePacket(XmlReader r)
        {
            if (r.NodeType == XmlNodeType.EndElement) {
                return;
            }

            if (r.Name.ToLower() != "packet") {
                Console.WriteLine("Invalid packet node");
                return;
            }

            string packetName = r["name"];
            if (string.IsNullOrEmpty(packetName)) {
                Console.WriteLine("Packet Without Name");
                return;
            }

            Tuple<string, string, string> tuple = ParseMembers(r);
            genPackets += string.Format(PacketFormat.packetFormat, packetName, tuple.Item1, tuple.Item2, tuple.Item3);
            packetEnums += string.Format(PacketFormat.packetEnumFormat, packetName, ++packetId) + Environment.NewLine + "\t";
            
            if(packetName.StartsWith("SC_") || packetName.StartsWith("SC_")) {
                clientRegister += string.Format(PacketFormat.managerRegisterFormat, packetName) + Environment.NewLine;
            } else if (packetName.StartsWith("CS_") || packetName.StartsWith("DS_")) {
                serverRegister += string.Format(PacketFormat.managerRegisterFormat, packetName) + Environment.NewLine;
		    } else if (packetName.StartsWith("SD_") || packetName.StartsWith("SD_")) {
				dbRegister += string.Format(PacketFormat.managerRegisterFormat, packetName) + Environment.NewLine;
            }

        }

        // {1} 멤버 변수 이름
        // {2} 멤버 변수 Read
        // {3} 멤버 변수 Write
        public static Tuple<string, string, string> ParseMembers(XmlReader r)
        {
            string packetName = r["name"];

            string memberCode = "";
            string readCode = "";
            string writeCode = "";

            int memberDepth = r.Depth + 1;
            while (r.Read()) {
                if (r.Depth != memberDepth) {
                    break;
                }

                string memberName = r["name"];
                if (string.IsNullOrEmpty(memberName)) {
                    Console.WriteLine("Member without name");
                    return null;
                }

                if (string.IsNullOrEmpty(memberCode) == false) {
                    memberCode += Environment.NewLine;
                }
                if (string.IsNullOrEmpty(readCode) == false) {
                    readCode += Environment.NewLine;
                }
                if (string.IsNullOrEmpty(writeCode) == false) {
                    writeCode += Environment.NewLine;
                }

                string memberType = r.Name.ToLower();
                switch (memberType) {
                    case "byte":
                    case "sbyte":
                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
                        readCode += string.Format(PacketFormat.readByteFormat, memberName, memberType);
                        writeCode += string.Format(PacketFormat.writeFormat, memberName, memberType);
                        break;
                    case "bool":
                    case "short":
                    case "ushort":
                    case "int":
                    case "long":
                    case "float":
                    case "double":
                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
                        readCode += string.Format(PacketFormat.readFormat, memberName, ToMemberType(memberType), memberType);
                        writeCode += string.Format(PacketFormat.writeFormat, memberName, memberType);
                        break;
                    case "string":
                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
                        readCode += string.Format(PacketFormat.readStringFormat, memberName);
                        writeCode += string.Format(PacketFormat.writeStringFormat, memberName);
                        break;
                    case "list":
                        Tuple<string, string, string> tuple = ParseList(r);
                        memberCode += tuple.Item1;
                        readCode += tuple.Item2;
                        writeCode += tuple.Item3;
                        break;
                    default:
                        break;
                }
            }

            memberCode = memberCode.Replace("\n", "\n\t");
            readCode = readCode.Replace("\n", "\n\t\t");
            writeCode = writeCode.Replace("\n", "\n\t\t");

            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
        }

        public static Tuple<string, string, string> ParseList(XmlReader r)
        {
            string listName = r["name"];
            if (string.IsNullOrEmpty(listName)) {
                Console.WriteLine("List without name");
                return null;
            }

            Tuple<string, string, string> tuple = ParseMembers(r);

            string memberCode = string.Format(PacketFormat.memberListFormat,
                FirstCharToUpper(listName),
                FirstCharToLower(listName),
                tuple.Item1,
                tuple.Item2,
                tuple.Item3);

            string readCode = string.Format(PacketFormat.readListFormat,
                FirstCharToUpper(listName),
                FirstCharToLower(listName));

            string writeCode = string.Format(PacketFormat.writeListFormat,
                            FirstCharToUpper(listName),
                            FirstCharToLower(listName));

            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
        }

        public static string ToMemberType(string memberType)
        {
            switch (memberType) {
                case "bool":
                    return "ToBoolean";
                // case "byte": // 따로 정의되어있지는 않음
                case "short":
                    return "ToInt16";
                case "ushort":
                    return "ToUInt16";
                case "int":
                    return "ToInt32";
                case "long":
                    return "ToInt64";
                case "float":
                    return "ToSingle";
                case "double":
                    return "ToDouble";
                default:
                    return string.Empty;
            }
        }

        public static string FirstCharToUpper(string input)
        {
            if (string.IsNullOrEmpty(input)) {
                return string.Empty;
            }

            return input[0].ToString().ToUpper() + input.Substring(1);
        }

        public static string FirstCharToLower(string input)
        {
            if (string.IsNullOrEmpty(input)) {
                return string.Empty;
            }

            return input[0].ToString().ToLower() + input.Substring(1);
        }
    }
}




//// <패킷 제네레이터 5#> 22.03.04 - 패킷 매니저 자동화
//namespace PacketGenerator
//{
//    class Program
//    {
//        static string genPackets;
//        static ushort packetId;
//        static string packetEnums;

//        // 1.
//        static string managerRegister;

//        static void Main(string[] args)
//        {
//            string pdlPath = "../PDL.xml";

//            XmlReaderSettings settings = new XmlReaderSettings() {
//                IgnoreComments = true,
//                IgnoreWhitespace = true
//            };

//            if (args.Length >= 1) {
//                pdlPath = args[0];
//            }

//            using (XmlReader reader = XmlReader.Create(pdlPath, settings)) {
//                reader.MoveToContent();

//                while (reader.Read()) {
//                    if (reader.Depth == 1 && reader.NodeType == XmlNodeType.Element) {
//                        ParsePacket(reader);
//                    }
//                }

//                string fileText = string.Format(PacketFormat.fileFormat, packetEnums, genPackets);
//                File.WriteAllText("GenPackets.cs", fileText);

//                // 2.
//                string managerText = string.Format(PacketFormat.managerFormat, managerRegister);
//                File.WriteAllText("PacketManager.cs", managerText);
//            }
//        }

//        public static void ParsePacket(XmlReader r)
//        {
//            if (r.NodeType == XmlNodeType.EndElement) {
//                return;
//            }

//            if (r.Name.ToLower() != "packet") {
//                Console.WriteLine("Invalid packet node");
//                return;
//            }

//            string packetName = r["name"];
//            if (string.IsNullOrEmpty(packetName)) {
//                Console.WriteLine("Packet Without Name");
//                return;
//            }

//            Tuple<string, string, string> tuple = ParseMembers(r);
//            genPackets += string.Format(PacketFormat.packetFormat, packetName, tuple.Item1, tuple.Item2, tuple.Item3);
//            packetEnums += string.Format(PacketFormat.packetEnumFormat, packetName, ++packetId) + Environment.NewLine + "\t";
//            // 3.
//            managerRegister += string.Format(PacketFormat.managerRegisterFormat, packetName) + Environment.NewLine;
//        }

//        // {1} 멤버 변수 이름
//        // {2} 멤버 변수 Read
//        // {3} 멤버 변수 Write
//        public static Tuple<string, string, string> ParseMembers(XmlReader r)
//        {
//            string packetName = r["name"];

//            string memberCode = "";
//            string readCode = "";
//            string writeCode = "";

//            int memberDepth = r.Depth + 1;
//            while (r.Read()) {
//                if (r.Depth != memberDepth) {
//                    break;
//                }

//                string memberName = r["name"];
//                if (string.IsNullOrEmpty(memberName)) {
//                    Console.WriteLine("Member without name");
//                    return null;
//                }

//                if (string.IsNullOrEmpty(memberCode) == false) {
//                    memberCode += Environment.NewLine;
//                }
//                if (string.IsNullOrEmpty(readCode) == false) {
//                    readCode += Environment.NewLine;
//                }
//                if (string.IsNullOrEmpty(writeCode) == false) {
//                    writeCode += Environment.NewLine;
//                }

//                string memberType = r.Name.ToLower();
//                switch (memberType) {
//                    case "byte":
//                    case "sbyte":
//                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
//                        readCode += string.Format(PacketFormat.readByteFormat, memberName, memberType);
//                        writeCode += string.Format(PacketFormat.writeFormat, memberName, memberType);
//                        break;
//                    case "bool":
//                    case "short":
//                    case "ushort":
//                    case "int":
//                    case "long":
//                    case "float":
//                    case "double":
//                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
//                        readCode += string.Format(PacketFormat.readFormat, memberName, ToMemberType(memberType), memberType);
//                        writeCode += string.Format(PacketFormat.writeFormat, memberName, memberType);
//                        break;
//                    case "string":
//                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
//                        readCode += string.Format(PacketFormat.readStringFormat, memberName);
//                        writeCode += string.Format(PacketFormat.writeStringFormat, memberName);
//                        break;
//                    case "list":
//                        Tuple<string, string, string> tuple = ParseList(r);
//                        memberCode += tuple.Item1;
//                        readCode += tuple.Item2;
//                        writeCode += tuple.Item3;
//                        break;
//                    default:
//                        break;
//                }
//            }

//            memberCode = memberCode.Replace("\n", "\n\t");
//            readCode = readCode.Replace("\n", "\n\t\t");
//            writeCode = writeCode.Replace("\n", "\n\t\t");

//            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
//        }

//        public static Tuple<string, string, string> ParseList(XmlReader r)
//        {
//            string listName = r["name"];
//            if (string.IsNullOrEmpty(listName)) {
//                Console.WriteLine("List without name");
//                return null;
//            }

//            Tuple<string, string, string> tuple = ParseMembers(r);

//            string memberCode = string.Format(PacketFormat.memberListFormat,
//                FirstCharToUpper(listName),
//                FirstCharToLower(listName),
//                tuple.Item1,
//                tuple.Item2,
//                tuple.Item3);

//            string readCode = string.Format(PacketFormat.readListFormat,
//                FirstCharToUpper(listName),
//                FirstCharToLower(listName));

//            string writeCode = string.Format(PacketFormat.writeListFormat,
//                            FirstCharToUpper(listName),
//                            FirstCharToLower(listName));

//            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
//        }

//        public static string ToMemberType(string memberType)
//        {
//            switch (memberType) {
//                case "bool":
//                    return "ToBoolean";
//                // case "byte": // 따로 정의되어있지는 않음
//                case "short":
//                    return "ToInt16";
//                case "ushort":
//                    return "ToUInt16";
//                case "int":
//                    return "ToInt32";
//                case "long":
//                    return "ToInt64";
//                case "float":
//                    return "ToSingle";
//                case "double":
//                    return "ToDouble";
//                default:
//                    return string.Empty;
//            }
//        }

//        public static string FirstCharToUpper(string input)
//        {
//            if (string.IsNullOrEmpty(input)) {
//                return string.Empty;
//            }

//            return input[0].ToString().ToUpper() + input.Substring(1);
//        }

//        public static string FirstCharToLower(string input)
//        {
//            if (string.IsNullOrEmpty(input)) {
//                return string.Empty;
//            }

//            return input[0].ToString().ToLower() + input.Substring(1);
//        }
//    }
//}



//// <패킷 제네레이터 4#> 22.03.01 - 배치파일 만들어서 PDL 수정 시 양쪽의 GenPacket이 자동으로 고쳐주는 작업
//// < 1. 배치파일 추가하기>
//// - 프로젝트 경로에 커먼 / 패킷 폴더 추가
//// - 해당 패킷 폴더에 배치 파일 추가 -> 새 text파일 추가 후 확장자 명 bat으로 변경
//// - 배치 파일이란 -> 윈도우에서 명령어를 사용해 한번에 어떤 처리를 할 수 있는 것
//// - 배치 파일의 exe 다음의 인자는 Program의 Main 함수의 arg로 들어가게 됨 !!
//// <2. 배치파일에 XCOPY 추가>
//// - 파일을 이동 시킬 수 있음, 이동 시 /Y 옵션으로 덮어쓰기 가능

//namespace PacketGenerator
//{
//    class Program
//    {
//        static string genPackets;
//        static ushort packetId;
//        static string packetEnums;

//        static string managerRegister;

//        static void Main(string[] args)
//        {
//            string pdlPath = "../PDL.xml";

//            XmlReaderSettings settings = new XmlReaderSettings() {
//                IgnoreComments = true,
//                IgnoreWhitespace = true
//            };

//            if (args.Length >= 1) {
//                pdlPath = args[0];
//            }

//            using (XmlReader reader = XmlReader.Create(pdlPath, settings)) {
//                reader.MoveToContent();

//                while (reader.Read()) {
//                    if (reader.Depth == 1 && reader.NodeType == XmlNodeType.Element) {
//                        ParsePacket(reader);
//                    }
//                }

//                string fileText = string.Format(PacketFormat.fileFormat, packetEnums, genPackets);
//                File.WriteAllText("GenPackets.cs", fileText);
//                string managerText = string.Format(PacketFormat.managerFormat, managerRegister);
//                File.WriteAllText("PacketManager.cs", managerText);
//            }
//        }

//        public static void ParsePacket(XmlReader r)
//        {
//            if (r.NodeType == XmlNodeType.EndElement) {
//                return;
//            }

//            if (r.Name.ToLower() != "packet") {
//                Console.WriteLine("Invalid packet node");
//                return;
//            }

//            string packetName = r["name"];
//            if (string.IsNullOrEmpty(packetName)) {
//                Console.WriteLine("Packet Without Name");
//                return;
//            }

//            Tuple<string, string, string> tuple = ParseMembers(r);
//            genPackets += string.Format(PacketFormat.packetFormat, packetName, tuple.Item1, tuple.Item2, tuple.Item3);
//            packetEnums += string.Format(PacketFormat.packetEnumFormat, packetName, ++packetId) + Environment.NewLine + "\t";
//        }

//        // {1} 멤버 변수 이름
//        // {2} 멤버 변수 Read
//        // {3} 멤버 변수 Write
//        public static Tuple<string, string, string> ParseMembers(XmlReader r)
//        {
//            string packetName = r["name"];

//            string memberCode = "";
//            string readCode = "";
//            string writeCode = "";

//            int memberDepth = r.Depth + 1;
//            while (r.Read()) {
//                if (r.Depth != memberDepth) {
//                    break;
//                }

//                string memberName = r["name"];
//                if (string.IsNullOrEmpty(memberName)) {
//                    Console.WriteLine("Member without name");
//                    return null;
//                }

//                if (string.IsNullOrEmpty(memberCode) == false) {
//                    memberCode += Environment.NewLine;
//                }
//                if (string.IsNullOrEmpty(readCode) == false) {
//                    readCode += Environment.NewLine;
//                }
//                if (string.IsNullOrEmpty(writeCode) == false) {
//                    writeCode += Environment.NewLine;
//                }

//                string memberType = r.Name.ToLower();
//                switch (memberType) {
//                    case "byte":
//                    case "sbyte":
//                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
//                        readCode += string.Format(PacketFormat.readByteFormat, memberName, memberType);
//                        writeCode += string.Format(PacketFormat.writeFormat, memberName, memberType);
//                        break;
//                    case "bool":
//                    case "short":
//                    case "ushort":
//                    case "int":
//                    case "long":
//                    case "float":
//                    case "double":
//                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
//                        readCode += string.Format(PacketFormat.readFormat, memberName, ToMemberType(memberType), memberType);
//                        writeCode += string.Format(PacketFormat.writeFormat, memberName, memberType);
//                        break;
//                    case "string":
//                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
//                        readCode += string.Format(PacketFormat.readStringFormat, memberName);
//                        writeCode += string.Format(PacketFormat.writeStringFormat, memberName);
//                        break;
//                    case "list":
//                        Tuple<string, string, string> tuple = ParseList(r);
//                        memberCode += tuple.Item1;
//                        readCode += tuple.Item2;
//                        writeCode += tuple.Item3;
//                        break;
//                    default:
//                        break;
//                }
//            }

//            memberCode = memberCode.Replace("\n", "\n\t");
//            readCode = readCode.Replace("\n", "\n\t\t");
//            writeCode = writeCode.Replace("\n", "\n\t\t");

//            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
//        }

//        public static Tuple<string, string, string> ParseList(XmlReader r)
//        {
//            string listName = r["name"];
//            if (string.IsNullOrEmpty(listName)) {
//                Console.WriteLine("List without name");
//                return null;
//            }

//            Tuple<string, string, string> tuple = ParseMembers(r);

//            string memberCode = string.Format(PacketFormat.memberListFormat,
//                FirstCharToUpper(listName),
//                FirstCharToLower(listName),
//                tuple.Item1,
//                tuple.Item2,
//                tuple.Item3);

//            string readCode = string.Format(PacketFormat.readListFormat,
//                FirstCharToUpper(listName),
//                FirstCharToLower(listName));

//            string writeCode = string.Format(PacketFormat.writeListFormat,
//                            FirstCharToUpper(listName),
//                            FirstCharToLower(listName));

//            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
//        }

//        public static string ToMemberType(string memberType)
//        {
//            switch (memberType) {
//                case "bool":
//                    return "ToBoolean";
//                // case "byte": // 따로 정의되어있지는 않음
//                case "short":
//                    return "ToInt16";
//                case "ushort":
//                    return "ToUInt16";
//                case "int":
//                    return "ToInt32";
//                case "long":
//                    return "ToInt64";
//                case "float":
//                    return "ToSingle";
//                case "double":
//                    return "ToDouble";
//                default:
//                    return string.Empty;
//            }
//        }

//        public static string FirstCharToUpper(string input)
//        {
//            if (string.IsNullOrEmpty(input)) {
//                return string.Empty;
//            }

//            return input[0].ToString().ToUpper() + input.Substring(1);
//        }

//        public static string FirstCharToLower(string input)
//        {
//            if (string.IsNullOrEmpty(input)) {
//                return string.Empty;
//            }

//            return input[0].ToString().ToLower() + input.Substring(1);
//        }
//    }
//}




//// <패킷 제네레이터 4#> 22.02.28 - PLD 경로를 인자를 받아 처리할 수 있도록
//namespace PacketGenerator
//{
//    class Program
//    {
//        static string genPackets;
//        static ushort packetId;
//        static string packetEnums;

//        static void Main(string[] args)
//        {
//            // 1. 
//            // 3. 2번에서 bin에 빌드파일 생성되도록 변경했으므로 pld 참조 경로도 변경
//            //string pdlPath = "../PDL.xml";
//            string pdlPath = "../PDL.xml";

//            XmlReaderSettings settings = new XmlReaderSettings() {
//                IgnoreComments = true,
//                IgnoreWhitespace = true
//            };

//            // 2. 프로그램이 실행될 때 뭔가 인자로 받은 경우
//            // - 파일이 bin 산하로 생성되도록 설정 변경 (4:55)
//            // - 프로젝트 -> 속성 -> 빌드 -> 모든 구성 설정 후 출력 경로 변경
//            // - 프로젝트 파일을 Notepade에서 열어서 AppendTargetFrameworkToOutputPath 키워드 false 추가
//            if (args.Length >= 1) {
//                pdlPath = args[0];
//            }

//            using (XmlReader reader = XmlReader.Create(pdlPath, settings)) {
//                reader.MoveToContent();

//                while (reader.Read()) {
//                    if (reader.Depth == 1 && reader.NodeType == XmlNodeType.Element) {
//                        ParsePacket(reader);
//                    }
//                }

//                string fileText = string.Format(PacketFormat.fileFormat, packetEnums, genPackets);
//                File.WriteAllText("GenPackets.cs", fileText);
//            }
//        }

//        public static void ParsePacket(XmlReader r)
//        {
//            if (r.NodeType == XmlNodeType.EndElement) {
//                return;
//            }

//            if (r.Name.ToLower() != "packet") {
//                Console.WriteLine("Invalid packet node");
//                return;
//            }

//            string packetName = r["name"];
//            if (string.IsNullOrEmpty(packetName)) {
//                Console.WriteLine("Packet Without Name");
//                return;
//            }

//            Tuple<string, string, string> tuple = ParseMembers(r);
//            genPackets += string.Format(PacketFormat.packetFormat, packetName, tuple.Item1, tuple.Item2, tuple.Item3);
//            packetEnums += string.Format(PacketFormat.packetEnumFormat, packetName, ++packetId) + Environment.NewLine + "\t";
//        }

//        // {1} 멤버 변수 이름
//        // {2} 멤버 변수 Read
//        // {3} 멤버 변수 Write
//        public static Tuple<string, string, string> ParseMembers(XmlReader r)
//        {
//            string packetName = r["name"];

//            string memberCode = "";
//            string readCode = "";
//            string writeCode = "";

//            int memberDepth = r.Depth + 1;
//            while (r.Read()) {
//                if (r.Depth != memberDepth) {
//                    break;
//                }

//                string memberName = r["name"];
//                if (string.IsNullOrEmpty(memberName)) {
//                    Console.WriteLine("Member without name");
//                    return null;
//                }

//                if (string.IsNullOrEmpty(memberCode) == false) {
//                    memberCode += Environment.NewLine;
//                }
//                if (string.IsNullOrEmpty(readCode) == false) {
//                    readCode += Environment.NewLine;
//                }
//                if (string.IsNullOrEmpty(writeCode) == false) {
//                    writeCode += Environment.NewLine;
//                }

//                string memberType = r.Name.ToLower();
//                switch (memberType) {
//                    case "byte":
//                    case "sbyte":
//                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
//                        readCode += string.Format(PacketFormat.readByteFormat, memberName, memberType);
//                        writeCode += string.Format(PacketFormat.writeFormat, memberName, memberType);
//                        break;
//                    case "bool":
//                    case "short":
//                    case "ushort":
//                    case "int":
//                    case "long":
//                    case "float":
//                    case "double":
//                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
//                        readCode += string.Format(PacketFormat.readFormat, memberName, ToMemberType(memberType), memberType);
//                        writeCode += string.Format(PacketFormat.writeFormat, memberName, memberType);
//                        break;
//                    case "string":
//                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
//                        readCode += string.Format(PacketFormat.readStringFormat, memberName);
//                        writeCode += string.Format(PacketFormat.writeStringFormat, memberName);
//                        break;
//                    case "list":
//                        Tuple<string, string, string> tuple = ParseList(r);
//                        memberCode += tuple.Item1;
//                        readCode += tuple.Item2;
//                        writeCode += tuple.Item3;
//                        break;
//                    default:
//                        break;
//                }
//            }

//            memberCode = memberCode.Replace("\n", "\n\t");
//            readCode = readCode.Replace("\n", "\n\t\t");
//            writeCode = writeCode.Replace("\n", "\n\t\t");

//            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
//        }

//        public static Tuple<string, string, string> ParseList(XmlReader r)
//        {
//            string listName = r["name"];
//            if (string.IsNullOrEmpty(listName)) {
//                Console.WriteLine("List without name");
//                return null;
//            }

//            Tuple<string, string, string> tuple = ParseMembers(r);

//            string memberCode = string.Format(PacketFormat.memberListFormat,
//                FirstCharToUpper(listName),
//                FirstCharToLower(listName),
//                tuple.Item1,
//                tuple.Item2,
//                tuple.Item3);

//            string readCode = string.Format(PacketFormat.readListFormat,
//                FirstCharToUpper(listName),
//                FirstCharToLower(listName));

//            string writeCode = string.Format(PacketFormat.writeListFormat,
//                            FirstCharToUpper(listName),
//                            FirstCharToLower(listName));

//            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
//        }

//        public static string ToMemberType(string memberType)
//        {
//            switch (memberType) {
//                case "bool":
//                    return "ToBoolean";
//                // case "byte": // 따로 정의되어있지는 않음
//                case "short":
//                    return "ToInt16";
//                case "ushort":
//                    return "ToUInt16";
//                case "int":
//                    return "ToInt32";
//                case "long":
//                    return "ToInt64";
//                case "float":
//                    return "ToSingle";
//                case "double":
//                    return "ToDouble";
//                default:
//                    return string.Empty;
//            }
//        }

//        public static string FirstCharToUpper(string input)
//        {
//            if (string.IsNullOrEmpty(input)) {
//                return string.Empty;
//            }

//            return input[0].ToString().ToUpper() + input.Substring(1);
//        }

//        public static string FirstCharToLower(string input)
//        {
//            if (string.IsNullOrEmpty(input)) {
//                return string.Empty;
//            }

//            return input[0].ToString().ToLower() + input.Substring(1);
//        }
//    }
//}




//// <패킷 제네레이터 3#> 22.02.28 - Enum / Using 등 자동화 보강
//namespace PacketGenerator
//{
//    class Program
//    {
//        static string genPackets;
//        // 3. packetEnums를 만들기 위해서는 패킷을 몇개를 처리했는지 기억하고 있을 packetId가 있어야 함
//        static ushort packetId;
//        static string packetEnums;

//        static void Main(string[] args)
//        {
//            XmlReaderSettings settings = new XmlReaderSettings() {
//                IgnoreComments = true,
//                IgnoreWhitespace = true
//            };

//            using (XmlReader reader = XmlReader.Create("PDL.xml", settings)) {
//                reader.MoveToContent();

//                while (reader.Read()) {
//                    if (reader.Depth == 1 && reader.NodeType == XmlNodeType.Element) {
//                        ParsePacket(reader);
//                    }
//                }

//                // 1.파일 포맷 추가
//                // 2. 2번 인자에 해당하는 패킷 목록은 genPackets와 같음
//                string fileText = string.Format(PacketFormat.fileFormat, packetEnums, genPackets);
//                File.WriteAllText("GenPackets.cs", fileText);
//            }
//        }

//        public static void ParsePacket(XmlReader r)
//        {
//            if (r.NodeType == XmlNodeType.EndElement) {
//                return;
//            }

//            if (r.Name.ToLower() != "packet") {
//                Console.WriteLine("Invalid packet node");
//                return;
//            }

//            string packetName = r["name"];
//            if (string.IsNullOrEmpty(packetName)) {
//                Console.WriteLine("Packet Without Name");
//                return;
//            }

//            Tuple<string, string, string> tuple = ParseMembers(r);
//            genPackets += string.Format(PacketFormat.packetFormat, packetName, tuple.Item1, tuple.Item2, tuple.Item3);
//            // 4. 패킷 하나를 파싱할 때마다 늘림
//            // 5. 패킷 Id의 경우는 정책에 따라 달라짐 ->
//            packetEnums += string.Format(PacketFormat.packetEnumFormat, packetName, ++packetId) + Environment.NewLine + "\t";
//        }

//        // {1} 멤버 변수 이름
//        // {2} 멤버 변수 Read
//        // {3} 멤버 변수 Write
//        public static Tuple<string, string, string> ParseMembers(XmlReader r)
//        {
//            string packetName = r["name"];

//            string memberCode = "";
//            string readCode = "";
//            string writeCode = "";

//            int memberDepth = r.Depth + 1;
//            while (r.Read()) {
//                if (r.Depth != memberDepth) {
//                    break;
//                }

//                string memberName = r["name"];
//                if (string.IsNullOrEmpty(memberName)) {
//                    Console.WriteLine("Member without name");
//                    return null;
//                }

//                if (string.IsNullOrEmpty(memberCode) == false) {
//                    memberCode += Environment.NewLine;
//                }
//                if (string.IsNullOrEmpty(readCode) == false) {
//                    readCode += Environment.NewLine;
//                }
//                if (string.IsNullOrEmpty(writeCode) == false) {
//                    writeCode += Environment.NewLine;
//                }

//                // 6. 바이트 처리 추가
//                string memberType = r.Name.ToLower();
//                switch (memberType) {
//                    case "byte":
//                    case "sbyte":
//                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
//                        readCode += string.Format(PacketFormat.readByteFormat, memberName, memberType);
//                        writeCode += string.Format(PacketFormat.writeFormat, memberName, memberType);
//                        break;
//                    case "bool":
//                    case "short":
//                    case "ushort":
//                    case "int":
//                    case "long":
//                    case "float":
//                    case "double":
//                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
//                        readCode += string.Format(PacketFormat.readFormat, memberName, ToMemberType(memberType), memberType);
//                        writeCode += string.Format(PacketFormat.writeFormat, memberName, memberType);
//                        break;
//                    case "string":
//                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
//                        readCode += string.Format(PacketFormat.readStringFormat, memberName);
//                        writeCode += string.Format(PacketFormat.writeStringFormat, memberName);
//                        break;
//                    case "list":
//                        Tuple<string, string, string> tuple = ParseList(r);
//                        memberCode += tuple.Item1;
//                        readCode += tuple.Item2;
//                        writeCode += tuple.Item3;
//                        break;
//                    default:
//                        break;
//                }
//            }

//            memberCode = memberCode.Replace("\n", "\n\t");
//            readCode = readCode.Replace("\n", "\n\t\t");
//            writeCode = writeCode.Replace("\n", "\n\t\t");

//            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
//        }

//        public static Tuple<string, string, string> ParseList(XmlReader r)
//        {
//            string listName = r["name"];
//            if (string.IsNullOrEmpty(listName)) {
//                Console.WriteLine("List without name");
//                return null;
//            }

//            Tuple<string, string, string> tuple = ParseMembers(r);

//            string memberCode = string.Format(PacketFormat.memberListFormat,
//                FirstCharToUpper(listName),
//                FirstCharToLower(listName),
//                tuple.Item1,
//                tuple.Item2,
//                tuple.Item3);

//            string readCode = string.Format(PacketFormat.readListFormat,
//                FirstCharToUpper(listName),
//                FirstCharToLower(listName));

//            string writeCode = string.Format(PacketFormat.writeListFormat,
//                            FirstCharToUpper(listName),
//                            FirstCharToLower(listName));

//            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
//        }

//        public static string ToMemberType(string memberType)
//        {
//            switch (memberType) {
//                case "bool":
//                    return "ToBoolean";
//                // case "byte": // 따로 정의되어있지는 않음
//                case "short":
//                    return "ToInt16";
//                case "ushort":
//                    return "ToUInt16";
//                case "int":
//                    return "ToInt32";
//                case "long":
//                    return "ToInt64";
//                case "float":
//                    return "ToSingle";
//                case "double":
//                    return "ToDouble";
//                default:
//                    return string.Empty;
//            }
//        }

//        public static string FirstCharToUpper(string input)
//        {
//            if (string.IsNullOrEmpty(input)) {
//                return string.Empty;
//            }

//            return input[0].ToString().ToUpper() + input.Substring(1);
//        }

//        public static string FirstCharToLower(string input)
//        {
//            if (string.IsNullOrEmpty(input)) {
//                return string.Empty;
//            }

//            return input[0].ToString().ToLower() + input.Substring(1);
//        }
//    }
//}



//// <패킷 제네레이터 2#> 22.02.28 - List 자동화
//namespace PacketGenerator
//{
//    class Program
//    {
//        static string genPackets;

//        static void Main(string[] args)
//        {
//            XmlReaderSettings settings = new XmlReaderSettings() {
//                IgnoreComments = true,
//                IgnoreWhitespace = true
//            };

//            using (XmlReader reader = XmlReader.Create("PDL.xml", settings)) {
//                reader.MoveToContent();

//                while (reader.Read()) {
//                    if (reader.Depth == 1 && reader.NodeType == XmlNodeType.Element) {
//                        ParsePacket(reader);
//                    }
//                }

//                File.WriteAllText("GenPackets.cs", genPackets);
//            }
//        }

//        public static void ParsePacket(XmlReader r)
//        {
//            if (r.NodeType == XmlNodeType.EndElement) {
//                return;
//            }

//            if (r.Name.ToLower() != "packet") {
//                Console.WriteLine("Invalid packet node");
//                return;
//            }

//            string packetName = r["name"];
//            if (string.IsNullOrEmpty(packetName)) {
//                Console.WriteLine("Packet Without Name");
//                return;
//            }

//            Tuple<string, string, string> tuple = ParseMembers(r);
//            genPackets += string.Format(PacketFormat.packetFormat, packetName, tuple.Item1, tuple.Item2, tuple.Item3);
//        }

//        // {1} 멤버 변수 이름
//        // {2} 멤버 변수 Read
//        // {3} 멤버 변수 Write
//        public static Tuple<string, string, string> ParseMembers(XmlReader r)
//        {
//            string packetName = r["name"];

//            string memberCode = "";
//            string readCode = "";
//            string writeCode = "";

//            int memberDepth = r.Depth + 1;
//            while (r.Read()) {
//                if (r.Depth != memberDepth) {
//                    break;
//                }

//                string memberName = r["name"];
//                if (string.IsNullOrEmpty(memberName)) {
//                    Console.WriteLine("Member without name");
//                    return null;
//                }

//                if (string.IsNullOrEmpty(memberCode) == false) {
//                    memberCode += Environment.NewLine;
//                }
//                if (string.IsNullOrEmpty(readCode) == false) {
//                    readCode += Environment.NewLine;
//                }
//                if (string.IsNullOrEmpty(writeCode) == false) {
//                    writeCode += Environment.NewLine;
//                }

//                string memberType = r.Name.ToLower();
//                switch (memberType) {
//                    case "bool":
//                    case "byte":
//                    case "short":
//                    case "ushort":
//                    case "int":
//                    case "long":
//                    case "float":
//                    case "double":
//                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
//                        readCode += string.Format(PacketFormat.readFormat, memberName, ToMemberType(memberType), memberType);
//                        writeCode += string.Format(PacketFormat.writeFormat, memberName, memberType);
//                        break;
//                    case "string":
//                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
//                        readCode += string.Format(PacketFormat.readStringFormat, memberName);
//                        writeCode += string.Format(PacketFormat.writeStringFormat, memberName);
//                        break;
//                        // 1. 
//                    case "list":
//                        Tuple<string, string, string> tuple = ParseList(r);
//                        memberCode += tuple.Item1;
//                        readCode += tuple.Item2;
//                        writeCode += tuple.Item3;
//                        break;
//                    default:
//                        break;
//                }
//            }

//            memberCode = memberCode.Replace("\n", "\n\t");
//            readCode = readCode.Replace("\n", "\n\t\t");
//            writeCode = writeCode.Replace("\n", "\n\t\t");

//            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
//        }

//        public static Tuple<string, string, string> ParseList(XmlReader r)
//        {
//            string listName = r["name"];
//            if (string.IsNullOrEmpty(listName)) {
//                Console.WriteLine("List without name");
//                return null;
//            }

//            Tuple<string, string, string> tuple = ParseMembers(r);

//            string memberCode = string.Format(PacketFormat.memberListFormat,
//                FirstCharToUpper(listName),
//                FirstCharToLower(listName),
//                tuple.Item1,
//                tuple.Item2,
//                tuple.Item3);

//            string readCode = string.Format(PacketFormat.readListFormat,
//                FirstCharToUpper(listName),
//                FirstCharToLower(listName));

//            string writeCode = string.Format(PacketFormat.writeListFormat,
//                            FirstCharToUpper(listName),
//                            FirstCharToLower(listName));

//            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
//        }

//        public static string ToMemberType(string memberType)
//        {
//            switch (memberType) {
//                case "bool":
//                    return "ToBoolean";
//                // case "byte": // 따로 정의되어있지는 않음
//                case "short":
//                    return "ToInt16";
//                case "ushort":
//                    return "ToUInt16";
//                case "int":
//                    return "ToInt32";
//                case "long":
//                    return "ToInt64";
//                case "float":
//                    return "ToSingle";
//                case "double":
//                    return "ToDouble";
//                default:
//                    return string.Empty;
//            }
//        }

//        public static string FirstCharToUpper(string input)
//        {
//            if (string.IsNullOrEmpty(input)) {
//                return string.Empty;
//            }

//            // 2. ToUpper
//            return input[0].ToString().ToUpper() + input.Substring(1);
//        }

//        public static string FirstCharToLower(string input)
//        {
//            if (string.IsNullOrEmpty(input)) {
//                return string.Empty;
//            }

//            return input[0].ToString().ToLower() + input.Substring(1);
//        }
//    }
//}



//// <패킷 제네레이터 2#> 22.02.28 - PacketFormat 적용 (자동화)
//namespace PacketGenerator
//{
//    class Program
//    {
//        // 2. 실시간으로 만들어지는 Packetstring을 만들어서 1번에 넣어줌
//        static string genPackets;

//        static void Main(string[] args)
//        {
//            XmlReaderSettings settings = new XmlReaderSettings() {
//                IgnoreComments = true,
//                IgnoreWhitespace = true
//            };

//            using (XmlReader reader = XmlReader.Create("PDL.xml", settings)) {
//                reader.MoveToContent();

//                while (reader.Read()) {
//                    if (reader.Depth == 1 && reader.NodeType == XmlNodeType.Element) {
//                        ParsePacket(reader);
//                    }
//                }

//                // 1. xml 파일 읽고 파일 추가
//                File.WriteAllText("GenPackets.cs", genPackets);
//            }
//        }

//        public static void ParsePacket(XmlReader r)
//        {
//            if (r.NodeType == XmlNodeType.EndElement) {
//                return;
//            }

//            if (r.Name.ToLower() != "packet") {
//                Console.WriteLine("Invalid packet node");
//                return;
//            }

//            string packetName = r["name"];
//            if (string.IsNullOrEmpty(packetName)) {
//                Console.WriteLine("Packet Without Name");
//                return;
//            }

//            Tuple<string,string,string> tuple = ParseMembers(r);
//            // 3.
//            genPackets += string.Format(PacketFormat.packetFormat, packetName, tuple.Item1, tuple.Item2, tuple.Item3);
//        }

//        // {1} 멤버 변수 이름
//        // {2} 멤버 변수 Read
//        // {3} 멤버 변수 Write
//        // 4. 튜플로 받아올 수 있도록 변경
//        public static Tuple<string, string, string> ParseMembers(XmlReader r)
//        {
//            string packetName = r["name"];

//            // 5.
//            string memberCode = "";
//            string readCode = "";
//            string writeCode = "";

//            int memberDepth = r.Depth + 1;
//            while (r.Read()) {
//                if (r.Depth != memberDepth) {
//                    break;
//                }

//                string memberName = r["name"];
//                if (string.IsNullOrEmpty(memberName)) {
//                    Console.WriteLine("Member without name");
//                    return null;
//                }

//                // 7. 줄바꿈 처리
//                if(string.IsNullOrEmpty(memberCode) == false) {
//                    memberCode += Environment.NewLine;
//                }
//                if (string.IsNullOrEmpty(readCode) == false) {
//                    readCode += Environment.NewLine;
//                }
//                if (string.IsNullOrEmpty(writeCode) == false) {
//                    writeCode += Environment.NewLine;
//                }

//                string memberType = r.Name.ToLower();
//                switch (memberType) {
//                    case "bool":
//                    case "byte":
//                    case "short":
//                    case "ushort":
//                    case "int":
//                    case "long":
//                    case "float":
//                    case "double":
//                        // 8.
//                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
//                        readCode += string.Format(PacketFormat.readFormat, memberName, ToMemberType(memberType), memberType);
//                        writeCode += string.Format(PacketFormat.writeFormat, memberName, memberType);
//                        break;
//                    case "string":
//                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
//                        readCode += string.Format(PacketFormat.readStringFormat, memberName);
//                        writeCode += string.Format(PacketFormat.writeStringFormat, memberName);
//                        break;
//                    case "list":
//                        break;
//                    default:
//                        break;
//                }
//            }

//            // 9. 코드 정렬한번 해주자 -> 줄바꿈 있는 애들은 탭해주자
//            memberCode = memberCode.Replace("\n", "\n\t");
//            readCode = readCode.Replace("\n", "\n\t\t");
//            writeCode = writeCode.Replace("\n", "\n\t\t");

//            // 6.
//            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
//        }

//        // 9. PacketFormat의 To 변수 부분 자동화 함수
//        public static string ToMemberType(string memberType)
//        {
//            switch (memberType) {
//                case "bool":
//                    return "ToBoolean";
//                // case "byte": // 따로 정의되어있지는 않음
//                case "short":
//                    return "ToInt16";
//                case "ushort":
//                    return "ToUInt16";
//                case "int":
//                    return "ToInt32";
//                case "long":
//                    return "ToInt64";
//                case "float":
//                    return "ToSingle";
//                case "double":
//                    return "ToDouble";
//                default:
//                    return string.Empty;
//            }
//        }
//    }
//}




// <패킷 제네레이터 1#> 22.02.27 - 패킷 XML 파싱 부분 추가
//namespace PacketGenerator
//{
//    class Program
//    {
//        static void Main(string[] args)
//        {
//            XmlReaderSettings settings = new XmlReaderSettings() {
//                IgnoreComments = true,
//                IgnoreWhitespace = true
//            };

//            using (XmlReader reader = XmlReader.Create("PDL.xml", settings)) {
//                reader.MoveToContent();

//                while (reader.Read()) {
//                    // 1. 패킷인지 아닌지 구별하는 부분 추가 - name 비교를 하기도 하지만
//                    // - depth라는 것을 쓰면 좀 더 쉽게 할 수 있음

//                    // 3. depth 체크 외에도 닫는 태그는 제외하기 위해 NotType 비교 추가 
//                    if(reader.Depth == 1 && reader.NodeType == XmlNodeType.Element) {
//                        ParsePacket(reader);
//                    }

//                    // Console.WriteLine(reader.Name + " " + reader["name"]);
//                }
//            }
//        }

//        // 2.
//        public static void ParsePacket(XmlReader r)
//        {
//            if(r.NodeType == XmlNodeType.EndElement) {
//                return;
//            }

//            // 4. 소문자 변환 후 패킷 여부 판단 추가
//            if(r.Name.ToLower() != "packet") {
//                Console.WriteLine("Invalid packet node");
//                return;
//            }

//            string packetName = r["name"];
//            if (string.IsNullOrEmpty(packetName)) {
//                Console.WriteLine("Packet Without Name");
//                return;
//            }

//            // 5. 패킷 내부까지 들어왔으니 더 까자
//            ParseMembers(r);
//        }

//        public static void ParseMembers(XmlReader r)
//        {
//            string packetName = r["name"];

//            int memberDepth = r.Depth + 1; 
//            // 6. 패킷 안에 있는 내용까지
//            while (r.Read()) {
//                if(r.Depth != memberDepth) {
//                    break;
//                }

//                string memberName = r["name"];
//                if (string.IsNullOrEmpty(memberName)) {
//                    Console.WriteLine("Member without name");
//                    return;
//                }

//                // 7. 타입 비교부분 추가
//                // 8. 다되면 PacketFormat 파일로 이동
//                string memberType = r.Name.ToLower();
//                switch (memberType) {
//                    case "bool":
//                    case "byte":
//                    case "short":
//                    case "ushort":
//                    case "int":
//                    case "long":
//                    case "float":
//                    case "double":
//                    case "string":
//                    case "list":
//                        break;
//                    default:
//                        break;
//                }
//            }
//        }
//    }
//}



// <패킷 제네레이터 1#> 22.02.27 - 패킷 xml 읽어오는 부분 추가
//namespace PacketGenerator
//{
//    class Program
//    {
//        static void Main(string[] args)
//        {
//            // 2. 1번의 Create에서 그냥 파일을 읽어올수도 있지만, settings를 추가로 전달 할 수 있음
//            XmlReaderSettings settings = new XmlReaderSettings()
//            {
//                // 주석과 스페이스바 무시
//                IgnoreComments = true,
//                IgnoreWhitespace = true
//            };

//            // 1. XML이 작성된 후에 패킷 파싱을 해주자
//            // - C++의 경우는 XML을 읽으려면 오픈 라이브러리를 사용해야하지만, C#은 XMLReader 제공
//                // XmlReader reader = XmlReader.Create("PDL.xml", settings);

//            // 4. Using 키워드 추가해서 알아서 닫아주도록 함
//            using(XmlReader reader = XmlReader.Create("PDL.xml", settings)) {
//                // 5. 헤더 같은 것들은 건너뛰자
//                reader.MoveToContent();

//                // 6. While 돌면서 Read해주자 (stream 방식으로 읽으므로 한줄 한줄)
//                while (reader.Read()) {
//                    Console.WriteLine(reader.Name + " " + reader["name"]);
//                }
//            }

//            // 3. 원래는 이렇게 열면 Dispose로 닫아줘야 하는데 1번 부분에 using 키워드 추가 시 알아서 닫아줌
//            //reader.Dispose();
//        }
//    }
//}
