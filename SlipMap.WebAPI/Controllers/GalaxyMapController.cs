using Microsoft.AspNetCore.Mvc;
using DarkDhamon.Common.API.Models;
using SlipMap.Domain.DataAccessLayer.Abstract;
using SlipMap.Domain.ViewModels;

namespace SlipMap.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GalaxyMapController : ControllerBase
    {
        public ILogger<GalaxyMapController> Logger { get; }
        public IGalaxyRepository GalaxyRepository { get; }

        public GalaxyMapController(ILogger<GalaxyMapController> logger, IGalaxyRepository galaxyRepository)
        {
            Logger = logger;
            GalaxyRepository = galaxyRepository;
        }

        [HttpGet]
        [Route("StarSystems/{galaxyId:int}/{page:int}")]
        public PagedApiResponse<StarSystemSummary> GetGalaxyStarSystems(int galaxyId, int page = 1)
        {
            const int numPerPage = 1000;
            page-=1;
            var systems = GalaxyRepository.GetStarSystems(galaxyId);
            return new PagedApiResponse<StarSystemSummary>()
            {
                Data = systems.Skip(numPerPage*page).Take(numPerPage).Select(system=>new StarSystemSummary()
                {
                    Id = system.Id,
                    Name = system.Name,
                    Coordinates = system.Coordinates
                }).ToList(),
                Page = page,
                NumPerPage = numPerPage,
                TotalPages = systems.Count()/numPerPage
            };
        }

    }
}
