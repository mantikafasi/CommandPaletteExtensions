// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.CommandPalette.Extensions;
using YoutubeDLSharp;

namespace YTDLPExtension;

[Guid("3422a894-842e-41eb-a465-f9f42c304640")]
public sealed partial class YTDLPExtension : IExtension, IDisposable {
    public static YoutubeDL YoutubeDl = new();
    private readonly ManualResetEvent _extensionDisposedEvent;
    private readonly YTDLPExtensionCommandsProvider _provider = new();

    public YTDLPExtension(ManualResetEvent extensionDisposedEvent) {
        _extensionDisposedEvent = extensionDisposedEvent;
    }

    public object? GetProvider(ProviderType providerType) {
        return providerType switch {
            ProviderType.Commands => _provider,
            _ => null
        };
    }

    public void Dispose() {
        _extensionDisposedEvent.Set();
    }
}