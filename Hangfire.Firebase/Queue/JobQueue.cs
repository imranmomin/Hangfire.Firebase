using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using FireSharp;
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
            int index = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                lock (syncLock)
                {
                    using (new FirebaseDistributedLock(dequeueLockKey, defaultLockTimeout, connection.Client))
                    {
                        QueryBuilder buidler = QueryBuilder.New();
                        buidler.OrderBy("$key");
                        buidler.LimitToFirst(1);

                        string queue = queues.ElementAt(index);
                        FirebaseResponse response = connection.Client.Get($"queue/{queue}", buidler);
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            Dictionary<string, string> collection = response.ResultAs<Dictionary<string, string>>();
                            var data = collection?.Select(q => new { Queue = queue, Reference = q.Key, JobId = q.Value })
                                .FirstOrDefault();

                            if (!string.IsNullOrEmpty(data?.JobId) && !string.IsNullOrEmpty(data.Reference))
                            {
                                connection.Client.Delete($"queue/{data.Queue}/{data.Reference}");
                                return new FetchedJob(storage, data.Queue, data.JobId, data.Reference);
                            }
                        }
                    }
                }

                Thread.Sleep(checkInterval);
                index = (index + 1) % queues.Length;
            }
        }

        public void Enqueue(string queue, string jobId) => connection.Client.Push($"queue/{queue}", jobId);
    }
}