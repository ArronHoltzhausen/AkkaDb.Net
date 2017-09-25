using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaDb_1.Message
{
    public enum JobType
    {
        WriteData,
        ReadData
    }


    public class Job
    {
        public Job(JobType jobType, User user)
        {
            JobType = jobType;
            User = user;
        }

        public JobType JobType { get; private set; }
        public User User { get; private set; }
    }

    public class User
    {
        public User(string name, string surname, DateTime createdAt, string id=null)
        {
            Name = name;
            Surname = surname;
            CreatedAt = createdAt;
            _id = id;
        }

        public string Name { get; private set; }
        public string Surname { get; private set; }
        public DateTime CreatedAt { get; private set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; private set; }
    }

    public class RetryableQuery
    {
        public RetryableQuery(object query, int allowableTries) : this(query, allowableTries, 0)
        {
        }

        private RetryableQuery(object query, int allowableTries, int currentAttempt)
        {
            AllowableTries = allowableTries;
            Query = query;
            CurrentAttempt = currentAttempt;
        }


        public object Query { get; private set; }

        public int AllowableTries { get; private set; }

        public int CurrentAttempt { get; private set; }

        public bool CanRetry { get { return RemainingTries > 0; } }
        public int RemainingTries { get { return AllowableTries - CurrentAttempt; } }

        public RetryableQuery NextTry()
        {
            return new RetryableQuery(Query, AllowableTries, CurrentAttempt + 1);
        }
    }
}
