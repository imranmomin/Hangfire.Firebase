using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using Hangfire.Storage;
using FireSharp.Response;


namespace Hangfire.Firebase.Queue
{
    internal class JobQueue : IPersistentJobQueue
    {
        private readonly FirebaseStorage storage;
        private readonly FirebaseConnection connection;
        private readonly string dequeueLockKey = "locks:job:dequeue";
        private readonly TimeSpan defaultLockTimeout = TimeSpan.FromMinutes(2);
        private readonly TimeSpan checkInterval = TimeSpan.FromSeconds(10);
        private readonly object syncLock = new object();

        public JobQueue(FirebaseStorage storage)
        {
            this.storage = storage;
            this.connection = (FirebaseConnection)storage.GetConnection();
        }

        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                lock (syncLock)
                {
                    using (FirebaseDistributedLock @lock = new FirebaseDistributedLock(dequeueLockKey, defaultLockTimeout, connection.Client))
                    {
                        FirebaseResponse response = connection.Client.Get("queue");
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            Dictionary<string, Dictionary<string, string>> collection = response.ResultAs<Dictionary<string, Dictionary<string, string>>>();
                            if (collection != null)
                            {
                                var data = collection.Select(q => new { Queue = q.Key, Data = q.Value.Select(v => new { Reference = v.Key, JobId = v.Value }).FirstOrDefault() })
                                                     .Select(q => new { q.Queue, q.Data.Reference, q.Data.JobId })
                                                     .FirstOrDefault();

                                if (!string.IsNullOrEmpty(data.JobId) && !string.IsNullOrEmpty(data.Reference))
                                {
                                    connection.Client.Delete($"queue/{data.Queue}/{data.Reference}");
                                    return new FetchedJob(storage, data.Queue, data.JobId, data.Reference);
                                }
                            }
                        }
                    }
                }
                Thread.Sleep(checkInterval);
            }
        }

        public void Enqueue(string queue, string jobId) => connection.Client.Push($"queue/{queue}", jobId);
    }
}