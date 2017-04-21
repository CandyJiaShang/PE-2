using System;
using System.Collections.Generic;
using System.IO;

namespace PEFile
{
    #region Cursor

    // PE文件中的光标结构
    class IMAGE_CURSOR
    {
        public CURSOR_HEADER Header;
        public CURSOR_DIRECTORY Directory;

        public CURSOR_FILE_HEADER FileHeader;
        public CURSOR_INFO_HEADER InfoHeader;
        public byte[] FileBody;
        private int ID;

        public IMAGE_CURSOR(byte[] buff, Image_Resource_Directory resourceDir, int cursorID)
        {
            ID = cursorID;
            FileBody = buff;
            FindHeader(resourceDir);
            FileHeader = new CURSOR_FILE_HEADER(Header);
            InfoHeader = new CURSOR_INFO_HEADER(Directory);
        }

        private void FindHeader(Image_Resource_Directory resourceDir)
        {
            int entryCount = resourceDir.NumberOfIdEntries + resourceDir.NumberOfNamedEntries;
            for (int i = 0; i < entryCount; i++)
            {
                Image_Resource_Directory_Entry entry = resourceDir.ImageResourceDirectoryEntries[i];
                if ((entry.OffsetToData & 0x80000000) != 0)
                {
                    Image_Resource_Directory child = (Image_Resource_Directory)entry.ChildEntry;
                    FindHeader(child);
                }
                else
                {
                    Image_Resource_Data_Entry data = (Image_Resource_Data_Entry)entry.ChildEntry;
                    CURSOR_HEADER hd = new CURSOR_HEADER(data.Data);
                    CURSOR_DIRECTORY[] cds = new CURSOR_DIRECTORY[hd.Count];
                    for (int j = 0; j < cds.Length; j++)
                    {
                        cds[j] = new CURSOR_DIRECTORY(data.Data, 6 + j * 14);
                        if (cds[j].CursorID == this.ID)
                        {
                            Header = hd;
                            Directory = cds[j];
                            return;
                        }
                    }
                }
            }
        }

        public void Export(string output)
        {
            Stream outfile = File.Open(output, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            byte[] header = this.FileHeader.ToBytes();
            byte[] infoHeader = this.InfoHeader.ToBytes();
            outfile.Write(header, 0, header.Length);
            outfile.Write(infoHeader, 0, infoHeader.Length);
            outfile.Write(FileBody, 0, FileBody.Length);
            outfile.Flush();
            outfile.Close();
        }
    }

    // 这部分数据从组光标中得到(ResourceType = 12)
    struct CURSOR_HEADER
    {
        public UInt16 Reserved;  // 保留字段，总是为0
        public UInt16 Type;      // 光标资源类型，总是为2
        public UInt16 Count;     // 光标个数

        public CURSOR_HEADER(byte[] buff)
        {
            Reserved = (UInt16)(((0xffff & buff[1]) << 8) + buff[0]);
            Type = (UInt16)(((0xffff & buff[3]) << 8) + buff[2]);
            Count = (UInt16)(((0xffff & buff[5]) << 8) + buff[4]);
        }

    }

    // 这部分数据从组光标中得到(ResourceType = 12)
    struct CURSOR_DIRECTORY
    {
        public byte Width;
        public byte Height;
        public byte ColorCount;
        public byte Reserved;
        public UInt16 Planes;
        public UInt16 BitCount;
        public UInt32 BytesInRes;
        public UInt16 CursorID;  //ID
        public CURSOR_DIRECTORY(byte[] buff, int offset)
        {
            Width = buff[offset];
            Height = buff[offset + 1];
            ColorCount = buff[offset + 2];
            Reserved = buff[offset + 3];
            Planes = (UInt16)(((0xffff & buff[offset + 5]) << 8) + buff[offset + 4]);
            BitCount = (UInt16)(((0xffff & buff[offset + 7]) << 8) + buff[offset + 6]);
            BytesInRes = (0xffffffff & buff[offset + 8]) +
                        ((0xffffffff & buff[offset + 9]) << 8) +
                        ((0xffffffff & buff[offset + 10]) << 16) +
                        ((0xffffffff & buff[offset + 11]) << 24);
            CursorID = (UInt16)(((0xffff & buff[offset + 13]) << 8) + buff[offset + 12]);
        }  
    }

    // 这部分数据从光标中得到(ResourceType = 1)
    struct CursorComponent
    {
        public UInt16 HotSpotX;
        public UInt16 HotSpotY;
    }

    // 光标文件头需要从光标资源头转化而来
    struct CURSOR_FILE_HEADER
    {
        public UInt16 Reserved;
        public UInt16 Type;
        public UInt16 Count;

        public CURSOR_FILE_HEADER(CURSOR_HEADER header)
        {
            Reserved = header.Reserved;
            Type = header.Type;
            Count = 1;
        }

        public byte[] ToBytes()
        {
            byte[] ret = new byte[6];
            ret[0] = (byte)(Reserved & 0xff);
            ret[1] = (byte)((Reserved >> 8) & 0xff);
            ret[2] = (byte)(Type & 0xff);
            ret[3] = (byte)((Type >> 8) & 0xff);
            ret[4] = (byte)(Count & 0xff);
            ret[5] = (byte)((Count >> 8) & 0xff);
            return ret;
        }
    }

    struct CURSOR_INFO_HEADER
    {
        public byte Width;
        public byte Height;
        public byte ColorCount;
        public byte Reserved;
        public UInt16 Planes;
        public UInt16 BitCount;
        public UInt32 BytesInRes;
        public UInt32 ImageOffset;  // 好像固定是24

        public CURSOR_INFO_HEADER(CURSOR_DIRECTORY dir)
        {
            Width = dir.Width;
            Height = dir.Height;
            ColorCount = dir.ColorCount;
            Reserved = dir.Reserved;
            Planes = dir.Planes;
            BitCount = dir.BitCount;
            BytesInRes = dir.BytesInRes;
            ImageOffset = 24;
        }

        public byte[] ToBytes()
        {
            byte[] temp = new byte[16];
            temp[0] = Width;
            temp[1] = Height;
            temp[2] = ColorCount;
            temp[3] = Reserved;
            temp[4] = (byte)(Planes & 0xff);
            temp[5] = (byte)((Planes >> 8) & 0xff);
            temp[6] = (byte)(BitCount & 0xff);
            temp[7] = (byte)((BitCount >> 8) & 0xff);
            temp[8] = (byte)(BytesInRes & 0xff);
            temp[9] = (byte)((BytesInRes >> 8) & 0xff);
            temp[10] = (byte)((BytesInRes >> 16) & 0xff);
            temp[11] = (byte)((BytesInRes >> 24) & 0xff);
            temp[12] = (byte)(ImageOffset & 0xff);
            temp[13] = (byte)((ImageOffset >> 8) & 0xff);
            temp[14] = (byte)((ImageOffset >> 16) & 0xff);
            temp[15] = (byte)((ImageOffset >> 24) & 0xff);

            return temp;
        }    
    }

    #endregion

    #region BitMap
    class RESOURCE_BITMAP
    {
        public BITMAP_HEADER BitMapHeader;
        public BITMAP_INFO BitMapInfo;
        public RGBQuad[] ColorTable;
        public int DataOffset;   // 存下RawData和图像数据偏移，取的时候，只需要根据偏移复制就可以了，节约内存
        private byte[] RawData;

        public RESOURCE_BITMAP(byte[] buff)
        {
            RawData = buff;
            BitMapInfo = new BITMAP_INFO(buff);
            ColorTable = GetColorList(buff, 40, BitMapInfo.BitCount);
            // PE文件中，没有文件头，因此数据部分之前就只有位图信息头和颜色表，一共 40 + 4 * 颜色个数
            DataOffset = 40 + 4 * ColorTable.Length;
            BitMapHeader = new BITMAP_HEADER(buff.Length, DataOffset);
        }

        public void Export(string file)
        {
            Stream st = File.OpenWrite(file);
            byte[] header = BitMapHeader.ToBytes();
            st.Write(header, 0, 14);
            st.Write(RawData, 0, RawData.Length);
            st.Flush();
            st.Close();
            st.Dispose();
        }

        // offset是颜色表在buff中的偏移
        private RGBQuad[] GetColorList(byte[] buff, int offset, int bitCount)
        {
            if (bitCount < 1 || bitCount > 32)
            {
                return new RGBQuad[0];
            }

            if (bitCount == 16 || bitCount == 24 || bitCount == 32)
            {
                return new RGBQuad[0];
            }

            // 只有256色和以下的位图，会有颜色表
            if (bitCount == 1 || bitCount == 2 || bitCount == 4 || bitCount == 8)
            {
                int clrCount = (1 << bitCount); //color count = 2^n

                if (buff.Length < (40 + clrCount * 4))
                {
                    return new RGBQuad[0];
                }

                RGBQuad[] colorList = new RGBQuad[clrCount];
                for (int i = 0; i < clrCount; i++)
                {
                    colorList[i] = new RGBQuad(buff[offset + i * 4 + 2],    //red
                                               buff[offset + i * 4 + 1],    //green
                                               buff[offset + i * 4],        //blue
                                               buff[offset + i * 4 + 3]);   //alpha
                }
                return colorList;
            }
            return new RGBQuad[0];
        }
    }

    struct BITMAP_HEADER
    {
        public UInt16 BitMapType;    // 固定为BM
        public UInt32 FileSize;      // 文件大小，由于PE文件里的资源没有文件头，因此，此处可以是资源大小加文件头大小。
        public UInt16 Reserved1;     // 保留值
        public UInt16 Reserved2;     // 保留值
        public UInt32 Offset;       // 数据起始位置，以字节为单位

        //PE文件里的位图资源没有文件头，因此构造一个文件头用来以文件格式输出
        public BITMAP_HEADER(int size, int offset)
        {
            BitMapType = 0x424D;
            FileSize = (UInt32)(size + 14);
            Reserved1 = 0;
            Reserved2 = 0;
            Offset = (UInt32)offset;  
        }

        public Byte[] ToBytes()
        {
            byte[] temp = new byte[14];
            temp[0] = 0x42;
            temp[1] = 0x4d;
            temp[2] = (byte)(FileSize & 0xff);
            temp[3] = (byte)((FileSize >> 8) & 0xff);
            temp[4] = (byte)((FileSize >> 16) & 0xff);
            temp[5] = (byte)((FileSize >> 24) & 0xff);
            temp[6] = 0;
            temp[7] = 0;
            temp[8] = 0;
            temp[9] = 0;
            temp[10] = (byte)(Offset & 0xff);
            temp[11] = (byte)((Offset >> 8) & 0xff);
            temp[12] = (byte)((Offset >> 16) & 0xff);
            temp[13] = (byte)((Offset >> 24) & 0xff);

            return temp;
        }
    }

    struct BITMAP_INFO
    {
        public UInt32 HeaderSize;
        public UInt32 Width;
        public UInt32 Height;
        public UInt16 Planes;
        public UInt16 BitCount;
        public UInt32 Compression;
        public UInt32 ImageSize;    // 图像数据大小，以字节为单位
        public UInt32 PelsPerMeterX;
        public UInt32 PelsPerMeterY;
        public UInt32 ColorUsed;
        public UInt32 ColorImportant;

        public BITMAP_INFO(byte[] buff)
        {
            HeaderSize = (0xffffffff & buff[0]) +
                        ((0xffffffff & buff[1]) << 8) +
                        ((0xffffffff & buff[2]) << 16) +
                        ((0xffffffff & buff[3]) << 24);
            Width = (0xffffffff & buff[4]) +
                    ((0xffffffff & buff[5]) << 8) +
                    ((0xffffffff & buff[6]) << 16) +
                    ((0xffffffff & buff[7]) << 24);
            Height = (0xffffffff & buff[8]) +
                     ((0xffffffff & buff[9]) << 8) +
                     ((0xffffffff & buff[10]) << 16) +
                     ((0xffffffff & buff[11]) << 24);
            Planes = (UInt16)(((0xffff & buff[13]) << 8) + buff[12]);
            BitCount = (UInt16)(((0xffff & buff[15]) << 8) + buff[14]);
            Compression = (0xffffffff & buff[16]) +
                          ((0xffffffff & buff[17]) << 8) +
                          ((0xffffffff & buff[18]) << 16) +
                          ((0xffffffff & buff[19]) << 24);
            ImageSize =   (0xffffffff & buff[20]) +
                          ((0xffffffff & buff[21]) << 8) +
                          ((0xffffffff & buff[22]) << 16) +
                          ((0xffffffff & buff[23]) << 24);
            PelsPerMeterX = (0xffffffff & buff[24]) +
                            ((0xffffffff & buff[25]) << 8) +
                            ((0xffffffff & buff[26]) << 16) +
                            ((0xffffffff & buff[27]) << 24);
            PelsPerMeterY = (0xffffffff & buff[28]) +
                            ((0xffffffff & buff[29]) << 8) +
                            ((0xffffffff & buff[30]) << 16) +
                            ((0xffffffff & buff[31]) << 24);
            ColorUsed = (0xffffffff & buff[32]) +
                        ((0xffffffff & buff[33]) << 8) +
                        ((0xffffffff & buff[34]) << 16) +
                        ((0xffffffff & buff[35]) << 24);
            ColorImportant = (0xffffffff & buff[32]) +
                             ((0xffffffff & buff[33]) << 8) +
                             ((0xffffffff & buff[34]) << 16) +
                             ((0xffffffff & buff[35]) << 24);
        }
    }

    struct RGBQuad
    {
        public byte Blue;
        public byte Green;
        public byte Red;
        public byte Reserved;

        public RGBQuad(byte red, byte green, byte blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Reserved = 0;
        }

        public RGBQuad(byte red, byte green, byte blue, byte alpha)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Reserved = alpha;
        }

        public RGBQuad(byte[] rgbColor)
        {
            if ((rgbColor != null) && (rgbColor.Length == 4))
            {
                Red = rgbColor[2];
                Green = rgbColor[1];
                Blue = rgbColor[0];
                Reserved = rgbColor[3];
            }
            else
            {
                Red = 0;
                Green = 0;
                Blue = 0;
                Reserved = 0;              
            }
        }
    }

    #endregion

    #region ICON
    // PE文件中的图标结构
    class IMAGE_ICON
    {
        public ICON_HEADER Header;        // 数据在ResourceType = 14中
        public ICON_DIRECTORY Directory;  // 数据在ResourceType = 14中

        public ICON_FILE_HEADER FileHeader;  // 文件头，需要构造
        public ICON_INFO_HEADER InfoHeader;  // 信息块，需要构造
        public byte[] FileBody;   // 资源段中的数据
        private int ID;

        public IMAGE_ICON(byte[] buff, Image_Resource_Directory resourceDir, int iconID)
        {
            ID = iconID;
            FileBody = buff;
            FindHeader(resourceDir);
            FileHeader = new ICON_FILE_HEADER(Header);
            InfoHeader = new ICON_INFO_HEADER(Directory);
        }

        private void FindHeader(Image_Resource_Directory resourceDir)
        {
            int entryCount = resourceDir.NumberOfIdEntries + resourceDir.NumberOfNamedEntries;
            for (int i = 0; i < entryCount; i++)
            {
                Image_Resource_Directory_Entry entry = resourceDir.ImageResourceDirectoryEntries[i];
                if ((entry.OffsetToData & 0x80000000) != 0)
                {
                    Image_Resource_Directory child = (Image_Resource_Directory)entry.ChildEntry;
                    FindHeader(child);
                }
                else
                {
                    Image_Resource_Data_Entry data = (Image_Resource_Data_Entry)entry.ChildEntry;        
                    ICON_HEADER hd = new ICON_HEADER(data.Data);
                    ICON_DIRECTORY[] ds = new ICON_DIRECTORY[hd.Count];
                    for(int j=0;j<ds.Length;j++)
                    {
                        ds[j] = new ICON_DIRECTORY(data.Data, 6 + j*14);
                        if (ds[j].IConID == this.ID)
                        {
                            Header = hd;
                            Directory = ds[j];
                            return;
                        }
                    }
                }
            }
        }

        public void Export(string output)
        {
            Stream outfile = File.Open(output, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            byte[] header = this.FileHeader.ToBytes();
            byte[] infoHeader = this.InfoHeader.ToBytes();
            outfile.Write(header, 0, header.Length);
            outfile.Write(infoHeader, 0, infoHeader.Length);
            outfile.Write(FileBody, 0, FileBody.Length);
            outfile.Flush();
            outfile.Close();
        }
    }

    // 6字节
    struct ICON_HEADER   //此结构体在组图标中，ResourceType=14
    {
        public UInt16 Reserved;
        public UInt16 Type;
        public UInt16 Count;

        public ICON_HEADER(byte[] buff)
        {
            Reserved = (UInt16)(((0xffff & buff[1]) << 8) + buff[0]);
            Type = (UInt16)(((0xffff & buff[3]) << 8) + buff[2]);
            Count = (UInt16)(((0xffff & buff[5]) << 8) + buff[4]);
        }
    }

    // 14 字节
    struct ICON_DIRECTORY  //此结构体在组图标中，ResourceType=14
    {
        public byte Width;
        public byte Height;
        public byte ColorCount;
        public byte Reserved;
        public UInt16 Planes;
        public UInt16 BitCount;
        public UInt32 BytesInRes;
        public UInt16 IConID;  //ID
        public ICON_DIRECTORY(byte[] buff, int offset)
        {
            Width = buff[offset];
            Height = buff[offset + 1];
            ColorCount = buff[offset + 2];
            Reserved = buff[offset + 3];
            Planes = (UInt16)(((0xffff & buff[offset + 5]) << 8) + buff[offset + 4]);
            BitCount = (UInt16)(((0xffff & buff[offset + 7]) << 8) + buff[offset + 6]);
            BytesInRes = (0xffffffff & buff[offset + 8]) +
                        ((0xffffffff & buff[offset + 9]) << 8) +
                        ((0xffffffff & buff[offset + 10]) << 16) +
                        ((0xffffffff & buff[offset + 11]) << 24);
            IConID = (UInt16)(((0xffff & buff[offset + 13]) << 8) + buff[offset + 12]);
        }
    }

    struct ICON_FILE_HEADER  //此结构体在资源数据中不存在，导出成文件时，需要根据资源14里的信息生成
    {
        public UInt16 Reserved;
        public UInt16 Type;
        public UInt16 Count;
        
        public ICON_FILE_HEADER(ICON_HEADER header)
        {
            Reserved = header.Reserved;
            Type = header.Type;
            Count = 1;
        }

        public byte[] ToBytes()
        {
            byte[] ret = new byte[6];
            ret[0] = (byte)(Reserved & 0xff);
            ret[1] = (byte)((Reserved >> 8) & 0xff);
            ret[2] = (byte)(Type & 0xff);
            ret[3] = (byte)((Type >> 8) & 0xff);
            ret[4] = (byte)(Count & 0xff);
            ret[5] = (byte)((Count >> 8) & 0xff);
            return ret;
        }
    }

    struct ICON_INFO_HEADER  //此结构体在资源数据中不存在，导出成文件时，需要根据资源14里的信息生成
    {
        public byte Width;
        public byte Height;
        public byte ColorCount;
        public byte Reserved;
        public UInt16 Planes;
        public UInt16 BitCount;
        public UInt32 BytesInRes;
        public UInt32 ImageOffset;  // 好像固定是20

        public ICON_INFO_HEADER(ICON_DIRECTORY dir)
        {
            Width = dir.Width;
            Height = dir.Height;
            ColorCount = dir.ColorCount;
            Reserved = dir.Reserved;
            Planes = dir.Planes;
            BitCount = dir.BitCount;
            BytesInRes = dir.BytesInRes;
            ImageOffset = 22;
        }

        public byte[] ToBytes()
        {
            byte[] temp = new byte[16];
            temp[0] = Width;
            temp[1] = Height;
            temp[2] = ColorCount;
            temp[3] = Reserved;
            temp[4] = (byte)(Planes & 0xff);
            temp[5] = (byte)((Planes >> 8) & 0xff);
            temp[6] = (byte)(BitCount & 0xff);
            temp[7] = (byte)((BitCount >> 8) & 0xff);
            temp[8] = (byte)(BytesInRes & 0xff);
            temp[9] = (byte)((BytesInRes >> 8) & 0xff);
            temp[10] = (byte)((BytesInRes >> 16) & 0xff);
            temp[11] = (byte)((BytesInRes >> 24) & 0xff);
            temp[12] = (byte)(ImageOffset & 0xff);
            temp[13] = (byte)((ImageOffset >> 8) & 0xff);
            temp[14] = (byte)((ImageOffset >> 16) & 0xff);
            temp[15] = (byte)((ImageOffset >> 24) & 0xff);

            return temp;
        }
    }

    #endregion

    #region String Table

    // 字符串资源是一个字符串表，包含16个字符串
    // 每个字符串结构为[Length][Length个字符]，Length部分占2字节，字符部分每个字符占2字节。
    // 如果Length为0，则字符部分不占字节，如果Length不为0，则字符部分占 2 * Length字节。
    // 比如，如果某个字符串表，包含14个长度为0的字符串和2个长度为18的字符串，则该字符串表总字节数：14 * 2 + 2 * (18 + 2) = 68字节
    struct Resource_String_Table
    {
        public Resource_String[] StringTable;
        public Resource_String_Table(byte[] buff)
        {
            StringTable = new Resource_String[16];
            int offset = 0;
            for (int i = 0; i < 16; i++)
            {
                UInt16 len = (UInt16)(((0xffff & buff[offset + 1]) << 8) + buff[offset]);
                StringTable[i].Length = len;
                offset += 2;
                if (len > 0)
                {
                    StringTable[i].Data = new byte[2 * len];
                    for (int j = 0; j < 2 * len; j++)
                    {
                        StringTable[i].Data[j] = buff[offset];
                        offset++;
                    }
                }
            }
        }
    }

    struct Resource_String  // Resource Type = 6
    {
        public UInt16 Length;
        public byte[] Data;

        public override string ToString()
        {
            if (Length == 0)
            {
                return "";
            }
            else
            {
                char[] ret = new Char[Length];
                for(int i=0;i<Length;i++)
                {
                    ret[i] = (char)(((0xffff & Data[2 * i + 1]) << 8) + Data[2 * i]);
                }

                string output = new string(ret);
                return new string(ret);
            }
        }
    }
    #endregion

    #region Version Info
    struct VS_VERSION_INFO
    {
        public UInt16 Length;
        public UInt16 ValueLength;
        public UInt16 Type;  // 0: Binary, 1: String, Information Type，不起作用
        public Char[] Key;   // 长度为16，此值永远是：VS_VERSION_INFO
        public UInt16 Padding;  
        public VS_FIXEDFILEINFO Value;
        public VersionFileInfo[] Children;  // 由于数组成员类型不一样，因此，用一个{类型，数值}的结构体数组来实现

        public VS_VERSION_INFO(byte[] buff)
        {
            Length = (UInt16)(((0xffff & buff[1]) << 8) + buff[0]);
            ValueLength = (UInt16)(((0xffff & buff[3]) << 8) + buff[2]);
            Type = (UInt16)(((0xffff & buff[5]) << 8) + buff[4]);
            Key = new char[16];
            for (int i = 0; i < Key.Length; i++)
            {
                Key[i] = (char)(((0xffff & buff[2*i + 7]) << 8) + buff[2*i + 6]);
            }
            Padding = (UInt16)(((0xffff & buff[39]) << 8) + buff[38]);
            Value = new VS_FIXEDFILEINFO(buff, 40);  // 该结构体共52字节

            // 把StringFileInfo和VarFileInfo全部按顺序存入链表
            Link child = new Link();
            Link currentNode = child;
            int currentPos = 92;
            while (currentPos < buff.Length)
            {
                UInt16 fileInfoType = (char)(((0xffff & buff[currentPos + 7]) << 8) + buff[currentPos + 6]);
                // 接下来是以 StringFileInfo 或者 VarFileInfo 为开始标志的数据部分，这里简略判断第一字符是否是以“S”开头。
                if (fileInfoType == 'S')
                {
                    StringFileInfo sfi = new StringFileInfo(buff, currentPos);
                    currentNode.Value = sfi;
                    currentNode.NodeType = 1;
                    currentNode.Next = new Link();
                    currentNode = currentNode.Next;
                    child.NodeCount += 1;
                    currentPos += sfi.Length;
                    if ((currentPos % 4) != 0)
                    {
                        currentPos += 2; // 按4字节对齐
                    }
                }
                else
                {
                    VarFileInfo vfi = new VarFileInfo(buff, currentPos);
                    currentNode.Value = vfi;
                    currentNode.NodeType = 0;
                    child.NodeCount += 1;
                    currentNode.Next = new Link();
                    currentNode = currentNode.Next;
                    currentPos += vfi.Length;
                    if ((currentPos % 4) != 0)
                    {
                        currentPos += 2; // 按4字节对齐
                    }
                } 
            }

            // 把存入链表的StringFileInfo和VarFileInfo按顺序拷贝到数组里
            Children = null;
            if (child.NodeCount > 0)
            {
                Children = new VersionFileInfo[child.NodeCount];
                currentNode = child;
                for (int i = 0; i < Children.Length; i++)
                {
                    Children[i].InfoType = currentNode.NodeType;
                    Children[i].Value = currentNode.Value;
                    currentNode = currentNode.Next;
                }
            }
        }

        public void Export(string file)
        {
            string temp = "VS_VERSION_INFO\r\n";
            temp += "Length :" + Length.ToString() + "\r\n";
            temp += "ValueLength :" + ValueLength.ToString() + "\r\n";
            temp += "Type :" + Type.ToString() + "\r\n";
            temp += "Key :" + new String(Key) + "\r\n";
            temp += "Padding :" + Padding.ToString() + "\r\n";
            temp += "\r\n";
            File.WriteAllText(file, temp);
            Value.Export(file);
            for (int i=0;i<Children.Length;i++)
            {
                if (Children[i].InfoType == 1)
                {
                    ((StringFileInfo)Children[i].Value).Export(file);
                }
                else
                {
                    ((VarFileInfo)Children[i].Value).Export(file);
                }
            }
        }
    }
    struct VS_FIXEDFILEINFO
    {
        public UInt32 Signature;       
        public UInt32 StrucVersion;    
        public UInt32 FileVersionMS;   
        public UInt32 FileVersionLS;   
        public UInt32 ProductVersionMS;
        public UInt32 ProductVersionLS;
        public UInt32 FileFlagsMask;   
        public UInt32 FileFlags;       
        public UInt32 FileOS;          
        public UInt32 FileType;        
        public UInt32 FileSubtype;     
        public UInt32 FileDateMS;      
        public UInt32 FileDateLS;
        public VS_FIXEDFILEINFO(byte[] buff, long offset)
        {
            Signature = (0xffffffff & buff[offset]) +
                        ((0xffffffff & buff[offset + 1]) << 8) +
                        ((0xffffffff & buff[offset + 2]) << 16) +
                        ((0xffffffff & buff[offset + 3]) << 24);
            StrucVersion = (0xffffffff & buff[offset + 4]) +
                        ((0xffffffff & buff[offset + 5]) << 8) +
                        ((0xffffffff & buff[offset + 6]) << 16) +
                        ((0xffffffff & buff[offset + 7]) << 24);
            FileVersionMS = (0xffffffff & buff[offset + 8]) +
                        ((0xffffffff & buff[offset + 9]) << 8) +
                        ((0xffffffff & buff[offset + 10]) << 16) +
                        ((0xffffffff & buff[offset + 11]) << 24);
            FileVersionLS = (0xffffffff & buff[offset + 12]) +
                        ((0xffffffff & buff[offset + 13]) << 8) +
                        ((0xffffffff & buff[offset + 14]) << 16) +
                        ((0xffffffff & buff[offset + 15]) << 24);
            ProductVersionMS = (0xffffffff & buff[offset + 16]) +
                        ((0xffffffff & buff[offset + 17]) << 8) +
                        ((0xffffffff & buff[offset + 18]) << 16) +
                        ((0xffffffff & buff[offset + 19]) << 24);
            ProductVersionLS = (0xffffffff & buff[offset + 20]) +
                        ((0xffffffff & buff[offset + 21]) << 8) +
                        ((0xffffffff & buff[offset + 22]) << 16) +
                        ((0xffffffff & buff[offset + 23]) << 24);
            FileFlagsMask = (0xffffffff & buff[offset + 24]) +
                        ((0xffffffff & buff[offset + 25]) << 8) +
                        ((0xffffffff & buff[offset + 26]) << 16) +
                        ((0xffffffff & buff[offset + 27]) << 24);
            FileFlags = (0xffffffff & buff[offset + 28]) +
                        ((0xffffffff & buff[offset + 29]) << 8) +
                        ((0xffffffff & buff[offset + 30]) << 16) +
                        ((0xffffffff & buff[offset + 31]) << 24);
            FileOS = (0xffffffff & buff[offset + 32]) +
                        ((0xffffffff & buff[offset + 33]) << 8) +
                        ((0xffffffff & buff[offset + 34]) << 16) +
                        ((0xffffffff & buff[offset + 35]) << 24);
            FileType = (0xffffffff & buff[offset + 36]) +
                        ((0xffffffff & buff[offset + 37]) << 8) +
                        ((0xffffffff & buff[offset + 38]) << 16) +
                        ((0xffffffff & buff[offset + 39]) << 24);
            FileSubtype = (0xffffffff & buff[offset + 40]) +
                        ((0xffffffff & buff[offset + 41]) << 8) +
                        ((0xffffffff & buff[offset + 42]) << 16) +
                        ((0xffffffff & buff[offset + 43]) << 24);
            FileDateMS = (0xffffffff & buff[offset + 44]) +
                        ((0xffffffff & buff[offset + 45]) << 8) +
                        ((0xffffffff & buff[offset + 46]) << 16) +
                        ((0xffffffff & buff[offset + 47]) << 24);
            FileDateLS = (0xffffffff & buff[offset + 48]) +
                        ((0xffffffff & buff[offset + 49]) << 8) +
                        ((0xffffffff & buff[offset + 50]) << 16) +
                        ((0xffffffff & buff[offset + 51]) << 24);
        }

        public void Export(string file)
        {
            string temp = "VS_FIXEDFILEINFO\r\n";
            temp += "Signature :0x" + Signature.ToString("x8") + "\r\n";
            temp += "StrucVersion :0x" + StrucVersion.ToString("x8") + "\r\n";
            temp += "FileVersionMS :0x" + FileVersionMS.ToString("x8") + "\r\n";
            temp += "FileVersionLS :0x" + FileVersionLS.ToString("x8") + "\r\n";
            temp += "ProductVersionMS :0x" + ProductVersionMS.ToString("x8") + "\r\n";
            temp += "ProductVersionLS :0x" + ProductVersionLS.ToString("x8") + "\r\n";
            temp += "FileFlagsMask :0x" + FileFlagsMask.ToString("x8") + "\r\n";
            temp += "FileFlags :0x" + FileFlags.ToString("x8") + "\r\n";
            temp += "FileOS :0x" + FileOS.ToString("x8") + "\r\n";
            temp += "FileType :0x" + FileType.ToString("x8") + "\r\n";
            temp += "FileSubtype :0x" + FileSubtype.ToString("x8") + "\r\n";
            temp += "FileDateMS :0x" + FileDateMS.ToString("x8") + "\r\n";
            temp += "FileDateLS :0x" + FileDateLS.ToString("x8") + "\r\n";
            temp += "\r\n";
            File.AppendAllText(file, temp);
        }
    }
    struct StringFileInfo
    {
        public UInt16 Length;
        public UInt16 ValueLength;
        public UInt16 Type;  // 貌似永远为1
        public char[] Key;  // 此处总是为: StringFileInfo
        public UInt16 Padding;
        public StringTable[] Children;

        public StringFileInfo(byte[] buff, int offset)
        {
            Length = (UInt16)(((0xffff & buff[offset + 1]) << 8) + buff[offset]);
            ValueLength = (UInt16)(((0xffff & buff[offset + 3]) << 8) + buff[offset + 2]);
            Type = (UInt16)(((0xffff & buff[offset + 5]) << 8) + buff[offset + 4]);
            Key = new Char[14];
            for (int i = 0; i < 14; i++)
            {
                Key[i] = (char)(((0xffff & buff[offset + 7 + 2 * i]) << 8) + buff[offset + 6 + i * 2]);
            }
            Padding = (UInt16)(((0xffff & buff[offset + 35]) << 8) + buff[offset + 34]);
            int pos = offset + 36;
            int tableCount = 0;
            while (pos < offset + Length)  // 计算一共有多少个StringTable，貌似永远是两个
            {
                tableCount++;
                int len = (UInt16)(((0xffff & buff[pos + 1]) << 8) + buff[pos]);
                if (len == 0) break;
                pos += len;
                if ((pos % 4) > 0)
                {
                    pos += 2;// 每个Table按4字节对齐
                }
            }
            Children = new StringTable[tableCount];
            pos = offset + 36;
            for (int i = 0; i < Children.Length; i++)
            {
                Children[i] = new StringTable(buff, pos);
                int len = (UInt16)(((0xffff & buff[pos + 1]) << 8) + buff[pos]);
                pos += len;
                if ((pos % 4) > 0)
                {
                    pos += 2;// 每个Table按4字节对齐
                }
            }
        }

        public void Export(string file)
        {
            string temp = "String File Info \r\n";
            temp += "Length :" + Length.ToString() + "\r\n";
            temp += "ValueLength :" + ValueLength.ToString() + "\r\n";
            temp += "Type :" + Type.ToString() + "\r\n";
            temp += "Key :" + new String(Key) + "\r\n";
            temp += "Padding :" + Padding.ToString() + "\r\n";
            temp += "\r\n";
            File.AppendAllText(file, temp);
            for (int i = 0; i < Children.Length; i++)
            {
                Children[i].Export(file);
            }
        }
    }
    struct StringTable
    {
        public UInt16 Length;
        public UInt16 ValueLength;
        public UInt16 Type;
        public char[] Key;  // 16字节，8个char，表示语言ID，比如："040904B0"
        public UInt16 Padding;
        public VerString[] Children;
        public StringTable(byte[] buff, int offset)
        {
            Length = (UInt16)(((0xffff & buff[offset + 1]) << 8) + buff[offset]);
            ValueLength = (UInt16)(((0xffff & buff[offset + 3]) << 8) + buff[offset + 2]);
            Type = (UInt16)(((0xffff & buff[offset + 5]) << 8) + buff[offset + 4]);
            Key = new char[8];
            for (int i = 0; i < 8; i++)
            {
                Key[i] = (char)(((0xffff & buff[offset + 7 + 2*i]) << 8) + buff[offset + 6 + 2*i]);
            }
            Padding = (UInt16)(((0xffff & buff[offset + 23]) << 8) + buff[offset + 22]);
            Link child = new Link();
            Link currentNode = child;
            int start = offset + 24;
            int currentPos = offset + 24;
            while (currentPos < offset + Length)
            {
                VerString vString = new VerString(buff, currentPos);
                currentNode.Value = vString;
                currentNode.Next = new Link();
                currentNode = currentNode.Next;
                child.NodeCount += 1;
                currentPos += vString.Length;
                if ((currentPos % 4) != 0)
                {
                    currentPos += 2;
                }               
            }
            Children = null;
            if (child.NodeCount > 0)
            {
                Children = new VerString[child.NodeCount];
                currentNode = child;
                for (int i = 0; i < child.NodeCount; i++)
                {
                    Children[i] = (VerString)currentNode.Value;
                    currentNode = currentNode.Next;
                }
            }
        }

        public void Export(string file)
        {
            string temp = "String Table\r\n";
            temp += "Length :" + Length.ToString() + "\r\n";
            temp += "ValueLength :" + ValueLength.ToString() + "\r\n";
            temp += "Type :" + Type.ToString() + "\r\n";
            temp += "Key :" + new String(Key) + "\r\n";
            temp += "Padding :" + Padding.ToString() + "\r\n";
            File.AppendAllText(file, temp);
            for (int i = 0; i < Children.Length; i++)
            {
                Children[i].Export(file);
            }
            File.AppendAllText(file, "------------------------------------------------------\r\n");
        }
    }
    struct VerString
    {
        public UInt16 Length;
        public UInt16 ValueLength;
        public UInt16 Type;
        public char[] Key;
        public UInt16 Padding;  // 因为 Key 是以 \0 结尾的字符串，因此其后必然有一个空字节，所以这里Padding必然存在并且等于0
        public char[] Value;

        public VerString(byte[] buff, int offset)
        {
            Length = (UInt16)(((0xffff & buff[offset + 1]) << 8) + buff[offset]);
            ValueLength = (UInt16)(((0xffff & buff[offset + 3]) << 8) + buff[offset + 2]);
            Type = (UInt16)(((0xffff & buff[offset + 5]) << 8) + buff[offset + 4]);
            int currentPos = offset + 6;
            int keyLen = 0;
            for (int i = offset + 6; i < offset + Length; i += 2)
            {
                UInt16 temp = (UInt16)(((0xffff & buff[i + 1]) << 8) + buff[i]);
                if (temp == 0)
                {
                    keyLen = (i - currentPos) / 2;
                    break;
                }
            }
            Key = new Char[keyLen];
            
            for (int i = 0; i < keyLen; i++)
            {
                Key[i] = (char)(((0xffff & buff[currentPos + 1 + 2 * i]) << 8) + buff[currentPos + 2 * i]);
            }
            currentPos += 2 * keyLen;
            Padding = (UInt16)(((0xffff & buff[currentPos + 1]) << 8) + buff[currentPos]); ;
            currentPos += 2;
            Value = new char[ValueLength];
            for (int i = 0; i < ValueLength; i++)
            {
                Value[i] = (char)(((0xffff & buff[currentPos + 1 + 2 * i]) << 8) + buff[currentPos + 2 * i]);
            }
        }

        public void Export(string file)
        {
            string temp = "-------------------------------------------\r\n";
            temp += "Length :" + Length.ToString() + "\r\n";
            temp += "ValueLength :" + ValueLength.ToString() + "\r\n";
            temp += "Type :" + Type.ToString() + "\r\n";
            temp += "Key :" + new String(Key) + "\r\n";
            temp += "Padding :" + Padding.ToString() + "\r\n";
            temp += "Value :" + new string(Value) + "\r\n";
            File.AppendAllText(file, temp);
        }
    }
    struct VarFileInfo
    {
        public UInt16 Length;
        public UInt16 ValueLength;
        public UInt16 Type;
        public char[] Key;
        public UInt32 Padding;  // 因为Key为11个字符，为了数据部分4字节对齐，此处Padding为4字节
        public Var[] Children;

        public VarFileInfo(byte[] buff, int offset)
        {
            Length = (UInt16)(((0xffff & buff[offset + 1]) << 8) + buff[offset]);
            ValueLength = (UInt16)(((0xffff & buff[offset + 3]) << 8) + buff[offset + 2]);
            Type = (UInt16)(((0xffff & buff[offset + 5]) << 8) + buff[offset + 4]);
            Key = new Char[11];
            for (int i = 0; i < 11; i++)
            {
                Key[i] = (char)(((0xffff & buff[offset + 7 + 2 * i]) << 8) + buff[offset + 6 + i * 2]);
            }
            Padding = (0xffffffff & buff[offset + 28]) +
                        ((0xffffffff & buff[offset + 29]) << 8) +
                        ((0xffffffff & buff[offset + 30]) << 16) +
                        ((0xffffffff & buff[offset + 31]) << 24);
            int currentPos = offset + 32;
            Link child = new Link();
            Link currentNode = child;
            
            while (currentPos < offset + Length)
            {
                Var var = new Var(buff, currentPos);
                currentNode.Value = var;
                currentNode.Next = new Link();
                currentNode = currentNode.Next;
                child.NodeCount ++;
                currentPos += var.Length;
                if ((currentPos % 4) != 0)
                {
                    currentPos += 2;
                }
            }
            Children = null;
            if (child.NodeCount > 0)
            {
                Children = new Var[child.NodeCount];
                currentNode = child;
                for (int i = 0; i < Children.Length; i++)
                {
                    Children[i] = (Var)currentNode.Value;
                    currentNode = currentNode.Next;
                }
            }
        }

        public void Export(string file)
        {
            string temp = "\r\nVar File Info\r\n";
            temp += "Length :" + Length.ToString() + "\r\n";
            temp += "ValueLength :" + ValueLength.ToString() + "\r\n";
            temp += "Type :" + Type.ToString() + "\r\n";
            temp += "Key :" + new String(Key) + "\r\n";
            temp += "Padding :" + Padding.ToString() + "\r\n";
            File.AppendAllText(file, temp);
            for (int i = 0; i < Children.Length; i++)
            {
                Children[i].Export(file);
            }
        }
    }
    struct Var
    {
        public UInt16 Length;
        public UInt16 ValueLength;
        public UInt16 Type;
        public char[] Key;
        public UInt32 Padding; 
        public UInt32[] Value;  // 记录语言的ID
        public Var(byte[] buff, int offset)
        {
            Length = (UInt16)(((0xffff & buff[offset + 1]) << 8) + buff[offset]);
            ValueLength = (UInt16)(((0xffff & buff[offset + 3]) << 8) + buff[offset + 2]);
            Type = (UInt16)(((0xffff & buff[offset + 5]) << 8) + buff[offset + 4]);
            Key = new Char[11];
            for (int i = 0; i < 11; i++)
            {
                Key[i] = (char)(((0xffff & buff[offset + 7 + 2 * i]) << 8) + buff[offset + 6 + i * 2]);
            }
            Padding = (0xffffffff & buff[offset + 28]) +
                        ((0xffffffff & buff[offset + 29]) << 8) +
                        ((0xffffffff & buff[offset + 30]) << 16) +
                        ((0xffffffff & buff[offset + 31]) << 24); 
            int currentPos = offset + 32;
            int valueCount = ValueLength / 4;
            Value = null;      
            if (valueCount > 0)
            {
                Value = new UInt32[valueCount];
                for (int i = 0; i < Value.Length; i++)
                {
                    Value[i] = (0xffffffff & buff[currentPos + 4 * i]) +
                               ((0xffffffff & buff[currentPos + 4 * i + 1]) << 8) +
                               ((0xffffffff & buff[currentPos + 4 * i + 2]) << 16) +
                               ((0xffffffff & buff[currentPos + 4 * i + 3]) << 24); 
                }
            }
        }

        public void Export(string file)
        {
            string temp = "Var File Info\r\n";
            temp += "Length :" + Length.ToString() + "\r\n";
            temp += "ValueLength :" + ValueLength.ToString() + "\r\n";
            temp += "Type :" + Type.ToString() + "\r\n";
            temp += "Key :" + new String(Key) + "\r\n";
            temp += "Padding :" + Padding.ToString() + "\r\n";
            temp += "Values: ";
            for (int i = 0; i < Value.Length; i++)
            {
                temp += Value[i].ToString("x8");
                if (i == (Value.Length - 1))
                {
                    temp += "\r\n";
                }
                else
                {
                    temp += ",";
                }
            }
            File.AppendAllText(file, temp);
        }
    }
    struct VersionFileInfo
    {
        public int InfoType;
        public Object Value;
    }
    #endregion

    #region GroupCursor
    struct GROUP_CURSOR
    {
        public CURSOR_HEADER Header;
        public CURSOR_DIRECTORY[] Directories;

        public GROUP_CURSOR(byte[] buff)
        {
            Header = new CURSOR_HEADER(buff);
            Directories = new CURSOR_DIRECTORY[Header.Count];
            for (int i = 0; i < Directories.Length; i++)
            {
                Directories[i] = new CURSOR_DIRECTORY(buff, 6 + i * 14);
            }
        }

        public void Export(string output)
        {
            string temp = "Header\r\n";
            temp += "    Reserved: " + Header.Reserved + "\r\n";
            temp += "    Type: " + Header.Type + ", Type = 2 is Curor.\r\n";
            temp += "    Count: " + Header.Count + "\r\n\r\n";
            for (int i = 0; i < Directories.Length; i++)
            {
                temp += "Cursor " + (i + 1).ToString() + "\r\n";
                temp += "    Width: " + Directories[i].Width + "\r\n";
                temp += "    Height: " + Directories[i].Height + "\r\n";
                temp += "    ColorCount: " + Directories[i].ColorCount + "\r\n";
                temp += "    Reserved: " + Directories[i].Reserved + "\r\n";
                temp += "    Planes: " + Directories[i].Planes + "\r\n";
                temp += "    BitCount: " + Directories[i].BitCount + "\r\n";
                temp += "    BytesInRes: " + Directories[i].BytesInRes + "\r\n";
                temp += "    CursorID: " + Directories[i].CursorID + "\r\n";
                temp += "    *******************************************************\r\n";
            }
            File.WriteAllText(output, temp);
        }
    }
    #endregion

    #region GroupIcon

    struct GROUP_ICON
    {
        public ICON_HEADER Header;
        public ICON_DIRECTORY[] Directories;

        public GROUP_ICON(byte[] buff)
        {
            Header = new ICON_HEADER(buff);
            Directories = new ICON_DIRECTORY[Header.Count];
            for (int i = 0; i < Directories.Length; i++)
            {
                Directories[i] = new ICON_DIRECTORY(buff, 6 + i * 14);
            }
        }

        public void Export(string output)
        {
            string temp = "Header\r\n";
            temp += "    Reserved: " + Header.Reserved + "\r\n";
            temp += "    Type: " + Header.Type + ", Type = 1 is ICon.\r\n";
            temp += "    Count: " + Header.Count + "\r\n\r\n";
            for (int i = 0; i < Directories.Length; i++)
            {
                temp += "ICon " + (i+1).ToString() + "\r\n";
                temp += "    Width: " + Directories[i].Width + "\r\n";
                temp += "    Height: " + Directories[i].Height + "\r\n";
                temp += "    ColorCount: " + Directories[i].ColorCount + "\r\n";
                temp += "    Reserved: " + Directories[i].Reserved + "\r\n";
                temp += "    Planes: " + Directories[i].Planes + "\r\n";
                temp += "    BitCount: " + Directories[i].BitCount + "\r\n";
                temp += "    BytesInRes: " + Directories[i].BytesInRes + "\r\n";
                temp += "    IConID: " + Directories[i].IConID + "\r\n";
                temp += "    *******************************************************\r\n";
            }
            File.WriteAllText(output, temp);
        }
    }

    #endregion

    #region Resource Type
    enum ResourceType
    {
        RT_CURSOR = 1,   // 光标
        RT_BITMAP = 2,   // 位图
        RT_ICON = 3,     // 图标
        RT_MENU = 4,     // 菜单
        RT_DIALOG = 5,   // 对话框
        RT_STRING = 6,   // 字符串
        RT_FONTDIR = 7,  // 字体目录
        RT_FONT = 8,           // 字体
        RT_ACCELERATOR = 9,    // 快捷键
        RT_RCDATA = 10,   // 自定义
        RT_MESSAGETABLE = 11,  // 消息表
        RT_GROUP_CURSOR = 12,   // 光标组
        RT_GROUP_ICON = 14,  // 图标组
        RT_VERSION = 16, // 版本信息 
        RT_DLGINCLUDE = 17,  // 包含资源文件，字符串格式
        RT_PLUGPLAY = 19,  // Plug and Play
        RT_VXD = 20,       // VXD
        RT_ANICURSOR = 21,  // 动画光标
        RT_ANIICON = 22,    // 动画图标
        RT_HTML = 23,     // HTML文档
        RT_MANIFEST = 24    // Manifest
    }
    #endregion

    #region Helper Class
    class Link
    {
        public int NodeType = 0;  // 使用这个成员是为了对Object对象进行准确拆箱
        public int NodeCount = 0;
        public Link Next = null;
        public Object Value = null;
    }
    #endregion
}
