using BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            BLL.BLLApiUsers apiUser = new BLL.BLLApiUsers();

            apiUser.UserPrincipalName = "Alexraj1@hotmail.com";
            apiUser.Code = "99";
            apiUser.AccessToken = "122";
            apiUser.RefreshToken = "123";
            apiUser.AccessTokenValid = "QDSDF";
            apiUser.Id = "1111";
            apiUser.DisplayName = "QDSDF";
            apiUser.GivenName = "QDSDF";
            apiUser.SurName = "QDSDF";

            UserFactory apiUsersFactory = new UserFactory();
            apiUsersFactory.SyncUser(apiUser);
 }
    }
}
