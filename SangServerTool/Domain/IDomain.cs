using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SangServerTool.Domain
{

    internal interface IDomain
    {
        Task<DomainRes> GetRecordsAsync(string SubDomain,string Type);

    }

    /// <summary>
    /// 域名解析信息返回
    /// </summary>
    public record DomainRes
    {
        /// <summary>
        /// 数据获取成功与否
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// 记录值
        /// </summary>
        public string Value { get; set; } = "";
        /// <summary>
        /// 错误信息
        /// </summary>
        public string Msg { get; set; } = "";
        /// <summary>
        /// 记录ID
        /// </summary>
        public string Id { get; set; } = "";
    }
}
