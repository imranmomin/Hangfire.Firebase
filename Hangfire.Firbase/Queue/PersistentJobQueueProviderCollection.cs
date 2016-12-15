using System;
using System.Collections;
using System.Collections.Generic;

namespace Hangfire.Firbase.Queue
{
    public sealed class PersistentJobQueueProviderCollection : IEnumerable<IPersistentJobQueueProvider>
    {
        private readonly IPersistentJobQueueProvider provider;
        private readonly List<IPersistentJobQueueProvider> providers = new List<IPersistentJobQueueProvider>();
        private readonly Dictionary<string, IPersistentJobQueueProvider> providersByQueue = new Dictionary<string, IPersistentJobQueueProvider>(StringComparer.OrdinalIgnoreCase);

        public PersistentJobQueueProviderCollection(IPersistentJobQueueProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            this.provider = provider;
            providers.Add(this.provider);
        }

        public void Add(IPersistentJobQueueProvider provider, IEnumerable<string> queues)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (queues == null) throw new ArgumentNullException(nameof(queues));

            providers.Add(provider);
            foreach (string queue in queues)
            {
                providersByQueue.Add(queue, provider);
            }
        }

        public IPersistentJobQueueProvider GetProvider(string queue) => providersByQueue.ContainsKey(queue) ? providersByQueue[queue] : provider;
        public IEnumerator<IPersistentJobQueueProvider> GetEnumerator() => providers.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}