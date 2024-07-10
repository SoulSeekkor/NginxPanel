namespace NginxPanel.Services
{
    public class AuthState
    {
        public bool Authenticated {  get { return _authenticated; }
    }

        private bool _authenticated = false;

        public AuthState()
        {
            if (!AppConfig.DUOEnabled)
            {
                _authenticated = true;
            }
        }
    }
}