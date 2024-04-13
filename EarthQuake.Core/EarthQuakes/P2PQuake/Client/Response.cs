namespace EarthQuake.Core.EarthQuakes.P2PQuake.Client
{
    /// <summary>
    /// P2Pの送受信に使うレスポンス
    /// </summary>
    public class Response
    {
        private readonly string _response;
        public string[]? Body { get; }
        public int RelayCount { get; set; }
        public int Code { get; }
        public string? RawBody => Body is null ? null : string.Join(':', Body);
        public string Raw => _response;
        public Response(string response)
        {
            _response = response.Replace("\r\n", "");

            var data = _response.Split(' ');
            Code = int.Parse(data[0]);
            RelayCount = int.Parse(data[1]);
            if (data.Length > 2)
            {
                var rawBody = string.Join(' ', data[2..]);
                Body = rawBody.Split(':');
            }
        }
        public override string ToString()
        {
            return $"{Code} {RelayCount} {RawBody}";
        }
        public bool CheckSignature()
        {
            if (Body is null) return false;
            return Code == 555 ? QuakeKeys.CheckUserData(Body[2],Body[1], Body[4], Body[0], Body[3], Body[5]):QuakeKeys.CheckServerData(Body[0], Body[1], string.Join(':', Body[2..]));
            // 「データ署名」「有効期限」「公開鍵」「鍵署名」「鍵期限」「感知情報データ」
        }


    }
}
