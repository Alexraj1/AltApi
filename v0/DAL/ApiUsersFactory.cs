using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class ApiUsersFactory
    {
        private void UpdateUser(ApiUsers apiUser)
        {
            using (var context = new ApiContext())
            {
                apiUser.CreatedOn = DateTime.Now;
                context.ApiUsers.Add(apiUser);
                context.SaveChanges();
            }
        }
        private void InsertUser(ApiUsers apiUser)
        {
            using (var context = new ApiContext())
            {
                apiUser.ModifiedOn = DateTime.Now;
                context.ApiUsers.Add(apiUser);
                context.SaveChanges();
            }
        }
        private ApiUsers GetUser(string userName)
        {
            using (var context = new ApiContext())
            {
                if(context.ApiUsers.Where(x => x.UserPrincipalName == userName).Any())
                return context.ApiUsers.Where(x=>x.UserPrincipalName== userName).FirstOrDefault();
            }
            return null;

        }
        public void SyncUser(ApiUsers apiUsers)
        {
            if (GetUser(apiUsers.UserPrincipalName) == null)
                InsertUser(apiUsers);
            else
                UpdateUser(apiUsers);

        }
    }
}
