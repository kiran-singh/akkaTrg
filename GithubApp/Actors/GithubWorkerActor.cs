using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Octokit;

namespace GithubActors.Actors
{
    /// <summary>
    /// Individual actor responsible for querying the Github API
    /// </summary>
    public class GithubWorkerActor : ReceiveActor
    {
        #region Message classes

        public class QueryStarrers
        {
            public QueryStarrers(RepoKey key)
            {
                Key = key;
            }

            public RepoKey Key { get; private set; }
        }

        /// <summary>
        /// Query an individual starrer
        /// </summary>
        public class QueryStarrer
        {
            public QueryStarrer(string login)
            {
                Login = login;
            }

            public string Login { get; private set; }
        }

        public class StarredReposForUser
        {
            public StarredReposForUser(string login, IEnumerable<Repository> repos)
            {
                Repos = repos;
                Login = login;
            }

            public string Login { get; private set; }

            public IEnumerable<Repository> Repos { get; private set; }
        }

        #endregion

        private IGitHubClient _gitHubClient;
        private readonly Func<IGitHubClient> _gitHubClientFactory;

        public GithubWorkerActor(Func<IGitHubClient> gitHubClientFactory)
        {
            _gitHubClientFactory = gitHubClientFactory;
            InitialReceives();
        }

        protected override void PreStart()
        {
            _gitHubClient = _gitHubClientFactory();
        }

        private void InitialReceives()
        {
            Receive<RetryableQuery>(query => query.Query is QueryStarrer, query =>
            {
                var starrer = (query.Query as QueryStarrer).Login;

                var sender = Sender;
                _gitHubClient.Activity.Starring.GetAllForUser(starrer)
                    .ContinueWith<object>(tr =>
                    {
                        // query faulted
                        if (tr.IsFaulted || tr.IsCanceled)
                            return query.NextTry();
                        // query succeeded
                        return new StarredReposForUser(starrer, tr.Result);
                    }).PipeTo(sender);

            });

            Receive<RetryableQuery>(query => query.Query is QueryStarrers, query =>
            {
                var starrers = (query.Query as QueryStarrers).Key;

                var sender = Sender;
                _gitHubClient.Activity.Starring.GetAllStargazers(starrers.Owner, starrers.Repo)
                    .ContinueWith<object>(tr =>
                    {
                        // query faulted
                        if (tr.IsFaulted || tr.IsCanceled)
                            return query.NextTry();
                        return tr.Result.ToArray();
                    }).PipeTo(sender);
            });
        }
    }
}
