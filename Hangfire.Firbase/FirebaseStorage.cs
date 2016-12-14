using System;
using Hangfire.Storage;

namespace Hangfire.Firbase
{
    public sealed class FirebaseStorage : JobStorage
    {
        public FirebaseStorage()
        {
        }

        public override IStorageConnection GetConnection() => new FirebaseConnection();

        public override IMonitoringApi GetMonitoringApi() => new FirebaseMonitoringApi();
    }
}
