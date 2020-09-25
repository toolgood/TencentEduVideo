using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using ToolGood.Bedrock;
using ToolGood.ReadyGo3;

namespace TencentEduVideo
{
    class Program
    {
        static void Main(string[] args)
        {
            //var files = Directory.GetFiles(@"E:\C#软谋", "*.sqlite", SearchOption.TopDirectoryOnly);
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.sqlite", SearchOption.TopDirectoryOnly);
            foreach (var file in files) {
                Console.WriteLine("当前文件：" + file);
                GetVideo(file);
            }
        }

        static void GetVideo(string file)
        {

            var helper = SqlHelperFactory.OpenDatabase($"Data Source={file};", SqlType.SQLite);
            var list = helper.Select<caches>("select key from  caches");

            var token = GetQueryString(list[1].key, "token");
            var keys = list[1].value;
            //var t = Encoding.UTF8.GetString(list[0].value);

            Dictionary<long, caches> dict = new Dictionary<long, caches>();
            for (int i = 0; i < list.Count; i++) {
                if (list[i].key.StartsWith("https://ke.qq.com")) {
                    var db = helper.FirstOrDefault<caches>("select * from  caches where key=@0", list[i].key);
                    keys = db.value;
                }
                var k = GetQueryString(list[i].key, "start");
                if (string.IsNullOrWhiteSpace(k) == false) {
                    dict[long.Parse(k)] = list[i];
                }
            }

            var dir = Path.GetDirectoryName(file);
            var vFile = Path.Combine(dir, Path.GetFileNameWithoutExtension(file) + ".ts");
            var fs = File.Create(vFile);

            foreach (var item in dict.OrderBy(q => q.Key)) {
                var db = helper.FirstOrDefault<caches>("select * from  caches where key=@0", item.Value.key);
                var bs = AesDecypt(keys, db.value, item.Key);
                fs.Write(bs, 0, bs.Length);
            }
            fs.Close();
        }


        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="encrypted"></param>
        /// <returns></returns>
        static byte[] AesDecypt(byte[] keyBytes, byte[] bytes, long start)
        {
            var t = 411785983 - 411019120;
            using (RijndaelManaged cipher = new RijndaelManaged()) {
                cipher.Mode = CipherMode.CBC;
                //cipher.Padding = PaddingMode.PKCS7;
                cipher.KeySize = 128;
                cipher.BlockSize = 128;
                cipher.Key = keyBytes;
                // cipher.IV = keyBytes;


                using (ICryptoTransform decryptor = cipher.CreateDecryptor()) {
                    using (MemoryStream msDecrypt = new MemoryStream(bytes)) {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)) {
                            using (MemoryStream originalMemory = new MemoryStream()) {
                                Byte[] Buffer = new Byte[1024];
                                Int32 readBytes = 0;
                                while ((readBytes = csDecrypt.Read(Buffer, 0, Buffer.Length)) > 0) {
                                    originalMemory.Write(Buffer, 0, readBytes);
                                }
                                return originalMemory.ToArray();
                            }
                        }
                    }
                }
            }
        }

        static string GetQueryString(string url, string key)
        {
            var uri = new Uri(url).Query;
            var sp = uri.Split(new char[] { '?', '&' });
            foreach (var item in sp) {
                if (item.StartsWith(key + "=")) {
                    return item.Substring(key.Length + 1);
                }
            }
            return "";
        }

    }
    public class caches
    {
        public string key { get; set; }
        public byte[]? value { get; set; }
    }
}
