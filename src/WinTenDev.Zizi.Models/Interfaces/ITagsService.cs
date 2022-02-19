using System.Collections.Generic;
using System.Threading.Tasks;
using WinTenDev.Zizi.Models.Tables;

namespace WinTenDev.Zizi.Models.Interfaces;

public interface ITagsService
{
    Task<List<CloudTag>> GetTagsAsync();
}