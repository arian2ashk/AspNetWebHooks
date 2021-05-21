// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.AspNet.WebHooks.Controllers
{
    public class WebHookFiltersControllerTests
    {
        private readonly WebHookFiltersController _controller;

        public WebHookFiltersControllerTests()
        {
            WildcardWebHookFilterProvider provider = new WildcardWebHookFilterProvider();
            IWebHookFilterManager filterManager = new WebHookFilterManager(new[] { provider });
            _controller = new WebHookFiltersController(filterManager);
        }

        [Fact]
        public async Task Get_Returns_ExpectedFilters()
        {
            // Act
            var actual = (await _controller.Get()).GetValue<IEnumerable<WebHookFilter>>();
            
            // Assert
            Assert.Single(actual);
            Assert.Equal(WildcardWebHookFilterProvider.Name, actual.Single().Name);
        }
    }
}
