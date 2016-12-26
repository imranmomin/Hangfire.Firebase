using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Hangfire.Logging;
using Hangfire.Server;
using FireSharp.Response;
using Hangfire.Firebase.Entities;

namespace Hangfire.Firebase
{
#pragma warning disable 618
    internal class ExpirationManager : IServerComponent
#pragma warning restore 618
    {
        private static readonly ILog Logger = LogProvider.For<ExpirationManager>();
        private const string distributedLockKey = "expirationmanager";
        private static readonly TimeSpan defaultLockTimeout = TimeSpan.FromMinutes(5);
        private static readonly string[] documetns = new[] { "counters", "jobs", "lists", "sets", "hashs" };
        private readonly FirebaseConnection connection;
        private readonly TimeSpan checkInterval;

        public ExpirationManager(FirebaseStorage storage, TimeSpan checkInterval)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            this.connection = (FirebaseConnection)storage.GetConnection();
            this.checkInterval = checkInterval;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            foreach (var document in documetns)
            {
                Logger.Debug($"Removing outdated records from the '{document}' document.");

                using (FirebaseDistributedLock @lock = new FirebaseDistributedLock(distributedLockKey, defaultLockTimeout, connection.Client))
                {
                    FirebaseResponse respone = connection.Client.Get($"{document}");
                    if (respone.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Dictionary<string, IExpireEntity> collection = respone.ResultAs<Dictionary<string, IExpireEntity>>();
                        string[] references = collection.Where(c => c.Value.ExpireOn < DateTime.UtcNow).Select(c => c.Key).ToArray();

                        List<Task<FirebaseResponse>> tasks = new List<Task<FirebaseResponse>>();
                        Array.ForEach(references, reference =>
                        {
                            Task<FirebaseResponse> task = Task.Run(() => connection.Client.Delete($"{document}/{reference}"));
                            tasks.Add(task);
                        });
                        Task.WaitAll(tasks.ToArray());
                    }
                }

                Logger.Trace($"Outdated records removed from the '{document}' document.");

                cancellationToken.WaitHandle.WaitOne(checkInterval);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

    }
}

