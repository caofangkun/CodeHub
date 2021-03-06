using System;
using CodeFramework.Elements;
using MonoTouch.UIKit;
using System.Linq;
using CodeHub.Core.ViewModels.Changesets;
using ReactiveUI;
using System.Reactive.Linq;
using GitHubSharp.Models;
using Xamarin.Utilities.ViewControllers;
using Xamarin.Utilities.DialogElements;
using System.Reactive;
using System.Collections.Generic;
using CodeHub.iOS.WebViews;

namespace CodeHub.iOS.Views.Source
{
    public class ChangesetView : ViewModelPrettyDialogViewController<ChangesetViewModel>
    {
        private readonly SplitButtonElement _split = new SplitButtonElement();
        private readonly Section _commentSection = new Section();

        public ChangesetView()
        {
            this.WhenViewModel(x => x.ShowMenuCommand).Subscribe(x => 
                NavigationItem.RightBarButtonItem = x != null ? x.ToBarButtonItem(UIBarButtonSystemItem.Action) : null);
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var additions = _split.AddButton("Additions", "-");
            var deletions = _split.AddButton("Deletions", "-");
            var parents = _split.AddButton("Parents", "-");

            var commentsElement = new WebElement("comments");
            commentsElement.UrlRequested = ViewModel.GoToUrlCommand.ExecuteIfCan;
            //commentsSection.Add(commentsElement);

            var headerSection = new Section(HeaderView) { _split };
            Root.Reset(headerSection);

            ViewModel.WhenAnyValue(x => x.Commit).IsNotNull().SubscribeSafe(x =>
            {
                HeaderView.Image = Images.LoginUserUnknown;
                HeaderView.ImageUri = x.GenerateGravatarUrl();

                var msg = x.Commit.Message ?? string.Empty;
                var firstLine = msg.IndexOf("\n", StringComparison.Ordinal);
                HeaderView.Text = firstLine > 0 ? msg.Substring(0, firstLine) : msg;

                HeaderView.SubText = "Commited " + (x.Commit.Committer.Date).ToDaysAgo();

                additions.Text = x.Stats.Additions.ToString();
                deletions.Text = x.Stats.Deletions.ToString();
                parents.Text = x.Parents.Count.ToString();

                ReloadData();
            });

            ViewModel.WhenAnyValue(x => x.Commit).Where(x => x != null).Subscribe(Render);

            ViewModel.Comments.Changed
                .Select(_ => new Unit())
                .StartWith(new Unit())
                .Subscribe(x =>
            {
                    var commentModels = ViewModel.Comments.Select(c => 
                        new Comment(c.User.AvatarUrl, c.User.Login, c.BodyHtml, c.CreatedAt.ToDaysAgo()));
                    var razorView = new CommentsView { Model = commentModels };
                    var html = razorView.GenerateString();
                    commentsElement.Value = html;

                    if (commentsElement.GetRootElement() == null && ViewModel.Comments.Count > 0)
                        _commentSection.Add(commentsElement);
            });
        }

        public void Render(CommitModel commitModel)
        {
            var headerSection = new Section(HeaderView) { _split };
            var detailSection = new Section();
            Root.Reset(headerSection, detailSection);

            var user = "Unknown";
            if (commitModel.Commit.Author != null)
                user = commitModel.Commit.Author.Name;
            if (commitModel.Commit.Committer != null)
                user = commitModel.Commit.Committer.Name;

            detailSection.Add(new MultilinedElement(user, commitModel.Commit.Message)
            {
                CaptionColor = Theme.CurrentTheme.MainTextColor,
                ValueColor = Theme.CurrentTheme.MainTextColor,
                BackgroundColor = UIColor.White
            });

            if (ViewModel.ShowRepository)
            {
                var repo = new StyledStringElement(ViewModel.RepositoryName) { 
                    Accessory = MonoTouch.UIKit.UITableViewCellAccessory.DisclosureIndicator, 
                    Lines = 1, 
                    Font = StyledStringElement.DefaultDetailFont, 
                    TextColor = StyledStringElement.DefaultDetailColor,
                    Image = Images.Repo
                };
                repo.Tapped += () => ViewModel.GoToRepositoryCommand.Execute(null);
                detailSection.Add(repo);
            }

			var paths = commitModel.Files.GroupBy(y => {
				var filename = "/" + y.Filename;
				return filename.Substring(0, filename.LastIndexOf("/", System.StringComparison.Ordinal) + 1);
			}).OrderBy(y => y.Key);

			foreach (var p in paths)
			{
				var fileSection = new Section(p.Key);
				foreach (var x in p)
				{
					var y = x;
					var file = x.Filename.Substring(x.Filename.LastIndexOf('/') + 1);
					var sse = new ChangesetElement(file, x.Status, x.Additions, x.Deletions);
					sse.Tapped += () => ViewModel.GoToFileCommand.Execute(y);
					fileSection.Add(sse);
				}
				Root.Add(fileSection);
			}

            Root.Add(_commentSection);

            var addComment = new StyledStringElement("Add Comment") { Image = Images.Pencil };
            addComment.Tapped += () => ViewModel.GoToCommentCommand.ExecuteIfCan();
            Root.Add(new Section { addComment });
        }
    }
}

