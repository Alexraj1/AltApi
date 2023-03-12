using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class ApiUsers
    {
        [Key]
        public string UserPrincipalName { get; set; }
        public string Code { get; set; }
      //  public string AccessToken { get; set; }
      //  public string RefreshToken { get; set; }
        public string AccessTokenValid { get; set; }
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string GivenName { get; set; }
        public string SurName { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string TokenType { get; set; }
        public string AccessToken { get; set; }
        public int AccessTokenExpirationDuration { get; set; }
        public string RefreshToken { get; set; }
        public string Scopes { get; set; }
        public string AuthenticationToken { get; set; }

    }
}
