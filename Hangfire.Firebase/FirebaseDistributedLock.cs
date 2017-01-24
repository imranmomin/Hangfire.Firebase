using System;
using System.Linq;
using System.Collections.Generic;

using FireSharp;
using FireSharp.Response;
using Hangfire.Firebase.Entities;

namespace Hangfire.Firebase
{
    internal class FirebaseDistributedLock : IDisposable
    {
        private FirebaseClient client;
        private string lockReference;
        private object syncLock = new object();

        public FirebaseDistributedLock(string resource, TimeSpan timeout, FirebaseClient client)
        {
            this.client = client;
            Acquire(resource, timeout);
        }

        public void Dispose() => Relase();

        private void Acquire(string resource, TimeSpan timeout)
        {
            System.Diagnostics.Stopwatch acquireStart = new System.Diagnostics.Stopwatch();
            acquireStart.Start();

            while (true)
            {
                FirebaseResponse response = client.Get("locks");
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Dictionary<string, Lock> locks = response.ResultAs<Dictionary<string, Lock>>();
                    string reference = locks?.Where(l => l.Value.Resource == resource).Select(l => l.Key).FirstOrDefault();
                    if (string.IsNullOrEmpty(reference))
                    {
                        response = client.Push("locks", new Lock { Resource = resource, ExpireOn = DateTime.UtcNow.Add(timeout) });
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            reference = ((PushResponse)response).Result.name;
                            if (!string.IsNullOrEmpty(reference))
                            {
                                lockReference = reference;
                                break;
                            }
                        }
                    }
                }

                // check the timeout
                if (acquireStart.ElapsedMilliseconds > timeout.TotalMilliseconds)
                {
                    throw new FirebaseDistributedLockException($"Could not place a lock on the resource '{resource}': Lock timeout.");
                }

                // sleep for 500 millisecond
                System.Threading.Thread.Sleep(500);
            }
        }

        private void Relase()
        {
            lock (syncLock)
            {
                QueryBuilder builder = QueryBuilder.New($@"equalTo=""{lockReference}""");
                builder.OrderBy("$key");
                FirebaseResponse response = client.Get("locks", builder);
                if (response.StatusCode == System.Net.HttpStatusCode.OK && !response.IsNull())
                {
                    client.Delete($"locks/{lockReference}");
                }
            }
        }
    }
}