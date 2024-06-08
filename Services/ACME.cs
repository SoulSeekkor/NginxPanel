namespace NginxPanel.Services
{
    public class ACME
	{

        #region Variables

        private CLI _CLI;

        private string _version = "";

        #endregion

        #region Properties

        public string Version
        {
            get { return _version; }
        }

        #endregion

        #region Constructors

        public ACME(CLI CLI)
        {
            _CLI = CLI;

            GetVersion();
        }

        #endregion

        public void GetVersion()
        {
            _version = "";

            _CLI.RunCommand(".acme.sh/acme.sh", "-v");
            _version = _CLI.StandardOut;
        }
    }
}