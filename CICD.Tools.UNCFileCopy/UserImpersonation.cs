namespace Skyline.DataMiner.CICD.Tools.UNCFileCopy
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security.Principal;

    using Microsoft.Win32.SafeHandles;

    /// <summary>
    /// Allows for user impersonation in Windows to perform actions under a different user account.
    /// </summary>
    internal class UserImpersonation
    {
        private readonly string domain;
        private readonly string password;
        private readonly string username;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserImpersonation"/> class.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="domain">The domain.</param>
        /// <param name="password">The password.</param>
        /// <exception cref="ArgumentException">Thrown when a required argument is null or empty.</exception>
        public UserImpersonation(string username, string domain, string password)
        {
            if (String.IsNullOrWhiteSpace(username)) throw new ArgumentException("User name must not be null or empty.", nameof(username));
            if (String.IsNullOrWhiteSpace(domain)) throw new ArgumentException("Domain must not be null or empty.", nameof(domain));
            if (String.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password must not be null or empty.", nameof(password));

            this.username = username;
            this.domain = domain;
            this.password = password;
        }

        /// <summary>
        /// Executes a given action as the impersonated user.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <exception cref="ArgumentNullException">Thrown if the action is null.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if user logon fails.</exception>
        public void RunAsUser(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            const int logon32ProviderDefault = 0;
            const int logon32LogonInteractive = 2;

            bool returnValue = LogonUser(username, domain, password, logon32LogonInteractive, logon32ProviderDefault, out SafeAccessTokenHandle safeAccessTokenHandle);
            if (!returnValue)
            {
                throw new UnauthorizedAccessException("User logon failed. Check username, password, and domain.");
            }

            using (safeAccessTokenHandle)
            {
                WindowsIdentity.RunImpersonated(safeAccessTokenHandle, action);
            }
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LogonUser(string username, string domain, string password, int logonType, int logonProvider, out SafeAccessTokenHandle token);
    }
}