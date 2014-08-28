using System;
using System.Collections.Generic;
using System.Data.Entity;
using HomeAutomationServer.Migrations;
using Microsoft.AspNet.Identity.EntityFramework;

namespace HomeAutomationServer.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public Guid ApiKey { get; set; }

        public DateTime? LastUpdate { get; set; }

        public DateTime? PumpDelayTo { get; set; }
        public bool? IsPumpOn { get; set; }

        public virtual ICollection<SensorRecord> Companies { get; set; }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection")
        {
            Database.SetInitializer<ApplicationDbContext>(new MigrateDatabaseToLatestVersion<ApplicationDbContext,Configuration>());

        }

        public DbSet<SensorRecord> SensorRecords { get; set; }
        public DbSet<PumpScheduleRecord> PumpScheduleRecords { get; set; }
    }
}