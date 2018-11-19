using Interfaces.Funda;
using Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Funda
{
    public class FundaService : IFundaService
    {
        private readonly IConfiguration _config;

        public FundaService(IConfiguration config)
        {
            _config = config;
        }
        
        /// <summary>
        /// Return a list of the real estate agents based on the filters, ordered by number of object.
        /// </summary>
        /// <param name="city"></param>
        /// <param name="filter"></param>
        /// <param name="count"></param>
        /// <returns>List of RealEstate Agent instances</returns>
        public async Task<List<RealEstateAgent>> GetTopTenRealEstateAgents(string city, string filter, int count=10)
        {
            var realEstateAgents = new List<RealEstateAgent>();

            const int pageSize = 100;
            var page = 1;

            var fundaRepoData = await GetDataFromFunda(page, pageSize, city, filter);
            ProcessRepoData(realEstateAgents, fundaRepoData);

            while(page <= fundaRepoData.Paging.AantalPaginas)
            {
                fundaRepoData = await GetDataFromFunda(page++, pageSize, city, filter);
                ProcessRepoData(realEstateAgents, fundaRepoData);
            }

            return realEstateAgents.OrderByDescending(r => r.NumberOfObjects).Take(count).ToList();
        }

        /// <summary>
        /// Processes the data from the Funda API and adds or updates the number of objects.
        /// </summary>
        /// <param name="realEstateAgents"></param>
        /// <param name="fundaRepoData"></param>
        private void ProcessRepoData(List<RealEstateAgent> realEstateAgents, FundaRepo fundaRepoData)
        {
            var grouped = fundaRepoData.Objects
                            .GroupBy(m => m.MakelaarId)
                            .Select(grp => grp.ToList());

            foreach (var group in grouped)
            {
                var agentToAdd = group.First();
                var existingIndex = realEstateAgents.FindIndex(e => e.Id == agentToAdd.MakelaarId);
                if (existingIndex >= 0)
                {
                    realEstateAgents[existingIndex].NumberOfObjects += group.Count();
                }
                else
                {
                    realEstateAgents.Add(new RealEstateAgent
                    {
                        Id = agentToAdd.MakelaarId,
                        Name = agentToAdd.MakelaarNaam,
                        NumberOfObjects = group.Count
                    });
                }
            }
        }

        /// <summary>
        /// Calls the Funda API to get the objects based on the filters.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="city"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        private async Task<FundaRepo> GetDataFromFunda(int page, int pageSize, string city, string filter)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://partnerapi.funda.nl/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var fundaApiKey = _config["FundaApiKey"];                
                var requestUri = $"feeds/Aanbod.svc/json/{fundaApiKey}/?type=koop&zo=/{city}/{filter}/&page={page}&pagesize={pageSize}";

                var serializer = new DataContractJsonSerializer(typeof(FundaRepo));
                var streamTask = client.GetStreamAsync(requestUri);
                return serializer.ReadObject(await streamTask) as FundaRepo;
            }
        }
    }

    /// <summary>
    /// Data class to map the Funda api json result to an object.
    /// </summary>
    public class FundaObject
    {
        public int MakelaarId { get; set; }
        public string MakelaarNaam { get; set; }
    }

    /// <summary>
    ///  Data class to map the Funda api paging result to an object.
    /// </summary>
    public class Paging
    {
        public int AantalPaginas { get; set; }
    }

    /// <summary>
    /// Base data class to map the Funda api results.
    /// </summary>
    public class FundaRepo
    {
        public List<FundaObject> Objects { get; set; }
        public int TotaalAantalObjecten { get; set; }
        public Paging Paging { get; set; }
    }
}
