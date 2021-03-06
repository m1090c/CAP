﻿using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.PostgreSql
{
    public class CapPublisher : CapPublisherBase
    {
        private readonly ILogger _logger;
        private readonly PostgreSqlOptions _options;
        private readonly DbContext _dbContext;

        public CapPublisher(IServiceProvider provider,
            ILogger<CapPublisher> logger,
            PostgreSqlOptions options)
        {
            ServiceProvider = provider;
            _options = options;
            _logger = logger;

            if (_options.DbContextType != null)
            {
                IsUsingEF = true;
                _dbContext = (DbContext)ServiceProvider.GetService(_options.DbContextType);           
            }
        }

        protected override void PrepareConnectionForEF()
        {
            DbConnection = _dbContext.Database.GetDbConnection();
            var transaction = _dbContext.Database.CurrentTransaction;
            if (transaction == null)
            {
                IsCapOpenedTrans = true;
                transaction = _dbContext.Database.BeginTransaction(IsolationLevel.ReadCommitted);
            }
            DbTranasaction = transaction.GetDbTransaction();
        }

        protected override void Execute(IDbConnection dbConnection, IDbTransaction dbTransaction, CapPublishedMessage message)
        {
            dbConnection.Execute(PrepareSql(), message, dbTransaction);

            _logger.LogInformation("Published Message has been persisted in the database. name:" + message.ToString());
        }

        protected override async Task ExecuteAsync(IDbConnection dbConnection, IDbTransaction dbTransaction, CapPublishedMessage message)
        {
            await dbConnection.ExecuteAsync(PrepareSql(), message, dbTransaction);

            _logger.LogInformation("Published Message has been persisted in the database. name:" + message.ToString());
        }

        private string PrepareSql()
        {
            return $"INSERT INTO \"{_options.Schema}\".\"published\" (\"Name\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")VALUES(@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName)";
        }
    }
}