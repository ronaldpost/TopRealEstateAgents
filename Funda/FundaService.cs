using Interfaces.Funda;
using Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Linq;

namespace Funda
{
    public class FundaService : IFundaService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="city"></param>
        /// <param name="filter"></param>
        /// <param name="count"></param>
        /// <returns></returns>
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

        private async Task<FundaRepo> GetDataFromFunda(int page, int pageSize, string city, string filter)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://partnerapi.funda.nl/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                const string key = "ac1b0b1572524640a0ecc54de453ea9f";

                var requestUri = $"feeds/Aanbod.svc/json/{key}/?type=koop&zo=/{city}/{filter}/&page={page}&pagesize={pageSize}";

                var serializer = new DataContractJsonSerializer(typeof(FundaRepo));
                var streamTask = client.GetStreamAsync(requestUri);
                return serializer.ReadObject(await streamTask) as FundaRepo;
            }
        }
    }

    public class FundaObject
    {
        public int MakelaarId { get; set; }
        public string MakelaarNaam { get; set; }
    }

    public class Paging
    {
        public int AantalPaginas { get; set; }
    }

    public class FundaRepo
    {
        public List<FundaObject> Objects { get; set; }
        public int TotaalAantalObjecten { get; set; }
        public Paging Paging { get; set; }
    }
}
