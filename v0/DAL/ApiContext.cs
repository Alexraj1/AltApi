using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class ApiContext : DbContext, IDisposable
    {
        public ApiContext() : base("name=ConnectionString") {

            Database.SetInitializer<ApiContext>(new CreateDatabaseIfNotExists<ApiContext>());
        }
        protected override void OnModelCreating(DbModelBuilder ModelBuilder)
        {
            ModelBuilder.Entity<ApiUsers>().HasKey(s=> s.UserPrincipalName);
            base.OnModelCreating(ModelBuilder);

        }
        public virtual DbSet<ApiUsers> ApiUsers { get; set; }
    }
}
