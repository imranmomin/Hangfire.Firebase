﻿using System;

namespace Hangfire.Firbase
{
    public class FirebaseStorageOptions
    {
        public TimeSpan? RequestTimeout { get; set; }
        public bool PrepareSchema { get; set; }
        public string[] Queues { get; set; }

        public FirebaseStorageOptions()
        {
            RequestTimeout = TimeSpan.FromSeconds(30);
            PrepareSchema = true;
            Queues = new[] { "default" };
        }
    }
}
