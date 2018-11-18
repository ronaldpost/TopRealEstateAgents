using Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Interfaces.Funda
{
    public interface IFundaService
    {
        Task<List<RealEstateAgent>>  GetTopTenRealEstateAgents(string city, string filter, int count);
    }
}
