﻿using System;
using System.Reactive.Linq;
using CodeHub.Core.Services;
using GitHubSharp.Models;
using ReactiveUI;

using Xamarin.Utilities.Core.ViewModels;

namespace CodeHub.Core.ViewModels.PullRequests
{
    public class PullRequestsViewModel : BaseViewModel, ILoadableViewModel
    {
        public IReadOnlyReactiveList<PullRequestItemViewModel> PullRequests { get; private set; }

        public string RepositoryOwner { get; set; }

        public string RepositoryName { get; set; }

        private int _selectedFilter;
        public int SelectedFilter
        {
            get { return _selectedFilter; }
            set { this.RaiseAndSetIfChanged(ref _selectedFilter, value); }
        }

        public IReactiveCommand LoadCommand { get; private set; }

        private string _searchKeyword;
        public string SearchKeyword
        {
            get { return _searchKeyword; }
            set { this.RaiseAndSetIfChanged(ref _searchKeyword, value); }
        }

        public PullRequestsViewModel(IApplicationService applicationService)
		{
            Title = "Pull Requests";

            var pullRequests = new ReactiveList<PullRequestModel>();
            PullRequests = pullRequests.CreateDerivedCollection(
                x => new PullRequestItemViewModel(x, () =>
                {
                    var vm = CreateViewModel<PullRequestViewModel>();
                    vm.RepositoryOwner = RepositoryOwner;
                    vm.RepositoryName = RepositoryName;
                    vm.Id = (int)x.Number;
                    //vm.PullRequest = x.PullRequest;
                    //              vm.WhenAnyValue(x => x.PullRequest).Skip(1).Subscribe(x =>
                    //              {
                    //                    var index = PullRequests.IndexOf(pullRequest);
                    //                    if (index < 0) return;
                    //                    PullRequests[index] = x;
                    //                    PullRequests.Reset();
                    //              });
                    ShowViewModel(vm);
                }),
                filter: x => x.Title.ContainsKeyword(SearchKeyword),
                signalReset: this.WhenAnyValue(x => x.SearchKeyword));

            LoadCommand = ReactiveCommand.CreateAsyncTask(t =>
            {
                var state = SelectedFilter == 0 ? "open" : "closed";
			    var request = applicationService.Client.Users[RepositoryOwner].Repositories[RepositoryName].PullRequests.GetAll(state: state);
                return pullRequests.SimpleCollectionLoad(request, t as bool?);
            });

            this.WhenAnyValue(x => x.SelectedFilter).Skip(1).Subscribe(_ => LoadCommand.ExecuteIfCan());
		}
    }
}
