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
    /// <summary>
    /// 阿里云域名解析管理
    /// 文档 https://help.aliyun.com/document_detail/29771.html
    /// 签名 https://help.aliyun.com/document_detail/29747.html
    /// </summary>
    public class AliyunDomain : IDomain
    {
        private readonly string _AccessKeyId;
        private readonly string _AccessKeySecret;
        private readonly string _Host = "https://alidns.aliyuncs.com/";

        public AliyunDomain(string accessKeyId, string accessKeySecret)
        {
            _AccessKeyId = accessKeyId;
            _AccessKeySecret = accessKeySecret;
        }

        /// <summary>
        /// 修改解析记录
        /// </summary>
        /// <param name="DomainName">域名</param>
        /// <param name="RR">记录</param>
        /// <param name="Type">类型</param>
        /// <param name="Value">记录值</param>
        /// <returns>域名设置信息</returns>
        public async Task<DomainRes> UpdateRecordsAsync(string RecordId, string RR, string Type, string Value)
        {

            var parameters = new Dictionary<string, string>();
            parameters.Add("Action", "UpdateDomainRecord");
            parameters.Add("RecordId", RecordId);
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

            return new DomainRes(true, "ok", json["RecordId"].ToString());
        }

        /// <summary>
        /// 删除解析记录
        /// </summary>
        /// <param name="RecordId">解析记录的ID</param>
        /// <returns></returns>
        public async Task<DomainRes> DelRecordsAsync(string RecordId)
        {

            var parameters = new Dictionary<string, string>();
            parameters.Add("Action", "DeleteDomainRecord");
            parameters.Add("RecordId", RecordId);


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

            return new DomainRes(true, "ok", json["RecordId"].ToString());
        }

        /// <summary>
        /// 获取子域名解析记录列表
        /// </summary>
        /// <param name="SubDomain"></param>
        /// <param name="Type">解析类型 A、MX、CNAME、TXT、REDIRECT_URL、FORWORD_URL、NS、AAAA、SRV</param>
        /// <returns>域名解析信息</returns>
        public async Task<DomainRes> GetRecordsAsync(string SubDomain, string Type = "")
        {
            var parameters = new Dictionary<string, string>();
            parameters.Add("Action", "DescribeSubDomainRecords");
            parameters.Add("SubDomain", SubDomain);
            if (!string.IsNullOrEmpty(Type)) parameters.Add("Type", Type);

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
            if (json["TotalCount"] is null)
            {
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
        /// <param name="DomainName">域名</param>
        /// <param name="RR">记录</param>
        /// <param name="Type">类型</param>
        /// <param name="Value">记录值</param>
        /// <returns>域名设置信息</returns>
        public async Task<DomainRes> AddRecordsAsync(string DomainName, string RR, string Type, string Value)
        {

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

            return new DomainRes(true, "ok", json["RecordId"].ToString());
        }

        /// <summary>
        /// 签名请求的URL
        /// </summary>
        /// <param name="parameters">参数，非公共</param>
        /// <param name="method">请求类型</param>
        /// <returns></returns>
        private string SignUrl(Dictionary<string, string> parameters, HttpMethod method)
        {
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
