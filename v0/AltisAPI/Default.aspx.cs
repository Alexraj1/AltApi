using AltApi;
using AltApi.Api;
using BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AltApi
{
    public partial class Default : AltisUI
    {
        private string fileToUpload = @"C:\Users\User\Documents\GitHub\Altis\AltApi\Sample\test.pdf";
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
                if (Request.QueryString["login"] != null)
                {
                    lblstatut.Text = "Login in";
                  //  RegisterAsyncTask(new PageAsyncTask(UploaddDocumentAsync));
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
                        Session["OneDrive"] = OneDriveApi;
                        // Upload the file to the root of the OneDrive
                        //var data = await OneDriveApi.UploadFile(fileToUpload, await OneDriveApi.GetDriveRoot());
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

        //private async Task UploaddDocumentAsync()
        //{
        //    try
        //    {
        //        UserFactory apiUsersFactory = new UserFactory();
        //        BLLApiUsers bllUser = apiUsersFactory.Login("123");
        //        OneDriveApi = new OneDriveGraphApi(GetConfig("GraphApiApplicationId"), GetConfig("LocalUrl"), GetConfig("GraphApiClientSecret"));
        //        OneDriveApi.ProxyConfiguration = System.Net.WebRequest.DefaultWebProxy;
        //        OneDriveApi.AccessToken = new OneDriveAccessToken();
        //        OneDriveApi.SetAuthorizationToken(bllUser.Code);
        //       // _ = await OneDriveApi.GetAccessToken();

        //        OneDriveApi.AccessToken.TokenType = bllUser.TokenType;
        //        OneDriveApi.AccessToken.AccessToken = bllUser.AccessToken;
        //        OneDriveApi.AccessToken.AccessTokenExpirationDuration = bllUser.AccessTokenExpirationDuration;
        //        OneDriveApi.AccessToken.RefreshToken = bllUser.RefreshToken;
        //        OneDriveApi.AccessToken.Scopes = bllUser.Scopes;
        //        OneDriveApi.AccessToken.AuthenticationToken = bllUser.AuthenticationToken;
        //        //   OneDriveApi.AuthorizationToken = bllUser.AccessToken;
        //        // await OneDriveApi.GetAccessToken();
        //        if (OneDriveApi.AccessToken != null)
        //        {
        //        }


        //        string fileToUpload = txtUploadFile.Text;

        //        // Define the anonynous method to respond to the file upload progress events
        //        EventHandler<OneDriveUploadProgressChangedEventArgs> progressHandler = delegate (object s, OneDriveUploadProgressChangedEventArgs a) { txtResult.Text += $"Uploading - {a.BytesSent} bytes sent / {a.TotalBytes} bytes total ({a.ProgressPercentage}%){Environment.NewLine}"; };

        //        // Subscribe to the upload progress event
        //        OneDriveApi.UploadProgressChanged += progressHandler;

        //        // Upload the file to the root of the OneDrive
        //        var data = await OneDriveApi.UploadFile(fileToUpload, await OneDriveApi.GetDriveRoot());

        //        // Unsubscribe from the upload progress event
        //        OneDriveApi.UploadProgressChanged -= progressHandler;

        //        // Display the result of the upload
        //        txtResult.Text = data != null ? data.OriginalJson : "Not available";

        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //}

        #endregion

        //protected void btnUpload_Click(object sender, EventArgs e)
        //{
        //    RegisterAsyncTask(new PageAsyncTask(UploaddDocumentAsync));
        //}
    }
}