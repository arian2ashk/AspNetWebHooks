// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.WebHooks.Custom.Api.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.WebHooks.Controllers
{
    /// <summary>
    /// The <see cref="WebHookRegistrationsController"/> allows the caller to create, modify, and manage WebHooks
    /// through a REST-style interface.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WebHookRegistrationsController : ControllerBase
    {
        private readonly IWebHookRegistrationsManager _registrationsManager;
        private readonly IWebHookIdValidator _webHookIdValidator;
        private readonly IEnumerable<IWebHookRegistrar> _webHookRegistrars;
        private readonly ILogger<WebHookRegistrationsController> _logger;

        public WebHookRegistrationsController(IWebHookRegistrationsManager registrationsManager, IWebHookIdValidator webHookIdValidator, IEnumerable<IWebHookRegistrar> webHookRegistrars, ILogger<WebHookRegistrationsController> logger)
        {
            _registrationsManager = registrationsManager ?? throw new ArgumentNullException(nameof(registrationsManager));
            _logger = logger;
            _webHookIdValidator = webHookIdValidator;
            _webHookRegistrars = webHookRegistrars;
        }

        /// <summary>
        /// Gets all registered WebHooks for a given user.
        /// </summary>
        /// <returns>A collection containing the registered <see cref="WebHook"/> instances for a given user.</returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var webHooks = await _registrationsManager.GetWebHooksAsync(User, RemovePrivateFilters);
                return Ok(webHooks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(WebHookRegistrationsController));
            }
        }

        /// <summary>
        /// Looks up a registered WebHook with the given <paramref name="id"/> for a given user.
        /// </summary>
        /// <returns>The registered <see cref="WebHook"/> instance for a given user.</returns>
        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> Lookup(string id)
        {
            try
            {
                var webHook = await _registrationsManager.LookupWebHookAsync(User, id, RemovePrivateFilters);
                if (webHook != null)
                {
                    return Ok(webHook);
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(WebHookRegistrationsController));
            }
        }

        /// <summary>
        /// Registers a new WebHook for a given user.
        /// </summary>
        /// <param name="webHook">The <see cref="WebHook"/> to create.</param>
        [HttpPost]
        public async Task<IActionResult> Post(WebHook webHook)
        {
            if (webHook == null)
            {
                return BadRequest();
            }

            try
            {
                // Validate the provided WebHook ID (or force one to be created on server side)
                await _webHookIdValidator.ValidateIdAsync(Request, webHook);

                // Validate other parts of WebHook
                await _registrationsManager.VerifySecretAsync(webHook);
                await _registrationsManager.VerifyFiltersAsync(webHook);
                await _registrationsManager.VerifyAddressAsync(webHook);
            }
            catch (Exception ex)
            {
                var message = string.Format(CultureInfo.CurrentCulture, CustomApiResources.RegistrationController_RegistrationFailure, ex.Message);
                _logger.LogError(ex, message);
                return BadRequest(new
                {
                    detail = message,
                    instance = nameof(WebHookRegistrationsController)
                });
            }

            try
            {
                // Add WebHook for this user.
                var result = await _registrationsManager.AddWebHookAsync(User, webHook, AddPrivateFilters);
                if (result == StoreResult.Success)
                {
                    return CreatedAtRoute(new { id = webHook.Id }, webHook);
                }
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                var message = string.Format(CultureInfo.CurrentCulture, CustomApiResources.RegistrationController_RegistrationFailure, ex.Message);
                _logger.LogError(ex, message);
                return Problem(ex.Message, nameof(WebHookRegistrationsController));
            }
        }

        /// <summary>
        /// Updates an existing WebHook registration.
        /// </summary>
        /// <param name="id">The WebHook ID.</param>
        /// <param name="webHook">The new <see cref="WebHook"/> to use.</param>
        [Route("{id}")]
        [HttpPost]
        public async Task<IActionResult> Put(string id, WebHook webHook)
        {
            if (webHook == null)
            {
                return BadRequest();
            }
            if (!string.Equals(id, webHook.Id, StringComparison.OrdinalIgnoreCase))
            {
                var message = string.Format(CultureInfo.CurrentCulture, CustomApiResources.RegistrationController_RegistrationFailureOnId);
                return BadRequest(message);
            }

            try
            {
                // Validate parts of WebHook
                await _registrationsManager.VerifySecretAsync(webHook);
                await _registrationsManager.VerifyFiltersAsync(webHook);
                await _registrationsManager.VerifyAddressAsync(webHook);
            }
            catch (Exception ex)
            {
                var message = string.Format(CultureInfo.CurrentCulture, CustomApiResources.RegistrationController_RegistrationFailure, ex.Message);
                _logger.LogError(ex, message);
                return BadRequest(new
                {
                    detail = message,
                    instance = nameof(WebHookRegistrationsController)
                });
            }

            try
            {
                // Update WebHook for this user
                var result = await _registrationsManager.UpdateWebHookAsync(User, webHook, AddPrivateFilters);
                if (result == StoreResult.Success)
                {
                    return CreatedAtRoute(new { id = webHook.Id }, webHook);
                }
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                var message = string.Format(CultureInfo.CurrentCulture, CustomApiResources.RegistrationController_UpdateFailure, ex.Message);
                _logger.LogError(ex, message);
                return Problem(ex.Message, nameof(WebHookRegistrationsController));
            }
        }

        /// <summary>
        /// Deletes an existing WebHook registration.
        /// </summary>
        /// <param name="id">The WebHook ID.</param>
        [Route("{id}")]
        [HttpDelete]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var result = await _registrationsManager.DeleteWebHookAsync(User, id);
                if (result == StoreResult.Success)
                {
                    return Ok();
                }
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                var message = string.Format(CultureInfo.CurrentCulture, CustomApiResources.RegistrationController_DeleteFailure, ex.Message);
                _logger.LogError(ex, message);
                return Problem(ex.Message, nameof(WebHookRegistrationsController));
            }
        }

        /// <summary>
        /// Deletes all existing WebHook registrations.
        /// </summary>
        [Route("all")]
        [HttpDelete]
        public async Task<IActionResult> DeleteAll()
        {
            try
            {
                await _registrationsManager.DeleteAllWebHooksAsync(User);
                return Ok();
            }
            catch (Exception ex)
            {
                var message = string.Format(CultureInfo.CurrentCulture, CustomApiResources.RegistrationController_DeleteAllFailure, ex.Message);
                _logger.LogError(ex, message);
                return Problem(ex.Message, nameof(WebHookRegistrationsController));
            }
        }

        /// <summary>
        /// Removes all private (server side) filters from the given <paramref name="webHook"/>.
        /// </summary>
        protected virtual Task RemovePrivateFilters(string user, WebHook webHook)
        {
            if (webHook == null)
            {
                throw new ArgumentNullException(nameof(webHook));
            }

            var filters = webHook.Filters.Where(f => f.StartsWith(WebHookRegistrar.PrivateFilterPrefix, StringComparison.OrdinalIgnoreCase)).ToArray();
            foreach (var filter in filters)
            {
                webHook.Filters.Remove(filter);
            }
            return Task.FromResult(true);
        }

        /// <summary>
        /// Executes all <see cref="IWebHookRegistrar"/> instances for server side manipulation, inspection, or
        /// rejection of registrations. This can for example be used to add server side only filters that
        /// are not governed by <see cref="IWebHookFilterManager"/>.
        /// </summary>
        protected virtual async Task AddPrivateFilters(string user, WebHook webHook)
        {
            foreach (var registrar in _webHookRegistrars)
            {
                try
                {
                    await registrar.RegisterAsync(Request, webHook);
                }
                catch (Exception ex)
                {
                    var message = string.Format(CultureInfo.CurrentCulture, CustomApiResources.RegistrationController_RegistrarException, registrar.GetType().Name, nameof(IWebHookRegistrar), ex.Message);
                    _logger.LogError(ex, message);
                    throw new Exception(message);
                }
            }
        }
    }
}
