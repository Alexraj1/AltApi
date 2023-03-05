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
            var app= GetUser(apiUser.UserPrincipalName);
            if(app.ModifiedOn!=null)
                app.ModifiedOn =  DateTime.Now;
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
            return app;
        }
    }
}
