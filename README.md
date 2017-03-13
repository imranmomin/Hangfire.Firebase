# Hangfire.Firebase

[![Official Site](https://img.shields.io/badge/site-hangfire.io-blue.svg)](http://hangfire.io)
[![Latest version](https://img.shields.io/badge/nuget-v1.0.1-blue.svg)](https://www.nuget.org/packages/Hangfire.Firebase) 
[![Build status](https://ci.appveyor.com/api/projects/status/8bail001djs64inu?svg=true)](https://ci.appveyor.com/project/imranmomin/hangfire-firebase)

This repo will add a [Google Firebase](https://firebase.google.com) storage support to [Hangfire](http://hangfire.io) - fire-and-forget, delayed and recurring tasks runner for .NET. Scalable and reliable background job runner. Supports multiple servers, CPU and I/O intensive, long-running and short-running jobs.

Installation
-------------

[Hangfire.Firebase](https://www.nuget.org/packages/Hangfire.Firebase) is available as a NuGet package. Install it using the NuGet Package Console window:

```
PM> Install-Package Hangfire.Firebase
```

Usage
------
Use one the following ways to initialize `FirebaseStorage`

```csharp
GlobalConfiguration.Configuration.UseFirebaseStorage("<url>", "<authSecret>");

Firebase.FirebaseStorage firebaseStorage = new Firebase.FirebaseStorage("<url>", "<authSecret>");
GlobalConfiguration.Configuration.UseStorage(firebaseStorage);

// customize any options
Firebase.FirebaseStorageOptions firebaseOptions = new Firebase.FirebaseStorageOptions
{
    Queues = new[] { "default", "critical" },
    RequestTimeout = System.TimeSpan.FromSeconds(30),
	ExpirationCheckInterval = TimeSpan.FromMinutes(15);
    CountersAggregateInterval = TimeSpan.FromMinutes(1);
    QueuePollInterval = TimeSpan.FromSeconds(2);
};
Firebase.FirebaseStorage firebaseStorage = new Firebase.FirebaseStorage("<url>", "<authSecret>", firebaseOptions);
GlobalConfiguration.Configuration.UseStorage(firebaseStorage);
```

Firebase Rule Setup
------
Under the Firebase database, please update the json so `.indexOn` is aviable for query. Note **hangfire** is the parent node. If you had additional queue, repeat the `.indexOn` for the queue. 
```json
{
  "rules": {
    "hangfire": {
      "servers": {
        ".indexOn": [ "_id", "server_id" ]
      },
      "queue": {
        "default": {
          ".indexOn": [ "_id", "server_id" ]
        },
        "critical": {
          ".indexOn": [ "_id", "server_id" ]
        }
      },
      "jobs": {
        ".indexOn": [ "_id", "server_id", "state_name" ]
      },
      "sets": {
        ".indexOn": [ "key" ]
      }
    },
    ".read": "auth != null",
    ".write": "auth != null"
  }
}
```

Questions? Problems?
---------------------

Open-source project are developing more smoothly, when all discussions are held in public.

If you have any questions or problems related to Hangfire.Firebase itself or this storage implementation or want to discuss new features, please create under [issues](https://github.com/imranmomin/Hangfire.Firebase/issues/new) and assign the correct label for discussion. 

If you've discovered a bug, please report it to the [GitHub Issues](https://github.com/imranmomin/Hangfire.Firebase/pulls). Detailed reports with stack traces, actual and expected behavours are welcome. 
