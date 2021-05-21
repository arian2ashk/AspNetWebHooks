// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.TestUtilities;
using Moq;
using Xunit;

namespace Microsoft.AspNet.WebHooks.Controllers
{
    public class WebHookRegistrationsControllerTests
    {
        private const string Address = "http://localhost";
        private const string TestUser = "TestUser";
        private const string FilterName = "Filter";
        private const int WebHookCount = 8;

        private readonly ClaimsPrincipal _principal;
        private readonly WebHookRegistrationsControllerMock _controller;

        private readonly Mock<IWebHookRegistrationsManager> _regsMock;
        private readonly Mock<IWebHookRegistrar> _registrarMock;
        private readonly Mock<IWebHookIdValidator> _idValidator;

        public WebHookRegistrationsControllerTests()
        {
            _regsMock = new Mock<IWebHookRegistrationsManager>();
            _registrarMock = new Mock<IWebHookRegistrar>();
            _idValidator = new Mock<IWebHookIdValidator>();
            var logger = new Mock<ILogger<WebHookRegistrationsController>>();

            _principal = new ClaimsPrincipal();

            _controller = new WebHookRegistrationsControllerMock(_regsMock.Object, _idValidator.Object,
                new[] {_registrarMock.Object}, logger.Object)
            {
                ControllerContext = new ControllerContext() {HttpContext = new DefaultHttpContext() {User = _principal}}
            };
        }

        public static TheoryData<StoreResult, Type> StatusData =>
            new()
            {
                {StoreResult.Conflict, typeof(BadRequestObjectResult)},
                {StoreResult.NotFound, typeof(BadRequestObjectResult)},
                {StoreResult.OperationError, typeof(BadRequestObjectResult)},
            };

        public static TheoryData<IEnumerable<string>, IEnumerable<string>> PrivateFilterData
        {
            get
            {
                var empty = new string[0];
                return new TheoryData<IEnumerable<string>, IEnumerable<string>>
                {
                    {new[] {"你", "好", "世", "界"}, new[] {"你", "好", "世", "界"}},
                    {new[] {"MS_Private_"}, empty},

                    {new[] {"ms_private_abc"}, empty},
                    {new[] {"MS_Private_abc"}, empty},
                    {new[] {"MS_PRIVATE_abc"}, empty},
                    {new[] {"MS_PRIVATE_ABC"}, empty},

                    {new[] {"a", "ms_private_abc"}, new[] {"a"}},
                    {new[] {"a", "MS_Private_abc"}, new[] {"a"}},
                    {new[] {"a", "MS_PRIVATE_abc"}, new[] {"a"}},
                    {new[] {"a", "MS_PRIVATE_ABC"}, new[] {"a"}},

                    {new[] {"ms_private_abc", "a"}, new[] {"a"}},
                    {new[] {"MS_Private_abc", "a"}, new[] {"a"}},
                    {new[] {"MS_PRIVATE_abc", "a"}, new[] {"a"}},
                    {new[] {"MS_PRIVATE_ABC", "a"}, new[] {"a"}},
                };
            }
        }

        [Fact]
        public async Task Get_Returns_WebHooks()
        {
            // Arrange
            IEnumerable<WebHook> hooks = CreateWebHooks();
            _regsMock.Setup(r => r.GetWebHooksAsync(_principal, It.IsAny<Func<string, WebHook, Task>>()))
                .ReturnsAsync(hooks)
                .Verifiable();

            // Act
            var actual = (await _controller.Get()).GetValue<IEnumerable<WebHook>>();

            // Assert
            _regsMock.Verify();
            Assert.Equal(WebHookCount, actual.Count());
        }

        [Fact]
        public async Task Get_Returns_EmptyList()
        {
            // Arrange
            _regsMock.Setup(r => r.GetWebHooksAsync(_principal, It.IsAny<Func<string, WebHook, Task>>()))
                .ReturnsAsync(new WebHook[0])
                .Verifiable();

            // Act
            var actual = (await _controller.Get()).GetValue<IEnumerable<WebHook>>();

            // Assert
            _regsMock.Verify();
            Assert.Empty(actual);
        }

        [Fact]
        public async Task Lookup_Returns_WebHook()
        {
            // Arrange
            var hook = CreateWebHook();
            _regsMock.Setup(r => r.LookupWebHookAsync(_principal, TestUser, It.IsAny<Func<string, WebHook, Task>>()))
                .ReturnsAsync(hook)
                .Verifiable();

            // Act
            var result = await _controller.Lookup(TestUser);
            var actual = result.GetValue<WebHook>();

            // Assert
            Assert.Equal(TestUser, actual.Id);
        }

        [Fact]
        public async Task Lookup_ReturnsNotFound_IfNotFoundWebHook()
        {
            // Arrange
            _regsMock.Setup(r => r.LookupWebHookAsync(_principal, TestUser, It.IsAny<Func<string, WebHook, Task>>()))
                .ReturnsAsync((WebHook)null)
                .Verifiable();

            // Act
            var actual = await _controller.Lookup(TestUser);

            // Assert
            Assert.IsType<NotFoundResult>(actual);
        }

        [Fact]
        public async Task Post_ReturnsBadRequest_IfNoRequestBody()
        {
            // Act
            var actual = await _controller.Post(webHook: null);

            // Assert
            Assert.IsType<BadRequestResult>(actual);
        }

        [Theory]
        [InlineData(true, false, false, false)]
        [InlineData(false, true, false, false)]
        [InlineData(false, false, true, false)]
        [InlineData(false, false, false, true)]
        public async Task Post_ReturnsBadRequest_IfValidationFails(bool failId, bool failSecret, bool failFilters, bool failAddress)
        {
            // Arrange
            var webHook = CreateWebHook();
            if (failId)
            {
                _idValidator.Setup(v => v.ValidateIdAsync(It.IsAny<HttpRequest>(), webHook))
                    .Throws<Exception>();
            }
            if (failSecret)
            {
                _regsMock.Setup(v => v.VerifySecretAsync(webHook))
                    .Throws<Exception>();
            }
            if (failFilters)
            {
                _regsMock.Setup(v => v.VerifyFiltersAsync(webHook))
                    .Throws<Exception>();
            }
            if (failAddress)
            {
                _regsMock.Setup(v => v.VerifyAddressAsync(webHook))
                    .Throws<Exception>();
            }

            // Act
            var actual = await _controller.Post(webHook);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, actual.GetStatusCode());
        }

        [Fact]
        public async Task Post_ReturnsInternalServerError_IfStoreThrows()
        {
            // Arrange
            var webHook = CreateWebHook();
            _regsMock.Setup(s => s.AddWebHookAsync(_principal, webHook, It.IsAny<Func<string, WebHook, Task>>()))
                .Throws<Exception>();

            // Act
            var actual = await _controller.Post(webHook);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, (actual.GetStatusCode()));
        }

        [Theory]
        [MemberData(nameof(StatusData))]
        public async Task Post_ReturnsError_IfStoreReturnsNonsuccess(StoreResult result, Type response)
        {
            // Arrange
            var webHook = CreateWebHook();
            _regsMock.Setup(s => s.AddWebHookAsync(_principal, webHook, It.IsAny<Func<string, WebHook, Task>>()))
                .ReturnsAsync(result);

            // Act
            var actual = await _controller.Post(webHook);

            // Assert
            Assert.IsType(response, actual);
        }

        [Fact]
        public async Task Post_ReturnsCreated_IfValidWebHook()
        {
            // Arrange
            var webHook = CreateWebHook();
            _regsMock.Setup(s => s.AddWebHookAsync(_principal, webHook, It.IsAny<Func<string, WebHook, Task>>()))
                .ReturnsAsync(StoreResult.Success);

            // Act
            var actual = await _controller.Post(webHook);

            // Assert
            Assert.IsType<CreatedAtRouteResult>(actual);
        }

        [Fact]
        public async Task Put_ReturnsBadRequest_IfNoRequestBody()
        {
            // Act
            var actual = await _controller.Put(TestUser, webHook: null);

            // Assert
            Assert.IsType<BadRequestResult>(actual);
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public async Task Put_ReturnsBadRequest_IfValidationFails(bool failSecret, bool failFilters, bool failAddress)
        {
            // Arrange
            var webHook = CreateWebHook();
            if (failSecret)
            {
                _regsMock.Setup(v => v.VerifySecretAsync(webHook))
                    .Throws<Exception>();
            }
            if (failFilters)
            {
                _regsMock.Setup(v => v.VerifyFiltersAsync(webHook))
                    .Throws<Exception>();
            }
            if (failAddress)
            {
                _regsMock.Setup(v => v.VerifyAddressAsync(webHook))
                    .Throws<Exception>();
            }

            // Act
            var actual = await _controller.Put(TestUser, webHook);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, actual.GetStatusCode());
        }

        [Fact]
        public async Task Put_ReturnsBadRequest_IfWebHookIdDiffersFromUriId()
        {
            // Arrange
            var webHook = CreateWebHook();

            // Act
            var actual = await _controller.Put("unknown", webHook);

            // Assert
            Assert.IsType<BadRequestObjectResult>(actual);
        }

        [Fact]
        public async Task Put_ReturnsInternalServerError_IfStoreThrows()
        {
            // Arrange
            var webHook = CreateWebHook();
            _regsMock.Setup(s => s.UpdateWebHookAsync(_principal, webHook, It.IsAny<Func<string, WebHook, Task>>()))
                .Throws<Exception>();

            // Act
            var actual = await _controller.Put(TestUser, webHook);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, actual.GetStatusCode());
        }

        [Theory]
        [MemberData(nameof(StatusData))]
        public async Task Put_ReturnsError_IfStoreReturnsNonsuccess(StoreResult result, Type response)
        {
            // Arrange
            var webHook = CreateWebHook();
            _regsMock.Setup(s => s.UpdateWebHookAsync(_principal, webHook, It.IsAny<Func<string, WebHook, Task>>()))
                .ReturnsAsync(result);

            // Act
            var actual = await _controller.Put(TestUser, webHook);

            // Assert
            Assert.IsType(response, actual);
        }

        [Fact]
        public async Task Put_ReturnsOk_IfValidWebHook()
        {
            // Arrange
            var webHook = CreateWebHook();
            _regsMock.Setup(s => s.UpdateWebHookAsync(_principal, webHook, null))
                .ReturnsAsync(StoreResult.Success);

            // Act
            var actual = await _controller.Put(webHook.Id, webHook);

            // Assert
            Assert.IsType<CreatedAtRouteResult>(actual);
        }

        [Fact]
        public async Task AddPrivateFilters_Calls_RegistrarWithNoFilter()
        {
            // Arrange
            var webHook = new WebHook();
            _registrarMock.Setup(r => r.RegisterAsync(It.IsAny<HttpRequest>(), webHook))
                .Returns(Task.FromResult(true))
                .Verifiable();

            // Act
            await _controller.AddPrivateFilters("12345", webHook);

            // Assert
            _registrarMock.Verify();
        }

        [Fact]
        public async Task AddPrivateFilters_Calls_RegistrarWithFilter()
        {
            // Arrange
            var webHook = new WebHook();
            webHook.Filters.Add(FilterName);
            _registrarMock.Setup(r => r.RegisterAsync(It.IsAny<HttpRequest>(), webHook))
                .Returns(Task.FromResult(true))
                .Verifiable();

            // Act
            await _controller.AddPrivateFilters("12345", webHook);

            // Assert
            _registrarMock.Verify();
        }

        [Fact]
        public async Task AddPrivateFilters_Throws_IfRegistrarThrows()
        {
            // Arrange
            var ex = new Exception("Catch this!");
            var webHook = new WebHook();
            webHook.Filters.Add(FilterName);
            _registrarMock.Setup(r => r.RegisterAsync(It.IsAny<HttpRequest>(), webHook))
                .Throws(ex);

            // Act
            var rex = await Assert.ThrowsAsync<Exception>(() => _controller.AddPrivateFilters("12345", webHook));

            // Assert
            Assert.Equal("The 'IWebHookRegistrarProxy' implementation of 'IWebHookRegistrar' caused an exception: Catch this!", rex.Message);
        }

        [Theory]
        [MemberData(nameof(PrivateFilterData))]
        public void RemovePrivateFilters_Succeeds(string[] input, string[] expected)
        {
            // Arrange
            var webHook = new WebHook();
            foreach (var i in input)
            {
                webHook.Filters.Add(i);
            }

            // Act
            _controller.RemovePrivateFilters(TestUser, webHook);

            // Assert
            Assert.Equal(expected, webHook.Filters);
        }

        private static WebHook CreateWebHook(string id = TestUser, string filterName = FilterName,
            bool addPrivateFilter = false)
        {
            var webHook = new WebHook()
            {
                Id = id,
                WebHookUri = new Uri(Address)
            };

            webHook.Filters.Add(filterName);
            if (addPrivateFilter)
            {
                var privateFilter = WebHookRegistrar.PrivateFilterPrefix + "abc";
                webHook.Filters.Add(privateFilter);
            }

            return webHook;
        }

        private Collection<WebHook> CreateWebHooks(bool addPrivateFilter = false)
        {
            var hooks = new Collection<WebHook>();
            for (var i = 0; i < WebHookCount; i++)
            {
                var webHook = CreateWebHook(id: i.ToString(), filterName: "a" + i.ToString(),
                    addPrivateFilter: addPrivateFilter);
                hooks.Add(webHook);
            }

            return hooks;
        }

        private class WebHookRegistrationsControllerMock : WebHookRegistrationsController
        {
            public new Task RemovePrivateFilters(string user, WebHook webHook)
            {
                return base.RemovePrivateFilters(user, webHook);
            }

            public new Task AddPrivateFilters(string user, WebHook webHook)
            {
                return base.AddPrivateFilters(user, webHook);
            }

            public WebHookRegistrationsControllerMock(IWebHookRegistrationsManager registrationsManager,
                IWebHookIdValidator webHookIdValidator, IEnumerable<IWebHookRegistrar> webHookRegistrars,
                ILogger<WebHookRegistrationsController> logger) : base(registrationsManager, webHookIdValidator,
                webHookRegistrars, logger)
            {
            }
        }
    }
}
