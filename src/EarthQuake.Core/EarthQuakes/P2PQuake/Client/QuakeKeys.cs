using System.Security.Cryptography;
using System.Text;

namespace EarthQuake.Core.EarthQuakes.P2PQuake.Client
{
    public class QuakeKeys
    {
        public QuakeKeys(string privateKey, string publicKey, string signature, string invalidationDate)
        {
            var rsaParams = Asn1PKCS.Decoder.PKCS8DERDecoder.DecodePrivateKey(privateKey);
            RSACryptoServiceProvider rsa = new();
            rsa.ImportParameters(rsaParams);
            _privateKey = rsa;
            PublicKey = publicKey;
            Signature = signature;
            PrivateKey = privateKey;
            InvalidationDate = DateTime.ParseExact(invalidationDate, Format, null);
        }
        private readonly RSA _privateKey;
        public string PublicKey { get; }
        public string Signature { get; }
        public string PrivateKey { get; }
        public DateTime InvalidationDate { get; }
        public static string FilePath => @"E:\source\EarthQuake\key.txt";
        public static string Format => P2PClient.Format;
        public static RSA P2PServerPublicKey
        {
            get
            {
                var base64 = "MIGdMA0GCSqGSIb3DQEBAQUAA4GLADCBhwKBgQC8p/vth2yb/k9x2/PcXKdb6oI3gAbhvr/HPTOwla5tQHB83LXNF4Y+Sv/Mu4Uu0tKWz02FrLgA5cuJZfba9QNULTZLTNUgUXIB0m/dq5Rx17IyCfLQ2XngmfFkfnRdRSK7kGnIXvO2/LOKD50JsTf2vz0RQIdw6cEmdl+Aga7i8QIBEQ==";
                var rsaParams = Asn1PKCS.Decoder.PKCS8DERDecoder.DecodePublicKey(base64);
                RSACryptoServiceProvider rsa = new();
                rsa.ImportParameters(rsaParams);
                return rsa;
            }
        }
        public static RSA P2PUserPublicKey
        {
            get
            {
                var base64 = "MIGdMA0GCSqGSIb3DQEBAQUAA4GLADCBhwKBgQDTJKLLO7wjCHz80kpnisqcPDQvA9voNY5QuAA+bOWeqvl4gmPSiylzQZzldS+n/M5p4o1PRS24WAO+kPBHCf4ETAns8M02MFwxH/FlQnbvMfi9zutJkQAu3Hq4293rHz+iCQW/MWYB5IfzFBnWtEdjkhqHsGy6sZMMe+qx/F1rcQIBEQ==";
                var rsaParams = Asn1PKCS.Decoder.PKCS8DERDecoder.DecodePublicKey(base64);
                RSACryptoServiceProvider rsa = new();
                rsa.ImportParameters(rsaParams);
                return rsa;
            }
        }
        public static QuakeKeys? Create(Response response)
        {
            if (response.Code == 295) return null; // 鍵が割り当て済みレスポンスの場合は切る
            if (response.Body is null) return null;
            return new(response.Body![0], response.Body![1], response.Body![3], response.Body![2]);
        }
        public void SaveFile()
        {
            File.WriteAllText(FilePath, $"""
                {PrivateKey}
                {PublicKey}
                {InvalidationDate.ToString(Format)}
                {Signature}
                """);
        }
        public string Generate(byte[] data)
        {
            return Convert.ToBase64String(_privateKey.SignData(data, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1));
        }
        public static QuakeKeys? LoadFile()
        {
            
            try
            {
                using StreamReader sr = new(FilePath);
                string? pr = null;
                string? pu = null;
                string? va = null;
                string? si = null;
                string? line;
                var i = 0;
                while ((line = sr.ReadLine()) is not null)
                {
                    if (line is not null)
                    {
                        switch (i)
                        {
                            case 0:
                                pr = line;
                                break;
                            case 1:
                                pu = line;
                                break;
                            case 2: 
                                va = line;
                                break;
                            case 3:
                                si = line;
                                break;
                        }
                    }
                    i++;
                }
                if (pr is null ||  pu is null || si is null || va is null)
                {
                    return null;
                }
                return new QuakeKeys(pr, pu, si, va);

            }
            catch (Exception e)
            {
                // エラー処理
                Console.WriteLine("ファイルの読み込み中にエラーが発生しました: " + e.Message+e.StackTrace);
                return null;
            }
        }
        public static bool CheckServerData(string signature, string time, string body)
        {
            var hash = MD5.HashData(BufferedNetworkStream.ShiftGis.GetBytes(body));
            var validate = Encoding.ASCII.GetBytes(time);
            return IsValidSignature([..validate.Concat(hash)], P2PServerPublicKey, signature) && DateTime.Now.CompareTo(DateTime.ParseExact(time, Format, null)) > 0;
        }
        public static bool CheckUserData(string publickey, string time,string keyTime, string dataSignature,string keySignature, string body)
        {
            var publickeyBytes = Convert.FromBase64String(publickey);
            var validate = Encoding.ASCII.GetBytes(keyTime);
            var keysignature = IsValidSignature([.. publickeyBytes.Concat(validate)], P2PUserPublicKey, keySignature);
            var rsaParams = Asn1PKCS.Decoder.PKCS8DERDecoder.DecodePublicKey(publickey);
            RSACryptoServiceProvider rsa = new();
            rsa.ImportParameters(rsaParams);
            var validate2 = Encoding.ASCII.GetBytes(time); 
            var hash2 = MD5.HashData(BufferedNetworkStream.ShiftGis.GetBytes(body));
            var datasignature = IsValidSignature([.. validate2.Concat(hash2)], rsa, dataSignature);
            var valid = DateTime.Now.CompareTo(DateTime.ParseExact(time, Format, null)) < 0;
            return keysignature && datasignature && valid;
        }
        private static bool IsValidSignature(byte[] data, RSA publicKey, string signature) /*こちらは公式のものを使用しています*/
        {

            //   - SHA1指定
            var sha1ByteArray = SHA1.HashData(data);

            RSAPKCS1SignatureDeformatter deformatter = new(publicKey);
            deformatter.SetHashAlgorithm("SHA1");

            //   - 検証
            var signatureByteArray = Convert.FromBase64String(signature);
            return deformatter.VerifySignature(sha1ByteArray, signatureByteArray);
        }
    }
    
}
