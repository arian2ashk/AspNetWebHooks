using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Microsoft.AspNet.WebHooks.Custom.WebHooks
{
    public interface IWebHookNotificationsManager
    {
        /// <summary>
        /// Submits a notification to all matching registered WebHooks. To match, the <see cref="WebHook"/> must be registered by the
        /// current <see cref="ControllerBase.User"/> and have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="user">The user for which to lookup and dispatch matching WebHooks.</param>
        /// <param name="action">The action describing the notification.</param>
        /// <param name="data">Optional additional data to include in the WebHook request.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        Task<int> NotifyAsync(IPrincipal user, string action, object data);

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
        Task<int> NotifyAsync(IPrincipal user, string action, object data, Func<WebHook, string, bool> predicate);

        /// <summary>
        /// Submits a notification to all matching registered WebHooks. To match, the <see cref="WebHook"/> must be registered by the
        /// current <see cref="ControllerBase.User"/> and have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="user">The user for which to lookup and dispatch matching WebHooks.</param>
        /// <param name="notifications">The set of notifications to include in the WebHook.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        Task<int> NotifyAsync(IPrincipal user, params NotificationDictionary[] notifications);

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
        Task<int> NotifyAsync(IPrincipal user, IEnumerable<NotificationDictionary> notifications, Func<WebHook, string, bool> predicate);

        /// <summary>
        /// Submits a notification to all matching registered WebHooks across all users. To match, the <see cref="WebHook"/> must 
        /// have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="action">The action describing the notification.</param>
        /// <param name="data">Optional additional data to include in the WebHook request.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        Task<int> NotifyAllAsync(string action, object data);

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
        Task<int> NotifyAllAsync(string action, object data, Func<WebHook, string, bool> predicate);

        /// <summary>
        /// Submits a notification to all matching registered WebHooks across all users. To match, the <see cref="WebHook"/> must 
        /// have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="notifications">The set of notifications to include in the WebHook.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        Task<int> NotifyAllAsync(params NotificationDictionary[] notifications);

        /// <summary>
        /// Submits a notification to all matching registered WebHooks across all users. To match, the <see cref="WebHook"/> must
        /// have a filter that matches one or more of the actions provided for the notification.
        /// </summary>
        /// <param name="notifications">The set of notifications to include in the WebHook.</param>
        /// <param name="predicate">A function to test each <see cref="WebHook"/> to see whether it fulfills the condition. The
        /// predicate is passed the <see cref="WebHook"/> and the user who registered it. If the predicate returns <c>true</c> then
        /// the <see cref="WebHook"/> is included; otherwise it is not.</param>
        /// <returns>The number of <see cref="WebHook"/> instances that were selected and subsequently notified about the actions.</returns>
        Task<int> NotifyAllAsync(IEnumerable<NotificationDictionary> notifications, Func<WebHook, string, bool> predicate);
    }
}