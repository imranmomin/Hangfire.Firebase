using System;
using System.Net;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using FireSharp;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using FireSharp.Response;
using FireSharp.Interfaces;
using Hangfire.Firebase.Json;
using Hangfire.Firebase.Queue;
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
            FireSharp.Extensions.ObjectExtensions.Serializer = new JsonSerializer();
        }

        public override IDisposable AcquireDistributedLock(string resource, TimeSpan timeout) => new FirebaseDistributedLock(resource, timeout, Client);
        public override IWriteOnlyTransaction CreateWriteTransaction() => new FirebaseWriteOnlyTransaction(this);

        #region Job

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
                    List<Parameter> jobParameters = parameters.Select(parameter => new Parameter
                    {
                        Name = parameter.Key,
                        Value = parameter.Value
                    }).ToList();

                    List<Task<PushResponse>> tasks = new List<Task<PushResponse>>();
                    jobParameters.ForEach(parameter =>
                    {
                        Task<PushResponse> task = Task.Run(() => Client.Push($"jobs/{reference}/parameters", parameter));
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
                return reference;
            }

            return null;
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

        #endregion

        #region Parameter

        public override string GetJobParameter(string id, string name)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (name == null) throw new ArgumentNullException(nameof(name));

            FirebaseResponse response = Client.Get($"jobs/{id}/parameters");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, Parameter> parameters = response.ResultAs<Dictionary<string, Parameter>>();
                return parameters?.Select(p => p.Value).Where(p => p.Name == name).Select(p => p.Value).FirstOrDefault();
            }

            return null;
        }

        public override void SetJobParameter(string id, string name, string value)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (name == null) throw new ArgumentNullException(nameof(name));

            bool isExsits = false;
            FirebaseResponse response = Client.Get($"jobs/{id}/parameters");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, Parameter> parameters = response.ResultAs<Dictionary<string, Parameter>>();
                string parameterReference = parameters?.Where(p => p.Value.Name == name).Select(p => p.Key).FirstOrDefault();
                if (!string.IsNullOrEmpty(parameterReference))
                {
                    isExsits = true;
                    response = Client.Set($"jobs/{id}/parameters/{parameterReference}/value", value);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new HttpRequestException(response.Body);
                    }
                }
            }

            // new parameter
            if (!isExsits)
            {
                Parameter parameter = new Parameter
                {
                    Name = name,
                    Value = value
                };

                response = Client.Push($"jobs/{id}/parameters", parameter);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new HttpRequestException(response.Body);
                }
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
            if (serverId == null) throw new ArgumentNullException(nameof(serverId));
            if (context == null) throw new ArgumentNullException(nameof(context));

            FirebaseResponse response = Client.Get("servers");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, Entities.Server> servers = response.ResultAs<Dictionary<string, Entities.Server>>();
                string serverReferenceKey = servers?.Where(s => s.Value.Id == serverId).Select(s => s.Key).FirstOrDefault();
                Entities.Server server;

                if (!string.IsNullOrEmpty(serverReferenceKey) && servers.TryGetValue(serverReferenceKey, out server))
                {
                    server.LastHeartbeat = DateTime.UtcNow;
                    server.Workers = context.WorkerCount;
                    server.Queues = context.Queues;

                    response = Client.Set($"servers/{serverReferenceKey}", server);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new HttpRequestException(response.Body);
                    }
                }
                else
                {
                    server = new Entities.Server
                    {
                        Id = serverId,
                        Workers = context.WorkerCount,
                        Queues = context.Queues,
                        CreatedOn = DateTime.UtcNow,
                        LastHeartbeat = DateTime.UtcNow
                    };

                    response = Client.Push("servers", server);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new HttpRequestException(response.Body);
                    }
                }
            }
        }

        public override void Heartbeat(string serverId)
        {
            if (serverId == null) throw new ArgumentNullException(nameof(serverId));
            FirebaseResponse response = Client.Get("servers");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, Entities.Server> servers = response.ResultAs<Dictionary<string, Entities.Server>>();
                string serverReferenceKey = servers?.Where(s => s.Value.Id == serverId).Select(s => s.Key).FirstOrDefault();
                Entities.Server server;

                if (!string.IsNullOrEmpty(serverReferenceKey) && servers.TryGetValue(serverReferenceKey, out server))
                {
                    server.LastHeartbeat = DateTime.UtcNow;
                    SetResponse setResponse = Client.Set($"servers/{serverReferenceKey}", server);
                    if (setResponse.StatusCode != HttpStatusCode.OK)
                    {
                        throw new HttpRequestException(setResponse.Body);
                    }
                }
            }
        }

        public override void RemoveServer(string serverId)
        {
            if (serverId == null) throw new ArgumentNullException(nameof(serverId));

            FirebaseResponse response = Client.Get("servers");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, Entities.Server> servers = response.ResultAs<Dictionary<string, Entities.Server>>();
                string serverReference = servers?.Where(s => s.Value.Id == serverId).Select(s => s.Key).Single();
                if (!string.IsNullOrEmpty(serverReference))
                {
                    Client.Delete($"servers/{serverReference}");
                }
            }
        }

        public override int RemoveTimedOutServers(TimeSpan timeOut)
        {
            if (timeOut.Duration() != timeOut)
            {
                throw new ArgumentException("The `timeOut` value must be positive.", nameof(timeOut));
            }

            FirebaseResponse response = Client.Get("servers");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, Entities.Server> servers = response.ResultAs<Dictionary<string, Entities.Server>>();

                // get the firebase reference key for each timeout server
                DateTime lastHeartbeat = DateTime.UtcNow.Add(timeOut.Negate());
                string[] timeoutServers = servers?.Where(s => s.Value.LastHeartbeat < lastHeartbeat)
                                                  .Select(s => s.Key)
                                                  .ToArray();
                if (timeoutServers != null)
                {
                    // remove all timeout server.
                    Array.ForEach(timeoutServers, server => Client.Delete($"servers/{server}"));
                    return timeoutServers.Length;
                }
                return default(int);
            }
            return default(int);
        }

        #endregion

        #region Hash

        public override Dictionary<string, string> GetAllEntriesFromHash(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            FirebaseResponse response = Client.Get($"hashes/{key}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, Hash> hashes = response.ResultAs<Dictionary<string, Hash>>();
                return hashes?.Select(h => h.Value).ToDictionary(h => h.Field, h => h.Value);
            }

            return null;
        }

        public override void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (keyValuePairs == null) throw new ArgumentNullException(nameof(keyValuePairs));

            List<Task<FirebaseResponse>> tasks = new List<Task<FirebaseResponse>>();
            List<Hash> hashes = keyValuePairs.Select(k => new Hash
            {
                Field = k.Key,
                Value = k.Value
            }).ToList();

            FirebaseResponse response = Client.Get($"hashes/{key}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, Hash> existingHashes = response.ResultAs<Dictionary<string, Hash>>();
                string[] hashReferences = existingHashes?.Select(h => h.Value).Where(h => hashes.Any(k => k.Field == h.Field))
                                                                              .Select(h => h.Value)
                                                                              .ToArray();
                if (hashReferences != null)
                {
                    // updates 
                    Array.ForEach(hashReferences, hashReference =>
                    {
                        Hash hash;
                        if (existingHashes.TryGetValue(hashReference, out hash) && hashes.Any(k => k.Field == hash.Field))
                        {
                            string value = hashes.Where(k => k.Field == hash.Field).Select(k => k.Value).Single();
                            Task<FirebaseResponse> task = Task.Run(() => (FirebaseResponse)Client.Set($"hashes/{key}/{hashReferences}/value", value));
                            tasks.Add(task);

                            // remove the hash from the list
                            hashes.RemoveAll(x => x.Field == hash.Field);
                        }
                    });
                }
            }

            // new 
            hashes.ForEach(hash =>
            {
                Task<FirebaseResponse> task = Task.Run(() => (FirebaseResponse)Client.Push($"hashes/{key}", hash));
                tasks.Add(task);
            });

            if (tasks.Count > 0)
            {
                Task.WaitAll(tasks.ToArray());

                bool isFailed = tasks.Any(t => t.Result.StatusCode != HttpStatusCode.OK);
                if (isFailed)
                {
                    string body = string.Join("; ", tasks.Where(t => t.Result.StatusCode != HttpStatusCode.OK).Select(t => t.Result.Body));
                    throw new HttpRequestException(body);
                }
            }
        }

        public override long GetHashCount(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            QueryBuilder builder = QueryBuilder.New().Shallow(true);
            FirebaseResponse response = Client.Get($"hashes/{key}", builder);
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

            FirebaseResponse response = Client.Get($"hashes/{key}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, Hash> hashes = response.ResultAs<Dictionary<string, Hash>>();
                return hashes?.Select(h => h.Value).Where(h => h.Field == name).Select(v => v.Value).FirstOrDefault();
            }

            return null;
        }

        public override TimeSpan GetHashTtl(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            FirebaseResponse response = Client.Get($"hashes/{key}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, Hash> hashes = response.ResultAs<Dictionary<string, Hash>>();
                DateTime? expireOn = hashes?.Select(h => h.Value).Min(h => h.ExpireOn);
                if (expireOn.HasValue) return expireOn.Value - DateTime.UtcNow;
            }

            return TimeSpan.FromSeconds(-1);
        }

        #endregion

        #region List

        public override List<string> GetAllItemsFromList(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            FirebaseResponse response = Client.Get($"lists/{key}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, List> lists = response.ResultAs<Dictionary<string, List>>();
                if (lists != null)
                {
                    return lists.Select(l => l.Value).Select(l => l.Value).ToList();
                }
            }

            return new List<string>();
        }

        public override List<string> GetRangeFromList(string key, int startingFrom, int endingAt)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            FirebaseResponse response = Client.Get($"lists/{key}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, List> lists = response.ResultAs<Dictionary<string, List>>();
                if (lists != null)
                {
                    return lists?.Select(l => l.Value)
                                 .OrderBy(l => l.ExpireOn)
                                 .Skip(startingFrom).Take(endingAt)
                                 .Select(l => l.Value).ToList();
                }
            }

            return new List<string>();
        }

        public override TimeSpan GetListTtl(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            FirebaseResponse response = Client.Get($"lists/{key}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, List> lists = response.ResultAs<Dictionary<string, List>>();
                DateTime? expireOn = lists?.Select(l => l.Value).Min(l => l.ExpireOn);
                if (expireOn.HasValue) return expireOn.Value - DateTime.UtcNow;
            }

            return TimeSpan.FromSeconds(-1);
        }

        public override long GetListCount(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            QueryBuilder builder = QueryBuilder.New().Shallow(true);
            FirebaseResponse response = Client.Get($"lists/{key}", builder);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string[] lists = response.ResultAs<string[]>();
                if (lists != null)
                {
                    return lists.LongCount();
                }
            }

            return default(long);
        }

        #endregion

    }
}