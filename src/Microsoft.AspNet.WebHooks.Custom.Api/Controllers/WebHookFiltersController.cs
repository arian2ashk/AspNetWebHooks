// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNet.WebHooks.Controllers
{
    /// <summary>
    /// The <see cref="WebHookRegistrationsController"/> allows the caller to get the list of filters 
    /// with which a WebHook can be registered. This enables a client to provide a user experience
    /// indicating which filters can be used when registering a <see cref="WebHook"/>. 
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WebHookFiltersController : ControllerBase
    {
        [NotNull] private readonly IWebHookFilterManager _filterManager;

        public WebHookFiltersController([NotNull] IWebHookFilterManager filterManager)
        {
            _filterManager = filterManager ?? throw new ArgumentNullException(nameof(filterManager));
        }

        /// <summary>
        /// Gets all WebHook filters that a user can register with. The filters indicate which WebHook
        /// events that this WebHook will be notified for.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the operation.</returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            IDictionary<string, WebHookFilter> filters = await _filterManager.GetAllWebHookFiltersAsync();
            return Ok(filters.Values);
        }
    }
}
