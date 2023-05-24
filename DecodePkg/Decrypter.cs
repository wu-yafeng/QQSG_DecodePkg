using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DecodePkg
{
    internal class Decrypter
    {
        /// <summary>
        /// 解密区块
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static byte[] DecryptBlock(byte[] buffer, byte[] key)
        {
            if (buffer.Length != 8)
            {
                return Array.Empty<byte>();
            }

            if (key.Length < 16)
            {
                return Array.Empty<byte>();
            }

            var value1 = (uint)IPAddress.HostToNetworkOrder(BitConverter.ToInt32(buffer.AsSpan()[0..4]));
            var value2 = (uint)IPAddress.HostToNetworkOrder(BitConverter.ToInt32(buffer.AsSpan()[4..8]));

            var matrix = new uint[4];

            for (var i = 0; i < matrix.Length; i++)
            {
                var begin = i * 4;
                matrix[i] = (uint)IPAddress.HostToNetworkOrder(BitConverter.ToInt32(key.AsSpan()[begin..(begin + 4)]));
            }

            var rnd = 0xE3779B90;

            for (var i = 16; i > 0; i--)
            {
                value2 -= (rnd + value1) ^ (matrix[2] + 16 * value1) ^ (matrix[3] + (value1 >> 5));
                var tmp = (rnd + value2) ^ (matrix[0] + 16 * value2) ^ (matrix[1] + (value2 >> 5));
                rnd += 1640531527;
                value1 -= tmp;
            }

            return BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)value1)).Concat(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)value2))).ToArray();
        }


        public static byte[] Decrypt(byte[] body, byte[] key)
        {
            var remain_j = 0;


            var prev_block = new byte[8];
            var prev_original_block = new byte[8];

            var result = new List<byte>();

            // 最后的8个字节不知道是干嘛的，应该是签名校验的东西
            for (var i = 0; i < body.Length - 8; i += 8)
            {
                var decrypt_block = new byte[8];
                for (var k = 0; k < 8; k++)
                {
                    decrypt_block[k] = (byte)(prev_block[k] ^ body[i + k]);
                }

                var block = DecryptBlock(decrypt_block, key);

                var j = remain_j;

                // first block
                if (i == 0)
                {
                    var header_offset = block[0] & 7;

                    //result = new List<byte>(body.Length - header_offset - 10);

                    j = header_offset + 3;


                }
                for (; j < 8; j++)
                {
                    result.Add((byte)(block[j] ^ prev_original_block[j]));
                }

                // next block
                if (j > 8)
                {
                    remain_j = j % 8;
                }
                else
                {
                    remain_j = 0;
                }

                prev_block = block;
                prev_original_block = body[i..(i + 8)];
            }

            // 删除最后几个null字节
            for (var i = result.Count - 1; i != 0; i--)
            {
                if (result[i] == 0)
                {
                    result.RemoveAt(i);
                }
            }

            return result.ToArray();
        }
    }
}
