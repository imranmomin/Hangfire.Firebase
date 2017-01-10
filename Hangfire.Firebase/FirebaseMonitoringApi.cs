using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;

using FireSharp;
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
            throw new NotImplementedException();
        }

        public JobDetailsDto JobDetails(string jobId)
        {
            throw new NotImplementedException();
        }

        public StatisticsDto GetStatistics()
        {
            throw new NotImplementedException();
        }

        public JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int @from, int perPage)
        {
            throw new NotImplementedException();
        }

        public JobList<FetchedJobDto> FetchedJobs(string queue, int @from, int perPage)
        {
            throw new NotImplementedException();
        }

        public JobList<ProcessingJobDto> ProcessingJobs(int @from, int count)
        {
            throw new NotImplementedException();
        }

        public JobList<ScheduledJobDto> ScheduledJobs(int @from, int count)
        {
            throw new NotImplementedException();
        }

        public JobList<SucceededJobDto> SucceededJobs(int @from, int count)
        {
            throw new NotImplementedException();
        }

        public JobList<FailedJobDto> FailedJobs(int @from, int count)
        {
            throw new NotImplementedException();
        }

        public JobList<DeletedJobDto> DeletedJobs(int @from, int count)
        {
            throw new NotImplementedException();
        }

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

            FirebaseResponse response = connection.Client.Get("jobs");
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
