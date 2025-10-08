using Microsoft.EntityFrameworkCore; 
using MOTPDualAuthWebsite.API.Models; 
  
namespace MOTPDualAuthWebsite.API.Data  
{ 
    public class ApplicationDbContext : DbContext  
    { 
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)  
        { } 
  
        public DbSet<User> Users { get; set; }  
        public DbSet<OTPCode> OTPCodes { get; set; }  
        public DbSet<BackupCode> BackupCodes { get; set; }
        public DbSet<SessionInfo> Sessions { get; set; } 
    }  
} 
