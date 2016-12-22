using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using FireSharp;
using FireSharp.Interfaces;
using Hangfire.Firebase.Queue;
using FireSharp.Response;
using Hangfire.Firebase.Entities;

namespace Hangfire.Firebase
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

            if (response.StatusCode == HttpStatusCode.OK)
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
                    if (result.StatusCode != HttpStatusCode.OK)
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

        public override JobData GetJobData(string jobId)
        {
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));

            FirebaseResponse response = Client.Get($"jobs/{jobId}");
            if (response.StatusCode == HttpStatusCode.OK)
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

        #region Parameter

        public override string GetJobParameter(string id, string name)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (name == null) throw new ArgumentNullException(nameof(name));

            FirebaseResponse response = Client.Get($"jobs/{id}/parameters");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, Parameter> parameters = response.ResultAs<Dictionary<string, Parameter>>();
                return parameters.Select(p => p.Value).Where(p => p.Name == name).Select(p => p.Value).FirstOrDefault();
            }

            return null;
        }

        public override void SetJobParameter(string id, string name, string value)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (name == null) throw new ArgumentNullException(nameof(name));

            Parameter parameter = new Parameter
            {
                Name = name,
                Value = value
            };

            PushResponse response = Client.Push($"jobs/{id}/parameters", parameter);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new HttpRequestException(response.Body);
            }
        }

        #endregion

        #region Set

        public override HashSet<string> GetAllItemsFromSet(string key)
        {
            throw new NotImplementedException();
        }

        public override string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore)
        {
            throw new NotImplementedException();
        }

        public override StateData GetStateData(string jobId)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Server

        public override void AnnounceServer(string serverId, ServerContext context)
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

        #endregion

        #region Hash

        public override Dictionary<string, string> GetAllEntriesFromHash(string key)
        {
            throw new NotImplementedException();
        }

        public override void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (keyValuePairs == null) throw new ArgumentNullException(nameof(keyValuePairs));

            List<Hash> hashes = keyValuePairs.Select(k => new Hash
            {
                Field = k.Key,
                Value = k.Value
            }).ToList();

            List<Task<PushResponse>> tasks = new List<Task<PushResponse>>();
            hashes.ForEach(hash =>
            {
                Task<PushResponse> task = Task.Run(() => Client.Push($"hash/{key}", hash));
                tasks.Add(task);
            });
            Task.WaitAll(tasks.ToArray());

            bool isFailed = tasks.Any(t => t.Result.StatusCode != HttpStatusCode.OK);
            if (isFailed)
            {
                string body = string.Join("; ", tasks.Where(t => t.Result.StatusCode != HttpStatusCode.OK).Select(t => t.Result.Body));
                throw new HttpRequestException(body);
            }
        }

        public override long GetHashCount(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            QueryBuilder builder = QueryBuilder.New().Shallow(true);
            FirebaseResponse response = Client.Get($"hash/{key}", builder);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string[] hashes = response.ResultAs<string[]>();
                return hashes.LongCount();
            }

            return default(long);
        }

        public override string GetValueFromHash(string key, string name)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (name == null) throw new ArgumentNullException(nameof(name));

            FirebaseResponse response = Client.Get($"hash/{key}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, Hash> hashes = response.ResultAs<Dictionary<string, Hash>>();
                return hashes.Select(h => h.Value).Where(h => h.Field == name).Select(v => v.Value).FirstOrDefault();
            }

            return null;
        }

        public override TimeSpan GetHashTtl(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            FirebaseResponse response = Client.Get($"hash/{key}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, Hash> hashes = response.ResultAs<Dictionary<string, Hash>>();
                DateTime? expireOn = hashes.Select(h => h.Value).Min(h => h.ExpireOn);
                if (expireOn.HasValue) return expireOn.Value - DateTime.UtcNow;
            }

            return TimeSpan.FromSeconds(-1);
        }

        #endregion

        #region List

        public override List<string> GetAllItemsFromList(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            FirebaseResponse response = Client.Get($"list/{key}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, List> lists = response.ResultAs<Dictionary<string, List>>();
                return lists.Select(l => l.Value).Select(l => l.Value).ToList();
            }

            return new List<string>();
        }

        public override List<string> GetRangeFromList(string key, int startingFrom, int endingAt)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            FirebaseResponse response = Client.Get($"list/{key}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, List> lists = response.ResultAs<Dictionary<string, List>>();
                return lists.Select(l => l.Value)
                            .OrderBy(l => l.ExpireOn)
                            .Skip(startingFrom).Take(endingAt)
                            .Select(l => l.Value).ToList();
            }

            return new List<string>();
        }

        public override TimeSpan GetListTtl(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            FirebaseResponse response = Client.Get($"list/{key}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, List> lists = response.ResultAs<Dictionary<string, List>>();
                DateTime? expireOn = lists.Select(l => l.Value).Min(l => l.ExpireOn);
                if (expireOn.HasValue) return expireOn.Value - DateTime.UtcNow;
            }

            return TimeSpan.FromSeconds(-1);
        }

        public override long GetListCount(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            QueryBuilder builder = QueryBuilder.New().Shallow(true);
            FirebaseResponse response = Client.Get($"list/{key}", builder);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string[] lists = response.ResultAs<string[]>();
                return lists.LongCount();
            }

            return default(long);
        }

        #endregion

    }
}