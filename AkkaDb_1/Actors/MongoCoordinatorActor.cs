using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using AkkaDb_1.Message;
using MongoDB.Bson;

namespace AkkaDb_1.Actors
{
    public class MongoCoordinatorActor : ReceiveActor
    {
        #region Message classes

        public class BeginJob
        {
            public BeginJob(Job job)
            {
                Job = job;
            }

            public Job Job { get; private set; }
        }

        public class SubscribeToProgressUpdates
        {
            public SubscribeToProgressUpdates(IActorRef subscriber)
            {
                Subscriber = subscriber;
            }

            public IActorRef Subscriber { get; private set; }
        }

        public class PublishUpdate
        {
            private PublishUpdate() { }
            private static readonly PublishUpdate _instance = new PublishUpdate();

            public static PublishUpdate Instance
            {
                get { return _instance; }
            }
        }

        /// <summary>
        /// Let the subscribers know we failed
        /// </summary>
        public class JobFailed
        {
            public JobFailed(Job job)
            {
                Job = job;
            }

            public Job Job { get; private set; }
        }

        #endregion

        private IActorRef _mongoWorker;

        private Job _currentJob;
        private HashSet<IActorRef> _subscribers;
        private ICancelable _publishTimer;

        private bool _receivedInitialUsers = false;

        public MongoCoordinatorActor()
        {
            Waiting();
        }

        protected override void PreStart()
        {
            //_mongoWorker = Context.ActorOf(Props.Create(() => new MongoWorkerActor()));

            _mongoWorker = Context.ActorOf(Props.Create(() =>new MongoWorkerActor()).WithRouter(new RoundRobinPool(20)));

        }

        private void Waiting()
        {
            Receive<MongoCommanderActor.CanAcceptJob>(job => Sender.Tell(new MongoCommanderActor.AbleToAcceptJob(job.Job)));
            Receive<BeginJob>(job =>
            {
                BecomeWorking(job.Job);

                switch (job.Job.JobType)
                {
                    case JobType.WriteData:
                        _mongoWorker.Tell(new RetryableQuery(new MongoWorkerActor.WriteData(job.Job.User), 4));
                        break;
                    case JobType.ReadData:
                        _mongoWorker.Tell(new RetryableQuery(new MongoWorkerActor.ReadData(job.Job.User), 4));
                        break;
                    default:
                        break;
                }
            });
        }

        private void BecomeWorking(Job job)
        {
            _receivedInitialUsers = false;
            _currentJob = job;
            _subscribers = new HashSet<IActorRef>();
            _publishTimer = new Cancelable(Context.System.Scheduler);
            Become(Working);
        }

        private void BecomeWaiting()
        {
            //stop publishing
            _publishTimer.Cancel();
            Become(Waiting);
        }

        private void Working()
        {

            Receive<MongoWorkerActor.ReturnData>(user =>
            {
                Console.WriteLine("[{0}]:[INSERT] {1}", Sender, user.User.ToJson());
                Become(Waiting);
            });

            Receive<MongoWorkerActor.ReturnQueryData>(users =>
            {
                Console.WriteLine("[{0}]:[QUERY] {1}", Sender, users.Users.ToJson());
                Become(Waiting);
            });


            Receive<MongoCommanderActor.CanAcceptJob>(job => Sender.Tell(new MongoCommanderActor.UnableToAcceptJob(job.Job)));

            //query failed, but can be retried
            Receive<RetryableQuery>(query => query.CanRetry, query => _mongoWorker.Tell(query));

            //query failed, can't be retried, and it's a QueryStarrers operation - means the entire job failed
            Receive<RetryableQuery>(query => !query.CanRetry && query.Query is MongoWorkerActor.ReadData, query =>
            {
                _receivedInitialUsers = true;
                foreach (var subscriber in _subscribers)
                {
                    subscriber.Tell(new JobFailed(_currentJob));
                }
                BecomeWaiting();
            });

            //query failed, can't be retried, and it's a QueryStarrers operation - means the entire job failed
            Receive<RetryableQuery>(query => !query.CanRetry && query.Query is MongoWorkerActor.WriteData, query =>
            {
                _receivedInitialUsers = true;
                foreach (var subscriber in _subscribers)
                {
                    subscriber.Tell(new JobFailed(_currentJob));
                }
                BecomeWaiting();
            });

        }
    }
}
