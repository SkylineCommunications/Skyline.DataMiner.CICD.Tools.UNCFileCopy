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
		private readonly string _domain;

		private readonly string _password;

		private readonly string _userName;

		/// <summary>
		/// Initializes a new instance of the <see cref="UserImpersonation"/> class.
		/// </summary>
		/// <param name="userName">The user name.</param>
		/// <param name="domain">The domain.</param>
		/// <param name="password">The password.</param>
		/// <exception cref="ArgumentException">Thrown when a required argument is null or empty.</exception>
		public UserImpersonation(string userName, string domain, string password)
		{
			if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("User name must not be null or empty.", nameof(userName));
			if (string.IsNullOrWhiteSpace(domain)) throw new ArgumentException("Domain must not be null or empty.", nameof(domain));
			if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password must not be null or empty.", nameof(password));

			_userName = userName;
			_domain = domain;
			_password = password;
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

			const int LOGON32_PROVIDER_DEFAULT = 0;
			const int LOGON32_LOGON_INTERACTIVE = 2;

			bool returnValue = LogonUser(_userName, _domain, _password, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, out SafeAccessTokenHandle safeAccessTokenHandle);
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