// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Crank.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net.Http;
using System.Text;
using System.Net;
using Azure.Core;
using Azure.Identity;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.ConstrainedExecution;

namespace Microsoft.Crank.Controller.Serializers
{
    public class JobSerializer
    {
        public static Task WriteJobResultsToSqlAsync(
            JobResults jobResults, 
            string sqlConnectionString,
            string tableName,
            string session,
            string scenario,
            string description,
            bool certBasedAuth = false,
            string certPath = ""
            )
        {
            var utcNow = DateTime.UtcNow;

            var document = JsonConvert.SerializeObject(jobResults, Formatting.None, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

            return RetryOnExceptionAsync(5, () =>
                 WriteResultsToSql(
                    utcNow,
                    sqlConnectionString,
                    tableName,
                    session,
                    scenario,
                    description,
                    document
                    )
                , 5000);
        }

        public static async Task InitializeDatabaseAsync(
            string connectionString,
            string tableName,
            bool certBasedAuth = false,
            string certPath = "",
            string certThumbprint = "",
            string certTenantId = "",
            string certClientId = "")
        {
            var createCmd =
                @"
                IF OBJECT_ID(N'dbo." + tableName + @"', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[" + tableName + @"](
                        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Excluded] [bit] DEFAULT 0,
                        [DateTimeUtc] [datetimeoffset](7) NOT NULL,
                        [Session] [nvarchar](200) NOT NULL,
                        [Scenario] [nvarchar](200) NOT NULL,
                        [Description] [nvarchar](200) NOT NULL,
                        [Document] [nvarchar](max) NOT NULL
                    )
                END
                ";

            await RetryOnExceptionAsync(5, () => InitializeDatabaseInternalAsync(connectionString, createCmd, certBasedAuth, certPath, certThumbprint, certTenantId, certClientId), 5000);

            static async Task InitializeDatabaseInternalAsync(string connectionString, string createCmd, bool certBasedAuth, string certPath, string certThumbprint, string certTenantId, string certClientId)
            {
                using (var connection = GetSqlConnection(connectionString, certBasedAuth, certPath, certThumbprint, certTenantId, certClientId))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(createCmd, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        private static async Task WriteResultsToSql(
            DateTime utcNow,
            string connectionString,
            string tableName,
            string session,
            string scenario,
            string description,
            string document,
            bool certBasedAuth = false,
            string certPath = "",
            string certThumbprint = "",
            string certTenantId = "",
            string certClientId = ""
            )
        {

            var insertCmd =
                @"
                INSERT INTO [dbo].[" + tableName + @"]
                           ([DateTimeUtc]
                           ,[Session]
                           ,[Scenario]
                           ,[Description]
                           ,[Document])
                     VALUES
                           (@DateTimeUtc
                           ,@Session
                           ,@Scenario
                           ,@Description
                           ,@Document)
                ";

            using (var connection = GetSqlConnection(connectionString, certBasedAuth, certPath, certThumbprint, certTenantId, certClientId))
            {
                await connection.OpenAsync();
                var transaction = connection.BeginTransaction();

                try
                {
                    var command = new SqlCommand(insertCmd, connection, transaction);
                    var p = command.Parameters;
                    p.AddWithValue("@DateTimeUtc", utcNow);
                    p.AddWithValue("@Session", session);
                    p.AddWithValue("@Scenario", scenario ?? "");
                    p.AddWithValue("@Description", description ?? "");
                    p.AddWithValue("@Document", document);

                    await command.ExecuteNonQueryAsync();

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                finally
                {
                    transaction.Dispose();
                }
            }
        }
 public static Task WriteJobResultsToEsAsync(
                 JobResults jobResults,
                 string elasticSearchUrl,
                 string indexName,
                 string session,
                 string scenario,
                 string description
                 )
      {
          var utcNow = DateTime.UtcNow;
          return RetryOnExceptionAsync(5, () =>
               WriteResultsToEs(
                  utcNow,
                  elasticSearchUrl,
                  indexName,
                  session,
                  scenario,
                  description,
                  jobResults
                  )
              , 5000);
      }

      public static async Task InitializeElasticSearchAsync(string elasticSearchUrl, string indexName)
      {
          var mappingQuery =
              @"{""settings"": {""number_of_shards"": 1},""mappings"": { ""dynamic"": false, 
              ""properties"": {
              ""DateTimeUtc"": { ""type"": ""date"" },
              ""Session"": { ""type"": ""keyword"" },
              ""Scenario"": { ""type"": ""keyword"" },
              ""Description"": { ""type"": ""keyword"" },
              ""Document"": { ""type"": ""nested"" }}}}";

          await RetryOnExceptionAsync(5, () => InitializeDatabaseInternalAsync(elasticSearchUrl,indexName, mappingQuery), 5000);

          static async Task InitializeDatabaseInternalAsync(string elasticSearchUrl,string indexName, string mappingQuery)
          {
              using (var httpClient = new HttpClient())
              {
                  HttpResponseMessage hrm = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"{elasticSearchUrl}/{indexName.ToLower()}"));
                  if (hrm.StatusCode == HttpStatusCode.NotFound)
                  {
                      hrm = await httpClient.PutAsync($"{elasticSearchUrl}/{indexName.ToLower()}", new StringContent(mappingQuery, Encoding.UTF8, "application/json"));
                      if (hrm.StatusCode != HttpStatusCode.OK)
                      {
                          throw new System.Exception(await hrm.Content.ReadAsStringAsync());
                      }
                  }
              }
          }
      }

      private static async Task WriteResultsToEs(
          DateTime utcNow,
          string elasticSearchUrl,
          string indexName,
          string session,
          string scenario,
          string description,
          JobResults jobResults
          )
      {
          var result = new
          {
              DateTimeUtc = utcNow,
              Session = session,
              Scenario = scenario,
              Description = description,
              Document = jobResults
          };

          using (var httpClient = new HttpClient())
          {
              var item = JsonConvert.SerializeObject(result, Formatting.None, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
              HttpResponseMessage hrm = await httpClient.PostAsync(elasticSearchUrl+"/"+indexName.ToLower() + "/_doc/" + session, new StringContent(item.ToString(), Encoding.UTF8, "application/json"));
              if (!hrm.IsSuccessStatusCode)
              {
                  throw new Exception(hrm.RequestMessage?.ToString());
              }
          }
      }

        private static SqlConnection GetSqlConnection(
            string connectionString,
            bool certBasedAuth,
            string certPath,
            string certThumbprint,
            string certTenantId,
            string certClientId)
        {
            AccessToken token = default;          
            if (certBasedAuth)
            {
                ClientCertificateCredential ccc = null;
                X509Store store = null;
                for (int i = 1; i <= 8; i++)
                {
                    store = new X509Store((StoreName)i, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    foreach (var cert in store.Certificates)
                    {
                        if (cert.Thumbprint == certThumbprint)
                        {
                            ccc = new ClientCertificateCredential(certTenantId, certClientId, cert);
                            break;
                        }
                    }
                    if (ccc != null)
                    {
                        break;
                    }
                }
                if(ccc == null)
                {
                    ccc = new ClientCertificateCredential(certTenantId, certClientId, certPath);
                }
                TokenRequestContext trc = new TokenRequestContext(new string[] { "https://database.windows.net/.default" });
                token = ccc.GetToken(trc);
            }
            var connection = new SqlConnection(connectionString);
            if(certBasedAuth)
            {
                connection.AccessToken = token.Token;
            }
            return connection;
        }

        private async static Task RetryOnExceptionAsync(int retries, Func<Task> operation, int milliSecondsDelay = 0)
        {
            var attempts = 0;
            do
            {
                try
                {
                    attempts++;
                    await operation();
                    return;
                }
                catch (Exception e)
                {
                    if (attempts == retries + 1)
                    {
                        throw;
                    }

                    Log($"Attempt {attempts} failed: {e.Message}");

                    if (milliSecondsDelay > 0)
                    {
                        await Task.Delay(milliSecondsDelay);
                    }
                }
            } while (true);
        }

        private static void Log(string message)
        {
            var time = DateTime.Now.ToString("hh:mm:ss.fff");
            Console.WriteLine($"[{time}] {message}");
        }
    }
}
