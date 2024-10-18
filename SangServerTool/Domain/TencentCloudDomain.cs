namespace SangServerTool.Domain
{

    /// <summary>
    /// 腾讯云域名解析管理
    /// 文档 https://cloud.tencent.com/document/api/1427/56153
    /// 签名 https://cloud.tencent.com/document/api/1427/56189
    /// </summary>
    public class TencentCloudDomain : IDomain
    {
        private readonly string _SecretId;
        private readonly string _SecretKey;
        private readonly string _Host = "https://dnspod.tencentcloudapi.com/";

        /// <summary>
        /// 初始化设置AK，SK
        /// </summary>
        /// <param name="accessKeyId">SecretId</param>
        /// <param name="accessKeySecret">SecretKey</param>
        public TencentCloudDomain(string accessKeyId, string accessKeySecret)
        {
            _SecretId = accessKeyId;
            _SecretKey = accessKeySecret;
        }

        public Task<DomainRes> AddRecordsAsync(string DomainName, string RR, string Type, string Value)
        {
            throw new NotImplementedException();
        }

        public Task<DomainRes> DelRecordsAsync(string RecordId)
        {
            throw new NotImplementedException();
        }

        public Task<DomainRes> GetRecordsAsync(string SubDomain, string Type = "")
        {
            throw new NotImplementedException();
        }

        public Task<DomainRes> UpdateRecordsAsync(string RecordId, string RR, string Type, string Value)
        {
            throw new NotImplementedException();
        }
    }
}
