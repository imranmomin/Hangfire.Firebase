using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using FireSharp;
using FireSharp.Interfaces;
using Hangfire.Firbase.Queue;
using FireSharp.Response;
using Hangfire.Firbase.Entities;

namespace Hangfire.Firbase
{
    public sealed class FirebaseConnection : JobStorageConnection
    {
        public FirebaseClient Client { get; }
        public PersistentJobQueueProviderCollection QueueProviders { get; }

        public FirebaseConnection(IFirebaseConfig config, PersistentJobQueueProviderCollection queueProviders)
        {
            Client = new FirebaseClient(config);
            QueueProviders = queueProviders;
        }

        public override IDisposable AcquireDistributedLock(string resource, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public override void AnnounceServer(string serverId, ServerContext context)
        {
            throw new NotImplementedException();
        }

        public override string CreateExpiredJob(Common.Job job, IDictionary<string, string> parameters, DateTime createdAt, TimeSpan expireIn)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            InvocationData invocationData = InvocationData.Serialize(job);
            PushResponse response = Client.Push("jobs", new Entities.Job
            {
                InvocationData = invocationData,
                Arguments = invocationData.Arguments,
                CreatedOn = createdAt,
                ExpireOn = createdAt.Add(expireIn)
            });

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string reference = response.Result.name;
                if (parameters.Count > 0)
                {
                    List<Parameter> para = new List<Parameter>();
                    foreach (var parameter in parameters)
                    {
                        para.Add(new Parameter
                        {
                            Name = parameter.Key,
                            Value = parameter.Value
                        });
                    }
                    SetResponse result = Client.Set($"jobs/{reference}/parameters", para);
                    if (result.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new System.Net.WebException();
                    }
                }
                return reference;
            }

            throw new InvalidOperationException();
        }

        public override IWriteOnlyTransaction CreateWriteTransaction()
        {
            throw new NotImplementedException();
        }

        public override IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null || queues.Length == 0) throw new ArgumentNullException(nameof(queues));

            IPersistentJobQueueProvider[] providers = queues.Select(q => QueueProviders.GetProvider(q))
                                                            .Distinct()
                                                            .ToArray();

            if (providers.Length != 1)
            {
                throw new InvalidOperationException($"Multiple provider instances registered for queues: {string.Join(", ", queues)}. You should choose only one type of persistent queues per server instance.");
            }

            IPersistentJobQueue persistentQueue = providers.Single().GetJobQueue();
            Task<IFetchedJob> queue = persistentQueue.Dequeue(queues, cancellationToken);
            return queue.Result;
        }

        public override Dictionary<string, string> GetAllEntriesFromHash(string key)
        {
            throw new NotImplementedException();
        }

        public override HashSet<string> GetAllItemsFromSet(string key)
        {
            throw new NotImplementedException();
        }

        public override string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore)
        {
            throw new NotImplementedException();
        }

        public override JobData GetJobData(string jobId)
        {
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));

            FirebaseResponse response = Client.Get($"jobs/{jobId}");
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Entities.Job data = response.ResultAs<Entities.Job>();
                InvocationData invocationData = data.InvocationData;
                invocationData.Arguments = data.Arguments;

                Common.Job job = null;
                JobLoadException loadException = null;

                try
                {
                    job = invocationData.Deserialize();
                }
                catch (JobLoadException ex)
                {
                    loadException = ex;
                }

                return new JobData
                {
                    Job = job,
                    State = data.StateName,
                    CreatedAt = data.CreatedOn,
                    LoadException = loadException
                };
            }

            return null;
        }

        public override string GetJobParameter(string id, string name)
        {
            throw new NotImplementedException();
        }

        public override StateData GetStateData(string jobId)
        {
            throw new NotImplementedException();
        }

        public override void Heartbeat(string serverId)
        {
            throw new NotImplementedException();
        }

        public override void RemoveServer(string serverId)
        {
            throw new NotImplementedException();
        }

        public override int RemoveTimedOutServers(TimeSpan timeOut)
        {
            if (timeOut.Duration() != timeOut)
            {
                throw new ArgumentException("The `timeOut` value must be positive.", nameof(timeOut));
            }

            FirebaseResponse response = Client.Get("servers");
            List<Entities.Server> servers = response.ResultAs<List<Entities.Server>>();
            servers = servers.Where(s => s.LastHeartbeat < DateTime.UtcNow.Add(timeOut.Negate())).ToList();
            servers.ForEach(server => Client.Delete($"servers/{server.Id}"));

            return servers.Count;
        }

        public override void SetJobParameter(string id, string name, string value)
        {
            throw new NotImplementedException();
        }

        public override void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            throw new NotImplementedException();
        }

        #region Hash

        public override long GetHashCount(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            FirebaseResponse response = Client.Get("hash");
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                List<Hash> hashes = response.ResultAs<List<Hash>>();
                return hashes.Where(h => h.Key == key).LongCount();
            }

            return default(long);
        }

        public override string GetValueFromHash(string key, string name)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (name == null) throw new ArgumentNullException(nameof(name));

            FirebaseResponse response = Client.Get("hash");
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                List<Hash> hashes = response.ResultAs<List<Hash>>();
                return hashes.Where(h => h.Key == key && h.Value == name).Select(v => v.Value).FirstOrDefault();
            }

            return null;
        }

        public override TimeSpan GetHashTtl(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            FirebaseResponse response = Client.Get("hash");
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                List<Hash> hashes = response.ResultAs<List<Hash>>();
                DateTime? expireOn = hashes.Where(h => h.Key == key).Min(v => v.ExpireOn);
                if (expireOn.HasValue) return expireOn.Value - DateTime.UtcNow;
            }

            return TimeSpan.FromSeconds(-1);
        }

        #endregion

        #region List

        public override List<string> GetAllItemsFromList(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            FirebaseResponse response = Client.Get("list");
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                List<List> lists = response.ResultAs<List<List>>();
                return lists.Where(l => l.Key == key).Select(l => l.Value).ToList();
            }

            return new List<string>();
        }

        public override List<string> GetRangeFromList(string key, int startingFrom, int endingAt)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            FirebaseResponse response = Client.Get("list");
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                List<List> lists = response.ResultAs<List<List>>();
                return lists.Where(l => l.Key == key).OrderBy(l => l.ExpireOn).Skip(startingFrom).Take(endingAt).Select(l => l.Value).ToList();
            }

            return new List<string>();
        }

        public override TimeSpan GetListTtl(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            FirebaseResponse response = Client.Get("list");
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                List<List> lists = response.ResultAs<List<List>>();
                DateTime? expireOn = lists.Where(l => l.Key == key).Min(l => l.ExpireOn);
                if (expireOn.HasValue) return expireOn.Value - DateTime.UtcNow;
            }

            return TimeSpan.FromSeconds(-1);
        }

        public override long GetListCount(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            FirebaseResponse response = Client.Get("list");
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                List<List> lists = response.ResultAs<List<List>>();
                return lists.Where(l => l.Key == key).LongCount();
            }

            return default(long);
        }

        #endregion

    }
}