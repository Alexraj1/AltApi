using BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AltApi
{
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            UserFactory apiUsersFactory = new UserFactory();
            BLLApiUsers bllUser= apiUsersFactory.Login(txtPin.Value);
            Session.Remove("FMUsers");
            Session["FMUsers"] = bllUser;
            Response.Redirect("AltScan.aspx");
        }
    }
}