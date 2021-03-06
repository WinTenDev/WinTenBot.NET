﻿using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Serilog;
using Zizi.Bot.IO;
using Zizi.Bot.Common;
using Zizi.Bot.Models;

namespace Zizi.Bot.Tools.GoogleCloud
{
    public static class CloudFirestore
    {
        private const string ProjectId = "wintenbot";
        private static FirestoreDb Db { get; set; }

        private static void MakeClient()
        {
            var credPath = BotSettings.GoogleCloudCredentialsPath.SanitizeSlash();
            Log.Information("Create Firestore client, cred {CredPath}", credPath);
            var clientBuilder = new FirestoreDbBuilder()
            {
                CredentialsPath = credPath,
                ProjectId = ProjectId
            };

            var client = clientBuilder.Build();
            Db = client;
        }

        public static void Create(string path, object data)
        {
            if (Db == null) MakeClient();

            // var a = FirestoreDb.Create(projectId);
            Log.Information("Adding data to {Path}", path);
            Log.Debug("Data: {V}", data.ToJson(true));

            var collection = Db.Collection(path);
            collection.AddAsync(data);
        }

        public static async Task ListDocument()
        {
            var collectionReference = Db.Collection("");
            var docs = await collectionReference.GetSnapshotAsync();

            foreach (var documentSnapshot in docs)
            {
                Log.Information("SnapShot: {0}", documentSnapshot.ToJson(true));
            }
        }
    }
}