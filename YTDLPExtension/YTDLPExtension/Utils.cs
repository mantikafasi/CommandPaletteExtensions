using System.Diagnostics;
using System.Globalization;
using Windows.UI.Notifications;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Toolkit.Uwp.Notifications;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace YTDLPExtension;

public class Utils {
    public static void Log(string message, MessageState state = MessageState.Info) {
        ExtensionHost.ShowStatus(new StatusMessage {
            Message = message,
            State = state
        }, StatusContext.Page);
    }

    public static string DurationToString(float duration) {
        var ts = TimeSpan.FromSeconds(duration);
        return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    public static IDetailsElement[] GetMetadata(VideoData data, FormatData format) {
        var details = new List<IDetailsElement>();
        if (!string.IsNullOrEmpty(format.Resolution))
            details.Add(new DetailsElement {
                Key = "Resolution",
                Data = new DetailsLink { Text = format.Resolution }
            });
        if (format.FrameRate.HasValue)
            details.Add(new DetailsElement {
                Key = "FPS",
                Data = new DetailsLink { Text = format.FrameRate.ToString() }
            });
        if (data.Duration != null)
            details.Add(new DetailsElement {
                Key = "Duration",
                Data = new DetailsLink { Text = DurationToString(data.Duration.Value) }
            });
        if (!string.IsNullOrEmpty(data.Uploader))
            details.Add(new DetailsElement {
                Key = "Uploader",
                Data = new DetailsLink { Text = data.Uploader }
            });
        if (data.ViewCount.HasValue)
            details.Add(new DetailsElement {
                Key = "View Count",
                Data = new DetailsLink { Text = data.ViewCount.ToString() }
            });
        if (data.LikeCount.HasValue)
            details.Add(new DetailsElement {
                Key = "Like Count",
                Data = new DetailsLink { Text = data.LikeCount.ToString() }
            });
        if (data.DislikeCount.HasValue)
            details.Add(new DetailsElement {
                Key = "Dislike Count",
                Data = new DetailsLink { Text = data.DislikeCount.ToString() }
            });
        if (!string.IsNullOrEmpty(format.VideoCodec))
            details.Add(new DetailsElement {
                Key = "Video Codec",
                Data = new DetailsLink { Text = format.VideoCodec }
            });
        if (!string.IsNullOrEmpty(format.AudioCodec))
            details.Add(new DetailsElement {
                Key = "Audio Codec",
                Data = new DetailsLink { Text = format.AudioCodec }
            });
        if (format.AudioChannels.HasValue)
            details.Add(new DetailsElement {
                Key = "Audio Channels",
                Data = new DetailsLink { Text = format.AudioChannels.ToString() }
            });
        if (!string.IsNullOrEmpty(format.FormatId))
            details.Add(new DetailsElement {
                Key = "Format",
                Data = new DetailsLink { Text = format.FormatId }
            });

        return details.ToArray();
    }


    public static async Task DownloadVideo(string filename, string url, string format) {
        var progressToast = new ToastContentBuilder()
            .AddText("Downloading Video")
            .AddVisualChild(new AdaptiveProgressBar {
                Title = filename == ExtensionSettings.Instance.YtdlpOutput.Value ? "" : filename,
                Value = new BindableProgressBarValue("progressValue"),
                Status = "Downloading..."
            }).GetToastContent();

        var toast = new ToastNotification(progressToast.GetXml());

        toast.Data = new NotificationData();

        toast.Data.SequenceNumber = 1;
        toast.Tag = "yt-dlp-download";
        toast.Data.Values["progressValue"] = "0";
        ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);

        var progress = new Progress<DownloadProgress>(p => {
            if (p.State == DownloadState.Success) {
                ToastNotificationManagerCompat.CreateToastNotifier().Hide(toast);
                return;
            }

            toast.Data.Values["progressValue"] = p.Progress.ToString(CultureInfo.InvariantCulture);
            toast.Data.SequenceNumber += 1;

            ToastNotificationManagerCompat.CreateToastNotifier().Update(toast.Data, "yt-dlp-download");
        });

        var output = filename!.Contains(".") ? filename : filename + ".%(ext)s";

        var video = await YTDLPExtension.YoutubeDl.RunVideoDownload(url, format, progress: progress,
            overrideOptions: new OptionSet {
                Output = Path.Combine(ExtensionSettings.Instance.OutputPath.Value, output)
            }, output: new Progress<string>(s => { Log(s); }));

        if (video.Success) {
            Log("Download complete: " + video.Data);

            OnActivated? ev = null;

            ev = toastArgs => {
                Log(toastArgs.Argument);
                var args = ToastArguments.Parse(toastArgs.Argument);
                Process.Start("explorer.exe", "/select, \"" + video.Data + "\"");
                ToastNotificationManagerCompat.OnActivated -= ev;
            };
            ToastNotificationManagerCompat.OnActivated += ev;

            new ToastContentBuilder()
                .AddToastActivationInfo("app", ToastActivationType.Foreground)
                .AddText("Download complete")
                .AddText("Click to open file location")
                .AddArgument("path", video.Data)
                .Show();
        }
        else {
            Log("error downloading video: " + string.Join("\n", video.ErrorOutput));

            new ToastContentBuilder()
                .AddToastActivationInfo("app", ToastActivationType.Foreground)
                .AddText("Download failed")
                .AddText(string.Join("\n", video.ErrorOutput))
                .AddArgument("error", video.ErrorOutput.ToString())
                .Show();
        }
    }
}