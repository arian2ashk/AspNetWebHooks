﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using Microsoft.AspNet.WebHooks.Config;
using Microsoft.AspNet.WebHooks.Properties;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.WebHooks
{
    /// <summary>
    /// Provides an <see cref="IWebHookReceiver"/> implementation which supports WebHooks generated by Stripe. 
    /// Set the '<c>MS_WebHookReceiverSecret_Stripe</c>' application setting to the application key defined in Stripe.
    /// The corresponding WebHook URI is of the form '<c>https://&lt;host&gt;/api/webhooks/incoming/stripe/{id}</c>'.
    /// For details about Stripe WebHooks, see <c>https://stripe.com/docs/webhooks</c>.
    /// </summary>
    public class StripeWebHookReceiver : WebHookReceiver, IDisposable
    {
        // Application setting to enable test mode
        internal const string PassThroughTestEvents = "MS_WebHookStripePassThroughTestEvents";

        internal const string RecName = "stripe";
        internal const int SecretMinLength = 16;
        internal const int SecretMaxLength = 128;

        internal const string EventUriTemplate = "https://api.stripe.com/v1/events/{0}";
        internal const string TestId = "evt_00000000000000";

        private readonly HttpClient _httpClient;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="StripeWebHookReceiver"/> class.
        /// </summary>
        public StripeWebHookReceiver()
            : this(httpClient: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StripeWebHookReceiver"/> class with a given 
        /// <paramref name="httpClient"/>. This constructor is used for testing purposes.
        /// </summary>
        internal StripeWebHookReceiver(HttpClient httpClient)
        {
            _httpClient = httpClient ?? new HttpClient();
        }

        /// <summary>
        /// Gets the receiver name for this receiver.
        /// </summary>
        public static string ReceiverName
        {
            get { return RecName; }
        }

        /// <inheritdoc />
        public override string Name
        {
            get { return RecName; }
        }

        /// <inheritdoc />
        public override async Task<HttpResponseMessage> ReceiveAsync(string id, HttpRequestContext context, HttpRequestMessage request)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (request.Method == HttpMethod.Post)
            {
                // Read the request entity body
                JObject data = await ReadAsJsonAsync(request);

                // There is no security in this WebHook so we only pick out the ID and submit an independent request for the information.
                string notificationId = data.Value<string>("id");
                if (string.IsNullOrEmpty(notificationId))
                {
                    string msg = string.Format(CultureInfo.CurrentCulture, StripeReceiverResources.Receiver_BadBody, "id");
                    context.Configuration.DependencyResolver.GetLogger().Error(msg);
                    HttpResponseMessage badId = request.CreateErrorResponse(HttpStatusCode.BadRequest, msg);
                    return badId;
                }

                // If test ID then just return here.
                if (string.Equals(TestId, notificationId, StringComparison.OrdinalIgnoreCase))
                {
                    return await this.HandleTestEvent(id, context, request, data);
                }

                // Get data directly from Stripe as we don't know where the event comes from.
                data = await GetEventDataAsync(request, id, notificationId);
                string action = data.Value<string>("type");

                // Call registered handlers
                return await ExecuteWebHookAsync(id, context, request, new string[] { action }, data);
            }
            else
            {
                return CreateBadMethodResponse(request);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <b>false</b> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    if (_httpClient != null)
                    {
                        _httpClient.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the event data for this ID from the authenticated source so that we know that it is valid.
        /// </summary>
        protected virtual async Task<JObject> GetEventDataAsync(HttpRequestMessage request, string id, string notificationId)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }
            if (notificationId == null)
            {
                throw new ArgumentNullException("notificationId");
            }

            // Create HTTP request for requesting authoritative event data from Stripe
            string secretKey = await GetReceiverConfig(request, Name, id, SecretMinLength, SecretMaxLength);
            string address = string.Format(CultureInfo.InvariantCulture, EventUriTemplate, notificationId);
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, address);
            byte[] challenge = Encoding.UTF8.GetBytes(secretKey + ":");
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", EncodingUtilities.ToBase64(challenge, uriSafe: false));

            using (HttpResponseMessage rsp = await _httpClient.SendAsync(req))
            {
                if (!rsp.IsSuccessStatusCode)
                {
                    string msg = string.Format(CultureInfo.CurrentCulture, StripeReceiverResources.Receiver_BadId, notificationId);
                    request.GetConfiguration().DependencyResolver.GetLogger().Error(msg);
                    HttpResponseMessage badId = request.CreateErrorResponse(HttpStatusCode.BadRequest, msg);
                    throw new HttpResponseException(badId);
                }

                JObject result = await rsp.Content.ReadAsAsync<JObject>();
                return result;
            }
        }

        private async Task<HttpResponseMessage> HandleTestEvent(string id, HttpRequestContext context, HttpRequestMessage request, JObject data)
        {
            IDependencyResolver resolver = request.GetConfiguration().DependencyResolver;

            // Check to see if we have been configured to process test events
            SettingsDictionary settings = resolver.GetSettings();
            string passThroughTestEventsValue = settings.GetValueOrDefault(PassThroughTestEvents);

            bool passThroughTestEvents;
            if (bool.TryParse(passThroughTestEventsValue, out passThroughTestEvents) && passThroughTestEvents == true)
            {
                context.Configuration.DependencyResolver.GetLogger().Info(StripeReceiverResources.Receiver_TestEvent_Process);
                var action = data.Value<string>("type");
                return await ExecuteWebHookAsync(id, context, request, new string[] { action }, data);
            }
            else
            {
                context.Configuration.DependencyResolver.GetLogger().Info(StripeReceiverResources.Receiver_TestEvent);
                return request.CreateResponse();
            }
        }
    }
}
