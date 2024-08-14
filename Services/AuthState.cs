namespace NginxPanel.Services
{
    public class AuthState
    {
        private bool _authenticated = false;
        public bool Authenticated => (!AuthRequired || _authenticated);

        public bool AuthRequired => (AppConfig.UserRequired || AppConfig.DUORequired);

		public void SetAuthenticated()
        {
            _authenticated = true;
        }
    }
}