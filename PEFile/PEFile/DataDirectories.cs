using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PEFile
{
    #region Certificate Data
    struct Certificate_Directory
    {
        public Image_Certificate_Header[] Certificates;
        public int CertificateCount;
        public Certificate_Directory(byte[] buff)
        {
            Link head = new Link();
            Link current = head;
            Certificates = null;

            int offset = 0;
            while (offset < buff.Length)
            {
                Image_Certificate_Header ih = new Image_Certificate_Header(buff, offset);
                current.Value = ih;
                head.NodeCount++;
                current.Next = new Link();
                current = current.Next;
                offset += (int)ih.Length;
                if ((offset & 0x8) != 0)
                {
                    offset += 8 - (offset & 0x8);
                }
            }

            // 从链表中复制Certificate到数组里
            CertificateCount = head.NodeCount;
            if (CertificateCount > 0)
            {
                Certificates = new Image_Certificate_Header[CertificateCount];
                current = head;
                for (int i = 0; i < Certificates.Length; i++)
                {
                    Certificates[i] = (Image_Certificate_Header)current.Value;
                    current = current.Next;
                }
            }
        }

        public void Export(string output)
        {
            Directory.CreateDirectory(output + "\\Certificates");
            for (int i = 0; i < Certificates.Length; i++)
            {
                string dir = output + "\\Certificates\\" + (i+1).ToString();
                Directory.CreateDirectory(dir);
                Certificates[i].Export(dir);
            }
        }
    }

    struct Image_Certificate_Header
    {
        public UInt32 Length;
        public UInt16 Revision;
        public UInt16 CertificateType;
        public byte[] Certificate;
        public Image_Certificate_Header(byte[] buff, int offset)
        {
            Length = (0xffffffff & buff[offset]) +
                              ((0xffffffff & buff[offset + 1]) << 8) +
                              ((0xffffffff & buff[offset + 2]) << 16) +
                              ((0xffffffff & buff[offset + 3]) << 24);
            Revision = (UInt16)(((0xffff & buff[offset + 5]) << 8) + buff[offset + 4]);
            CertificateType = (UInt16)(((0xffff & buff[offset + 7]) << 8) + buff[offset + 6]);
            Certificate = new byte[Length - 8];
            for (int i = 0; i < Certificate.Length; i++)
            {
                Certificate[i] = buff[offset + i + 8];  // Certificate内容字节数不一定是8的倍数
            }
        }

        public void Export(string output)
        {
            string temp = "Certificate Details\r\n";
            temp += "Length: " + Length.ToString() + "\r\n";
            temp += "Revision: " + ((CertificateRevision)Revision).ToString() + "\r\n";
            temp += "CertificateType: " + ((CertType)CertificateType).ToString() + "\r\n";
            temp += "Certificate Data: \r\n";
            StringBuilder data = new StringBuilder();
            for (int i = 0; i < Certificate.Length; i++)
            {
                if ((i % 32) == 0)
                {
                    data.Append(i.ToString("X8") + ": ");
                }
                data.Append(Certificate[i].ToString("X2"));
                if ((i & 0x1) != 0)
                {
                    data.Append(" ");
                }
                if ((i % 32) == 31)
                {
                    data.Append("\r\n");
                }
            }

            temp += data.ToString();

            File.WriteAllText(output + "\\CertificateDetails.txt", temp);
            // 目前好像微软已经不支持非PKCS证书的文件签名了，绝大部分情况CertificateType都为2。
            if (CertificateType == (UInt16)CertType.WIN_CERT_TYPE_PKCS_SIGNED_DATA)
            {
                File.WriteAllBytes(output + "\\CertificateDetails.p7b", Certificate);
            }
            else
            {
                File.WriteAllBytes(output + "\\CertificateDetails.bin", Certificate);
            }
        }
    }

    enum CertType
    {
        WIN_CERT_TYPE_X509 = 0x0001,                // 证书包含一个 X.509证书，不支持
        WIN_CERT_TYPE_PKCS_SIGNED_DATA = 0x0002,    // 证书包含一个PKCS签名证书
        WIN_CERT_TYPE_RESERVED_1 = 0x0003,          // 未定义
        WIN_CERT_TYPE_TS_STACK_SIGNED = 0x0004,     // 终端协议栈签名证书，不支持
        WIN_CERT_TYPE_PKCS1_SIGN = 0x0009           //  证书包含 PKCS1_MODULE_SIGN范围
    }

    enum CertificateRevision
    {
        WIN_CERT_REVISION_1_0 = 0x0100,    // 旧版证书
        WIN_CERT_REVISION_2_0 = 0x0200     // 新版证书
    }

    #endregion


}
