// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace YTDLPExtension;

public partial class YTDLPExtensionCommandsProvider : CommandProvider {
    private readonly ICommandItem[] _commands;

    public YTDLPExtensionCommandsProvider() {
        Settings = ExtensionSettings.Instance.Settings;
        DisplayName = "Download with yt-dlp";
        Icon = IconHelpers.FromRelativePath("Assets\\square-play.svg");
        _commands = [
            new CommandItem(new YTDLPExtensionPage()) { Title = DisplayName, Icon = IconHelpers.FromRelativePath("Assets\\square-play.svg") },
        ];
    }

    public override ICommandItem[] TopLevelCommands() {
        return _commands;
    }
}