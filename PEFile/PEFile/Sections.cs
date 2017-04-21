using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace PEFile
{
    class Section
    {
        public string Name;
        public Object SectionInstance = null;
        private long FileOffset;

        public Section(string name, PEFileInfo fileInfo, long offset)
        {
            Name = name;
            FileOffset = offset;
            string cmpName = name.ToLower();
            if (cmpName == ".bss")
            {
            }
            else if (cmpName == ".cormeta")
            {
            
            }
            else if (cmpName == ".data")
            {
            
            }
            else if (cmpName == ".debug$f")
            {

            }
            else if (cmpName == ".debug$p")
            {

            }
            else if (cmpName == ".debug$s")
            {

            }
            else if (cmpName == ".debug$t")
            {

            }
            else if (cmpName == ".drectve")
            {

            }
            else if (cmpName == ".edata")
            {

            }
            else if (cmpName == ".idata")
            {

            }
            else if (cmpName == ".idlsym")
            {

            }
            else if (cmpName == ".pdata")
            {

            }
            else if (cmpName == ".rdata")
            {

            }
            else if (cmpName == ".reloc")
            {

            }
            else if (cmpName == ".rsrc")
            {
                SectionInstance = new Image_Resource_Directory(fileInfo, offset, offset, 0, 0, 0, 0); // 根节点的offset和和段的文件偏移相同
            }
            else if (cmpName == ".sbss")
            {

            }
            else if (cmpName == ".sdata")
            {

            }
            else if (cmpName == ".srdata")
            {

            }
            else if (cmpName == ".sxdata")
            {

            }
            else if (cmpName == ".text")
            {

            }
            else if (cmpName == ".tls")
            {

            }
            else if (cmpName == ".tls$")
            {

            }
            else if (cmpName == ".vsdata")
            {

            }
            else if (cmpName == ".xdata")
            {

            }
        }
    }


    #region .Rsrc Section

    // .Rsrc段是一个以Image_Resource_Directory结构为根节点的多叉树，理论上层数可以达到2^31层，微软实际只使用3层
    // Image_Resource_Directory后紧跟一个或多个Image_Resource_Directory_Entry
    // 每个Image_Resource_Directory_Entry指向一个Image_Resource_Data_Entry或者Image_Resource_Directory
    struct Image_Resource_Directory
    {
        public UInt32 Characteristics;
        public UInt32 TimeDateStamp;
        public UInt16 MajorVersion;
        public UInt16 MinorVersion;
        public UInt16 NumberOfNamedEntries;
        public UInt16 NumberOfIdEntries;
        public Image_Resource_Directory_Entry[] ImageResourceDirectoryEntries;
        
        public Image_Resource_Directory(PEFileInfo fileInfo, long offset, long rootOffset, int level, int resType, int id, int language)
        {
            byte[] buff = new byte[16];
            fileInfo.pFileStream.Position = offset;
            int len = fileInfo.pFileStream.Read(buff, 0, 16);

            // 获取Directory的成员
            Characteristics = (0xffffffff & buff[0]) +
                 ((0xffffffff & buff[1]) << 8) +
                 ((0xffffffff & buff[2]) << 16) +
                 ((0xffffffff & buff[3]) << 24);
            TimeDateStamp = (0xffffffff & buff[4]) +
                             ((0xffffffff & buff[5]) << 8) +
                             ((0xffffffff & buff[6]) << 16) +
                             ((0xffffffff & buff[7]) << 24);
            MajorVersion = (UInt16)(((0xffff & buff[9]) << 8) + buff[8]);
            MinorVersion = (UInt16)(((0xffff & buff[11]) << 8) + buff[10]);
            NumberOfNamedEntries = (UInt16)(((0xffff & buff[13]) << 8) + buff[12]);
            NumberOfIdEntries = (UInt16)(((0xffff & buff[15]) << 8) + buff[14]);

            // 获取后面的所有Entry
            int entryCount = 0;

            if (NumberOfNamedEntries > 0)
            {
                entryCount += NumberOfNamedEntries;
            }
            if (NumberOfIdEntries > 0)
            {
                entryCount += NumberOfIdEntries;
            }

            ImageResourceDirectoryEntries = new Image_Resource_Directory_Entry[entryCount];
            if (entryCount > 0)
            {
                buff = new byte[8 * entryCount];
                len = fileInfo.pFileStream.Read(buff, 0, 8 * entryCount);

                for (int i = 0; i < entryCount; i++)
                {
                    ImageResourceDirectoryEntries[i] = new Image_Resource_Directory_Entry(buff, 8 * i, fileInfo, rootOffset, level, resType, id, language);
                }
            }
        }

        // level为节点的深度，root深度为0；output为输出目录
        public void ExportResource(string output, int level, UInt32 resType)
        {
            string currentDir = output;
            if (level == 0)
            {
                currentDir += "\\Resources";
                Directory.CreateDirectory(currentDir);
            }
            for (int i = 0; i < ImageResourceDirectoryEntries.Length; i++)
            {
                Image_Resource_Directory_Entry current = ImageResourceDirectoryEntries[i];
                if (current.NameString != null)
                {
                    string myDir = currentDir + "\\" + ((IMAGE_RESOURCE_DIR_STRING_U)current.NameString).ToString();
                    Directory.CreateDirectory(myDir);
                    if ((current.OffsetToData >> 31) == 1)
                    {
                        if (level == 0)   //只有最顶层的ID才表示资源类型
                        {
                            resType = 0;  //0为Name型资源
                        }
                        ((Image_Resource_Directory)current.ChildEntry).ExportResource(myDir, level + 1, resType);
                    }
                    else
                    {
                        ((Image_Resource_Data_Entry)current.ChildEntry).ExportResource(myDir, resType);
                    }
                }
                else
                {
                    string myDir = currentDir + "\\" + current.Name.ToString();
                    Directory.CreateDirectory(myDir);
                    if (level == 0)  //只有最顶层的ID才表示资源类型
                    {
                        resType = current.Name;
                    }
                    if ((current.OffsetToData >> 31) == 1)
                    {
                        ((Image_Resource_Directory)current.ChildEntry).ExportResource(myDir, level + 1, resType);
                    }
                    else
                    {
                        ((Image_Resource_Data_Entry)current.ChildEntry).ExportResource(myDir, resType);
                    }
                }
            }
        }
    }

    // ANSI string
    struct IMAGE_RESOURCE_DIR_STRING
    {
        public UInt16 Length;
        public byte[] AString;  // AString的实际长度+2必须为4的倍数。

        public IMAGE_RESOURCE_DIR_STRING(Stream file, long offset)
        {
            file.Position = offset;
            byte[] buff = new byte[2];
            int len = file.Read(buff, 0, 2);
            Length = (UInt16)(((0xffff & buff[1]) << 8) + buff[0]);
            AString = new byte[(int)Length];
            len = file.Read(AString, 0, Length);
        }

        public override string ToString()
        {
            char[] ret = new char[Length];
            for (int i = 0; i < AString.Length; i++)
            {
                ret[i] = (char)AString[i];
            }

            return new string(ret);
        }
    }

    // Unicode String
    struct IMAGE_RESOURCE_DIR_STRING_U
    {
        public UInt16 Length;
        public byte[] UString;  // UString的实际长度为 4 * Length + 2。

        public IMAGE_RESOURCE_DIR_STRING_U(Stream file, long offset, long rootOffset)
        {
            file.Position = rootOffset + offset;
            byte[] buff = new byte[2];
            int len = file.Read(buff, 0, 2);
            Length = (UInt16)(((0xffff & buff[1]) << 8) + buff[0]);

            // 按4字节对齐，
            if ((Length % 2) > 0)
            {
                len = Length * 2 + 2;
            }
            else
            {
                len = Length * 2 + 4;
            }

            UString = new byte[len];
            len = file.Read(UString, 0, len);
        }

        public override string ToString()
        {
            char[] ret = new char[Length];
            for(int i=0;i<Length;i++)
            {
                ret[i] = (char)(((0xffff & UString[2 * i + 1]) << 8) + UString[2 * i]);
            }
            return new string(ret);
        }
    }


    struct Image_Resource_Directory_Entry
    {
        // Name成员，在Entry位于不同的层时，含义是不一样的
        // 在根节点上，如果Name是一个ID，则表示资源种类，如果在第二层，Name是ID则表示资源的语言类别
        // 最高位为1时，Name的低31位是一个从根节点开始的偏移地址，指向一个IMAGE_RESOURCE_DIR_STRING_U的字符串结构
        // 最高位为0时，则低31位是一个ID
        public UInt32 Name;
        
        // 根据OffsetToData最高位来决定ChildEntry是枝节点还是叶子，具体偏移由OffSetToData低31位来决定
        // 如果最高位为1，则低31位是从根节点开始的指向一个Image_Resource_Directory结构的偏移量
        // 如果最高位时0，则低31位是从根节点开始的指向一个Image_Resource_Data_Entry结构的偏移量
        public UInt32 OffsetToData;
        public Object NameString;
        public Object ChildEntry;

        public Image_Resource_Directory_Entry(byte[] buff, int offset, PEFileInfo fileInfo, long rootOffset, int level, int resType, int id, int language)
        {
            NameString = null;
            ChildEntry = null;

            Name = (0xffffffff & buff[offset]) +
                    ((0xffffffff & buff[offset+1]) << 8) +
                    ((0xffffffff & buff[offset+2]) << 16) +
                    ((0xffffffff & buff[offset+3]) << 24);
            OffsetToData = (0xffffffff & buff[offset + 4]) +
                            ((0xffffffff & buff[offset+5]) << 8) +
                            ((0xffffffff & buff[offset+6]) << 16) +
                            ((0xffffffff & buff[offset+7]) << 24);
            if ((Name >> 31) == 1) //最高位为1
            {
                long nameOffset = Name & 0x7fffffff;
                NameString = new IMAGE_RESOURCE_DIR_STRING_U(fileInfo.pFileStream, nameOffset, rootOffset);
            }
            else
            {
                if (level == 0)  // 第一层的ID表示资源类型
                {
                    resType = (int)Name;
                }
                else if (level == 1)  // 第二层的ID表示资源ID
                {
                    id = (int)Name;
                }
                else if (level == 2)  // 第三层的ID表示语言类型
                {
                    language = (int)Name;
                }
            }

            if ((OffsetToData >> 31) == 1)
            {
                long entryOffset = rootOffset + (OffsetToData & 0x7fffffff);
                ChildEntry = new Image_Resource_Directory(fileInfo, entryOffset, rootOffset, level + 1, resType, id, language);
            }
            else
            {
                byte[] buffData = new byte[16];
                fileInfo.pFileStream.Position = rootOffset + OffsetToData;
                int len = fileInfo.pFileStream.Read(buffData, 0, 16);
                ChildEntry = new Image_Resource_Data_Entry(buffData, fileInfo, rootOffset, resType, id, language);
            }
        }
    }

    struct Image_Resource_Data_Entry
    {
        public UInt32 OffsetToData;
        public UInt32 Size;
        public UInt32 CodePage;
        public UInt32 Reversed;
        public Byte[] Data;
        private Object Instance;
        private int Type;
        private int ID;
        private int Language;

        public Image_Resource_Data_Entry(byte[] buff, PEFileInfo fileInfo, long rootOffset, int resType, int id, int language)
        {
            Type = resType;
            ID = id;
            Language = language;
            
            OffsetToData = (0xffffffff & buff[0]) +
                    ((0xffffffff & buff[1]) << 8) +
                    ((0xffffffff & buff[2]) << 16) +
                    ((0xffffffff & buff[3]) << 24);
            Size = (0xffffffff & buff[4]) +
                    ((0xffffffff & buff[5]) << 8) +
                    ((0xffffffff & buff[6]) << 16) +
                    ((0xffffffff & buff[7]) << 24);
            CodePage = (0xffffffff & buff[8]) +
                    ((0xffffffff & buff[9]) << 8) +
                    ((0xffffffff & buff[10]) << 16) +
                    ((0xffffffff & buff[11]) << 24);
            Reversed = (0xffffffff & buff[12]) +
                    ((0xffffffff & buff[13]) << 8) +
                    ((0xffffffff & buff[14]) << 16) +
                    ((0xffffffff & buff[15]) << 24);

            fileInfo.pFileStream.Position = rootOffset + OffsetToData - fileInfo.SectionVirtualAddress;
            Data = new byte[Size];
            int len = fileInfo.pFileStream.Read(Data, 0, (int)Size);

            switch (resType)
            {
                case 1:  // 光标
                    {
                        byte[] resHeader = new byte[16];
                        fileInfo.pFileStream.Position = rootOffset;
                        len = fileInfo.pFileStream.Read(resHeader, 0, 16);
                        int entryNumber = (Int32)(((0xffff & resHeader[13]) << 8) + resHeader[12]) + (Int32)(((0xffff & resHeader[15]) << 8) + resHeader[14]);

                        byte[] entries = new byte[8 * entryNumber];
                        len = fileInfo.pFileStream.Read(entries, 0, 8 * entryNumber);

                        UInt32 address = 0;

                        for (int i = 0; i < entryNumber; i++)
                        {
                            UInt32 name = (0xffffffff & entries[8 * i]) +
                                          ((0xffffffff & entries[8 * i + 1]) << 8) +
                                          ((0xffffffff & entries[8 * i + 2]) << 16) +
                                          ((0xffffffff & entries[8 * i + 3]) << 24);
                            if (name == 12)
                            {
                                address = (0xffffffff & entries[8 * i + 4]) +
                                          ((0xffffffff & entries[8 * i + 5]) << 8) +
                                          ((0xffffffff & entries[8 * i + 6]) << 16) +
                                          ((0xffffffff & entries[8 * i + 7]) << 24);
                                break;
                            }
                        }
                        Image_Resource_Directory iRD = new Image_Resource_Directory(fileInfo, (address & 0x7fffffff) + rootOffset, rootOffset, 1, 12, 0, 0);
                        Instance = new IMAGE_CURSOR(Data, iRD, ID);
                        break;
                    }
                case 3:  // 图标
                    {
                        byte[] resHeader = new byte[16];
                        fileInfo.pFileStream.Position = rootOffset;
                        len = fileInfo.pFileStream.Read(resHeader, 0, 16);
                        int entryNumber = (Int32)(((0xffff & resHeader[13]) << 8) + resHeader[12]) + (Int32)(((0xffff & resHeader[15]) << 8) + resHeader[14]);

                        byte[] entries = new byte[8 * entryNumber];
                        len = fileInfo.pFileStream.Read(entries, 0, 8 * entryNumber);

                        UInt32 address = 0;

                        for (int i = 0; i < entryNumber; i++)
                        {
                            UInt32 name = (0xffffffff & entries[8 * i]) +
                                          ((0xffffffff & entries[8 * i + 1]) << 8) +
                                          ((0xffffffff & entries[8 * i + 2]) << 16) +
                                          ((0xffffffff & entries[8 * i + 3]) << 24);
                            if (name == 14)
                            {
                                address = (0xffffffff & entries[8 * i + 4]) +
                                          ((0xffffffff & entries[8 * i + 5]) << 8) +
                                          ((0xffffffff & entries[8 * i + 6]) << 16) +
                                          ((0xffffffff & entries[8 * i + 7]) << 24);
                                break;
                            }
                        }
                        Image_Resource_Directory iRD = new Image_Resource_Directory(fileInfo, rootOffset +(address & 0x7fffffff), rootOffset, 1, 14, 0, 0);
                        Instance = new IMAGE_ICON(Data, iRD, ID);
                        break;
                    }
                case 12:  // 组光标
                    {
                        Instance = new GROUP_CURSOR(Data);
                        break;
                    }
                case 14:  // 组图标
                    {
                        Instance = new GROUP_ICON(Data);
                        break;
                    }
                default:
                    {
                        Instance = null;
                        break;
                    }
            }
        }

        public void ExportResource(string output, UInt32 resType)
        {
            switch (resType)
            {
                case 0:  // 以名称标注的资源，一般是自定义格式资源，按二进制输出
                    {
                        File.WriteAllBytes(output + "\\Rawdata.bin", Data);
                        break;
                    }
                case 1:  // 光标
                    {
                        IMAGE_CURSOR imgCursor = (IMAGE_CURSOR)Instance;
                        imgCursor.Export(output + "\\Cursor.cur");
                        File.WriteAllBytes(output + "\\Rawdata.bin", Data);
                        break;
                    }
                case 2:  // 位图
                    {
                        File.WriteAllBytes(output + "\\Rawdata.bin", Data);
                        RESOURCE_BITMAP bmp = new RESOURCE_BITMAP(Data);
                        bmp.Export(output + "\\Resource.bmp");
                        break;    
                    }
                case 3:   // 图标
                    {
                        IMAGE_ICON imgIcon = (IMAGE_ICON)Instance;
                        imgIcon.Export(output + "\\Icon.ico");
                        File.WriteAllBytes(output + "\\Rawdata.bin", Data);
                        break;
                    }
                case 4:
                    {
                        File.WriteAllBytes(output + "\\Rawdata.bin", Data);
                        break;                    
                    }
                case 5:
                    {
                        File.WriteAllBytes(output + "\\Rawdata.bin", Data);
                        break;
                    }
                case 6:  // 字符串
                    {
                        string strBlock = "";
                        Resource_String_Table rst = new Resource_String_Table(Data);
                        for (int i = 0; i < rst.StringTable.Length; i++)
                        {
                            strBlock += "String " + (i + 1).ToString() + ":" + rst.StringTable[i].ToString() + "\r\n";
                            string temp = rst.StringTable[i].ToString();
                        }
                        File.WriteAllText(output + "\\String.txt", strBlock);
                        File.WriteAllBytes(output + "\\Rawdata.bin", Data);
                        break;
                    }
                case 12:
                    {
                        GROUP_CURSOR gCursor = (GROUP_CURSOR)Instance;
                        gCursor.Export(output + "\\GroupCursorDetails.txt");
                        File.WriteAllBytes(output + "\\Rawdata.bin", Data);
                        break;                                                                                
                    }
                case 14:
                    {
                        GROUP_ICON gIcon = (GROUP_ICON)Instance;
                        gIcon.Export(output + "\\GroupIconDetails.txt");
                        File.WriteAllBytes(output + "\\Rawdata.bin", Data);
                        break;                                                            
                    }

                case 16:   // 版本信息
                    {
                        VS_VERSION_INFO vi = new VS_VERSION_INFO(Data);
                        vi.Export(output + "\\FileVersion.txt");
                        File.WriteAllBytes(output + "\\Rawdata.bin", Data);
                        break;                                                            
                    }
                default:
                    {
                        File.WriteAllBytes(output + "\\Rawdata.bin", Data);
                        break;                                        
                    }
            }
            
        }
    }
    #endregion
}
