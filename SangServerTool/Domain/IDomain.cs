using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SangServerTool.Domain
{

    internal interface IDomain
    {
        Task<DomainRes> AddRecordsAsync(string DomainName, string RR, string Type, string Value);
        Task<DomainRes> DelRecordsAsync(string RecordId);
        Task<DomainRes> GetRecordsAsync(string SubDomain, string Type = "");
        Task<DomainRes> UpdateRecordsAsync(string RecordId, string RR, string Type, string Value);


    }

    /// <summary>
    /// 域名解析信息返回
    /// </summary>
    /// <param name="Success">数据获取成功与否</param>
    /// <param name="Msg">错误信息</param>
    /// <param name="Id">记录ID</param>
    /// <param name="Value">记录值</param>
    public record DomainRes(bool Success, string Msg = "", string Id = "", string Value="");

}
