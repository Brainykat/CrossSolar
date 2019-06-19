using CrossSolar.Controllers;
using CrossSolar.Domain;
using CrossSolar.Models;
using CrossSolar.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CrossSolar.Tests.Controller
{
	public class AnalyticsControllerTests
	{
		private AnalyticsController _analyticsController;

		private Mock<IAnalyticsRepository> _analyticsRepositoryMock = new Mock<IAnalyticsRepository>();
		private Mock<IPanelRepository> _panelRepositoryMock = new Mock<IPanelRepository>();
		internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
		{
			private readonly IQueryProvider _inner;

			internal TestAsyncQueryProvider(IQueryProvider inner)
			{
				_inner = inner;
			}

			public IQueryable CreateQuery(Expression expression)
			{
				return new TestAsyncEnumerable<TEntity>(expression);
			}

			public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
			{
				return new TestAsyncEnumerable<TElement>(expression);
			}

			public object Execute(Expression expression)
			{
				return _inner.Execute(expression);
			}

			public TResult Execute<TResult>(Expression expression)
			{
				return _inner.Execute<TResult>(expression);
			}

			public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
			{
				return new TestAsyncEnumerable<TResult>(expression);
			}

			public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
			{
				return Task.FromResult(Execute<TResult>(expression));
			}
		}

		internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
		{
			public TestAsyncEnumerable(IEnumerable<T> enumerable)
				: base(enumerable)
			{ }

			public TestAsyncEnumerable(Expression expression)
				: base(expression)
			{ }

			public IAsyncEnumerator<T> GetEnumerator()
			{
				return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
			}

			IQueryProvider IQueryable.Provider
			{
				get { return new TestAsyncQueryProvider<T>(this); }
			}
		}

		internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
		{
			private readonly IEnumerator<T> _inner;

			public TestAsyncEnumerator(IEnumerator<T> inner)
			{
				_inner = inner;
			}

			public void Dispose()
			{
				_inner.Dispose();
			}

			public T Current
			{
				get
				{
					return _inner.Current;
				}
			}

			public Task<bool> MoveNext(CancellationToken cancellationToken)
			{
				return Task.FromResult(_inner.MoveNext());
			}
		}
		public AnalyticsControllerTests()
		{
			// Create the Panel async enumerator with test data
			var panelData = new List<Panel>
			{
				new Panel{ Id = 1, Serial = "AAAA1111BBBB2222", Latitude = -35.492758, Longitude = 80.947992 },
				new Panel{ Id = 2, Serial = "CCCC3333DDDD4444", Latitude = 22.199302, Longitude = 139.400192 }
			}.AsQueryable();
			var panelMockSet = new Mock<DbSet<Panel>>();
			panelMockSet.As<IAsyncEnumerable<Panel>>().Setup(m => m.GetEnumerator()).Returns(new TestAsyncEnumerator<Panel>(panelData.GetEnumerator()));
			panelMockSet.As<IQueryable<Panel>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<Panel>(panelData.Provider));
			panelMockSet.As<IQueryable<Panel>>().Setup(m => m.Expression).Returns(panelData.Expression);
			panelMockSet.As<IQueryable<Panel>>().Setup(m => m.ElementType).Returns(panelData.ElementType);
			panelMockSet.As<IQueryable<Panel>>().Setup(m => m.GetEnumerator()).Returns(panelData.GetEnumerator());
			_panelRepositoryMock.Setup(m => m.Query()).Returns(panelMockSet.Object.AsQueryable());
			//
			var analyticsData = new List<OneHourElectricity>
			{
				new OneHourElectricity{ Id = 1, PanelId = "AAAA1111BBBB2222", KiloWatt = 12, DateTime = DateTime.UtcNow },
				new OneHourElectricity{ Id = 1, PanelId = "AAAA1111BBBB2222", KiloWatt = 25, DateTime = DateTime.UtcNow },
				new OneHourElectricity{ Id = 2, PanelId = "CCCC3333DDDD4444", KiloWatt = 31, DateTime = DateTime.UtcNow }
			}.AsQueryable();
			var analyticsMockSet = new Mock<DbSet<OneHourElectricity>>();
			analyticsMockSet.As<IAsyncEnumerable<OneHourElectricity>>().Setup(m => m.GetEnumerator()).Returns(new TestAsyncEnumerator<OneHourElectricity>(analyticsData.GetEnumerator()));
			analyticsMockSet.As<IQueryable<OneHourElectricity>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<OneHourElectricity>(analyticsData.Provider));
			analyticsMockSet.As<IQueryable<OneHourElectricity>>().Setup(m => m.Expression).Returns(analyticsData.Expression);
			analyticsMockSet.As<IQueryable<OneHourElectricity>>().Setup(m => m.ElementType).Returns(analyticsData.ElementType);
			analyticsMockSet.As<IQueryable<OneHourElectricity>>().Setup(m => m.GetEnumerator()).Returns(analyticsData.GetEnumerator());
			_analyticsRepositoryMock.Setup(m => m.Query()).Returns(analyticsMockSet.Object.AsQueryable());
			_analyticsController = new AnalyticsController(_analyticsRepositoryMock.Object, _panelRepositoryMock.Object);
		}

		[Fact]
		public async Task Get_ReturnsBadRequest_WhenpanelIdIdIsNull()
		{
			var result = await _analyticsController.Get(panelId: null);
			Assert.IsType<BadRequestResult>(result);
		}

		[Fact]
		public async Task Get_ReturnsBadRequestObject_WhenpanelIdIsInvalid()
		{
			var result = await _analyticsController.Get(panelId: "1234");

			var resultContent = Assert.IsType<BadRequestObjectResult>(result);

			Assert.Equal("Panel ID should have 16 characters", resultContent.Value);
		}

		[Fact]
		public async Task Get_ReturnsNotFound_WhenPanelNotFound()
		{
			var result = await _analyticsController.Get("1234567890123456");
			Assert.IsType<NotFoundResult>(result);
		}
		[Fact]
		public async Task Get_ReturnsOkViewResults_WithOneHourElectricityListModel()
		{
			var result = await _analyticsController.Get("CCCC3333DDDD4444");
			var okResult = Assert.IsType<OkObjectResult>(result);
			Assert.Equal(200, (result as OkObjectResult).StatusCode);
			var returnValue = Assert.IsType<OneHourElectricityListModel>(okResult.Value);
			Assert.NotNull(returnValue);
			Assert.Single(returnValue.OneHourElectricitys);
		}
		[Fact]
		public async Task DayResults_ReturnsBadRequest_WhenpanelIdIdIsNull()
		{
			var result = await _analyticsController.DayResults(panelId: null, It.IsAny<DateTime>());
			Assert.IsType<BadRequestResult>(result);
		}
		[Fact]
		public async Task DayResults_ReturnsBadRequestObject_WhenpanelIdIsInvalid()
		{
			var result = await _analyticsController.DayResults(panelId: "123", It.IsAny<DateTime>());
			var resultContent = Assert.IsType<BadRequestObjectResult>(result);
			Assert.Equal("Panel ID should have 16 characters", resultContent.Value);
		}
		[Fact]
		public async Task DayResults_ReturnsNotFound_WhenPanelNotFound()
		{
			var result = await _analyticsController.DayResults("1234567890123456", It.IsAny<DateTime>());
			Assert.IsType<NotFoundResult>(result);
		}
		[Fact]
		public async Task DayResults_ReturnsOkViewResults_WithOneDayElectricityModel()
		{
			var result = await _analyticsController.DayResults("AAAA1111BBBB2222", DateTime.Now);
			var okResult = Assert.IsType<OkObjectResult>(result);
			Assert.Equal(200, (result as OkObjectResult).StatusCode);
			var returnValue = Assert.IsType<OneDayElectricityModel>(okResult.Value);
			Assert.NotNull(returnValue);
			Assert.Equal(37,returnValue.Sum);
			Assert.Equal(25, returnValue.Maximum);
			Assert.Equal(12, returnValue.Minimum);
			Assert.Equal((decimal)18.5, returnValue.Average,1);
		}
		[Fact]
		public async Task Post_ReturnsBadRequest_WhenpanelIdIdIsNull()
		{
			var result = await _analyticsController.Post(panelId: null, It.IsAny<OneHourElectricityModel>());
			Assert.IsType<BadRequestResult>(result);
		}
		[Fact]
		public async Task Post_ReturnsBadRequestObject_WhenpanelIdIsInvalid()
		{
			var result = await _analyticsController.Post(panelId: "123", It.IsAny<OneHourElectricityModel>());
			var resultContent = Assert.IsType<BadRequestObjectResult>(result);
			Assert.Equal("Panel ID should have 16 characters", resultContent.Value);
		}
		[Fact]
		public async Task Post_ReturnsNotFound_WhenPanelNotFound()
		{
			var result = await _analyticsController.Post("1234567890123456", It.IsAny<OneHourElectricityModel>());
			Assert.IsType<NotFoundResult>(result);
		}
		[Fact]
		public async Task Post_ReturnsBadRequestResult_WhenModelIsInvalid()
		{
			var panelId = "CCCC3333DDDD4444";
			var oneHourElectricityContent = new OneHourElectricityModel
			{
				DateTime = DateTime.UtcNow
			};
			// Act
			_analyticsController.ModelState.AddModelError("KilloWatt", "Is Required");
			var result = await _analyticsController.Post(panelId, oneHourElectricityContent);
			// Assert
			Assert.NotNull(result);
			var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
			Assert.IsType<SerializableError>(badRequestResult.Value);
		}
		[Fact]
		public async Task Post_ReturnsCreatedResultOnPosting()
		{
			var panelId = "CCCC3333DDDD4444";
			var oneHourElectricityContent = new OneHourElectricityModel
			{
				KiloWatt = 10,
				DateTime = DateTime.UtcNow
			};
			// Act
			var result = await _analyticsController.Post(panelId,oneHourElectricityContent);
			// Assert
			Assert.NotNull(result);
			var createdResult = result as CreatedResult;
			Assert.NotNull(createdResult);
			Assert.Equal(201, createdResult.StatusCode);
		}
	}

}

