using DuoUniversal;
using System.Net.Sockets;

namespace NginxPanel.Services
{
	public class DUO
	{
		private static Client? _client = null;
		private static string _state = string.Empty;

		public DUO()
		{
			if (AppConfig.DUORequired)
			{
				_client = new ClientBuilder(AppConfig.DUOClientID, AppConfig.DUOSecretKey, AppConfig.DUOAPIHostname, $"https://localhost:{AppConfig.Port}/dashboard").Build();
			}
		}

		public static async Task<bool> DoHealthCheck()
		{
			bool result = false;

			if (AppConfig.DUORequired && _client != null)
				result = await _client.DoHealthCheck();

			return result;
		}

		public static string GetAuthURL(string username)
		{
			string result = string.Empty;

			if (AppConfig.DUORequired && _client != null)
			{
				// Generate a random state value to tie the authentication steps together
				_state = Client.GenerateState();

				// Get the URI of the Duo prompt from the client.  This includes an embedded authentication request.
				result = _client.GenerateAuthUri(username, _state);
			}	

			return result;
		}
	}
}