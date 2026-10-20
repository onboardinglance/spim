using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spim.Application.WeatherForecasts;
using Spim.Application.WeatherForecasts.Commands.CreateWeatherForecast;
using Spim.Application.WeatherForecasts.Commands.DeleteWeatherForecast;
using Spim.Application.WeatherForecasts.Commands.UpdateWeatherForecast;
using Spim.Application.WeatherForecasts.Queries.GetWeatherForecastById;
using Spim.Application.WeatherForecasts.Queries.GetWeatherForecasts;

namespace Spim.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/weather-forecasts")]
[Produces("application/json")]
public sealed class WeatherForecastsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<WeatherForecastDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WeatherForecastDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetWeatherForecastsQuery(), cancellationToken));

    [HttpGet("{id:int}", Name = nameof(GetById))]
    [ProducesResponseType<WeatherForecastDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeatherForecastDto>> GetById(int id, CancellationToken cancellationToken) =>
        await sender.Send(new GetWeatherForecastByIdQuery(id), cancellationToken);

    [HttpPost]
    [ProducesResponseType<int>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<int>> Create(CreateWeatherForecastCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtRoute(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateWeatherForecastCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Route id does not match body id.");

        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteWeatherForecastCommand(id), cancellationToken);
        return NoContent();
    }
}
