using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;

using FireSharp;
using Hangfire.Common;
using Hangfire.Storage;
using FireSharp.Response;
using Hangfire.Firebase.Queue;
using Hangfire.Firebase.Entities;
using Hangfire.Storage.Monitoring;

namespace Hangfire.Firebase
{
    public sealed class FirebaseMonitoringApi : IMonitoringApi
    {
        private readonly FirebaseConnection connection;
        private readonly FirebaseStorage storage;

        public FirebaseMonitoringApi(FirebaseStorage storage)
        {
            this.storage = storage;
            connection = (FirebaseConnection)storage.GetConnection();
        }

        public IList<QueueWithTopEnqueuedJobsDto> Queues()
        {
            throw new NotImplementedException();
        }

        public IList<ServerDto> Servers()
        {
            List<ServerDto> servers = new List<ServerDto>();

            FirebaseResponse response = connection.Client.Get("servers");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, Entities.Server> collections = response.ResultAs<Dictionary<string, Entities.Server>>();
                servers = collections?.Select(s => new ServerDto
                {
                    Name = s.Value.Id,
                    Heartbeat = s.Value.LastHeartbeat,
                    Queues = s.Value.Queues,
                    StartedAt = s.Value.CreatedOn,
                    WorkersCount = s.Value.Workers
                }).ToList();
            }

            return servers;
        }

        public JobDetailsDto JobDetails(string jobId)
        {
            List<Parameter> parameters = new List<Parameter>();
            List<StateHistoryDto> states = new List<StateHistoryDto>();

            FirebaseResponse response = connection.Client.Get($"jobs/{jobId}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Entities.Job job = response.ResultAs<Entities.Job>();
                InvocationData invocationData = job.InvocationData;
                invocationData.Arguments = job.Arguments;
                
                FirebaseResponse parameterResponse = connection.Client.Get($"jobs/{jobId}/parameters");
                if (parameterResponse.StatusCode == HttpStatusCode.OK)
                {
                    parameters = parameterResponse.ResultAs<List<Parameter>>();
                }

                FirebaseResponse stateResponse = connection.Client.Get($"states/{jobId}");
                if (stateResponse.StatusCode == HttpStatusCode.OK)
                {
                    Dictionary<string, State> collections = stateResponse.ResultAs<Dictionary<string, State>>();
                    states = collections.Select(s => new StateHistoryDto
                    {
                        Data = s.Value.Data,
                        CreatedAt = s.Value.CreatedOn,
                        Reason = s.Value.Reason,
                        StateName = s.Value.Name
                    }).ToList();
                }

                return new JobDetailsDto
                {
                    Job = invocationData.Deserialize(),
                    CreatedAt = job.CreatedOn,
                    ExpireAt = job.ExpireOn,
                    Properties = parameters.ToDictionary(p => p.Name, p => p.Value),
                    History = states
                };
            }

            return null;
        }

        public StatisticsDto GetStatistics()
        {
            throw new NotImplementedException();
        }

        #region Job List

        public JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int @from, int perPage)
        {
            return GetJobs(queue, @from, perPage, (job) =>
            {
                InvocationData invocationData = job.InvocationData;
                invocationData.Arguments = job.Arguments;

                return new EnqueuedJobDto
                {
                    Job = invocationData.Deserialize(),
                    State = job.StateName
                };
            });
        }

        public JobList<FetchedJobDto> FetchedJobs(string queue, int @from, int perPage)
        {
            return GetJobs(queue, @from, perPage, (job) =>
            {
                InvocationData invocationData = job.InvocationData;
                invocationData.Arguments = job.Arguments;

                return new FetchedJobDto
                {
                    Job = invocationData.Deserialize(),
                    State = job.StateName
                };
            });
        }

        public JobList<ProcessingJobDto> ProcessingJobs(int @from, int count)
        {
            return GetJobs(States.ProcessingState.StateName, @from, count, (state, invocationData) => new ProcessingJobDto
            {
                Job = invocationData.Deserialize(),
                ServerId = state.Data.ContainsKey("ServerId") ? state.Data["ServerId"] : state.Data["ServerName"],
                StartedAt = JobHelper.DeserializeDateTime(state.Data["StartedAt"]),
            });
        }

        public JobList<ScheduledJobDto> ScheduledJobs(int @from, int count)
        {
            return GetJobs(States.ScheduledState.StateName, @from, count, (state, invocationData) => new ScheduledJobDto
            {
                Job = invocationData.Deserialize(),
                EnqueueAt = JobHelper.DeserializeDateTime(state.Data["EnqueueAt"]),
                ScheduledAt = JobHelper.DeserializeDateTime(state.Data["ScheduledAt"])
            });
        }

        public JobList<SucceededJobDto> SucceededJobs(int @from, int count)
        {
            return GetJobs(States.SucceededState.StateName, @from, count, (state, invocationData) => new SucceededJobDto
            {
                Job = invocationData.Deserialize(),
                Result = state.Data["result"],
                TotalDuration = state.Data.ContainsKey("PerformanceDuration") && state.Data.ContainsKey("Latency")
                                ? (long?)long.Parse(state.Data["PerformanceDuration"]) + long.Parse(state.Data["Latency"])
                                : null,
                SucceededAt = JobHelper.DeserializeNullableDateTime(state.Data["SucceededAt"])
            });
        }

        public JobList<FailedJobDto> FailedJobs(int @from, int count)
        {
            return GetJobs(States.FailedState.StateName, @from, count, (state, invocationData) => new FailedJobDto
            {
                Job = invocationData.Deserialize(),
                Reason = state.Reason,
                FailedAt = JobHelper.DeserializeNullableDateTime(state.Data["FailedAt"]),
                ExceptionDetails = state.Data["ExceptionDetails"],
                ExceptionMessage = state.Data["ExceptionMessage"],
                ExceptionType = state.Data["ExceptionType"],
            });
        }

        public JobList<DeletedJobDto> DeletedJobs(int @from, int count)
        {
            return GetJobs(States.DeletedState.StateName, @from, count, (state, invocationData) => new DeletedJobDto
            {
                Job = invocationData.Deserialize(),
                DeletedAt = JobHelper.DeserializeNullableDateTime(state.Data["DeletedAt"])
            });
        }

        private JobList<T> GetJobs<T>(string stateName, int @from, int count, Func<State, InvocationData, T> selector)
        {
            List<KeyValuePair<string, T>> jobs = new List<KeyValuePair<string, T>>();
            QueryBuilder builder = QueryBuilder.New($@"equalTo=""{stateName}""");
            builder.OrderBy("state_name");

            FirebaseResponse response = connection.Client.Get("jobs", builder);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, Entities.Job> collections = response.ResultAs<Dictionary<string, Entities.Job>>();
                string[] references = collections?.Skip(@from).Take(count).Select(k => k.Key).ToArray();

                if (references != null && references.Length > 0)
                {
                    List<Task<KeyValuePair<string, T>>> tasks = new List<Task<KeyValuePair<string, T>>>();
                    Array.ForEach(references, reference =>
                    {
                        Entities.Job job;
                        if (collections.TryGetValue(reference, out job))
                        {
                            Task<KeyValuePair<string, T>> task = Task.Run(() =>
                            {
                                FirebaseResponse stateResponse = connection.Client.Get($"states/{reference}/{job.StateId}");
                                if (stateResponse.StatusCode == HttpStatusCode.OK)
                                {
                                    State state = stateResponse.ResultAs<State>();
                                    InvocationData invocationData = job.InvocationData;
                                    invocationData.Arguments = job.Arguments;

                                    T data = selector(state, invocationData);
                                    if (data != null)
                                    {
                                        return new KeyValuePair<string, T>(reference, data);
                                    }
                                }
                                return default(KeyValuePair<string, T>);
                            });
                            tasks.Add(task);
                        }
                    });

                    Task.WaitAll(tasks.ToArray());
                    jobs = tasks.Where(t => !t.Result.Equals(default(KeyValuePair<string, T>))).Select(t => t.Result).ToList();
                }
            }

            return new JobList<T>(jobs);
        }

        private JobList<T> GetJobs<T>(string queue, int @from, int count, Func<Entities.Job, T> selector)
        {
            List<KeyValuePair<string, T>> jobs = new List<KeyValuePair<string, T>>();

            FirebaseResponse response = connection.Client.Get($"queue/${queue}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, string> collection = response.ResultAs<Dictionary<string, string>>();
                string[] references = collection?.Skip(@from - 1).Take(count).Select(k => k.Value).ToArray();

                if (references != null && references.Length > 0)
                {
                    List<Task<KeyValuePair<string, T>>> tasks = new List<Task<KeyValuePair<string, T>>>();
                    Array.ForEach(references, reference =>
                    {
                        Task<KeyValuePair<string, T>> task = Task.Run(() =>
                        {
                            FirebaseResponse jobResponse = connection.Client.Get($"jobs/{reference}");
                            if (jobResponse.StatusCode == HttpStatusCode.OK)
                            {
                                Entities.Job job = jobResponse.ResultAs<Entities.Job>();
                                T data = selector(job);
                                if (data != null)
                                {
                                    return new KeyValuePair<string, T>(reference, data);
                                }
                            }
                            return default(KeyValuePair<string, T>);
                        });
                        tasks.Add(task);

                    });

                    Task.WaitAll(tasks.ToArray());
                    jobs = tasks.Where(t => !t.Result.Equals(default(KeyValuePair<string, T>))).Select(t => t.Result).ToList();
                }
            }

            return new JobList<T>(jobs);
        }

        #endregion

        #region Counts

        public long ScheduledCount() => GetNumberOfJobsByStateName(States.ScheduledState.StateName);

        public long EnqueuedCount(string queue)
        {
            IPersistentJobQueueProvider provider = storage.QueueProviders.GetProvider(queue);
            IPersistentJobQueueMonitoringApi monitoringApi = provider.GetJobQueueMonitoringApi();
            return monitoringApi.GetEnqueuedCount(queue);
        }

        public long FetchedCount(string queue)
        {
            IPersistentJobQueueProvider provider = storage.QueueProviders.GetProvider(queue);
            IPersistentJobQueueMonitoringApi monitoringApi = provider.GetJobQueueMonitoringApi();
            return monitoringApi.GetEnqueuedCount(queue);
        }

        public long FailedCount() => GetNumberOfJobsByStateName(States.FailedState.StateName);

        public long ProcessingCount() => GetNumberOfJobsByStateName(States.ProcessingState.StateName);

        public long SucceededListCount() => GetNumberOfJobsByStateName(States.SucceededState.StateName);

        public long DeletedListCount() => GetNumberOfJobsByStateName(States.DeletedState.StateName);

        private long GetNumberOfJobsByStateName(string state)
        {
            QueryBuilder builder = QueryBuilder.New($@"equalTo=""{state}""");
            builder.OrderBy("state_name");
            builder.Shallow(true);

            FirebaseResponse response = connection.Client.Get("jobs", builder);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, bool> jobs = response.ResultAs<Dictionary<string, bool>>();
                return jobs.LongCount();
            }

            return default(long);
        }

        public IDictionary<DateTime, long> SucceededByDatesCount() => GetDatesTimelineStats("succeeded");

        public IDictionary<DateTime, long> FailedByDatesCount() => GetDatesTimelineStats("failed");

        public IDictionary<DateTime, long> HourlySucceededJobs() => GetHourlyTimelineStats("succeeded");

        public IDictionary<DateTime, long> HourlyFailedJobs() => GetHourlyTimelineStats("failed");

        private Dictionary<DateTime, long> GetHourlyTimelineStats(string type)
        {
            List<DateTime> dates = Enumerable.Range(0, 24).Select(x => DateTime.UtcNow.AddHours(-x)).ToList();
            Dictionary<string, DateTime> keys = dates.ToDictionary(x => $"stats:{type}:{x:yyyy-MM-dd-HH}", x => x);
            return GetTimelineStats(keys);
        }

        private Dictionary<DateTime, long> GetDatesTimelineStats(string type)
        {
            List<DateTime> dates = Enumerable.Range(0, 7).Select(x => DateTime.UtcNow.AddDays(-x)).ToList();
            Dictionary<string, DateTime> keys = dates.ToDictionary(x => $"stats:{type}:{x:yyyy-MM-dd}", x => x);
            return GetTimelineStats(keys);
        }

        private Dictionary<DateTime, long> GetTimelineStats(Dictionary<string, DateTime> keys)
        {
            Dictionary<DateTime, long> result = new Dictionary<DateTime, long>();
            Parallel.ForEach(keys.Keys, key =>
            {
                FirebaseResponse response = connection.Client.Get($"counters/aggregrated/{key}");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Counter counter = response.ResultAs<Counter>();
                    DateTime date = keys.Where(k => k.Key == key).Select(k => k.Value).First();
                    result.Add(date, counter.Value);
                }
            });
            return result;
        }

        #endregion
    }
}
