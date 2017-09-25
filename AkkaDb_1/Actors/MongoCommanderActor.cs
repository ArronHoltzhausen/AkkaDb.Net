using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using AkkaDb_1.Message;
using System.Linq;

namespace AkkaDb_1.Actors
{
    public class MongoCommanderActor : ReceiveActor, IWithUnboundedStash
    {

        #region Message classes

        public class CanAcceptJob
        {
            public CanAcceptJob(Job job)
            {
                Job = job;
            }

            public Job Job { get; private set; }
        }

        public class AbleToAcceptJob
        {
            public AbleToAcceptJob(Job job)
            {
                Job = job;
            }

            public Job Job { get; private set; }
        }

        public class UnableToAcceptJob
        {
            public UnableToAcceptJob(Job job)
            {
                Job = job;
            }

            public Job Job { get; private set; }
        }

        #endregion

        private IActorRef _coordinator;
        private IActorRef _canAcceptJobSender;
        private int pendingJobReplies;
        private Job _userJob;

        public MongoCommanderActor()
        {
            Ready();
        }

        private void Ready()
        {
            Receive<CanAcceptJob>(job =>
            {
                _coordinator.Tell(job);
                _userJob = job.Job;
                BecomeAsking();
            });
        }

        private void BecomeAsking()
        {
            _canAcceptJobSender = Sender;
            pendingJobReplies = _coordinator.Ask<Routees>(new GetRoutees()).Result.Members.Count();
            Become(Asking);
        }

        private void Asking()
        {
            //stash any subsequent requests
            Receive<CanAcceptJob>(job => Stash.Stash());

            Receive<ReceiveTimeout>(timeout =>
            {
                _canAcceptJobSender.Tell(new UnableToAcceptJob(_userJob));
                BecomeReady();
            });

            Receive<UnableToAcceptJob>(job =>
            {
                pendingJobReplies--;
                if (pendingJobReplies == 0)
                {
                    Sender.Tell(job);
                    BecomeReady();
                }
            });

            Receive<AbleToAcceptJob>(job =>
            {
                Sender.Tell(job);

                //start processing messages
                Sender.Tell(new MongoCoordinatorActor.BeginJob(job.Job));

                BecomeReady();
            });
        }

        private void BecomeReady()
        {
            Become(Ready);
            Stash.UnstashAll();

            //cancel ReceiveTimeout
            Context.SetReceiveTimeout(null);
        }

        protected override void PreStart()
        {
            // create a broadcast router who will ask all 
            // of them if they're available for work
            // _coordinator = Context.ActorOf(Props.Create(() => new MongoCoordinatorActor()).WithRouter(FromConfig.Instance), ActorPaths.MongoCoordinatorActor.Name);
            _coordinator = Context.ActorOf(Props.Create(() => new MongoCoordinatorActor()).WithRouter(new RoundRobinPool(500)));
            base.PreStart();
        }

        protected override void PreRestart(Exception reason, object message)
        {
            //kill off the old coordinator so we can recreate it from scratch
            _coordinator.Tell(PoisonPill.Instance);
            base.PreRestart(reason, message);
        }

        public IStash Stash { get; set; }
    }
}
