using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.WebHooks.Custom.WebHooks
{
    public class WebHookNotificationsManager : IWebHookNotificationsManager
    {
        private readonly IWebHookManager _webHookManager;
        private readonly IWebHookUser _webHookUser;

        public WebHookNotificationsManager(IWebHookManager webHookManager, IWebHookUser webHookUser)
        {
            _webHookManager = webHookManager ?? throw new ArgumentNullException(nameof(webHookManager));
            _webHookUser = webHookUser ?? throw new ArgumentNullException(nameof(webHookUser));
        }

        /// <summary>
        /// Submits a notification to all matching registered WebHooks. To match, the <see cref="WebHook"/> must be registered by the
        /// current <see cref="ControllerBase.User"/> and have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="user">The user for which to lookup and dispatch matching WebHooks.</param>
        /// <param name="action">The action describing the notification.</param>
        /// <param name="data">Optional additional data to include in the WebHook request.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        public async Task<int> NotifyAsync(IPrincipal user, string action, object data)
        {
            var notifications = new[] { new NotificationDictionary(action, data) };
            return await NotifyAsync(user, notifications, predicate: null);
        }

        /// <summary>
        /// Submits a notification to all matching registered WebHooks. To match, the <see cref="WebHook"/> must be registered by the
        /// current <see cref="ControllerBase.User"/> and have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="user">The user for which to lookup and dispatch matching WebHooks.</param>
        /// <param name="action">The action describing the notification.</param>
        /// <param name="data">Optional additional data to include in the WebHook request.</param>
        /// <param name="predicate">A function to test each <see cref="WebHook"/> to see whether it fulfills the condition. The
        /// predicate is passed the <see cref="WebHook"/> and the user who registered it. If the predicate returns <c>true</c> then
        /// the <see cref="WebHook"/> is included; otherwise it is not.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        public async Task<int> NotifyAsync(IPrincipal user, string action, object data, Func<WebHook, string, bool> predicate)
        {
            var notifications = new[] { new NotificationDictionary(action, data) };
            return await NotifyAsync(user, notifications, predicate);
        }

        /// <summary>
        /// Submits a notification to all matching registered WebHooks. To match, the <see cref="WebHook"/> must be registered by the
        /// current <see cref="ControllerBase.User"/> and have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="user">The user for which to lookup and dispatch matching WebHooks.</param>
        /// <param name="notifications">The set of notifications to include in the WebHook.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        public async Task<int> NotifyAsync(IPrincipal user, params NotificationDictionary[] notifications)
        {
            return await NotifyAsync(user, notifications, predicate: null);
        }

        /// <summary>
        /// Submits a notification to all matching registered WebHooks. To match, the <see cref="WebHook"/> must be registered by the
        /// current <see cref="ControllerBase.User"/> and have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="user">The user for which to lookup and dispatch matching WebHooks.</param>
        /// <param name="notifications">The set of notifications to include in the WebHook.</param>
        /// <param name="predicate">A function to test each <see cref="WebHook"/> to see whether it fulfills the condition. The
        /// predicate is passed the <see cref="WebHook"/> and the user who registered it. If the predicate returns <c>true</c> then
        /// the <see cref="WebHook"/> is included; otherwise it is not.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        public async Task<int> NotifyAsync(IPrincipal user, IEnumerable<NotificationDictionary> notifications, Func<WebHook, string, bool> predicate)
        {
            if (notifications == null)
            {
                throw new ArgumentNullException(nameof(notifications));
            }
            if (!notifications.Any())
            {
                return 0;
            }

            // Get the User ID from the User principal
            string userId = await _webHookUser.GetUserIdAsync(user);

            // Send a notification to registered WebHooks with matching filters
            return await _webHookManager.NotifyAsync(userId, notifications, predicate);
        }

        /// <summary>
        /// Submits a notification to all matching registered WebHooks across all users. To match, the <see cref="WebHook"/> must 
        /// have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="action">The action describing the notification.</param>
        /// <param name="data">Optional additional data to include in the WebHook request.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        public async Task<int> NotifyAllAsync(string action, object data)
        {
            var notifications = new[] { new NotificationDictionary(action, data) };
            return await NotifyAllAsync(notifications, predicate: null);
        }

        /// <summary>
        /// Submits a notification to all matching registered WebHooks across all users. To match, the <see cref="WebHook"/> must 
        /// have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="action">The action describing the notification.</param>
        /// <param name="data">Optional additional data to include in the WebHook request.</param>
        /// <param name="predicate">A function to test each <see cref="WebHook"/> to see whether it fulfills the condition. The
        /// predicate is passed the <see cref="WebHook"/> and the user who registered it. If the predicate returns <c>true</c> then
        /// the <see cref="WebHook"/> is included; otherwise it is not.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        public async Task<int> NotifyAllAsync(string action, object data, Func<WebHook, string, bool> predicate)
        {
            var notifications = new[] { new NotificationDictionary(action, data) };
            return await NotifyAllAsync(notifications, predicate);
        }

        /// <summary>
        /// Submits a notification to all matching registered WebHooks across all users. To match, the <see cref="WebHook"/> must 
        /// have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="notifications">The set of notifications to include in the WebHook.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        public async Task<int> NotifyAllAsync(params NotificationDictionary[] notifications)
        {
            return await NotifyAllAsync(notifications, predicate: null);
        }

        /// <summary>
        /// Submits a notification to all matching registered WebHooks across all users. To match, the <see cref="WebHook"/> must
        /// have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="notifications">The set of notifications to include in the WebHook.</param>
        /// <param name="predicate">A function to test each <see cref="WebHook"/> to see whether it fulfills the condition. The
        /// predicate is passed the <see cref="WebHook"/> and the user who registered it. If the predicate returns <c>true</c> then
        /// the <see cref="WebHook"/> is included; otherwise it is not.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        public async Task<int> NotifyAllAsync(IEnumerable<NotificationDictionary> notifications, Func<WebHook, string, bool> predicate)
        {
            if (notifications == null)
            {
                throw new ArgumentNullException(nameof(notifications));
            }
            if (!notifications.Any())
            {
                return 0;
            }

            // Send a notification to registered WebHooks with matching filters
            return await _webHookManager.NotifyAllAsync(notifications, predicate);
        }
    }
}
