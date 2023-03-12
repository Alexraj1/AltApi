using AltApi;
using AltApi.Api;
using BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AltApi


{
    public class Db
    {

        public void AddUser(string UserPrincipalName, string Code, string AccessToken, string RefreshToken, string AccessTokenValid
            , string Id, string DisplayName, string GivenName, string SurName)
        {
            BLL.BLLApiUsers apiUser = new BLL.BLLApiUsers();
            apiUser.UserPrincipalName = UserPrincipalName;
            apiUser.Code = Code;
            apiUser.AccessToken = AccessToken;
            apiUser.RefreshToken = RefreshToken;
            apiUser.AccessTokenValid = AccessTokenValid;
            apiUser.Id = Id;
            apiUser.DisplayName = DisplayName;
            apiUser.GivenName = GivenName;
            apiUser.SurName = SurName;

            UserFactory apiUsersFactory = new UserFactory();
            apiUsersFactory.SyncUser(apiUser);
        }

        internal void AddUser(OneDriveGraphApi oneDriveApi)
        {
            try
            {
                BLL.BLLApiUsers apiUser = new BLL.BLLApiUsers();
                //  apiUser.AccessToken = oneDriveApi.AccessToken.AccessToken;
                //  apiUser.RefreshToken = oneDriveApi.AccessToken.RefreshToken;
                apiUser.AccessTokenValid = oneDriveApi.AccessTokenValidUntil.HasValue ? oneDriveApi.AccessTokenValidUntil.Value.ToString("dd-MM-yyyy HH:mm:ss") : "Not valid";
                apiUser.Code = oneDriveApi.AuthorizationToken;
                apiUser.UserPrincipalName = oneDriveApi.oneDriveUserProfile.userPrincipalName;
                apiUser.Id = oneDriveApi.oneDriveUserProfile.Id;
                apiUser.DisplayName = oneDriveApi.oneDriveUserProfile.DisplayName;
                apiUser.GivenName = oneDriveApi.oneDriveUserProfile.GivenName;
                apiUser.SurName = oneDriveApi.oneDriveUserProfile.SurName;
                apiUser.TokenType = oneDriveApi.AccessToken.TokenType;
                apiUser.AccessToken = oneDriveApi.AccessToken.AccessToken;
                apiUser.AccessTokenExpirationDuration = oneDriveApi.AccessToken.AccessTokenExpirationDuration;
                apiUser.RefreshToken = oneDriveApi.AccessToken.RefreshToken;
                apiUser.Scopes = oneDriveApi.AccessToken.Scopes;
                apiUser.AuthenticationToken = oneDriveApi.AccessToken.AuthenticationToken;
                UserFactory apiUsersFactory = new UserFactory();
                apiUsersFactory.SyncUser(apiUser);
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
    }

}