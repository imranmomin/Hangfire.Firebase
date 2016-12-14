using System;
using System.Threading;
using System.Collections.Generic;

using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;

namespace Hangfire.Firbase
{
    public sealed class FirebaseConnection : JobStorageConnection
    {
        public override IDisposable AcquireDistributedLock(string resource, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public override void AnnounceServer(string serverId, ServerContext context)
        {
            throw new NotImplementedException();
        }

        public override string CreateExpiredJob(Job job, IDictionary<string, string> parameters, DateTime createdAt, TimeSpan expireIn)
        {
            throw new NotImplementedException();
        }

        public override IWriteOnlyTransaction CreateWriteTransaction()
        {
            throw new NotImplementedException();
        }

        public override IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override void SetJobParameter(string id, string name, string value)
        {
            throw new NotImplementedException();
        }

        public override void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            throw new NotImplementedException();
        }
    }
}
