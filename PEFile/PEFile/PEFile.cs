using System;
using System.IO;

namespace PEFile
{
    // 这个类创建的对象不是镜像文件的实际数据，仅用来记录一些关键性数据，供全局各结构体的构造函数使用
    class PEFileInfo
    {
        public Stream pFileStream;
        public long ImageBase;
        public string FileType;
        public UInt16 Architecture;
        public long SectionVirtualAddress;
    }

    class PEFile
    {
        public bool HasDOSMZHeader = true;
        public bool HasPESignature = true;
        public bool HasOptionalHeader = true;
        public PEFileInfo PeFileInfo;

        public String FileName;
        public long FileSize;
        public string FileExtenstion;
        public UInt16 Architecture;  // 64位PE文件该值为 0x20b, 32位为0x10b，rom文件为0x107

        public IMAGE_DOS_HEADER ImageDosHeader;
        public IMAGE_IMPORT_DESCRIPTOR ImageImportDescriptor;
        public IMAGE_SECTION_HEADER[] ImageSectionHeaders;
        public REAL_MODE_PROGRAM RealModeProgram;
        public Section[] Sections;
        public Certificate_Directory CertificateDirectory;

        private Stream pFileStream;
        private int pSectionCount;

        public PEFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                long streamCurrsor = 0;
                FileName = fileName;
                pFileStream = File.Open(fileName, FileMode.Open, FileAccess.Read);
                FileSize = pFileStream.Length;
                FileExtenstion = fileName.Substring(fileName.LastIndexOf('.'));

                if (!FileExtenstion.ToLower().Equals(".exe") &&
                   !FileExtenstion.ToLower().Equals(".dll") &&
                   !FileExtenstion.ToLower().Equals(".sys") &&
                   !FileExtenstion.ToLower().Equals(".ocx") &&
                   !FileExtenstion.ToLower().Equals(".com"))
                {
                    throw new ArgumentException("Not supported file extension.");
                }

                PeFileInfo = new PEFileInfo();
                PeFileInfo.pFileStream = pFileStream;
                PeFileInfo.FileType = FileExtenstion;

                //检查是否有DOS MZ头部
                byte[] buff = new byte[2];
                int len = pFileStream.Read(buff, 0, 2);
                if ((buff[0] != 'M') && (buff[1] != 'Z'))
                {
                    HasDOSMZHeader = false;
                }

                // 获取 Image_Dos_Header
                pFileStream.Position = 0;
                if (HasDOSMZHeader)
                {
                    buff = new byte[64];
                    len = pFileStream.Read(buff, 0, 64);
                    ImageDosHeader = new IMAGE_DOS_HEADER(buff);

                    // 获取 REAL_MODE_PROGRAM
                    int length = (int)ImageDosHeader.e_lfanew - 64;
                    buff = new byte[length];
                    len = pFileStream.Read(buff, 0, length);
                    RealModeProgram = new REAL_MODE_PROGRAM(buff, length);

                    // 根据是否有MZ头定位文件头起始位置
                    pFileStream.Position = ImageDosHeader.e_lfanew;
                }

                // 检查是否有PE标志
                buff = new byte[4];
                len = pFileStream.Read(buff, 0, 4);
                pFileStream.Position -= 4;
                if ((buff[0] != 'P') && (buff[1] != 'E') && (buff[2] != 0) && (buff[3] != 0))
                {
                    throw new FormatException("Target file is not PE file.");
                }

                streamCurrsor = pFileStream.Position;
                // 检测镜像是64位还是32位
                buff = new byte[2];
                pFileStream.Position += 24;
                len = pFileStream.Read(buff, 0, 2);
                Architecture = (UInt16)(((0xffff & buff[1]) << 8) + buff[0]);
                PeFileInfo.Architecture = Architecture;

                // 获取 IMAGE_IMPORT_DESCRIPTOR
                pFileStream.Position = streamCurrsor;

                // 因为Optional Header的长度，在64位文件中比32位多16字节
                if (Architecture == 0x20b)
                {
                    buff = new byte[264];
                    len = pFileStream.Read(buff, 0, 264); 
                }
                else if (Architecture == 0x10b)
                {
                    buff = new byte[248];
                    len = pFileStream.Read(buff, 0, 248);
                }
                else
                {
                    throw new FormatException("Not supported image file architecture.");               
                }
                ImageImportDescriptor = new IMAGE_IMPORT_DESCRIPTOR(buff, PeFileInfo);

                // 获取 IMAGE_SECTION_HEADER
                pSectionCount = ImageImportDescriptor.FileHeader.NumberOfSections;
                Sections = new Section[pSectionCount];
                if (pSectionCount > 0)
                {
                    ImageSectionHeaders = new IMAGE_SECTION_HEADER[pSectionCount];

                    //记录SectionHeader开始处位置  
                    streamCurrsor = pFileStream.Position;
                    buff = new byte[40];
                    for (int i = 0; i < pSectionCount; i++)
                    {
                        //实例化段数据会改变Stream光标位置，因此每次循环需要重设光标位置
                        pFileStream.Position = streamCurrsor + i * 40; 
                        len = pFileStream.Read(buff, 0, 40);
                        ImageSectionHeaders[i] = new IMAGE_SECTION_HEADER(buff);

                        //实例化各个段内容，注意，此后需要重定位pFileStream的Position属性。
                        PeFileInfo.SectionVirtualAddress = ImageSectionHeaders[i].VirtualAddress;
                        Sections[i] = new Section(ImageSectionHeaders[i].GetName(), PeFileInfo, ImageSectionHeaders[i].PointerToRawData); 
                    }
                }

                // 分析证书部分
                IMAGE_DATA_DIRECTORY certificateData;
                if (Architecture == 0x20b)
                {
                    // 第5个数据目录是证书数据目录
                    certificateData =((IMAGE_OPTIONAL_HEADER_X64)ImageImportDescriptor.OptionalHeader).DataDirectory[4];
                }
                else
                {
                    // 第5个数据目录是证书数据目录
                    certificateData = ((IMAGE_OPTIONAL_HEADER)ImageImportDescriptor.OptionalHeader).DataDirectory[4];               
                }

                if (certificateData.Size > 0)
                {
                    pFileStream.Position = certificateData.VirtualAddress;
                    buff = new byte[certificateData.Size];
                    len = pFileStream.Read(buff, 0, (int)certificateData.Size);
                    CertificateDirectory = new Certificate_Directory(buff);
                }
            }
        }

        public void Export(string output)
        {
            // 输出Sections
            for (int i = 0; i < this.Sections.Length; i++)
            {
                if (this.Sections[i].SectionInstance != null && this.Sections[i].SectionInstance.GetType().Equals(typeof(Image_Resource_Directory)))
                {
                    ((Image_Resource_Directory)this.Sections[i].SectionInstance).ExportResource(output, 0, 0);
                 }
            }

            if (CertificateDirectory.CertificateCount > 0)
            {
                CertificateDirectory.Export(output);
            }

        }
    }
}
