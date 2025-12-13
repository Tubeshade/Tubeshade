using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NodaTime;
using NUnit.Framework;

namespace Ytdlp.Tests.Videos;

public sealed class VideoDataTestCaseSource : IEnumerable<TestCaseData<string, VideoData>>
{
    private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

    /// <inheritdoc />
    public IEnumerator<TestCaseData<string, VideoData>> GetEnumerator()
    {
        var stream = Assembly.GetManifestResourceStream(typeof(VideoDataTestCaseSource), "njX2bu-_Vw4.info.json")!;
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        yield return new(
            json,
            new VideoData
            {
                Type = "video",
                Id = "njX2bu-_Vw4",
                Title = "2020 LG OLED l  The Black 4K HDR 60fps",
                Formats =
                [
                    new FormatData
                    {
                        Url = "https://i.ytimg.com/sb/njX2bu-_Vw4/storyboard3_L0/default.jpg?sqp=-oaymwENSDfyq4qpAwVwAcABBqLzl_8DBgivid2oBg==&sigh=rs$AOn4CLCkfS7IxPXkYES_xuX9jpAK9xHuow",
                        Extension = "mhtml",
                        Format = "sb3 - 48x27 (storyboard)",
                        FormatId = "sb3",
                        FormatNote = "storyboard",
                        Width = 48,
                        Height = 27,
                        Resolution = "48x27",
                        Framerate = 0.7874015748031497m,
                    },
                    new FormatData
                    {
                        Url = "https://i.ytimg.com/sb/njX2bu-_Vw4/storyboard3_L3/M$M.jpg?sqp=-oaymwENSDfyq4qpAwVwAcABBqLzl_8DBgivid2oBg==&sigh=rs$AOn4CLDsp4LfLi2pWM76qmJdJI7G1OF0iQ",
                        Extension = "mhtml",
                        Format = "sb0 - 320x180 (storyboard)",
                        FormatId = "sb0",
                        FormatNote = "storyboard",
                        Width = 320,
                        Height = 180,
                        Resolution = "320x180",
                        Framerate = 0.5118110236220472m,
                    },
                    new FormatData
                    {
                        Url = "https://rr4---sn-a5uuxaxjvh-gpm6.googlevideo.com/videoplayback?expire=1765040151&ei=tws0aY3hH4qr0u8P5p7ewAk&ip=78.84.145.123&id=o-AGGb3Kr2A7vod8n2VCuGDrmgd4TD6__LjF912DbmfjEA&itag=249&source=youtube&requiressl=yes&xpc=EgVo2aDSNQ%3D%3D&cps=124&met=1765018551%2C&mh=zf&mm=31%2C29&mn=sn-a5uuxaxjvh-gpm6%2Csn-5go7ynl6&ms=au%2Crdu&mv=m&mvi=4&pl=17&rms=au%2Cau&initcwndbps=2893750&siu=1&bui=AdEuB5SmI26xVGS7U1aeQn7aFtHmR9rVJslAlJGIe7YFJRozFhlpMNBNvEG8UMYxdUcFwv77rw&vprv=1&svpuc=1&xtags=drc%3D1&mime=audio%2Fwebm&ns=mc2cSU0DF1mS63-DUZDhexUQ&rqh=1&gir=yes&clen=829689&dur=126.561&lmt=1728202054671849&mt=1765018147&fvip=1&keepalive=yes&lmw=1&fexp=51557447%2C51565115%2C51565682%2C51580970&c=TVHTML5&sefc=1&txp=5502434&n=5H05qZpgYr4b-A&sparams=expire%2Cei%2Cip%2Cid%2Citag%2Csource%2Crequiressl%2Cxpc%2Csiu%2Cbui%2Cvprv%2Csvpuc%2Cxtags%2Cmime%2Cns%2Crqh%2Cgir%2Cclen%2Cdur%2Clmt&sig=AJfQdSswRgIhAPjQyIC-ytXyfye55BR3vqEZ_iHx-TcwwjK2k4-RazRfAiEA5Bq8iTVCug0faQMSHq-Av-Mr4DUuuk2VEPsEfwjisiE%3D&lsparams=cps%2Cmet%2Cmh%2Cmm%2Cmn%2Cms%2Cmv%2Cmvi%2Cpl%2Crms%2Cinitcwndbps&lsig=APaTxxMwRgIhANI1QWfFSeyUwBU7UuViycnl1pzCOY1F2DG4hbIDF14fAiEA03kHvwuFikJ4Jw-Eyk0SFQuDLYZo79_GNS68MjtQ1d0%3D",
                        Extension = "webm",
                        Format = "249-drc - audio only (low, DRC)",
                        FormatId = "249-drc",
                        FormatNote = "low, DRC",
                        Resolution = "audio only",
                        FileSize = 829689,
                        ApproximateFileSize = 829686,
                    },
                    new FormatData
                    {
                        Url = "https://rr4---sn-a5uuxaxjvh-gpm6.googlevideo.com/videoplayback?expire=1765040151&ei=tws0aY3hH4qr0u8P5p7ewAk&ip=78.84.145.123&id=o-AGGb3Kr2A7vod8n2VCuGDrmgd4TD6__LjF912DbmfjEA&itag=256&source=youtube&requiressl=yes&xpc=EgVo2aDSNQ%3D%3D&cps=124&met=1765018551%2C&mh=zf&mm=31%2C29&mn=sn-a5uuxaxjvh-gpm6%2Csn-5go7ynl6&ms=au%2Crdu&mv=m&mvi=4&pl=17&rms=au%2Cau&initcwndbps=2893750&siu=1&bui=AdEuB5SmI26xVGS7U1aeQn7aFtHmR9rVJslAlJGIe7YFJRozFhlpMNBNvEG8UMYxdUcFwv77rw&vprv=1&svpuc=1&mime=audio%2Fmp4&ns=mc2cSU0DF1mS63-DUZDhexUQ&rqh=1&gir=yes&clen=3090032&dur=126.677&lmt=1728200690161827&mt=1765018147&fvip=1&keepalive=yes&lmw=1&fexp=51557447%2C51565115%2C51565682%2C51580970&c=TVHTML5&sefc=1&txp=5502434&n=5H05qZpgYr4b-A&sparams=expire%2Cei%2Cip%2Cid%2Citag%2Csource%2Crequiressl%2Cxpc%2Csiu%2Cbui%2Cvprv%2Csvpuc%2Cmime%2Cns%2Crqh%2Cgir%2Cclen%2Cdur%2Clmt&sig=AJfQdSswRAIgIrYd8LL5LhqCW2Pur5XdVGLUbse_2cVuS2bPDYOltXICIEFvNcFKVZYXh_kqVZ2ACHOcQjihr59TfTDNYcMLJQAW&lsparams=cps%2Cmet%2Cmh%2Cmm%2Cmn%2Cms%2Cmv%2Cmvi%2Cpl%2Crms%2Cinitcwndbps&lsig=APaTxxMwRgIhANI1QWfFSeyUwBU7UuViycnl1pzCOY1F2DG4hbIDF14fAiEA03kHvwuFikJ4Jw-Eyk0SFQuDLYZo79_GNS68MjtQ1d0%3D",
                        Extension = "m4a",
                        Format = "256 - audio only (low)",
                        FormatId = "256",
                        FormatNote = "low",
                        Resolution = "audio only",
                        FileSize = 3090032,
                        ApproximateFileSize = 3090016,
                    },
                    new FormatData
                    {
                        Url = "https://manifest.googlevideo.com/api/manifest/hls_playlist/expire/1765040151/ei/tws0ae66Jpi_0u8Prv-M-Q8/ip/78.84.145.123/id/9e35f66eefbf570e/itag/91/source/youtube/requiressl/yes/ratebypass/yes/pfa/1/sgoap/clen%3D773312%3Bdur%3D126.688%3Bgir%3Dyes%3Bitag%3D139%3Blmt%3D1728200695467528/sgovp/clen%3D621532%3Bdur%3D126.526%3Bgir%3Dyes%3Bitag%3D160%3Blmt%3D1728201502834667/rqh/1/hls_chunk_host/rr4---sn-a5uuxaxjvh-gpm6.googlevideo.com/xpc/EgVo2aDSNQ%3D%3D/cps/27/met/1765018551,/mh/zf/mm/31,29/mn/sn-a5uuxaxjvh-gpm6,sn-5goeenes/ms/au,rdu/mv/m/mvi/4/pcm2cms/yes/pl/17/rms/au,au/initcwndbps/2893750/siu/1/bui/AdEuB5T2aKHYJW1XLaxz7fRfajHivhD6rCpMpOq98ykbnAMtleDsHPFTgtPY_hO41hmW_M9Pyw/spc/6b0G_K1rhckIv6X9esRDQJe76PV61AW-_H-NuM4xmR-3TNORUDMz/vprv/1/playlist_type/CLEAN/dover/11/txp/550C434/mt/1765018147/fvip/4/keepalive/yes/fexp/51355912,51552689,51565115,51565682,51580968/sparams/expire,ei,ip,id,itag,source,requiressl,ratebypass,pfa,sgoap,sgovp,rqh,xpc,siu,bui,spc,vprv,playlist_type/sig/AJfQdSswRQIhAIcx09_mc6dViA1lU8YuaQGhMStdg-e0GzcdVHOPTZHaAiB2Tn4C5XcQXow_hy1Hfy1GoSr4OTWPxFe0VB9bkVfCvw%3D%3D/lsparams/hls_chunk_host,cps,met,mh,mm,mn,ms,mv,mvi,pcm2cms,pl,rms,initcwndbps/lsig/APaTxxMwRQIhAL4ODXzAMbjHhc3N7iQ_gCI1U4sH7CrgpS8K5Dzm4xJTAiB6ox0vZoBB4d4paQjFE4Dii6H-UZb-WVW8J4peWTPwpA%3D%3D/playlist/index.m3u8",
                        ManifestUrl = "https://manifest.googlevideo.com/api/manifest/hls_variant/expire/1765040151/ei/tws0ae66Jpi_0u8Prv-M-Q8/ip/78.84.145.123/id/9e35f66eefbf570e/source/youtube/requiressl/yes/xpc/EgVo2aDSNQ%3D%3D/playback_host/rr4---sn-a5uuxaxjvh-gpm6.googlevideo.com/cps/27/met/1765018551%2C/mh/zf/mm/31%2C29/mn/sn-a5uuxaxjvh-gpm6%2Csn-5goeenes/ms/au%2Crdu/mv/m/mvi/4/pcm2cms/yes/pl/17/rms/au%2Cau/tx/51539831/txs/51539830%2C51539831/hfr/1/maxh/4320/tts_caps/1/maudio/1/initcwndbps/2893750/siu/1/bui/AdEuB5T2aKHYJW1XLaxz7fRfajHivhD6rCpMpOq98ykbnAMtleDsHPFTgtPY_hO41hmW_M9Pyw/spc/6b0G_K1rhckIv6X9esRDQJe76PV61AW-_H-NuM4xmR-3TNORUDMz/vprv/1/go/1/rqh/5/mt/1765018147/fvip/4/nvgoi/1/ncsapi/1/keepalive/yes/fexp/51355912%2C51552689%2C51565115%2C51565682%2C51580968/dover/11/itag/0/playlist_type/CLEAN/sparams/expire%2Cei%2Cip%2Cid%2Csource%2Crequiressl%2Cxpc%2Ctx%2Ctxs%2Chfr%2Cmaxh%2Ctts_caps%2Cmaudio%2Csiu%2Cbui%2Cspc%2Cvprv%2Cgo%2Crqh%2Citag%2Cplaylist_type/sig/AJfQdSswRgIhALJsztISc42my94dXgkNJ-ybAAoxnIPNSbnPUjveiajRAiEAtseGGV_QDoxbwlABuGJW_zCBWbTxqo81_az8b1Skek8%3D/lsparams/playback_host%2Ccps%2Cmet%2Cmh%2Cmm%2Cmn%2Cms%2Cmv%2Cmvi%2Cpcm2cms%2Cpl%2Crms%2Cinitcwndbps/lsig/APaTxxMwRgIhALeJ_cQ3cek6w6l7SOgfnMVEshPTbHKKWjdzgcAIhrsoAiEA4zpgVofE_Zoid8L1crJhax4qTm3P4UaVA_iIShIBQA0%3D/file/index.m3u8",
                        Extension = "mp4",
                        Format = "91 - 256x144",
                        FormatId = "91",
                        Width = 256,
                        Height = 144,
                        Resolution = "256x144",
                        Framerate = 30.0m,
                    },
                    new FormatData
                    {
                        Url = "https://rr4---sn-a5uuxaxjvh-gpm6.googlevideo.com/videoplayback?expire=1765040151&ei=tws0aY3hH4qr0u8P5p7ewAk&ip=78.84.145.123&id=o-AGGb3Kr2A7vod8n2VCuGDrmgd4TD6__LjF912DbmfjEA&itag=160&aitags=133%2C134%2C135%2C136%2C160%2C242%2C243%2C244%2C247%2C278%2C298%2C299%2C302%2C303%2C308%2C315%2C330%2C331%2C332%2C333%2C334%2C335%2C336%2C337%2C694%2C695%2C696%2C697%2C698%2C699%2C700%2C701%2C779%2C780&source=youtube&requiressl=yes&xpc=EgVo2aDSNQ%3D%3D&cps=124&met=1765018551%2C&mh=zf&mm=31%2C29&mn=sn-a5uuxaxjvh-gpm6%2Csn-5go7ynl6&ms=au%2Crdu&mv=m&mvi=4&pl=17&rms=au%2Cau&initcwndbps=2893750&siu=1&bui=AdEuB5SmI26xVGS7U1aeQn7aFtHmR9rVJslAlJGIe7YFJRozFhlpMNBNvEG8UMYxdUcFwv77rw&vprv=1&svpuc=1&mime=video%2Fmp4&ns=mc2cSU0DF1mS63-DUZDhexUQ&rqh=1&gir=yes&clen=621532&dur=126.526&lmt=1728201502834667&mt=1765018147&fvip=1&keepalive=yes&lmw=1&fexp=51557447%2C51565115%2C51565682%2C51580970&c=TVHTML5&sefc=1&txp=550C434&n=5H05qZpgYr4b-A&sparams=expire%2Cei%2Cip%2Cid%2Caitags%2Csource%2Crequiressl%2Cxpc%2Csiu%2Cbui%2Cvprv%2Csvpuc%2Cmime%2Cns%2Crqh%2Cgir%2Cclen%2Cdur%2Clmt&sig=AJfQdSswRQIhAKMOlni8W06d1aLxGsAymnFDtwoj2fITo2lNImPQCvlrAiAEAr9lhGlUSyHTjM295HE8fASykVUK-jiuqwSJC6YfSA%3D%3D&lsparams=cps%2Cmet%2Cmh%2Cmm%2Cmn%2Cms%2Cmv%2Cmvi%2Cpl%2Crms%2Cinitcwndbps&lsig=APaTxxMwRgIhANI1QWfFSeyUwBU7UuViycnl1pzCOY1F2DG4hbIDF14fAiEA03kHvwuFikJ4Jw-Eyk0SFQuDLYZo79_GNS68MjtQ1d0%3D",
                        Extension = "mp4",
                        Format = "160 - 256x144 (144p)",
                        FormatId = "160",
                        FormatNote = "144p",
                        Width = 256,
                        Height = 144,
                        Resolution = "256x144",
                        Framerate = 30,
                        FileSize = 621532,
                        ApproximateFileSize = 621527,
                    },
                    new FormatData
                    {
                        Url = "https://rr4---sn-a5uuxaxjvh-gpm6.googlevideo.com/videoplayback?expire=1765040151&ei=tws0aY3hH4qr0u8P5p7ewAk&ip=78.84.145.123&id=o-AGGb3Kr2A7vod8n2VCuGDrmgd4TD6__LjF912DbmfjEA&itag=773&source=youtube&requiressl=yes&xpc=EgVo2aDSNQ%3D%3D&cps=124&met=1765018551%2C&mh=zf&mm=31%2C29&mn=sn-a5uuxaxjvh-gpm6%2Csn-5go7ynl6&ms=au%2Crdu&mv=m&mvi=4&pl=17&rms=au%2Cau&initcwndbps=2893750&siu=1&bui=AdEuB5SmI26xVGS7U1aeQn7aFtHmR9rVJslAlJGIe7YFJRozFhlpMNBNvEG8UMYxdUcFwv77rw&vprv=1&svpuc=1&mime=audio%2Fmp4&ns=mc2cSU0DF1mS63-DUZDhexUQ&rqh=1&gir=yes&clen=6028273&dur=126.564&lmt=1755628234014667&mt=1765018147&fvip=1&keepalive=yes&lmw=1&fexp=51557447%2C51565115%2C51565682%2C51580970&c=TVHTML5&sefc=1&txp=5532534&n=5H05qZpgYr4b-A&sparams=expire%2Cei%2Cip%2Cid%2Citag%2Csource%2Crequiressl%2Cxpc%2Csiu%2Cbui%2Cvprv%2Csvpuc%2Cmime%2Cns%2Crqh%2Cgir%2Cclen%2Cdur%2Clmt&sig=AJfQdSswRgIhALBrZ3o6o-VspPzi_v-rV5F6WGcjy72bGlHCMbPbZ63vAiEAiOvubKAt_qqWk1a0aUNCw0J0ogimjC2HQF6qgxvnyC4%3D&lsparams=cps%2Cmet%2Cmh%2Cmm%2Cmn%2Cms%2Cmv%2Cmvi%2Cpl%2Crms%2Cinitcwndbps&lsig=APaTxxMwRgIhANI1QWfFSeyUwBU7UuViycnl1pzCOY1F2DG4hbIDF14fAiEA03kHvwuFikJ4Jw-Eyk0SFQuDLYZo79_GNS68MjtQ1d0%3D",
                        Extension = "m4a",
                        Format = "773 - unknown (medium)",
                        FormatId = "773",
                        FormatNote = "medium",
                        FileSize = 6028273,
                        ApproximateFileSize = 6028259,
                    },
                    new FormatData
                    {
                        Url = "https://rr4---sn-a5uuxaxjvh-gpm6.googlevideo.com/videoplayback?expire=1765040151&ei=tws0aY3hH4qr0u8P5p7ewAk&ip=78.84.145.123&id=o-AGGb3Kr2A7vod8n2VCuGDrmgd4TD6__LjF912DbmfjEA&itag=701&aitags=133%2C134%2C135%2C136%2C160%2C242%2C243%2C244%2C247%2C278%2C298%2C299%2C302%2C303%2C308%2C315%2C330%2C331%2C332%2C333%2C334%2C335%2C336%2C337%2C694%2C695%2C696%2C697%2C698%2C699%2C700%2C701%2C779%2C780&source=youtube&requiressl=yes&xpc=EgVo2aDSNQ%3D%3D&cps=124&met=1765018551%2C&mh=zf&mm=31%2C29&mn=sn-a5uuxaxjvh-gpm6%2Csn-5go7ynl6&ms=au%2Crdu&mv=m&mvi=4&pl=17&rms=au%2Cau&initcwndbps=2893750&siu=1&bui=AdEuB5SmI26xVGS7U1aeQn7aFtHmR9rVJslAlJGIe7YFJRozFhlpMNBNvEG8UMYxdUcFwv77rw&vprv=1&svpuc=1&mime=video%2Fmp4&ns=mc2cSU0DF1mS63-DUZDhexUQ&rqh=1&gir=yes&clen=382021399&dur=126.543&lmt=1728201001842517&mt=1765018147&fvip=1&keepalive=yes&lmw=1&fexp=51557447%2C51565115%2C51565682%2C51580970&c=TVHTML5&sefc=1&txp=550C434&n=5H05qZpgYr4b-A&sparams=expire%2Cei%2Cip%2Cid%2Caitags%2Csource%2Crequiressl%2Cxpc%2Csiu%2Cbui%2Cvprv%2Csvpuc%2Cmime%2Cns%2Crqh%2Cgir%2Cclen%2Cdur%2Clmt&sig=AJfQdSswRQIhANWKPDAk8XGDQ1YXyORB0ynKc1m89pjnL1tgzLWzbM00AiA2mjD0qsMr0ieHc9nV3si00qDUYxni6Fs5Wr3E5_yfsA%3D%3D&lsparams=cps%2Cmet%2Cmh%2Cmm%2Cmn%2Cms%2Cmv%2Cmvi%2Cpl%2Crms%2Cinitcwndbps&lsig=APaTxxMwRgIhANI1QWfFSeyUwBU7UuViycnl1pzCOY1F2DG4hbIDF14fAiEA03kHvwuFikJ4Jw-Eyk0SFQuDLYZo79_GNS68MjtQ1d0%3D",
                        Extension = "mp4",
                        Format = "701 - 3840x2160 (2160p60 HDR)",
                        FormatId = "701",
                        FormatNote = "2160p60 HDR",
                        Width = 3840,
                        Height = 2160,
                        Resolution = "3840x2160",
                        Framerate = 60,
                        FileSize = 382021399,
                        ApproximateFileSize = 382021390,
                    }
                ],
                Url = null,
                Extension = "mp4",
                Format = "701 - 3840x2160 (2160p60 HDR)+258 - audio only (high)",
                PlayerUrl = null,
                Direct = null,
                AlternativeTitle = null,
                DisplayId = "njX2bu-_Vw4",
                Thumbnails =
                [
                    new() { Id = "24", Url = "https://i.ytimg.com/vi/njX2bu-_Vw4/default.jpg", Preference = -13 },
                    new() { Id = "25", Url = "https://i.ytimg.com/vi_webp/njX2bu-_Vw4/default.webp", Preference = -12 },
                    new() { Id = "26", Url = "https://i.ytimg.com/vi/njX2bu-_Vw4/mqdefault.jpg", Preference = -11 },
                    new() { Id = "27", Url = "https://i.ytimg.com/vi_webp/njX2bu-_Vw4/mqdefault.webp", Preference = -10 },
                    new() { Id = "28", Url = "https://i.ytimg.com/vi/njX2bu-_Vw4/0.jpg", Preference = -9 },
                    new() { Id = "29", Url = "https://i.ytimg.com/vi_webp/njX2bu-_Vw4/0.webp", Preference = -8 },
                    new() { Id = "30", Width = 168, Height = 94, Resolution = "168x94", Url = "https://i.ytimg.com/vi/njX2bu-_Vw4/hqdefault.jpg?sqp=-oaymwEbCKgBEF5IVfKriqkDDggBFQAAiEIYAXABwAEG&rs=AOn4CLBlCHLanMI3hpk6YbSTjJXcWVAZwQ", Preference = -7 },
                    new() { Id = "31", Width = 168, Height = 94, Resolution = "168x94", Url = "https://i.ytimg.com/vi/njX2bu-_Vw4/hqdefault.jpg?sqp=-oaymwEiCKgBEF5IWvKriqkDFQgBFQAAAAAYASUAAMhCPQCAokN4AQ==&rs=AOn4CLDWqktGAJVB7ry8aFI4qezPzqMvfg", Preference = -7 },
                    new() { Id = "32", Width = 196, Height = 110, Resolution = "196x110", Url = "https://i.ytimg.com/vi/njX2bu-_Vw4/hqdefault.jpg?sqp=-oaymwEbCMQBEG5IVfKriqkDDggBFQAAiEIYAXABwAEG&rs=AOn4CLBUn9-ZFRJUyuKOywrI184DjyRECQ", Preference = -7 },
                    new() { Id = "33", Width = 196, Height = 110, Resolution = "196x110", Url = "https://i.ytimg.com/vi/njX2bu-_Vw4/hqdefault.jpg?sqp=-oaymwEiCMQBEG5IWvKriqkDFQgBFQAAAAAYASUAAMhCPQCAokN4AQ==&rs=AOn4CLAYrUCCzuy6Tue9VxsMf5Wb8a9ZEw", Preference = -7 },
                    new() { Id = "34", Width = 246, Height = 138, Resolution = "246x138", Url = "https://i.ytimg.com/vi/njX2bu-_Vw4/hqdefault.jpg?sqp=-oaymwEcCPYBEIoBSFXyq4qpAw4IARUAAIhCGAFwAcABBg==&rs=AOn4CLDmB1fsmDK2HQ_iMPrRqfzaxSK9Dg", Preference = -7 },
                    new() { Id = "35", Width = 246, Height = 138, Resolution = "246x138", Url = "https://i.ytimg.com/vi/njX2bu-_Vw4/hqdefault.jpg?sqp=-oaymwEjCPYBEIoBSFryq4qpAxUIARUAAAAAGAElAADIQj0AgKJDeAE=&rs=AOn4CLC_tMZAEbtO4eG3OVmu72ehy2zYCA", Preference = -7 },
                    new() { Id = "36", Width = 336, Height = 188, Resolution = "336x188", Url = "https://i.ytimg.com/vi/njX2bu-_Vw4/hqdefault.jpg?sqp=-oaymwEcCNACELwBSFXyq4qpAw4IARUAAIhCGAFwAcABBg==&rs=AOn4CLDu6MMRTMHqqjM-gaoJHyHl4ZdW0Q", Preference = -7 },
                    new() { Id = "37", Width = 336, Height = 188, Resolution = "336x188", Url = "https://i.ytimg.com/vi/njX2bu-_Vw4/hqdefault.jpg?sqp=-oaymwEjCNACELwBSFryq4qpAxUIARUAAAAAGAElAADIQj0AgKJDeAE=&rs=AOn4CLCmSr7fDyL8oC0DBjd3qz4eBweRjw", Preference = -7 },
                    new() { Id = "38", Width = 480, Height = 360, Resolution = "480x360", Url = "https://i.ytimg.com/vi/njX2bu-_Vw4/hqdefault.jpg", Preference = -7 },
                    new() { Id = "39", Url = "https://i.ytimg.com/vi_webp/njX2bu-_Vw4/hqdefault.webp", Preference = -6 },
                    new() { Id = "40", Url = "https://i.ytimg.com/vi/njX2bu-_Vw4/sddefault.jpg", Preference = -5 },
                    new() { Id = "41", Url = "https://i.ytimg.com/vi_webp/njX2bu-_Vw4/sddefault.webp", Preference = -4 },
                    new() { Id = "42", Url = "https://i.ytimg.com/vi/njX2bu-_Vw4/hq720.jpg", Preference = -3 },
                    new() { Id = "43", Url = "https://i.ytimg.com/vi_webp/njX2bu-_Vw4/hq720.webp", Preference = -2 },
                    new() { Id = "44", Width = 1920, Height = 1080, Resolution = "1920x1080" , Url = "https://i.ytimg.com/vi/njX2bu-_Vw4/maxresdefault.jpg", Preference = -1 },
                    new() { Id = "45", Url = "https://i.ytimg.com/vi_webp/njX2bu-_Vw4/maxresdefault.webp", Preference = 0 },
                ],
                Thumbnail = "https://i.ytimg.com/vi/njX2bu-_Vw4/maxresdefault.jpg",
                Description = "The Power of SELF-LIT PiXELS\n\nMeet all new LG OLED with 100 million of SELF-LIT PiXELS. \nWhen every pixel lights by itself, what you see becomes more \nExpressive, \nRealistic, \nResponsive,\nand Artistic.\n\nThe Power of SELF-LIT PiXELS\n\nLearn more : https://www.lg.com/uk/oled-tvs  \n\n#LGOLED, #Black, #4KHDR, #SELFLIT, #4KHDR60fps,  #SELFLITPiXELS, #SELFLITOLED",
                Uploader = "LG Global",
                Channel = "LG Global",
                ChannelId = "UC2SIWgqcys7Gcb6JxsFTm1Q",
                ChannelUrl = "https://www.youtube.com/channel/UC2SIWgqcys7Gcb6JxsFTm1Q",
                DurationInSeconds = 127,
                Duration = Duration.FromSeconds(127),
                Timestamp = 1589531938,
                Categories = ["Science & Technology"],
                Tags =
                [
                    "LGTV", "4KTV", "LG4K", "LG8K", "8KOLEDTV", "8KTV", "Real8K", "BestTV", "BestSmartTV", "BestOLEDTV",
                    "Best4KTV", "Best8KTV LGOLEDTV", "OLED", "OLEDTV", "LGOLED", "CES2020", "selflitpixels",
                    "selflitpixel", "selflitlgoled", "BigscreenTV", "LargeScreenTV", "77inchTV", "77LGTV", "77LGOLED",
                    "88inchTV", "88LGTV", "88LGOLED", "newTV", "2020newTV", "LG2020tv", "smarttv", "PictureQuality",
                    "SoundQuality", "AI", "ArtificialIntelligence", "LGThinQ", "LGThinQAI", "Processor", "AIProcessor",
                    "IntelligentProcessor", "a9Gen3IntelligentProcessor", "DeepLearning", "DeepLearningTechnology",
                    "BestWatchingExperience"
                ],
                LikeCount = 157104,
                WebpageUrl = "https://www.youtube.com/watch?v=njX2bu-_Vw4",
                Availability = "public",
                LiveStatus = "not_live",
            })
        {
            TestName = "Public video with valid reponse",
        };
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
