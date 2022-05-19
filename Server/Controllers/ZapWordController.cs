using Microsoft.AspNetCore.Mvc;
using ZapWord.Shared.Models;

namespace ZapWord.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class ZapWordController : ControllerBase
{
    private readonly ILogger<ZapWordController> _logger;
    private readonly IGameFabric _gameFabric;

    public ZapWordController(ILogger<ZapWordController> logger, IGameFabric gameFabric)
    {
        _logger = logger;
        _gameFabric = gameFabric;
    }

    [HttpGet]
    public async Task<ZapWordModel> Get()
    {
        return await NewGame();
    }

    private async Task<ZapWordModel> NewGame()
    {
        return await _gameFabric.GameGet();
    }
}
