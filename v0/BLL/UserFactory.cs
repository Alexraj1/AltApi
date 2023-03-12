using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL
{
    public class UserFactory
    {
        public BLLApiUsers Login(string pin)
        {
            BLLApiUsers BllUser = new BLLApiUsers();
            using (var context = new ApiContext())
            {
                Users user = context.CardDetails.Where(x => x.CardNumber == pin).FirstOrDefault().Users;
                if (user == null)
                    throw new Exception("Code n'existe pas");
                if (!context.ApiUsers.Where(x => x.UserPrincipalName.Equals(user.EmailAddress)).Any())
                    throw new Exception("Token manquant. Envoie mail");

                DAL.ApiUsers apiUser = context.ApiUsers.Where(x => x.UserPrincipalName.Equals(user.EmailAddress)).FirstOrDefault();

                BllUser.Code = apiUser.Code;
                BllUser.AccessToken = apiUser.AccessToken;
                BllUser.RefreshToken = apiUser.RefreshToken;
                BllUser.AccessTokenValid = apiUser.AccessTokenValid;
            }
            return BllUser;
        }

        public DAL.ApiUsers GetUser(string userName)
        {
            using (var context = new ApiContext())
            {
                if (context.ApiUsers.Where(x => x.UserPrincipalName == userName).Any())
                    return context.ApiUsers.Where(x => x.UserPrincipalName == userName).FirstOrDefault();
            }
            return new ApiUsers();
        }
        public void SyncUser(BLLApiUsers apiUsers)
        {
            ApiUsers ap = GetApiUser(apiUsers);
            using (var db = new ApiContext())
            {
                if (db.ApiUsers.Where(x => x.UserPrincipalName == ap.UserPrincipalName).Any())
                {
                    db.ApiUsers.Attach(ap);
                    db.Entry(ap).State = System.Data.Entity.EntityState.Modified;
                }
                else
                {
                    db.ApiUsers.Add(ap);
                }
                db.SaveChanges();
            }
        }


        private ApiUsers GetApiUser(BLLApiUsers apiUser)
        {
            var app = GetUser(apiUser.UserPrincipalName);
            if (app.ModifiedOn != null)
                app.ModifiedOn = DateTime.Now;
            if (app.CreatedOn == null)
            {
                app.CreatedOn = DateTime.Now;
                app.ModifiedOn = DateTime.Now;
            }
            app.UserPrincipalName = apiUser.UserPrincipalName;
            app.Code = apiUser.Code;
            app.AccessToken = apiUser.AccessToken;
            app.RefreshToken = apiUser.RefreshToken;
            app.AccessTokenValid = apiUser.AccessTokenValid;
            app.Id = apiUser.Id;
            app.DisplayName = apiUser.DisplayName;
            app.GivenName = apiUser.GivenName;
            app.SurName = apiUser.SurName;
            app.TokenType = apiUser.TokenType;
            app.AccessToken = apiUser.AccessToken;
            app.AccessTokenExpirationDuration = apiUser.AccessTokenExpirationDuration;
            app.RefreshToken = apiUser.RefreshToken;
            app.Scopes = apiUser.Scopes;
            app.AuthenticationToken = apiUser.AuthenticationToken;

            return app;
        }
    }
}
