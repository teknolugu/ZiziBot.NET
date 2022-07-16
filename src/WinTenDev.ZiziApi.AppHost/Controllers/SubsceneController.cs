using AutoMapper;
using DotNurse.Injector.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace WinTenDev.ZiziApi.AppHost.Controllers;

[Route("[controller]")]
[ApiController]
public class SubsceneController : ControllerBase
{
    [InjectService]
    private SubsceneService _subsceneService;

    [InjectService]
    private IMapper _mapper;

    [HttpGet("SearchByTitle")]
    [ProducesResponseType(typeof(SubsceneTitleDto), 200)]
    public async Task<IActionResult> SearchTitle([FromQuery] string title)
    {
        var movieByTitle = await _subsceneService.GetOrFeedMovieByTitle(title);
        var mappedMovies = _mapper.Map<List<SubsceneTitleDto>>(movieByTitle);

        return Ok(mappedMovies);
    }

    [HttpGet("ListByMovieSlug")]
    [ProducesResponseType(typeof(SubsceneSubtitleDto), 200)]
    public async Task<IActionResult> SearchBySubtitle([FromQuery] string movieSlug)
    {
        var subtitlesBySlug = await _subsceneService.GetOrFeedSubtitleBySlug(movieSlug);
        var mappedSubtitles = _mapper.Map<List<SubsceneSubtitleDto>>(subtitlesBySlug);

        return Ok(mappedSubtitles);
    }
}