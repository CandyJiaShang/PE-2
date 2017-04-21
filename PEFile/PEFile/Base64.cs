using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PEFile
{
    class Base64
    {
        private static char[] Table = {'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z',
                                       'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z',
                                       '0','1','2','3','4','5','6','7','8','9','+','/'
                                      };

        // 这个表用来取字符在Table数组中的下标
        private static UInt32[] Char64 = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3e, 0x00, 0x00, 0x00, 0x3f,
                                           0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3a, 0x3b, 0x3c, 0x3d, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                           0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e,
                                           0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x00, 0x00, 0x00, 0x00, 0x00,
                                           0x00, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28,
                                           0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f, 0x30, 0x31, 0x32, 0x33, 0x00, 0x00, 0x00, 0x00, 0x00
                                         };

        public static string BytesToBase64(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }
            if (bytes.Length == 0)
            {
                return "";
            }

            int section = 0;
            int length = bytes.Length;
            if ((length % 3) == 0)
            {
                section = length / 3;
            }
            else
            {
                section += length / 3 + 1;
            }

            char[] output = new char[4 * section];

            // 把最后一节前面的节处理掉
            for (int i = 0; i < section - 1; i++)
            {
                UInt32 temp = bytes[i * 3 + 2] + ((0xffffffff & bytes[i * 3 + 1]) << 8) + ((0xffffffff & bytes[i * 3]) << 16);
                output[i * 4 + 3] = Table[temp & 0x3f];
                temp >>= 6;
                output[i * 4 + 2] = Table[temp & 0x3f];
                temp >>= 6;
                output[i * 4 + 1] = Table[temp & 0x3f];
                temp >>= 6;
                output[i * 4] = Table[temp & 0x3f];
            }

            // 处理可能需要补齐的最后一节
            UInt32 lastSection = 0;
            UInt32 emptyChar = 0;
            if ((length % 3) == 0)
            {
                lastSection = bytes[length - 1] + ((0xffffffff & bytes[length - 2]) << 8) + ((0xffffffff & bytes[length - 3]) << 16);
            }
            else if ((length % 3) == 2)
            {
                lastSection = ((0xffffffff & bytes[length - 2]) << 8) + ((0xffffffff & bytes[length - 3]) << 16);
                emptyChar = 1;
            }
            else
            {
                lastSection = ((0xffffffff & bytes[length - 3]) << 16);
                emptyChar = 2;          
            }

            // 填充最后一段
            output[(section - 1) * 4 + 3] = Table[lastSection & 0x3f];
            lastSection >>= 6;
            output[(section - 1) * 4 + 2] = Table[lastSection & 0x3f];
            lastSection >>= 6;
            output[(section - 1) * 4 + 1] = Table[lastSection & 0x3f];
            lastSection >>= 6; 
            output[(section - 1) * 4] = Table[lastSection & 0x3f];

            // 多余部分用等号填充
            for (int i = 1; i <= emptyChar; i++)
            {
                output[output.Length - i] = '=';
            }

            return new string(output);
        }

        public static byte[] Base64ToBytes(string base64)
        {
            if (base64 == null)
            {
                return null;
            }
            if (base64 == "")
            {
                return new byte[]{};
            }

            // 计算字符串结尾有几个等号
            int eqNumber = 0;
            for (int i = 1; i<= base64.Length ; i++)
            {
                if (base64[base64.Length - i] == '=')
                {
                    eqNumber++;
                }
                else
                {
                    break;
                }
            }
            
            // 计算应该输出多少个字节
            int length = base64.Length * 3 /4 - eqNumber;
            byte[] output = new byte[length];

            // 解码 base64.Length - 4 部分
            for (int i = 0; i < base64.Length / 4 - 1; i++) // 每3字节对应base64的4个字母
            {
                UInt32 temp = (Char64[base64[4 * i]] << 18) + (Char64[base64[4 * i + 1]] << 12) + (Char64[base64[4 * i + 2]] << 6) + Char64[base64[4 * i + 3]];
                output[i * 3 + 2] = (byte)(temp & 0xff);
                temp >>= 8;
                output[i * 3 + 1] = (byte)(temp & 0xff);
                temp >>= 8;
                output[i * 3] = (byte)(temp & 0xff);
            }

            // 解码最后一段
            UInt32 last = (Char64[base64[base64.Length - 4]] << 18) + (Char64[base64[base64.Length - 3]] << 12) + (Char64[base64[base64.Length - 2]] << 6) + Char64[base64[base64.Length - 1]];
            byte byte3 = (byte)(last & 0xff);
            last >>= 8;
            byte byte2 = (byte)(last & 0xff); 
            last >>= 8;
            byte byte1 = (byte)(last & 0xff);
            if (eqNumber == 0)
            {
                output[length - 3] = byte1;
                output[length - 2] = byte2;
                output[length - 1] = byte3;
            }
            else if (eqNumber == 1)
            {
                output[length - 2] = byte1;
                output[length - 1] = byte2;
            }
            else
            {
                output[length - 1] = byte1;            
            }
            return output;
        }
    }
}
