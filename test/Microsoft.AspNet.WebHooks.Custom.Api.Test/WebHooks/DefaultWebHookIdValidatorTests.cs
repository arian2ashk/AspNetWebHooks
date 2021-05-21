// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Microsoft.AspNet.WebHooks
{
    public class DefaultWebHookIdValidatorTests
    {
        private readonly HttpRequest _request;
        private readonly IWebHookIdValidator _validator;

        public DefaultWebHookIdValidatorTests()
        {
            _request = new Mock<HttpRequest>().Object;
            _validator = new DefaultWebHookIdValidator();
        }

        [Theory]
        [InlineData("a")]
        [InlineData("12345")]
        [InlineData("你好世界")]
        public async Task ValidateIfAsync_ForcesDefaultId(string id)
        {
            // Arrange
            WebHook webHook = new WebHook { Id = id };

            // Act
            await _validator.ValidateIdAsync(_request, webHook);

            // Assert
            Assert.NotEmpty(webHook.Id);
            Assert.NotEqual(id, webHook.Id);
        }
    }
}
