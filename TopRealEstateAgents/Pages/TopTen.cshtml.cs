using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Interfaces.Funda;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TopRealEstateAgents.ViewModels;

namespace TopRealEstateAgents.Pages
{
    public class TopTenModel : PageModel
    {
        private readonly IFundaService _fundaService;
        private readonly IMapper _mapper;

        public TopTenModel(IFundaService fundaService, IMapper mapper)
        {
            _fundaService = fundaService;
            _mapper = mapper;
        }

        [BindProperty]
        public List<RealEstateAgentViewModel> RealEstateAgents { get; set; }

        [BindProperty(SupportsGet = true)]
        public string filter { get; set; }

        public async Task OnGetAsync()
        {   
            var agentModels = await _fundaService.GetTopTenRealEstateAgents("Amsterdam", filter, 10);

            RealEstateAgents = new List<RealEstateAgentViewModel>();
            _mapper.Map(agentModels, RealEstateAgents);
        }
    }
}