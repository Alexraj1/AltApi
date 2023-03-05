using AltApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AltisAPI
{
    public partial class Default : AltisUI
    {
        #region Properties

        public OneDriveGraphApi OneDriveApi;
        public string AuthorizationCodeTextBox { get; set; }
        public string CurrentUrlTextBox { get; set; }
        public string AccessTokenTextBox { get; set; }
        public string RefreshTokenTextBox { get; set; }
        public string AccessTokenValidTextBox { get; set; }

        #endregion
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                InitiateOneDriveApi();
                LogVerification();
                if (Request.QueryString.Count == 0)
                {
                    Authenticate();
                }
                if (Request.QueryString["code"] != null)
                {
                    lblstatut.Text = "Redirecting";
                    RegisterAsyncTask(new PageAsyncTask(GetTokenAsync));
                }
            }
        }

        private void LogVerification()
        {
            try
            {
                if (!CheckLogPath())
                {
                    Log("Log path not accessible");
                    Response.Write("[Admin] Veuillez vérifier log path");
                    Response.End();
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }

        }

        #region OneDrive
        private void InitiateOneDriveApi()
        {
            try
            {
                OneDriveApi = new OneDriveGraphApi(GetConfig("GraphApiApplicationId"), GetConfig("LocalUrl"), GetConfig("GraphApiClientSecret"));
                OneDriveApi.ProxyConfiguration = System.Net.WebRequest.DefaultWebProxy;
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }
        private void Authenticate()
        {
            var authenticateUri = OneDriveApi.GetAuthenticationUri();
            Log("[API] Redirection : " + authenticateUri.AbsoluteUri);
            Response.Redirect(authenticateUri.AbsoluteUri);
        }
        private async Task GetTokenAsync()
        {
            try
            {


                AuthorizationCodeTextBox = Request.QueryString["code"];
                OneDriveApi.SetAuthorizationToken(AuthorizationCodeTextBox);
                Log("[API] Code : " + AuthorizationCodeTextBox);
                await OneDriveApi.GetAccessToken();
                lblstatut.Text = "Recuperation du code";
                if (OneDriveApi.AccessToken != null)
                {
                    // Show the access token information in the textboxes
                    AccessTokenTextBox = OneDriveApi.AccessToken.AccessToken;
                    RefreshTokenTextBox = OneDriveApi.AccessToken.RefreshToken;
                    AccessTokenValidTextBox = OneDriveApi.AccessTokenValidUntil.HasValue ? OneDriveApi.AccessTokenValidUntil.Value.ToString("dd-MM-yyyy HH:mm:ss") : "Not valid";
                    await OneDriveApi.GetProfil();
                    lblstatut.Text = "Recuperation votre information";
                    Log("[API] Code : " + AuthorizationCodeTextBox + Environment.NewLine + " User : " + OneDriveApi.oneDriveUserProfile.userPrincipalName + Environment.NewLine);
                    try
                    {
                        Db db1 = new Db();
                        db1.AddUser(OneDriveApi);
                        Log("[SQL] Code : " + AuthorizationCodeTextBox + Environment.NewLine + " User : " + OneDriveApi.oneDriveUserProfile.userPrincipalName + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        Log("[SQL] Code : " + AuthorizationCodeTextBox + Environment.NewLine + " SQL : " + ex.Message + Environment.NewLine);
                    }


                    lblstatut.Text = "Votre profil a été enregitrer avec succés";
                    //string db = "";
                    // Store the refresh token in the AppSettings so next time you don't have to log in anymore
                    // _configuration.AppSettings.Settings["OneDriveApiRefreshToken"].Value = RefreshTokenTextBox.Text;
                    //  _configuration.Save(ConfigurationSaveMode.Modified);
                    return;
                }
                else
                {
                    LogOut();
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }
        private void LogOut()
        {
            var signoutUri = OneDriveApi.GetSignOutUri();
            Response.Redirect(signoutUri.AbsoluteUri);
        }

        #endregion
    }
}