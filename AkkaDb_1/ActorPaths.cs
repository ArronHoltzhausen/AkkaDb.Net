using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaDb_1
{
    /// <summary>
    /// Static helper class used to define paths to fixed-name actors
    /// (helps eliminate errors when using <see cref="ActorSelection"/>)
    /// </summary>
    public static class ActorPaths
    {
        public static readonly ActorMetaData MongoValidatorActor = new ActorMetaData("validator", "akka://AkkaDb/user/validator");
        public static readonly ActorMetaData MongoCommanderActor = new ActorMetaData("commander", "akka://AkkaDb/user/commander");
        public static readonly ActorMetaData MongoCoordinatorActor = new ActorMetaData("coordinator", "akka://AkkaDb/user/commander/coordinator");
        public static readonly ActorMetaData MongoWorkerActor = new ActorMetaData("worker", "akka://AkkaDb/user/commander/coordinator/worker");
    }

    /// <summary>
    /// Meta-data class
    /// </summary>
    public class ActorMetaData
    {
        public ActorMetaData(string name, string path)
        {
            Name = name;
            Path = path;
        }

        public string Name { get; private set; }

        public string Path { get; private set; }
    }
}
