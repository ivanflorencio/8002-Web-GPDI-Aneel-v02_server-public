using System.Linq;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PeD.Core.Models;

namespace PeD.Data
{
    public class ContabPedDbContext : DbContext
    {
        #region DbSet's

        public DbSet<DadosContabPed> DadosContabPed { get; set; }

        ILoggerFactory _loggerFactory;

        #endregion

        public ContabPedDbContext(DbContextOptions<ContabPedDbContext> options, ILoggerFactory loggerFactory) : base(options)
        {
            _loggerFactory = loggerFactory;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DadosContabPed>(b =>
            {
                b.HasNoKey();
                b.ToView("VW_PED_CONTABIL");
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
#if DEBUG
            builder.UseLoggerFactory(_loggerFactory);
            builder.EnableSensitiveDataLogging();
#endif
        }
    }
}