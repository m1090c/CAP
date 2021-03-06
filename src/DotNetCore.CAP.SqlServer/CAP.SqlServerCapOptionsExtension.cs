﻿using System;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal class SqlServerCapOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<SqlServerOptions> _configure;

        public SqlServerCapOptionsExtension(Action<SqlServerOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<IStorage, SqlServerStorage>();
            services.AddScoped<IStorageConnection, SqlServerStorageConnection>();
            services.AddScoped<ICapPublisher, CapPublisher>();
            services.AddTransient<IAdditionalProcessor, DefaultAdditionalProcessor>();

            var sqlServerOptions = new SqlServerOptions();

            _configure(sqlServerOptions);

            if (sqlServerOptions.DbContextType != null)
            {
                services.AddSingleton(x =>
                {
                    var dbContext = (DbContext)x.GetService(sqlServerOptions.DbContextType);
                    sqlServerOptions.ConnectionString = dbContext.Database.GetDbConnection().ConnectionString;
                    return sqlServerOptions;
                });
            }
            else
            {
                services.AddSingleton(sqlServerOptions);
            }
        }
    }
}