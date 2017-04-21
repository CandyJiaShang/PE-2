using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace PEFile
{
    public partial class FileForm : Form
    {
        private PEFile PEFile = null;

        public FileForm()
        {
            InitializeComponent();
        }

        private void FileForm_Load(object sender, EventArgs e)
        {
            TxtSummary.Width = this.Width - 40;
            TxtSummary.Height = this.Height - 60;
        }

        private void FileForm_Resize(object sender, EventArgs e)
        {
            TxtSummary.Width = this.Width - 40;
            TxtSummary.Height = this.Height - 60;
        }

        public void OpenPEFile(string filename)
        {
            this.PEFile = new PEFile(filename);
            
        }

        public void DrawSummary()
        {
            string temp = " PE File Summary\r\n\r\n";
            temp += "   File Path: " + PEFile.FileName + "\r\n";
            temp += "   File format: " + PEFile.FileExtenstion + "\r\n";
            temp += "   File Size: " + PEFile.FileSize.ToString() + " bytes\r\n";

            IMAGE_DATA_DIRECTORY[] iddir = null;

            if (PEFile.Architecture == 0x020b)
            {
                temp += "   File Architecture: x64\r\n";
                IMAGE_OPTIONAL_HEADER_X64 oph = (IMAGE_OPTIONAL_HEADER_X64)PEFile.ImageImportDescriptor.OptionalHeader;
                temp += "   Image Base Address: 0x" + oph.ImageBase.ToString("x8") + "\r\n";
                temp += "   Linker Major Version: " + oph.MajorLinkerVersion.ToString() + "\r\n";
                temp += "   Linker Minor Version: " + oph.MinorLinkerVersion.ToString() + "\r\n";
                temp += "   Image Major Version: " + oph.MajorImageVersion.ToString() + "\r\n";
                temp += "   Image Mimor Version: " + oph.MinorImageVersion.ToString() + "\r\n";
                iddir = oph.DataDirectory;
            }
            else
            {
                temp += "   File Archietecture: x86\r\n";
                IMAGE_OPTIONAL_HEADER oph = (IMAGE_OPTIONAL_HEADER)PEFile.ImageImportDescriptor.OptionalHeader;
                temp += "   Image Base Address: 0x" + oph.ImageBase.ToString("x8") + "\r\n";
                temp += "   Linker Major Version: " + oph.MajorLinkerVersion.ToString() + "\r\n";
                temp += "   Linker Minor Version: " + oph.MinorLinkerVersion.ToString() + "\r\n";
                temp += "   Image Major Version: " + oph.MajorImageVersion.ToString() + "\r\n";
                temp += "   Image Mimor Version: " + oph.MinorImageVersion.ToString() + "\r\n";
                iddir = oph.DataDirectory;
            }


            temp += "   Number of Sections: " + PEFile.ImageImportDescriptor.FileHeader.NumberOfSections + "\r\n";
            temp += "   Section Details: \r\n";
            for (int i = 0; i < PEFile.Sections.Length; i++)
            {
                temp += "       " + PEFile.ImageSectionHeaders[i].GetName() + " Section: Size " + PEFile.ImageSectionHeaders[i].SizeOfRawData + " bytes, File Offset 0x" + PEFile.ImageSectionHeaders[i].PointerToRawData.ToString("x8") + "\r\n";
            }

            int usedDataDirectory = 0;
            for (int i = 0; i < iddir.Length; i++)
            {
                if (iddir[i].VirtualAddress != 0)
                {
                    usedDataDirectory += 1;
                }
            }

            temp += "   Used Data Directories: " + usedDataDirectory + "\r\n";
            temp += "   Data Directory Details: \r\n";
            for (int i = 0; i < iddir.Length; i++)
            {
                if (iddir[i].VirtualAddress == 0)
                {
                    temp += "       " + ((DataDirectoryUsage)i).ToString() + ": Not Used.\r\n";
                }
                else
                {
                    temp += "       " + ((DataDirectoryUsage)i).ToString() + ": Size " + iddir[i].Size.ToString() + " bytes, Addess 0x" + iddir[i].VirtualAddress.ToString("x8") + "\r\n" ;                
                }
            }

            temp += "\r\n";
            temp += "Press Ctrl + E to export the details. \r\n";
            TxtSummary.Text = temp;

        }

        public void Export(string output)
        {
            if (PEFile != null)
            {
                PEFile.Export(output);
                MessageBox.Show("输出完成！");
            }
            else
            {
                MessageBox.Show("请先打开需要输出的文件！");
            }
        }
    }
}
