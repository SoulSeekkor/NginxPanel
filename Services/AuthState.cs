namespace NginxPanel.Services
{
    public class AuthState
    {
        private bool _authenticated = false;
        public bool Authenticated
        {
            get
            {
                return (!AuthRequired || _authenticated);
            }
        }

        public bool AuthRequired
        {
            get
            {
                return (AppConfig.UserRequired || AppConfig.DUORequired);
            }
        }
        
        public void SetAuthenticated()
        {
            _authenticated = true;
        }
    }
}