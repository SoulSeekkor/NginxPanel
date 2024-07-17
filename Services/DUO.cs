using DuoUniversal;
using System.Net;
using System.Net.Sockets;

namespace NginxPanel.Services
{
	public class DUO
	{
		private Client? _client = null;

		public DUO()
		{
			if (AppConfig.DUORequired)
			{
				_client = new ClientBuilder(AppConfig.DUOClientID, AppConfig.DUOSecretKey, AppConfig.DUOAPIHostname, $"https://{GetLocalIPAddress()}:{AppConfig.Port}").Build();
			}
		}

		public async Task<bool> DoHealthCheck()
		{
			bool result = false;

			if (AppConfig.DUORequired && _client != null)
				result = await _client.DoHealthCheck();

			return result;
		}

		public string GetAuthURL(string username, ref string state)
		{
			string result = string.Empty;

			if (AppConfig.DUORequired && _client != null && !String.IsNullOrWhiteSpace(username))
			{
				// Generate a random state value to tie the authentication steps together
				state = Client.GenerateState();

				// Get the URI of the Duo prompt from the client.  This includes an embedded authentication request.
				result = _client.GenerateAuthUri(username, state);
			}	

			return result;
		}

		public async Task<bool> ValidateAuth(string code, string username)
		{
			bool result = false;

			try
			{
				if (AppConfig.DUORequired && _client != null)
				{
					IdToken token = await _client.ExchangeAuthorizationCodeFor2faResult(code, username);
                    if (token != null && token.AuthResult.Result == "allow")
                    {
						result = true;
                    }
                }
			}
			catch
			{
				// Placeholder
			}

			return result;
		}

		private string GetLocalIPAddress()
		{
#if DEBUG
			// For dev we have to hardcode this
			return "localhost";
#endif
			try
			{
				var host = Dns.GetHostEntry(Dns.GetHostName());
				foreach (var ip in host.AddressList)
				{
					if (ip.AddressFamily == AddressFamily.InterNetwork)
					{
						return ip.ToString();
					}
				}
			}
			catch
			{
				// Placeholder
			}

			return "localhost";
		}
	}
}