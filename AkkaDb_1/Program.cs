using System;
using Akka.Actor;
using AkkaDb_1.Actors;
using AkkaDb_1.Message;
using System.Threading;
using Akka.Routing;
namespace AkkaDb_1
{
    class Program
    {
        public static ActorSystem MyActorSystem;

        static void Main(string[] args)
        {
            MyActorSystem = ActorSystem.Create("AkkaDb");
            IActorRef _senderActor = Program.MyActorSystem.ActorOf(Props.Create(() => new MongoCommanderActor()).WithRouter(FromConfig.Instance), ActorPaths.MongoCommanderActor.Name);
            for (int i = 0; i <= 99999; i++) {

                var writeJob = new Job(JobType.WriteData, new User("bob_" + i.ToString(), "john", DateTime.Now));
                _senderActor.Tell(new MongoCommanderActor.CanAcceptJob(writeJob));
                
                var readJob = new Job(JobType.ReadData, new User("bob_" + i.ToString(), "john", DateTime.Now));
                _senderActor.Tell(new MongoCommanderActor.CanAcceptJob(readJob));
            }

            Console.ReadLine();
        }
    }
}
