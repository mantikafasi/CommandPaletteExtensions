using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using YoutubeDLSharp.Options;

namespace YTDLPExtension;

internal sealed partial class YTDLPExtensionPage : DynamicListPage {
    private readonly List<IListItem> _items;
    private CancellationTokenSource _cts = new();

    private string _currentFileName = "";
    private string _currentUrl = "";
    private Task _fetchTask;

    public YTDLPExtensionPage() {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Command Palette YTDLP plugin";
        Name = "Open";  

        _items = new List<IListItem> {
            new ListItem(new NoOpCommand()) { Title = "Please Enter a URL" }
        };
    }

    public override IListItem[] GetItems() {
        return _items.ToArray();
    }

    public override void UpdateSearchText(string oldSearch, string query) {
        if (oldSearch == query) return;

        var url = query.Contains(' ') ? query.Split(" ")[0] : query;

        // if the url changed and the previous task is still running, cancel it so we can start a new task
        if (_currentUrl != url) {
            _cts.CancelAsync();
            _cts = new CancellationTokenSource();
            _fetchTask = null;
            _items.Clear();
            _items.Add(new ListItem(new NoOpCommand()) { Title = "Please Enter a URL" });
        }


        if (Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
            (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)) {
            // after first argument its filename
            if (query.Contains(' '))
                _currentFileName = query.Split(" ", 2)[1];


            if (_fetchTask == null) {
                _fetchTask = Task.Run(async void () => {
                    IsLoading = true;

                    try {
                        var res = await YTDLPExtension.YoutubeDl.RunVideoDataFetch(url, _cts.Token,
                            overrideOptions: new OptionSet { FormatSort = "quality, hasvid, fps" });

                        if (_cts.Token.IsCancellationRequested) {
                            IsLoading = false;
                            return;
                        }

                        if (!res.Success || res.Data == null) {
                            Utils.Log(string.Concat("Error fetching video data: ",
                                string.Join("\n", res.ErrorOutput).AsSpan(0, 1000)));
                            _items[0] = new ListItem(new CopyTextCommand(string.Join("\n", res.ErrorOutput))) {
                                Title = "Error fetching video data; click to copy error",
                                Subtitle = string.Join("\n", res.ErrorOutput)
                            };
                        }
                        else {
                            ShowDetails = true;

                            Array.Reverse(res.Data.Formats);

                            foreach (var format in res.Data.Formats)
                                _items.Add(new ListItem(new AnonymousCommand(() => {
                                    Utils.DownloadVideo(_currentFileName, url, format.FormatId);
                                })) {
                                    Details = new Details {
                                        Title = res.Data.Title,
                                        // for some reason newlines dont show? maybe figure that out
                                        Body = res.Data.Description.Length > 1000
                                            ? res.Data.Description.AsSpan(0, 1000).ToString() + "..."
                                            : res.Data.Description,
                                        HeroImage = new IconInfo(res.Data.Thumbnail),
                                        Metadata = Utils.GetMetadata(res.Data, format)
                                    },
                                    Title = "Quality: " + format.FormatId + " " +
                                            (!string.IsNullOrEmpty(format.FormatNote)
                                                ? "(" + format.FormatNote + ")"
                                                : ""),
                                    Icon = IconHelpers.FromRelativePath(format.VideoCodec != "none" ?
                                        "Assets\\square-play.svg" :
                                        "Assets\\file-audio.svg")
                                });

                            RaiseItemsChanged(0);
                        }
                    }
                    catch (Exception e) {
                        Utils.Log(e.ToString(), MessageState.Error);
                    }

                    IsLoading = false;
                    RaiseItemsChanged(0);
                });

                _currentUrl = url;
            }

            // maybe I should add details to this one as well
            _items[0] = new ListItem(new AnonymousCommand(() => {
                Utils.DownloadVideo(_currentFileName, url, "bestvideo+bestaudio");
                RaiseItemsChanged(0);
            })) {
                Title = "Best Video+Audio",
                Icon = IconHelpers.FromRelativePath("Assets\\square-play.svg")
            };

            IsLoading = true;
        }
        else {
            _items[0] = new ListItem(new NoOpCommand())
                { Title = "Invalid URL", Subtitle = "Please enter a valid URL", Icon = IconHelpers.FromRelativePath("Assets\\square-play.svg")};
        }

        RaiseItemsChanged(0);
    }
}