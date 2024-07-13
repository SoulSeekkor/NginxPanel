namespace NginxPanel.Services
{
    public class AuthState
    {
        public bool Authenticated { get { return _authenticated; } }

        private bool _authenticated = false;

        public AuthState()
        {
            if (String.IsNullOrWhiteSpace(AppConfig.Username))
            {
                _authenticated = true;
            }
        }

        public void SetAuthenticated()
        {
            _authenticated = true;
        }
    }
}