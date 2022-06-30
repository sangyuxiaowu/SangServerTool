using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;

namespace SangServerTool.Domain
{
    public class AliyunDomain : IDomain
    {
        private readonly string _AccessKeyId;
        private readonly string _AccessKeySecret;
        private readonly string _Endpoint;
        private readonly string _Host = "https://alidns.aliyuncs.com/";

        /*
         * 造成此问题的主要原因是参数没有严格按照大小写排序。
         * 在使用 C#/.Net 调用 OpenAPI 时，在签名算法中，如果使用 SortedDictionary 来对参数排序，需要严格按照大小写排序。可参考以下代码：
         * SortedDictionaryparameters = new SortedDictionary(StringComparer.Ordinal);
         */

        public AliyunDomain(string accessKeyId, string accessKeySecret, string endpoint = "")
        {
            _AccessKeyId = accessKeyId;
            _AccessKeySecret = accessKeySecret;
            _Endpoint = endpoint;
        }

        /// <summary>
        /// 获取子域名解析记录列表
        /// DOC https://help.aliyun.com/document_detail/29778.html
        /// http(s)://alidns.aliyuncs.com/?Action=DescribeSubDomainRecords
        /// </summary>
        /// <param name="SubDomain"></param>
        /// <param name="Type">解析类型 A、MX、CNAME、TXT、REDIRECT_URL、FORWORD_URL、NS、AAAA、SRV</param>
        /// <returns>域名解析信息</returns>
        public async Task<DomainRes> GetRecordsAsync(string SubDomain, string Type="")
        {
            var parameters = new Dictionary<string, string>();
            parameters.Add("Action", "DescribeSubDomainRecords");
            parameters.Add("SubDomain", SubDomain);
            if(!string.IsNullOrEmpty(Type)) parameters.Add("Type", Type);

            JsonNode json;
            try
            {
                using var client = new HttpClient();
                var jsonstring = await client.GetStringAsync(SignUrl(parameters, HttpMethod.Get));
                json = JsonNode.Parse(jsonstring)!;
            }
            catch(Exception ex) {
                //请求或转换异常
                return new DomainRes(false, ex.Message);
            }

            // 返回有异常
            if (json["TotalCount"] is null) {
                return new DomainRes(false, "返回数据异常");
            }

            // 有解析数据返回解析结果
            if ((int)json["TotalCount"]! > 0)
            {
                var temp = json["DomainRecords"]!["Record"]![0]; 
                return new DomainRes(true, "ok", temp["RecordId"]!.ToString(), temp["Value"]!.ToString());

            }

            // 不存在解析信息
            return new DomainRes(true);
            
        }

        /// <summary>
        /// 添加域名解析
        /// </summary>
        /// <param name="DomainName"></param>
        /// <param name="RR"></param>
        /// <param name="Type"></param>
        /// <param name="Value"></param>
        /// <returns>域名设置信息</returns>
        public async Task<DomainRes> AddRecordsAsync(string DomainName, string RR,string Type,string Value) {

            var parameters = new Dictionary<string, string>();
            parameters.Add("Action", "AddDomainRecord");
            parameters.Add("DomainName", DomainName);
            parameters.Add("RR", RR);
            parameters.Add("Type", Type);
            parameters.Add("Value", Value);

            JsonNode json;
            try
            {
                using var client = new HttpClient();
                var jsonstring = await client.GetStringAsync(SignUrl(parameters, HttpMethod.Get));
                json = JsonNode.Parse(jsonstring)!;
            }
            catch (Exception ex)
            {
                //请求或转换异常
                return new DomainRes(false, ex.Message);
            }

            // 返回有异常
            if (json["RecordId"] is null)
            {
                return new DomainRes(false, "返回数据异常");
            }

            return new DomainRes(true,"ok", json["RecordId"].ToString());
        }

        /// <summary>
        /// 签名请求的URL
        /// </summary>
        /// <param name="parameters">参数，非公共</param>
        /// <param name="method">请求类型</param>
        /// <returns></returns>
        private string SignUrl(Dictionary<string, string> parameters, HttpMethod method) {
            parameters.Add("Format", "JSON");
            parameters.Add("Version", "2015-01-09");
            parameters.Add("SignatureMethod", "HMAC-SHA1");
            parameters.Add("Timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            parameters.Add("SignatureVersion", "1.0");
            parameters.Add("SignatureNonce", Guid.NewGuid().ToString());
            parameters.Add("AccessKeyId", _AccessKeyId);

            var canonicalizedQueryString = string.Join("&",
                parameters.OrderBy(x => x.Key)
                .Select(x => PercentEncode(x.Key) + "=" + PercentEncode(x.Value)));
            var stringToSign = method.ToString().ToUpper() + "&%2F&" + PercentEncode(canonicalizedQueryString);
            var keyBytes = Encoding.UTF8.GetBytes(_AccessKeySecret + "&");
            var hmac = new HMACSHA1(keyBytes);
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));

            parameters.Add("Signature", Convert.ToBase64String(hashBytes));
            return _Host + "?" + string.Join("&", parameters.Select(x => x.Key + "=" + HttpUtility.UrlEncode(x.Value)));
        }

        private string PercentEncode(string value)
        {
            return UpperCaseUrlEncode(value)
                .Replace("+", "%20")
                .Replace("*", "%2A")
                .Replace("%7E", "~");
        }

        private static string UpperCaseUrlEncode(string s)
        {
            char[] temp = HttpUtility.UrlEncode(s).ToCharArray();
            for (int i = 0; i < temp.Length - 2; i++)
            {
                if (temp[i] == '%')
                {
                    temp[i + 1] = char.ToUpper(temp[i + 1]);
                    temp[i + 2] = char.ToUpper(temp[i + 2]);
                }
            }
            return new string(temp);
        }

    }


}
