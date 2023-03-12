using AltApi.Api;
using AltApi.Api.Entities;
using BLL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AltApi
{
    public partial class AltScan : AltisUI
    {
        private async Task InitiateOneDriveApiAsync()
        {
            try
            {
                UserFactory apiUsersFactory = new UserFactory();
                BLLApiUsers bllUser = (BLLApiUsers)Session["FMUsers"];
                OneDriveApi = new OneDriveGraphApi(GetConfig("GraphApiApplicationId"), GetConfig("LocalUrl"), GetConfig("GraphApiClientSecret"));
                OneDriveApi.ProxyConfiguration = System.Net.WebRequest.DefaultWebProxy;
                OneDriveApi.AccessToken = new OneDriveAccessToken();
                OneDriveApi.SetAuthorizationToken(bllUser.Code);
                OneDriveApi.AccessToken.TokenType = bllUser.TokenType;
                OneDriveApi.AccessToken.AccessToken = bllUser.AccessToken;
                OneDriveApi.AccessToken.AccessTokenExpirationDuration = bllUser.AccessTokenExpirationDuration;
                OneDriveApi.AccessToken.RefreshToken = bllUser.RefreshToken;
                OneDriveApi.AccessToken.Scopes = bllUser.Scopes;
                OneDriveApi.AccessToken.AuthenticationToken = bllUser.AuthenticationToken;

            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["FMUsers"] == null)
            {
                Response.Redirect("Login.aspx");
            }
            else
            {
                RegisterAsyncTask(new PageAsyncTask(ValidateUser));
   
            }



        }
        private async Task ValidateUser()
        {
            try
            {
                await InitiateOneDriveApiAsync();
                await OneDriveApi.GetProfil();

            }
            catch (Exception ex)
            {

                Response.Write("Token INVALID. Send Mail(Todo) or  <a href='Default.aspx'>Log in</a>" + ex.Message);
                Response.End();
            }
        }
        private async Task UploaddDocumentAsync()
        {
            try
            {
                await InitiateOneDriveApiAsync();

                string fileToUpload = txtUploadFile.Text;

                // Define the anonynous method to respond to the file upload progress events
                EventHandler<OneDriveUploadProgressChangedEventArgs> progressHandler = delegate (object s, OneDriveUploadProgressChangedEventArgs a) { lblResult.Text += $"Uploading - {a.BytesSent} bytes sent / {a.TotalBytes} bytes total ({a.ProgressPercentage}%){Environment.NewLine}"; };

                // Subscribe to the upload progress event
                OneDriveApi.UploadProgressChanged += progressHandler;

                // Upload the file to the root of the OneDrive
                var data = await OneDriveApi.UploadFile(fileToUpload, await OneDriveApi.GetDriveRoot());

                // Unsubscribe from the upload progress event
                OneDriveApi.UploadProgressChanged -= progressHandler;

                // Display the result of the upload
                lblResult.Text = data != null ? data.OriginalJson : "Not available";

            }
            catch (Exception ex)
            {


                Response.Write(ex.Message);
                Response.End();
            }
        }
        private bool FileExists()
        {
            if ((filUpload.PostedFile != null) && (filUpload.PostedFile.ContentLength > 0))
            {
                string fn = System.IO.Path.GetFileName(filUpload.PostedFile.FileName);
                string SaveLocation = Server.MapPath("Upload") + "\\" + fn;
                try
                {
                    filUpload.PostedFile.SaveAs(SaveLocation);
                    txtUploadFile.Text = SaveLocation;
                }
                catch (Exception ex)
                {
                    lblResult.Text = "Error: " + ex.Message;
                }
            }
            return File.Exists(txtUploadFile.Text);

        }
        protected void btnUpload_Click(object sender, EventArgs e)
        {
            try
            {
                if (FileExists())
                    RegisterAsyncTask(new PageAsyncTask(UploaddDocumentAsync));
            }
            catch (Exception ex)
            {

                Response.Write(ex.Message);
                Response.End();
            }
          
           // _ = UploaddDocumentAsync();
        }
    }
}