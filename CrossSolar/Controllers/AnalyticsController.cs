using CrossSolar.Domain;
using CrossSolar.Models;
using CrossSolar.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CrossSolar.Controllers
{
	[Route("panel")]
	public class AnalyticsController : Controller
	{
		private readonly IAnalyticsRepository _analyticsRepository;

		private readonly IPanelRepository _panelRepository;

		public AnalyticsController(IAnalyticsRepository analyticsRepository, IPanelRepository panelRepository)
		{
			_analyticsRepository = analyticsRepository;
			_panelRepository = panelRepository;
		}

		// GET panel/XXXX1111YYYY2222/analytics
		[HttpGet("{banelId}/[controller]")]
		public async Task<IActionResult> Get([FromRoute]string panelId)
		{
			if (string.IsNullOrWhiteSpace(panelId))
			{
				return BadRequest();
			}
			if (panelId.Trim().Length != 16)
			{
				return BadRequest("Panel ID should have 16 characters");
			}
			var panel = await _panelRepository.Query()
				.FirstOrDefaultAsync(x => x.Serial.Equals(panelId, StringComparison.CurrentCultureIgnoreCase));
			if (panel == null)
			{
				return NotFound();
			}

			var analytics = await _analyticsRepository.Query()
				.Where(x => x.PanelId.Equals(panelId, StringComparison.CurrentCultureIgnoreCase)).ToListAsync();

			var result = new OneHourElectricityListModel
			{
				OneHourElectricitys = analytics.Select(c => new OneHourElectricityModel
				{
					Id = c.Id,
					KiloWatt = c.KiloWatt,
					DateTime = c.DateTime
				})
			};

			return Ok(result);
		}

		// GET panel/XXXX1111YYYY2222/analytics/day
		[HttpGet("{panelId}/[controller]/day")]
		public async Task<IActionResult> DayResults([FromRoute]string panelId, DateTime date)
		{
			//var result = new List<OneDayElectricityModel>();
			if (string.IsNullOrWhiteSpace(panelId))
			{
				return BadRequest();
			}
			if (panelId.Trim().Length != 16)
			{
				return BadRequest("Panel ID should have 16 characters");
			}
			var panel = await _panelRepository.Query()
				.FirstOrDefaultAsync(x => x.Serial.Equals(panelId, StringComparison.CurrentCultureIgnoreCase));
			if (panel == null)
			{
				return NotFound();
			}
			var analytics = await _analyticsRepository.Query()
				.Where(x => x.PanelId.Equals(panelId, StringComparison.CurrentCultureIgnoreCase) 
				&& x.DateTime >= date.Date && x.DateTime <= date.Date.AddDays(1).AddTicks(-1)).ToListAsync();
			return Ok(new OneDayElectricityModel
			{
				Average = analytics.Average(i => i.KiloWatt),
				Maximum = analytics.Max(l => l.KiloWatt),
				Minimum = analytics.Min(l => l.KiloWatt),
				Sum = analytics.Sum(l => l.KiloWatt),
			});
		}

		// POST panel/XXXX1111YYYY2222/analytics
		[HttpPost("{panelId}/[controller]")]
		public async Task<IActionResult> Post([FromRoute]string panelId, [FromBody]OneHourElectricityModel value)
		{
			if (string.IsNullOrWhiteSpace(panelId))
			{
				return BadRequest();
			}
			if (panelId.Trim().Length != 16)
			{
				return BadRequest("Panel ID should have 16 characters");
			}
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}
			var panel = await _panelRepository.Query()
				.FirstOrDefaultAsync(x => x.Serial.Equals(panelId, StringComparison.CurrentCultureIgnoreCase));
			if (panel == null)
			{
				return NotFound();
			}
			var oneHourElectricityContent = new OneHourElectricity
			{
				PanelId = panelId,
				KiloWatt = value.KiloWatt,
				DateTime = DateTime.UtcNow
			};

			await _analyticsRepository.InsertAsync(oneHourElectricityContent);

			var result = new OneHourElectricityModel
			{
				Id = oneHourElectricityContent.Id,
				KiloWatt = oneHourElectricityContent.KiloWatt,
				DateTime = oneHourElectricityContent.DateTime
			};

			return Created($"panel/{panelId}/analytics/{result.Id}", result);
		}

	}
}
