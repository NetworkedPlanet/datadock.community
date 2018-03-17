namespace DataDock.Worker
{
    public class WorkerConfiguration
    {
        /// <summary>
        /// The path to the Git executable
        /// </summary>
        public string GitPath { get; set; }

        /// <summary>
        /// The path to the directory to use for cloning user repositories
        /// </summary>
        public string RepoBaseDir { get; set; }

    }
}