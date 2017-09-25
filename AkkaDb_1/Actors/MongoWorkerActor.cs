using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using AkkaDb_1.Message;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver;
using System.Threading;

namespace AkkaDb_1.Actors
{
    public class MongoWorkerActor : ReceiveActor
    {
        #region messages
        public class WriteData
        {
            public WriteData(User user)
            {
                User = user;
            }

            public User User { get; private set; }
        }

        public class ReadData
        {
            public ReadData(User user)
            {
                User = user;
            }

            public User User { get; private set; }
        }


        public class ReturnData
        {
            public ReturnData(User user)
            {
                User = user;
            }

            public ReturnData(List<User> users)
            {
                Users = users;
            }

            public User User { get; private set; }

            public List<User> Users { get; private set; }
        }


        public class ReturnQueryData
        {
           
            public ReturnQueryData(List<User> users)
            {
                Users = users;
            }

            public List<User> Users { get; private set; }
        }
        #endregion


        protected static IMongoClient _client;
        protected static IMongoDatabase _database;


        public MongoWorkerActor()
        {
            InitialReceives();
        }

        protected override void PreStart()
        {
            _client = new MongoClient("mongodb://127.0.0.1:27017");
            _database = _client.GetDatabase("test");
            //INITIALIZE MONGO CLIENT
        }


        private void InitialReceives()
        {
            
            //query an individual starrer
            ReceiveAsync<RetryableQuery>(query => query.Query is WriteData, async query =>
            {
                // ReSharper disable once PossibleNullReferenceException (we know from the previous IS statement that this is not null)
                var user = (query.Query as WriteData).User;
                try
                {
                    var collection = _database.GetCollection<User>("User");
                    await collection.InsertOneAsync(user).ContinueWith<object>( result => 
                    {
                        if(!result.IsFaulted && !result.IsCanceled)
                        {
                            Sender.Tell(new ReturnData(user));
                        }
                        else
                        {
                            Sender.Tell(query.NextTry());
                        }
                        return result.IsFaulted;
                    });
                }
                catch (MongoException ex)
                {
                    //operation failed - let the parent know
                    Sender.Tell(query.NextTry());
                }
            });

            ReceiveAsync<RetryableQuery>(query => query.Query is ReadData, async query =>
            {
                // ReSharper disable once PossibleNullReferenceException (we know from the previous IS statement that this is not null)
                var user = (query.Query as ReadData).User;
                try
                {
                    var collection = _database.GetCollection<User>("User");
                    var filter = Builders<User>.Filter.Eq("Name", user.Name);
                    await collection.Find(filter).ToListAsync().ContinueWith<object>(result =>
                    {
                        if (!result.IsFaulted && !result.IsCanceled)
                        {
                            Sender.Tell(new ReturnQueryData(result.Result));
                        }
                        else
                        {
                            Sender.Tell(query.NextTry());
                        }
                        return result.IsFaulted;
                    });
                }
                catch (Exception ex)
                {
                    //operation failed - let the parent know
                    Sender.Tell(query.NextTry());
                }
            });
        }
    }
}
