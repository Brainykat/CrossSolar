using System.Threading.Tasks;
using CrossSolar.Controllers;
using CrossSolar.Domain;
using CrossSolar.Models;
using CrossSolar.Repository;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CrossSolar.Tests.Controller
{
    public class PanelControllerTests
    {
        private PanelController _panelController;

        private Mock<IPanelRepository> _panelRepositoryMock = new Mock<IPanelRepository>();

        public PanelControllerTests()
        {
            _panelController = new PanelController(_panelRepositoryMock.Object);
        }

        [Fact]
        public async Task Register_ShouldInsertPanel()
        {
            var panel = new PanelModel
            {
                Brand = "Areva",
                Latitude = 12.345678,
                Longitude = 98.7655432,
                Serial = "AAAA1111BBBB2222"
            };

            // Arrange

            // Act
            var result = await _panelController.Register(panel);

            // Assert
            Assert.NotNull(result);

            var createdResult = result as CreatedResult;
            Assert.NotNull(createdResult);
            Assert.Equal(201, createdResult.StatusCode);
        }
		[Fact]
		public async Task Register_ReturnsBadRequestResult_WhenSerialIsInvalid()
		{
			var panel = new PanelModel
			{
				Brand = "Areva",
				Latitude = 12.345345,
				Longitude = 98.765678,
				Serial = "AAAA111"
			};
			_panelController.ModelState.AddModelError("Serial", "Invalid");
			var result = await _panelController.Register(panel);
			Assert.NotNull(result);
			var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
			Assert.IsType<SerializableError>(badRequestResult.Value);
		}
		[Fact]
		public async Task Register_ReturnsBadRequestResult_WhenLatitudeIsInvalid()
		{
			var panel = new PanelModel
			{
				Brand = "Areva",
				Latitude = 12.3453,
				Longitude = 98.765678,
				Serial = "1234567890123456"
			};
			_panelController.ModelState.AddModelError("Latitude", "Invalid");
			var result = await _panelController.Register(panel);
			Assert.NotNull(result);
			var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
			Assert.IsType<SerializableError>(badRequestResult.Value);
		}
		[Fact]
		public async Task Register_ReturnsBadRequestResult_WhenLogitudeIsInvalid()
		{
			var panel = new PanelModel
			{
				Brand = "Areva",
				Latitude = 12.345345,
				Longitude = 98.7651,
				Serial = "1234567890123456"
			};
			_panelController.ModelState.AddModelError("Longitude", "Invalid");
			var result = await _panelController.Register(panel);
			Assert.NotNull(result);
			var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
			Assert.IsType<SerializableError>(badRequestResult.Value);
		}
	}
}
