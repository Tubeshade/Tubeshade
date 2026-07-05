using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Tubeshade.Server.Pages.Channels;

public interface IChannelsPage
{
    List<ChannelModel> Channels { get; }

    Task<IActionResult> OnPostSubscribe(Guid channelId);

    Task<IActionResult> OnPostUnsubscribe(Guid channelId);

    Task<IActionResult> OnPostScan(Guid channelId, bool? all);
}
