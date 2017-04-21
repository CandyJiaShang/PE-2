using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PEFile
{
    // 64字节
    struct IMAGE_DOS_HEADER
    {
        public UInt16 e_magic; // 魔术数字
        public UInt16 e_cblp; // 文件最后页的字节数
        public UInt16 e_cp; // 文件页数
        public UInt16 e_crlc; // 重定义元素个数
        public UInt16 e_cparhdr; // 头部尺寸，以段落为单位
        public UInt16 e_minalloc; // 所需的最小附加段
        public UInt16 e_maxalloc; // 所需的最大附加段
        public UInt16 e_ss; // 初始的SS值（相对偏移量）
        public UInt16 e_sp; // 初始的SP值
        public UInt16 e_csum; // 校验和
        public UInt16 e_ip; // 初始的IP值
        public UInt16 e_cs; // 初始的CS值（相对偏移量）
        public UInt16 e_lfarlc; // 重分配表文件地址
        public UInt16 e_ovno; // 覆盖号
        public UInt16[] e_res; // 保留字，8字节
        public UInt16 e_oemid; // OEM标识符（相对e_oeminfo）
        public UInt16 e_oeminfo; // OEM信息
        public UInt16[] e_res2; // 保留字，20字节
        public UInt32 e_lfanew; // 新exe头部的文件地址

        public IMAGE_DOS_HEADER(byte[] buff)
        {
            e_magic = (UInt16)(((0xffff & buff[1]) << 8) + buff[0]);
            e_cblp = (UInt16)(((0xffff & buff[3]) << 8) + buff[2]);
            e_cp = (UInt16)(((0xffff & buff[5]) << 8) + buff[4]);
            e_crlc = (UInt16)(((0xffff & buff[7]) << 8) + buff[6]);
            e_cparhdr = (UInt16)(((0xffff & buff[9]) << 8) + buff[8]);
            e_minalloc = (UInt16)(((0xffff & buff[11]) << 8) + buff[10]);
            e_maxalloc = (UInt16)(((0xffff & buff[13]) << 8) + buff[12]);
            e_ss = (UInt16)(((0xffff & buff[15]) << 8) + buff[14]);
            e_sp = (UInt16)(((0xffff & buff[17]) << 8) + buff[16]);
            e_csum = (UInt16)(((0xffff & buff[19]) << 8) + buff[18]);
            e_ip = (UInt16)(((0xffff & buff[21]) << 8) + buff[20]);
            e_cs = (UInt16)(((0xffff & buff[23]) << 8) + buff[22]);
            e_lfarlc = (UInt16)(((0xffff & buff[25]) << 8) + buff[24]);
            e_ovno = (UInt16)(((0xffff & buff[27]) << 8) + buff[26]);
            e_res = new UInt16[4];
            for (int i = 0; i < e_res.Length; i++)
            {
                e_res[i] = (UInt16)(((0xffff & buff[29 + i * 2]) << 8) + buff[28 + i * 2]);
            }
            e_oemid = (UInt16)(((0xffff & buff[37]) << 8) + buff[36]);
            e_oeminfo = (UInt16)(((0xffff & buff[39]) << 8) + buff[38]);
            e_res2 = new UInt16[10];
            for (int i = 0; i < e_res2.Length; i++)
            {
                e_res2[i] = (UInt16)(((0xffff & buff[41 + i * 2]) << 8) + buff[40 + i * 2]);
            }

            e_lfanew = (0xffffffff & buff[60]) +
                       ((0xffffffff & buff[61]) << 8) +
                       ((0xffffffff & buff[62]) << 16) +
                       ((0xffffffff & buff[63]) << 24);
        }

        public int GetSize()
        {
            return 64;
        }
    }

    struct REAL_MODE_PROGRAM
    {
        byte[] b_program;

        public REAL_MODE_PROGRAM(byte[] program, int length)
        {
            b_program = null;

            if (program.Length >= length)
            {
                b_program = new byte[length];
                for (int i = 0; i < b_program.Length; i++)
                {
                    b_program[i] = program[i];
                }
            }
        }
    }

    // 248字节（32位）或者262字节（64位）
    struct IMAGE_IMPORT_DESCRIPTOR
    {
        public UInt32 Sinature; // PE标志
        public IMAGE_FILE_HEADER FileHeader;  // 20 bytes
        public Object OptionalHeader;  // 224 bytes
        private UInt16 Architecture;  // 0x20b is 64bit;

        public IMAGE_IMPORT_DESCRIPTOR(byte[] buff, PEFileInfo fileInfo)
        {
            Architecture = fileInfo.Architecture;
            Sinature = (0xffffffff & buff[0]) +
                       ((0xffffffff & buff[1]) << 8) +
                       ((0xffffffff & buff[2]) << 16) +
                       ((0xffffffff & buff[3]) << 24);
            FileHeader = new IMAGE_FILE_HEADER(buff, 4);
            if (Architecture != 0x20B)
            {
                IMAGE_OPTIONAL_HEADER op = new IMAGE_OPTIONAL_HEADER(buff, 24);
                fileInfo.ImageBase = op.ImageBase;
                OptionalHeader = op;
                
            }
            else
            {
                IMAGE_OPTIONAL_HEADER_X64 op = new IMAGE_OPTIONAL_HEADER_X64(buff, 24);
                fileInfo.ImageBase = op.ImageBase;
                OptionalHeader = op;
            }
        }
    }

    struct IMAGE_NT_HEADERS
    {

    }

    // 40字节
    struct IMAGE_SECTION_HEADER
    {
        public byte[] Name1;  // 段名称，8字节，通常以 '.' 开头，比如：.text
        public UInt32 Misc;  //联合，包含成员：M_PhysicalAddress， M_VirtualSize
        public UInt32 M_PhysicalAddress;
        public UInt32 M_VirtualSize;        //和M_PhysicalAddress构成联合体，两个成员共占4个字节
        public UInt32 VirtualAddress;    // 该块装载到内存中的RVA。
        public UInt32 SizeOfRawData;     // 该块在磁盘文件中所占的大小。
        public UInt32 PointerToRawData;     // 该块在磁盘文件中的偏移
        public UInt32 PointerToRelocations; // 这部分在EXE文件中无意义。在OBJ文件中，表示本块重定位信息的偏移量。在OBJ文件中如果不是零，则会指向一个IMAGE_RELOCATION的数据结构。
        public UInt32 PointerToLineNumbers; // 行号地址
        public UInt16 NumberOfRelocations;  // 由PointerToRelocations指向的重定位的数目。
        public UInt16 NumberOfLinenumbers;  // 由NumberOfRelocations指向的行号的数目
        public UInt32 Characteristics;      // 段属性

        public IMAGE_SECTION_HEADER(byte[] buff)
        {
            Name1 = new byte[8];
            Array.Copy(buff, Name1, 8);
            Misc = (0xffffffff & buff[8]) +
                   ((0xffffffff & buff[9]) << 8) +
                   ((0xffffffff & buff[10]) << 16) +
                   ((0xffffffff & buff[11]) << 24);
            M_PhysicalAddress = Misc;
            M_VirtualSize = Misc;
            VirtualAddress = (0xffffffff & buff[12]) +
                             ((0xffffffff & buff[13]) << 8) +
                             ((0xffffffff & buff[14]) << 16) +
                             ((0xffffffff & buff[15]) << 24);
            SizeOfRawData = (0xffffffff & buff[16]) +
                             ((0xffffffff & buff[17]) << 8) +
                             ((0xffffffff & buff[18]) << 16) +
                             ((0xffffffff & buff[19]) << 24);
            PointerToRawData = (0xffffffff & buff[20]) +
                             ((0xffffffff & buff[21]) << 8) +
                             ((0xffffffff & buff[22]) << 16) +
                             ((0xffffffff & buff[23]) << 24);
            PointerToRelocations = (0xffffffff & buff[24]) +
                             ((0xffffffff & buff[25]) << 8) +
                             ((0xffffffff & buff[26]) << 16) +
                             ((0xffffffff & buff[27]) << 24);
            PointerToLineNumbers = (0xffffffff & buff[28]) +
                             ((0xffffffff & buff[29]) << 8) +
                             ((0xffffffff & buff[30]) << 16) +
                             ((0xffffffff & buff[31]) << 24);
            NumberOfRelocations = (UInt16)(((0xffff & buff[33]) << 8) + buff[32]);
            NumberOfLinenumbers = (UInt16)(((0xffff & buff[35]) << 8) + buff[34]);
            Characteristics = (0xffffffff & buff[36]) +
                             ((0xffffffff & buff[37]) << 8) +
                             ((0xffffffff & buff[38]) << 16) +
                             ((0xffffffff & buff[39]) << 24);
        }

        public string GetName()
        {
            //Get Section Name Length
            int len = Name1.Length;
            for (int i = 0; i < Name1.Length; i++)
            {
                if (Name1[i] == 0x00)
                {
                    len = i;
                    break;
                }
            }

            if (len == 0)
            {
                return "";
            }

            //Copy string
            char[] name = new char[len];
            for (int i = 0; i < len; i++)
            {
                name[i] = (char)(this.Name1[i]);
            }
            return new string(name);
        }
    }

    // 共 112 + 128 = 240 字节
    struct IMAGE_OPTIONAL_HEADER_X64
    {
        public UInt16 Magic;      // 魔术数字
        public byte MajorLinkerVersion;  // 连接器主版本
        public byte MinorLinkerVersion;  // 连接器小版本
        public UInt32 SizeOfCode; // 代码段字节数
        public UInt32 SizeOfInitializedData;    // 初始化数据段字节数
        public UInt32 SizeOfUninitializedData;  // 未初始化数据段字节数
        public UInt32 AddressOfEntryPoint;      // 函数入口点地址
        public UInt32 BaseOfCode;   // 代码段开始地址
        public UInt32 BaseOfData;   // 数据段开始地址
        public UInt32 ImageBase;    // PE文件载入内存的首选地址，DLL的默认值为 0x10000000. Exe文件默认值为 0x00400000.
        public UInt32 SectionAlignment; // 在内存中的段对齐字节数，必须比文件对齐字节数大
        public UInt32 FileAlignment;    // 在内存中的文件对齐字节数
        public UInt16 MajorOperatingSystemVersion;  // 操作系统主版本
        public UInt16 MinorOperatingSystemVersion;  // 操作系统小版本
        public UInt16 MajorImageVersion;  // 文件主版本
        public UInt16 MinorImageVersion;  // 文件小版本
        public UInt16 MajorSubsystemVersion;  // 子系统主版本
        public UInt16 MinorSubsystemVersion;  // 子系统小版本
        public UInt32 Win32VersionValue;  // Win32版本值
        public UInt32 SizeOfImage;  // PE文件镜像字节数，包含所有头部，必须是段对齐的倍数
        public UInt32 SizeOfHeaders;  // Combined size of the MS-DOS stub, the PE header, and the section //headers, rounded to a multiple of the value specified in the FileAlignment member.
        public UInt32 CheckSum;
        public UInt16 Subsystem;  // 子系统
        public UInt16 DllCharacteristics;  // Dll特性
        public UInt64 SizeOfStackReserve;  // 保留的栈大小
        public UInt64 SizeOfStackCommit;  // 必须的栈大小
        public UInt64 SizeOfHeapReserve;  // 保留的堆大小
        public UInt64 SizeOfHeapCommit;   // 必要的堆大小
        public UInt32 LoaderFlags;  // 载入标志
        public UInt32 NumberOfRvaAndSizes;  // Rva的数量和长度
        public IMAGE_DATA_DIRECTORY[] DataDirectory;  // 数据目录

        public IMAGE_OPTIONAL_HEADER_X64(byte[] buff, int offset)
        {
            Magic = (UInt16)(((0xffff & buff[offset + 1]) << 8) + buff[offset]);
            MajorLinkerVersion = buff[offset + 2];
            MinorLinkerVersion = buff[offset + 3];
            SizeOfCode = (0xffffffff & buff[offset + 4]) +
                         ((0xffffffff & buff[offset + 5]) << 8) +
                         ((0xffffffff & buff[offset + 6]) << 16) +
                         ((0xffffffff & buff[offset + 7]) << 24);
            SizeOfInitializedData = (0xffffffff & buff[offset + 8]) +
                         ((0xffffffff & buff[offset + 9]) << 8) +
                         ((0xffffffff & buff[offset + 10]) << 16) +
                         ((0xffffffff & buff[offset + 11]) << 24);
            SizeOfUninitializedData = (0xffffffff & buff[offset + 12]) +
                         ((0xffffffff & buff[offset + 13]) << 8) +
                         ((0xffffffff & buff[offset + 14]) << 16) +
                         ((0xffffffff & buff[offset + 15]) << 24);
            AddressOfEntryPoint = (0xffffffff & buff[offset + 16]) +
                         ((0xffffffff & buff[offset + 17]) << 8) +
                         ((0xffffffff & buff[offset + 18]) << 16) +
                         ((0xffffffff & buff[offset + 19]) << 24);
            BaseOfCode = (0xffffffff & buff[offset + 20]) +
                         ((0xffffffff & buff[offset + 21]) << 8) +
                         ((0xffffffff & buff[offset + 22]) << 16) +
                         ((0xffffffff & buff[offset + 23]) << 24);
            BaseOfData = (0xffffffff & buff[offset + 24]) +
                         ((0xffffffff & buff[offset + 25]) << 8) +
                         ((0xffffffff & buff[offset + 26]) << 16) +
                         ((0xffffffff & buff[offset + 27]) << 24);
            ImageBase = (0xffffffff & buff[offset + 28]) +
                         ((0xffffffff & buff[offset + 29]) << 8) +
                         ((0xffffffff & buff[offset + 30]) << 16) +
                         ((0xffffffff & buff[offset + 31]) << 24);
            SectionAlignment = (0xffffffff & buff[offset + 32]) +
                         ((0xffffffff & buff[offset + 33]) << 8) +
                         ((0xffffffff & buff[offset + 34]) << 16) +
                         ((0xffffffff & buff[offset + 35]) << 24);
            FileAlignment = (0xffffffff & buff[offset + 36]) +
                         ((0xffffffff & buff[offset + 37]) << 8) +
                         ((0xffffffff & buff[offset + 38]) << 16) +
                         ((0xffffffff & buff[offset + 39]) << 24);
            MajorOperatingSystemVersion = (UInt16)(((0xffff & buff[offset + 41]) << 8) + buff[offset + 40]);
            MinorOperatingSystemVersion = (UInt16)(((0xffff & buff[offset + 42]) << 8) + buff[offset + 43]);
            MajorImageVersion = (UInt16)(((0xffff & buff[offset + 45]) << 8) + buff[offset + 44]);
            MinorImageVersion = (UInt16)(((0xffff & buff[offset + 47]) << 8) + buff[offset + 46]);
            MajorSubsystemVersion = (UInt16)(((0xffff & buff[offset + 49]) << 8) + buff[offset + 48]);
            MinorSubsystemVersion = (UInt16)(((0xffff & buff[offset + 51]) << 8) + buff[offset + 50]);
            Win32VersionValue = (0xffffffff & buff[offset + 52]) +
                         ((0xffffffff & buff[offset + 53]) << 8) +
                         ((0xffffffff & buff[offset + 54]) << 16) +
                         ((0xffffffff & buff[offset + 55]) << 24);
            SizeOfImage = (0xffffffff & buff[offset + 56]) +
                        ((0xffffffff & buff[offset + 57]) << 8) +
                        ((0xffffffff & buff[offset + 58]) << 16) +
                        ((0xffffffff & buff[offset + 59]) << 24);
            SizeOfHeaders = (0xffffffff & buff[offset + 60]) +
                        ((0xffffffff & buff[offset + 61]) << 8) +
                        ((0xffffffff & buff[offset + 62]) << 16) +
                        ((0xffffffff & buff[offset + 63]) << 24);
            CheckSum = (0xffffffff & buff[offset + 64]) +
                        ((0xffffffff & buff[offset + 65]) << 8) +
                        ((0xffffffff & buff[offset + 66]) << 16) +
                        ((0xffffffff & buff[offset + 67]) << 24);
            Subsystem = (UInt16)(((0xffff & buff[offset + 69]) << 8) + buff[offset + 68]);
            DllCharacteristics = (UInt16)(((0xffff & buff[offset + 71]) << 8) + buff[offset + 70]);

            SizeOfStackReserve = (0xffffffffffffffff & buff[offset + 72]) +
                        ((0xffffffffffffffff & buff[offset + 73]) << 8) +
                        ((0xffffffffffffffff & buff[offset + 74]) << 16) +
                        ((0xffffffffffffffff & buff[offset + 75]) << 24) +
                        ((0xffffffffffffffff & buff[offset + 76]) << 32) +
                        ((0xffffffffffffffff & buff[offset + 77]) << 40) +
                        ((0xffffffffffffffff & buff[offset + 78]) << 48) +
                        ((0xffffffffffffffff & buff[offset + 79]) << 56);

            SizeOfStackCommit = (0xffffffffffffffff & buff[offset + 80]) +
                        ((0xffffffffffffffff & buff[offset + 81]) << 8) +
                        ((0xffffffffffffffff & buff[offset + 82]) << 16) +
                        ((0xffffffffffffffff & buff[offset + 83]) << 24) +
                        ((0xffffffffffffffff & buff[offset + 84]) << 32) +
                        ((0xffffffffffffffff & buff[offset + 85]) << 40) +
                        ((0xffffffffffffffff & buff[offset + 86]) << 48) +
                        ((0xffffffffffffffff & buff[offset + 87]) << 56);
            SizeOfHeapReserve = (0xffffffffffffffff & buff[offset + 88]) +
                        ((0xffffffffffffffff & buff[offset + 89]) << 8) +
                        ((0xffffffffffffffff & buff[offset + 90]) << 16) +
                        ((0xffffffffffffffff & buff[offset + 91]) << 24) +
                        ((0xffffffffffffffff & buff[offset + 92]) << 32) +
                        ((0xffffffffffffffff & buff[offset + 93]) << 40) +
                        ((0xffffffffffffffff & buff[offset + 94]) << 48) +
                        ((0xffffffffffffffff & buff[offset + 95]) << 56);
            SizeOfHeapCommit = (0xffffffffffffffff & buff[offset + 96]) +
                        ((0xffffffffffffffff & buff[offset + 97]) << 8) +
                        ((0xffffffffffffffff & buff[offset + 98]) << 16) +
                        ((0xffffffffffffffff & buff[offset + 99]) << 24) +
                        ((0xffffffffffffffff & buff[offset + 100]) << 32) +
                        ((0xffffffffffffffff & buff[offset + 101]) << 40) +
                        ((0xffffffffffffffff & buff[offset + 102]) << 48) +
                        ((0xffffffffffffffff & buff[offset + 103]) << 56);
            LoaderFlags = (0xffffffff & buff[offset + 104]) +
                        ((0xffffffff & buff[offset + 105]) << 8) +
                        ((0xffffffff & buff[offset + 106]) << 16) +
                        ((0xffffffff & buff[offset + 107]) << 24);
            NumberOfRvaAndSizes = (0xffffffff & buff[offset + 108]) +
                        ((0xffffffff & buff[offset + 109]) << 8) +
                        ((0xffffffff & buff[offset + 110]) << 16) +
                        ((0xffffffff & buff[offset + 111]) << 24);

            // 一共 16 个数据目录
            DataDirectory = new IMAGE_DATA_DIRECTORY[16];
            offset += 112;
            for (int i = 0; i < 16; i++)
            {
                DataDirectory[i] = new IMAGE_DATA_DIRECTORY(buff, offset);
                offset += 8;
            }
        }        
    }

    // 共 96 + 128 = 224 字节
    struct IMAGE_OPTIONAL_HEADER
    {
        public UInt16 Magic;      // 魔术数字
        public byte MajorLinkerVersion;  // 连接器主版本
        public byte MinorLinkerVersion;  // 连接器小版本
        public UInt32 SizeOfCode; // 代码段字节数
        public UInt32 SizeOfInitializedData;    // 初始化数据段字节数
        public UInt32 SizeOfUninitializedData;  // 未初始化数据段字节数
        public UInt32 AddressOfEntryPoint;      // 函数入口点地址
        public UInt32 BaseOfCode;   // 代码段开始地址
        public UInt32 BaseOfData;   // 数据段开始地址
        public UInt32 ImageBase;    // PE文件载入内存的首选地址，DLL的默认值为 0x10000000. Exe文件默认值为 0x00400000.
        public UInt32 SectionAlignment; // 在内存中的段对齐字节数，必须比文件对齐字节数大
        public UInt32 FileAlignment;    // 在内存中的文件对齐字节数
        public UInt16 MajorOperatingSystemVersion;  // 操作系统主版本
        public UInt16 MinorOperatingSystemVersion;  // 操作系统小版本
        public UInt16 MajorImageVersion;  // 文件主版本
        public UInt16 MinorImageVersion;  // 文件小版本
        public UInt16 MajorSubsystemVersion;  // 子系统主版本
        public UInt16 MinorSubsystemVersion;  // 子系统小版本
        public UInt32 Win32VersionValue;  // Win32版本值
        public UInt32 SizeOfImage;  // PE文件镜像字节数，包含所有头部，必须是段对齐的倍数
        public UInt32 SizeOfHeaders;  // Combined size of the MS-DOS stub, the PE header, and the section //headers, rounded to a multiple of the value specified in the FileAlignment member.
        public UInt32 CheckSum;
        public UInt16 Subsystem;  // 子系统
        public UInt16 DllCharacteristics;  // Dll特性
        public UInt32 SizeOfStackReserve;  // 保留的栈大小
        public UInt32 SizeOfStackCommit;  // 必须的栈大小
        public UInt32 SizeOfHeapReserve;  // 保留的堆大小
        public UInt32 SizeOfHeapCommit;   // 必要的堆大小
        public UInt32 LoaderFlags;  // 载入标志
        public UInt32 NumberOfRvaAndSizes;  // Rva的数量和长度
        public IMAGE_DATA_DIRECTORY[] DataDirectory;  // 数据目录

        public IMAGE_OPTIONAL_HEADER(byte[] buff, int offset)
        {
            Magic = (UInt16)(((0xffff & buff[offset + 1]) << 8) + buff[offset]);
            MajorLinkerVersion = buff[offset + 2];
            MinorLinkerVersion = buff[offset + 3];
            SizeOfCode = (0xffffffff & buff[offset + 4]) +
                         ((0xffffffff & buff[offset + 5]) << 8) +
                         ((0xffffffff & buff[offset + 6]) << 16) +
                         ((0xffffffff & buff[offset + 7]) << 24);
            SizeOfInitializedData = (0xffffffff & buff[offset + 8]) +
                         ((0xffffffff & buff[offset + 9]) << 8) +
                         ((0xffffffff & buff[offset + 10]) << 16) +
                         ((0xffffffff & buff[offset + 11]) << 24);
            SizeOfUninitializedData = (0xffffffff & buff[offset + 12]) +
                         ((0xffffffff & buff[offset + 13]) << 8) +
                         ((0xffffffff & buff[offset + 14]) << 16) +
                         ((0xffffffff & buff[offset + 15]) << 24);
            AddressOfEntryPoint = (0xffffffff & buff[offset + 16]) +
                         ((0xffffffff & buff[offset + 17]) << 8) +
                         ((0xffffffff & buff[offset + 18]) << 16) +
                         ((0xffffffff & buff[offset + 19]) << 24);
            BaseOfCode = (0xffffffff & buff[offset + 20]) +
                         ((0xffffffff & buff[offset + 21]) << 8) +
                         ((0xffffffff & buff[offset + 22]) << 16) +
                         ((0xffffffff & buff[offset + 23]) << 24);
            BaseOfData = (0xffffffff & buff[offset + 24]) +
                         ((0xffffffff & buff[offset + 25]) << 8) +
                         ((0xffffffff & buff[offset + 26]) << 16) +
                         ((0xffffffff & buff[offset + 27]) << 24);
            ImageBase = (0xffffffff & buff[offset + 28]) +
                         ((0xffffffff & buff[offset + 29]) << 8) +
                         ((0xffffffff & buff[offset + 30]) << 16) +
                         ((0xffffffff & buff[offset + 31]) << 24);
            SectionAlignment = (0xffffffff & buff[offset + 32]) +
                         ((0xffffffff & buff[offset + 33]) << 8) +
                         ((0xffffffff & buff[offset + 34]) << 16) +
                         ((0xffffffff & buff[offset + 35]) << 24);
            FileAlignment = (0xffffffff & buff[offset + 36]) +
                         ((0xffffffff & buff[offset + 37]) << 8) +
                         ((0xffffffff & buff[offset + 38]) << 16) +
                         ((0xffffffff & buff[offset + 39]) << 24);
            MajorOperatingSystemVersion = (UInt16)(((0xffff & buff[offset + 41]) << 8) + buff[offset + 40]);
            MinorOperatingSystemVersion = (UInt16)(((0xffff & buff[offset + 42]) << 8) + buff[offset + 43]);
            MajorImageVersion = (UInt16)(((0xffff & buff[offset + 45]) << 8) + buff[offset + 44]);
            MinorImageVersion = (UInt16)(((0xffff & buff[offset + 47]) << 8) + buff[offset + 46]);
            MajorSubsystemVersion = (UInt16)(((0xffff & buff[offset + 49]) << 8) + buff[offset + 48]);
            MinorSubsystemVersion = (UInt16)(((0xffff & buff[offset + 51]) << 8) + buff[offset + 50]);
            Win32VersionValue = (0xffffffff & buff[offset + 52]) +
                         ((0xffffffff & buff[offset + 53]) << 8) +
                         ((0xffffffff & buff[offset + 54]) << 16) +
                         ((0xffffffff & buff[offset + 55]) << 24);
            SizeOfImage = (0xffffffff & buff[offset + 56]) +
                        ((0xffffffff & buff[offset + 57]) << 8) +
                        ((0xffffffff & buff[offset + 58]) << 16) +
                        ((0xffffffff & buff[offset + 59]) << 24);
            SizeOfHeaders = (0xffffffff & buff[offset + 60]) +
                        ((0xffffffff & buff[offset + 61]) << 8) +
                        ((0xffffffff & buff[offset + 62]) << 16) +
                        ((0xffffffff & buff[offset + 63]) << 24);
            CheckSum = (0xffffffff & buff[offset + 64]) +
                        ((0xffffffff & buff[offset + 65]) << 8) +
                        ((0xffffffff & buff[offset + 66]) << 16) +
                        ((0xffffffff & buff[offset + 67]) << 24);
            Subsystem = (UInt16)(((0xffff & buff[offset + 69]) << 8) + buff[offset + 68]);
            DllCharacteristics = (UInt16)(((0xffff & buff[offset + 71]) << 8) + buff[offset + 70]);
            SizeOfStackReserve = (0xffffffff & buff[offset + 72]) +
                        ((0xffffffff & buff[offset + 73]) << 8) +
                        ((0xffffffff & buff[offset + 74]) << 16) +
                        ((0xffffffff & buff[offset + 75]) << 24);
            SizeOfStackCommit = (0xffffffff & buff[offset + 76]) +
                        ((0xffffffff & buff[offset + 77]) << 8) +
                        ((0xffffffff & buff[offset + 78]) << 16) +
                        ((0xffffffff & buff[offset + 79]) << 24);
            SizeOfHeapReserve = (0xffffffff & buff[offset + 80]) +
                        ((0xffffffff & buff[offset + 81]) << 8) +
                        ((0xffffffff & buff[offset + 82]) << 16) +
                        ((0xffffffff & buff[offset + 83]) << 24);
            SizeOfHeapCommit = (0xffffffff & buff[offset + 84]) +
                        ((0xffffffff & buff[offset + 85]) << 8) +
                        ((0xffffffff & buff[offset + 86]) << 16) +
                        ((0xffffffff & buff[offset + 87]) << 24);
            LoaderFlags = (0xffffffff & buff[offset + 88]) +
                        ((0xffffffff & buff[offset + 89]) << 8) +
                        ((0xffffffff & buff[offset + 90]) << 16) +
                        ((0xffffffff & buff[offset + 91]) << 24);
            NumberOfRvaAndSizes = (0xffffffff & buff[offset + 92]) +
                        ((0xffffffff & buff[offset + 93]) << 8) +
                        ((0xffffffff & buff[offset + 94]) << 16) +
                        ((0xffffffff & buff[offset + 95]) << 24);

            // 一共 16 个数据目录
            DataDirectory = new IMAGE_DATA_DIRECTORY[16];
            offset += 96;
            for (int i = 0; i < 16; i++)
            {
                DataDirectory[i] = new IMAGE_DATA_DIRECTORY(buff, offset);
                offset += 8;
            }
        }
    }

    // 8 bytes
    struct IMAGE_DATA_DIRECTORY
    {
        public UInt32 VirtualAddress;
        public UInt32 Size;

        public IMAGE_DATA_DIRECTORY(byte[] buff, int offset)
        {
            VirtualAddress = (0xffffffff & buff[offset]) +
                             ((0xffffffff & buff[offset + 1]) << 8) +
                             ((0xffffffff & buff[offset + 2]) << 16) +
                             ((0xffffffff & buff[offset + 3]) << 24);
            Size = (0xffffffff & buff[offset + 4]) +
                   ((0xffffffff & buff[offset + 5]) << 8) +
                   ((0xffffffff & buff[offset + 6]) << 16) +
                   ((0xffffffff & buff[offset + 7]) << 24);
        }
    }

    //20 bytes
    struct IMAGE_FILE_HEADER
    {
        public UInt16 Machine;            // 目标机器类型
        public UInt16 NumberOfSections;   // 段数目
        public UInt32 TimeDateStamp;      // 文件被创建时间
        public UInt32 PointerToSymbolTable;  // COFF符号表的文件地址偏移量
        public UInt32 NumberOfSymbols;       // 符号表中的符号数量
        public UInt16 SizeOfOptionalHeader;  // 可选文件头的大小
        public UInt16 Characteristics;       // 文件特征标志

        //offset = 4
        public IMAGE_FILE_HEADER(byte[] buff, int offset)
        {
            Machine = (UInt16)(((0xffff & buff[offset+1]) << 8) + buff[offset]);
            NumberOfSections = (UInt16)(((0xffff & buff[offset + 3]) << 8) + buff[offset + 2]);
            TimeDateStamp = (0xffffffff & buff[offset + 4]) +
                            ((0xffffffff & buff[offset + 5]) << 8) +
                            ((0xffffffff & buff[offset + 6]) << 16) +
                            ((0xffffffff & buff[offset + 7]) << 24);
            PointerToSymbolTable = (0xffffffff & buff[offset + 8]) +
                                   ((0xffffffff & buff[offset + 9]) << 8) +
                                   ((0xffffffff & buff[offset + 10]) << 16) +
                                   ((0xffffffff & buff[offset + 11]) << 24);
            NumberOfSymbols = (0xffffffff & buff[offset + 12]) +
                              ((0xffffffff & buff[offset + 13]) << 8) +
                              ((0xffffffff & buff[offset + 14]) << 16) +
                              ((0xffffffff & buff[offset + 15]) << 24);

            SizeOfOptionalHeader = (UInt16)(((0xffff & buff[offset + 17]) << 8) + buff[offset + 16]);
            Characteristics = (UInt16)(((0xffff & buff[offset + 19]) << 8) + buff[offset + 18]);
        }
    }

    // IMAGE_FILE_HEADER里的Machine取值范围
    enum IMAGE_MACHINE_TYPE
    {
        IMAGE_FILE_MACHINE_UNKNOWN = 0x0,     // Any CPU
        IMAGE_FILE_MACHINE_AM33 = 0x1d3,      // Matsushita AM33 处理器
        IMAGE_FILE_MACHINE_AMD64 = 0x8664,    // x64 处理器
        IMAGE_FILE_MACHINE_ARM = 0x1c0,     // ARM 小尾处理器
        IMAGE_FILE_MACHINE_EBC = 0xebc,     // EFI 字节码处理器
        IMAGE_FILE_MACHINE_I386 = 0x14c,     // Intel 386 或后续兼容处理器
        IMAGE_FILE_MACHINE_IA64 = 0x200,      // Intel Itanium 处理器家族
        IMAGE_FILE_MACHINE_M32R = 0x9041,     // Mitsubishi M32R 小尾处理器
        IMAGE_FILE_MACHINE_MIPS16 = 0x266,    // MIPS16 处理器
        IMAGE_FILE_MACHINE_MIPSFPU = 0x366,    // 带FPU 的MIPS 处理器
        IMAGE_FILE_MACHINE_MIPSFPU16 = 0x466,   // 带FPU 的MIPS16 处理器
        IMAGE_FILE_MACHINE_POWERPC = 0x1f0,     // PowerPC 小尾处理器
        IMAGE_FILE_MACHINE_POWERPCFP = 0x1f1,   // 带符点运算支持的PowerPC 处理器
        IMAGE_FILE_MACHINE_R4000 = 0x166,       // MIPS 小尾处理器
        IMAGE_FILE_MACHINE_SH3 = 0x1a2,       // Hitachi SH3 处理器
        IMAGE_FILE_MACHINE_SH3DSP = 0x1a3,    // Hitachi SH3 DSP 处理器
        IMAGE_FILE_MACHINE_SH4 = 0x1a6,       // Hitachi SH4 处理器
        IMAGE_FILE_MACHINE_SH5 = 0x1a8,      // Hitachi SH5 处理器
        IMAGE_FILE_MACHINE_THUMB = 0x1c2,     // Thumb 处理器
        IMAGE_FILE_MACHINE_WCEMIPSV2 = 0x169   // MIPS 小尾WCE v2 处理器
    }

    // IMAGE_FILE_HEADER里的Characteristics取值范围
    enum IMAGE_Characteristics
    {
        IMAGE_FILE_RELOCS_STRIPPED = 1,    // 文件中不存在重定位信息
        IMAGE_FILE_EXECUTABLE_IMAGE = 2,   // 文件是可执行的
        IMAGE_FILE_LINE_NUMS_STRIPPED = 4,  // 不存在行信息
        IMAGE_FILE_LOCAL_SYMS_STRIPPED = 8,  // 不存在符号信息
        IMAGE_FILE_AGGRESIVE_WS_TRIM = 16,   // 不再使用
        IMAGE_FILE_LARGE_ADDRESS_AWARE = 32, // 寻址大于2GB
        Reserved_1 = 64,                     // 保留位
        IMAGE_FILE_BYTES_REVERSED_LO = 128,  // 小尾方式
        IMAGE_FILE_32BIT_MACHINE = 256,      // 只在３２位平台运行
        IMAGE_FILE_DEBUG_STRIPPED = 512,     // 不包含调试信息
        IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP = 1024,   // 不能从可移动盘运行
        IMAGE_FILE_NET_RUN_FROM_SWAP = 2048,    // 不能从网络运行
        IMAGE_FILE_SYSTEM = 4096,   // 系统文件。不能直接运行
        IMAGE_FILE_DLL = 8192,      // ＤＬＬ文件
        IMAGE_FILE_UP_SYSTEM_ONLY = 16384,     // 文件不能在多处理器上运行
        IMAGE_FILE_BYTES_REVERSED_HI = 32768   // 大尾方式
    }

    // IMAGE_SECTION_HEADER里的Characteristics取值范围
    enum Section_Characteristics
    {
        Reserved_1 = 0x00000000,
        Reserved_2 = 0x00000001,
        Reserved_3 = 0x00000002,
        Reserved_4 = 0x00000004,
        IMAGE_SCN_TYPE_NO_PAD = 0x00000008,  // 已被 IMAGE_SCN_ALIGN_1BYTES 替代
        Reserved_5 = 0x00000010,
        IMAGE_SCN_CNT_CODE = 0x00000020,     // 段包含可执行代码
        IMAGE_SCN_CNT_INITIALIZED_DATA = 0x00000040,    // 段包含初始化数据
        IMAGE_SCN_CNT_UNINITIALIZED_DATA = 0x00000080,  // 段包含未初始化数据
        IMAGE_SCN_LNK_OTHER = 0x00000100,    // 保留
        IMAGE_SCN_LNK_INFO = 0x00000200,     // 段包含注释和其他信息，仅对Object文件有效
        Reserved_6 = 0x00000400,
        IMAGE_SCN_LNK_REMOVE = 0x00000800,   // 段在链接时被移除，仅对Object文件有效
        IMAGE_SCN_LNK_COMDAT = 0x00001000,   // 段包含 COMDAT 数据， 仅对Object文件有效
        Reserved_8 = 0x00002000,
        IMAGE_SCN_NO_DEFER_SPEC_EXC = 0x00004000, // Reset speculative exceptions handling bits in the TLB entries for this section.
        IMAGE_SCN_GPREL = 0x00008000,             // 段中包含被全局指针引用的数据
        Reserved_9 = 0x00010000,
        IMAGE_SCN_MEM_PURGEABLE = 0x00020000,     // 保留
        IMAGE_SCN_MEM_LOCKED = 0x00040000,        // 保留
        IMAGE_SCN_MEM_PRELOAD = 0x00080000,       // 保留
        IMAGE_SCN_ALIGN_1BYTES = 0x00100000,      // 放置数据的边界为1字节
        IMAGE_SCN_ALIGN_2BYTES = 0x00200000,
        IMAGE_SCN_ALIGN_4BYTES = 0x00300000,
        IMAGE_SCN_ALIGN_8BYTES = 0x00400000,
        IMAGE_SCN_ALIGN_16BYTES = 0x00500000,
        IMAGE_SCN_ALIGN_32BYTES = 0x00600000,
        IMAGE_SCN_ALIGN_64BYTES = 0x00700000,
        IMAGE_SCN_ALIGN_128BYTES = 0x00800000,
        IMAGE_SCN_ALIGN_256BYTES = 0x00900000,
        IMAGE_SCN_ALIGN_512BYTES = 0x00A00000,
        IMAGE_SCN_ALIGN_1024BYTES = 0x00B00000,
        IMAGE_SCN_ALIGN_2048BYTES = 0x00C00000,
        IMAGE_SCN_ALIGN_4096BYTES = 0x00D00000,
        IMAGE_SCN_ALIGN_8192BYTES = 0x00E00000,
        IMAGE_SCN_LNK_NRELOC_OVFL = 0x01000000,    // The section contains extended relocations.
        IMAGE_SCN_MEM_DISCARDABLE = 0x02000000,   // 段可以被丢弃
        IMAGE_SCN_MEM_NOT_CACHED = 0x04000000,   // 段中的数据在内存中不会被缓存
        IMAGE_SCN_MEM_NOT_PAGED = 0x08000000,   // 段中的数据在内存中不会被分页
        IMAGE_SCN_MEM_SHARED = 0x10000000,    // 段中的数据在内存中被共享
        IMAGE_SCN_MEM_EXECUTE = 0x20000000,   // 段可以作为代码执行
        IMAGE_SCN_MEM_READ = 0x40000000,      // 段可读
        IMAGE_SCN_MEM_WRITE = Int32.MinValue      // 段可写 0x80000000
    }

    // 数据目录的用途，16个数据目录所指向的数据是有指定用途的，并且按序号用途是固定的
    enum DataDirectoryUsage
    {
        ExportTable = 0,
        ImportTable = 1,
        ResourceTable = 2,
        ExceptionTable = 3,
        CertificateTable = 4,
        BaseRelocationTable = 5,
        DebugData = 6,
        Architecture = 7,
        GlobalPointer = 8,
        ThreadLocalStorageTable = 9,
        LoadConfigTable = 10,
        BoundImport = 11,
        ImportAddressTable = 12,
        DelayImportDescriptor = 13,
        ClrRuntimeHeader = 14,
        Reserved = 15
    }
}
