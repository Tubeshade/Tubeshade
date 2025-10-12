using System.Collections;
using System.Collections.Generic;
using NodaTime;
using NodaTime.Text;
using NUnit.Framework;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Tests.Services;

public sealed class ChaptersTestCaseSource : IEnumerable<TestCaseData<string?, Period, TextTrackCue[]?>>
{
    private static readonly IPattern<Duration> Pattern = StringExtensions.Pattern;

    /// <inheritdoc />
    public IEnumerator<TestCaseData<string?, Period, TextTrackCue[]?>> GetEnumerator()
    {
        yield return new TestCaseData<string?, Period, TextTrackCue[]?>(
            """
            Go to https://chatllm.abacus.ai/ygw and get access to ChatLLM Teams for only $10/month!

            With Russia resurgent and the United States focused on making a pivot to China, Europe finds itself in a unique position. For all of NATO's history, Europe was a secondary partner in the alliance. Today, though, the United States cannot cover major threats in both the Atlantic and the Pacific. As a result, the United States needs Europe now more than ever. But that also gives Europe bargaining power in the relationship it never had before. What Europe will do with that, however, remains to be seen.

            Check out my book "How Ukraine Survived": https://amzn.to/47gnlEf. You can also read it for free by signing up for a Kindle Unlimited trial at https://amzn.to/3QMsBr8. (I use affiliate links, meaning I earn a commission when you make a transaction through them. Even if you read for free, you are still supporting the channel.)

            0:00 The Greatest Irony in Geopolitics Today
            2:13 NATO's Historical Balance of Power
            5:00 U.S. Attitude Toward NATO Post-Cold War
            6:00 The True Story of Article 5's Only Invocation
            9:51 What the U.S. Public Thinks about NATO
            12:53 Why the United States Needs NATO Now
            19:55 Europe's Bargaining Power

            The appearance of U.S. Department of Defense (DoD), EU, and NATO visual information does not imply or constitute an endorsement.

            Media licensed under CC BY 4.0 (https://creativecommons.org/licenses/by/2.0/):

            By President of Taiwan:
            https://www.flickr.com/photos/presidentialoffice/54644924491

            Media licensed under CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/):

            By Kremlin.ru:
            http://kremlin.ru/events/president/news/58879
            http://kremlin.ru/events/president/news/70750
            http://kremlin.ru/events/president/news/71528
            http://kremlin.ru/events/president/news/73648
            http://kremlin.ru/events/president/news/73995
            http://kremlin.ru/events/president/news/76446
            """,
            new PeriodBuilder { Minutes = 22, Seconds = 36 }.Build(),
            [
                new TextTrackCue(Pattern.Parse("0:00").Value, Pattern.Parse("2:13").Value, "The Greatest Irony in Geopolitics Today"),
                new TextTrackCue(Pattern.Parse("2:13").Value, Pattern.Parse("5:00").Value, "NATO's Historical Balance of Power"),
                new TextTrackCue(Pattern.Parse("5:00").Value, Pattern.Parse("6:00").Value, "U.S. Attitude Toward NATO Post-Cold War"),
                new TextTrackCue(Pattern.Parse("6:00").Value, Pattern.Parse("9:51").Value, "The True Story of Article 5's Only Invocation"),
                new TextTrackCue(Pattern.Parse("9:51").Value, Pattern.Parse("12:53").Value, "What the U.S. Public Thinks about NATO"),
                new TextTrackCue(Pattern.Parse("12:53").Value, Pattern.Parse("19:55").Value, "Why the United States Needs NATO Now"),
                new TextTrackCue(Pattern.Parse("19:55").Value, Pattern.Parse("22:36").Value, "Europe's Bargaining Power"),
            ])
        {
            TestName = "With chapters in the middle"
        };

        yield return new TestCaseData<string?, Period, TextTrackCue[]?>(
            """
            0:00 intro
            2:13 outro

            ⸻

            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam eleifend justo augue, venenatis scelerisque lectus vehicula sit amet. Nam at tellus ut ipsum mollis mattis. Nullam in volutpat purus. Donec vulputate sem nulla. Nullam vel nisi vitae dolor euismod ultrices. Fusce scelerisque enim a sapien sodales ullamcorper. Nunc posuere blandit pretium. Aliquam erat volutpat. Quisque nec ipsum lacus. Integer arcu orci, dictum sit amet orci eu, lobortis varius massa. Vivamus porttitor nunc et mauris hendrerit dictum sed ac massa. Duis non tristique erat. Vivamus sed rutrum enim. Quisque id auctor sapien. Suspendisse non est et felis consequat malesuada vitae eu mauris. Sed volutpat facilisis est, vel consectetur dolor commodo in.

            Sed ornare, ex eu venenatis cursus, dui magna dictum libero, varius mattis sem augue in dolor. Nullam elementum varius dui, in commodo diam varius at. Phasellus mi risus, fermentum et vulputate in, pulvinar a nisl. Nulla malesuada cursus sapien non efficitur. Cras nisi urna, viverra id mauris sit amet, varius posuere est. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Donec ornare urna tristique nibh vulputate, ac bibendum nisl pretium. In molestie nisi risus, sit amet accumsan dui posuere id.

            Quisque id feugiat eros. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Vestibulum tempor nec metus at tincidunt. Suspendisse condimentum ex at dui suscipit, sit amet vulputate tortor laoreet. Nullam sit amet pretium ante, in euismod sapien. Mauris ultricies libero mauris, at fermentum ligula viverra in. Nulla semper augue a sapien semper, ut porta purus congue. Etiam sit amet viverra velit.

            Ut feugiat imperdiet ipsum, id aliquet nisi dictum imperdiet. Aliquam vehicula eget tortor semper eleifend. Nulla ultricies gravida pellentesque. Vivamus elit tortor, sagittis in interdum at, rhoncus quis sapien. Nunc lacinia cursus lacus, eget mollis lacus varius at. In eget felis nisi. Donec et cursus diam, in auctor nulla. Nunc eget ligula neque. Morbi et tempor elit. Mauris ut felis a felis auctor congue. Vestibulum orci sem, dictum nec varius ut, ornare maximus odio. Nunc ut libero euismod, varius magna vel, aliquam velit. Etiam laoreet erat dolor, ullamcorper porta leo vulputate bibendum. Curabitur in blandit metus.

            Integer efficitur odio quis ex facilisis, cursus elementum turpis vestibulum. Duis luctus, erat quis finibus malesuada, lacus nisl pellentesque eros, placerat cursus ligula ex lobortis sem. Mauris non tincidunt turpis. Aliquam erat volutpat. Nulla leo nisi, malesuada cursus vulputate eu, auctor eu arcu. Nunc tincidunt nulla at mauris fermentum volutpat. Aliquam erat volutpat. Mauris sit amet risus a nulla commodo lacinia. Quisque vehicula viverra mauris at malesuada. Aenean eu pellentesque metus. Donec auctor erat ac lorem facilisis, vitae placerat justo ultrices. Duis lobortis, libero eget dictum suscipit, ex lacus consectetur massa, vel suscipit ex purus sit amet turpis. Fusce ut tortor sit amet eros volutpat imperdiet. Nam vestibulum ultricies blandit. 
            """,
            new PeriodBuilder { Minutes = 22, Seconds = 36 }.Build(),
            [
                new TextTrackCue(Pattern.Parse("0:00").Value, Pattern.Parse("2:13").Value, "intro"),
                new TextTrackCue(Pattern.Parse("2:13").Value, Pattern.Parse("22:36").Value, "outro"),
            ])
        {
            TestName = "With chapters at the start"
        };

        yield return new TestCaseData<string?, Period, TextTrackCue[]?>(
            """
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam eleifend justo augue, venenatis scelerisque lectus vehicula sit amet. Nam at tellus ut ipsum mollis mattis. Nullam in volutpat purus. Donec vulputate sem nulla. Nullam vel nisi vitae dolor euismod ultrices. Fusce scelerisque enim a sapien sodales ullamcorper. Nunc posuere blandit pretium. Aliquam erat volutpat. Quisque nec ipsum lacus. Integer arcu orci, dictum sit amet orci eu, lobortis varius massa. Vivamus porttitor nunc et mauris hendrerit dictum sed ac massa. Duis non tristique erat. Vivamus sed rutrum enim. Quisque id auctor sapien. Suspendisse non est et felis consequat malesuada vitae eu mauris. Sed volutpat facilisis est, vel consectetur dolor commodo in.

            Sed ornare, ex eu venenatis cursus, dui magna dictum libero, varius mattis sem augue in dolor. Nullam elementum varius dui, in commodo diam varius at. Phasellus mi risus, fermentum et vulputate in, pulvinar a nisl. Nulla malesuada cursus sapien non efficitur. Cras nisi urna, viverra id mauris sit amet, varius posuere est. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Donec ornare urna tristique nibh vulputate, ac bibendum nisl pretium. In molestie nisi risus, sit amet accumsan dui posuere id.

            Quisque id feugiat eros. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Vestibulum tempor nec metus at tincidunt. Suspendisse condimentum ex at dui suscipit, sit amet vulputate tortor laoreet. Nullam sit amet pretium ante, in euismod sapien. Mauris ultricies libero mauris, at fermentum ligula viverra in. Nulla semper augue a sapien semper, ut porta purus congue. Etiam sit amet viverra velit.

            Ut feugiat imperdiet ipsum, id aliquet nisi dictum imperdiet. Aliquam vehicula eget tortor semper eleifend. Nulla ultricies gravida pellentesque. Vivamus elit tortor, sagittis in interdum at, rhoncus quis sapien. Nunc lacinia cursus lacus, eget mollis lacus varius at. In eget felis nisi. Donec et cursus diam, in auctor nulla. Nunc eget ligula neque. Morbi et tempor elit. Mauris ut felis a felis auctor congue. Vestibulum orci sem, dictum nec varius ut, ornare maximus odio. Nunc ut libero euismod, varius magna vel, aliquam velit. Etiam laoreet erat dolor, ullamcorper porta leo vulputate bibendum. Curabitur in blandit metus.

            Integer efficitur odio quis ex facilisis, cursus elementum turpis vestibulum. Duis luctus, erat quis finibus malesuada, lacus nisl pellentesque eros, placerat cursus ligula ex lobortis sem. Mauris non tincidunt turpis. Aliquam erat volutpat. Nulla leo nisi, malesuada cursus vulputate eu, auctor eu arcu. Nunc tincidunt nulla at mauris fermentum volutpat. Aliquam erat volutpat. Mauris sit amet risus a nulla commodo lacinia. Quisque vehicula viverra mauris at malesuada. Aenean eu pellentesque metus. Donec auctor erat ac lorem facilisis, vitae placerat justo ultrices. Duis lobortis, libero eget dictum suscipit, ex lacus consectetur massa, vel suscipit ex purus sit amet turpis. Fusce ut tortor sit amet eros volutpat imperdiet. Nam vestibulum ultricies blandit. 

            ⸻

            Timestamps:
            0:00 intro
            1:26 ipsum dolor
            6:33 sit amet, consectetur
            10:36 adipiscing elit
            14:34 Nullam eleifend
            18:44 justo augue, venenatis
            22:39 scelerisque
            27:20 lectus vehicula
            30:19 sit amet
            34:44 Nam at tellus
            40:38 ut ipsum
            46:47 mollis mattis
            54:21 Nullam in volutpat purus
            58:00 Sed ornare, ex eu venenatis cursus, dui magna dictum libero, varius mattis sem augue in dolor
            61:15 Quisque id feugiat eros
            66:03 Pellentesque habitant
            70:52 morbi tristique
            73:52 outro – don’t forget to like and subscribe 💗
            """,
            new PeriodBuilder { Hours = 1, Minutes = 15, Seconds = 54 }.Build(),
            [
                new TextTrackCue(Pattern.Parse("0:00").Value, Pattern.Parse("1:26").Value, "intro"),
                new TextTrackCue(Pattern.Parse("1:26").Value, Pattern.Parse("6:33").Value, "ipsum dolor"),
                new TextTrackCue(Pattern.Parse("6:33").Value, Pattern.Parse("10:36").Value, "sit amet, consectetur"),
                new TextTrackCue(Pattern.Parse("10:36").Value, Pattern.Parse("14:34").Value, "adipiscing elit"),
                new TextTrackCue(Pattern.Parse("14:34").Value, Pattern.Parse("18:44").Value, "Nullam eleifend"),
                new TextTrackCue(Pattern.Parse("18:44").Value, Pattern.Parse("22:39").Value, "justo augue, venenatis"),
                new TextTrackCue(Pattern.Parse("22:39").Value, Pattern.Parse("27:20").Value, "scelerisque"),
                new TextTrackCue(Pattern.Parse("27:20").Value, Pattern.Parse("30:19").Value, "lectus vehicula"),
                new TextTrackCue(Pattern.Parse("30:19").Value, Pattern.Parse("34:44").Value, "sit amet"),
                new TextTrackCue(Pattern.Parse("34:44").Value, Pattern.Parse("40:38").Value, "Nam at tellus"),
                new TextTrackCue(Pattern.Parse("40:38").Value, Pattern.Parse("46:47").Value, "ut ipsum"),
                new TextTrackCue(Pattern.Parse("46:47").Value, Pattern.Parse("54:21").Value, "mollis mattis"),
                new TextTrackCue(Pattern.Parse("54:21").Value, Pattern.Parse("58:00").Value, "Nullam in volutpat purus"),
                new TextTrackCue(Pattern.Parse("58:00").Value, Pattern.Parse("61:15").Value, "Sed ornare, ex eu venenatis cursus, dui magna dictum libero, varius mattis sem augue in dolor"),
                new TextTrackCue(Pattern.Parse("61:15").Value, Pattern.Parse("66:03").Value, "Quisque id feugiat eros"),
                new TextTrackCue(Pattern.Parse("66:03").Value, Pattern.Parse("70:52").Value, "Pellentesque habitant"),
                new TextTrackCue(Pattern.Parse("70:52").Value, Pattern.Parse("73:52").Value, "morbi tristique"),
                new TextTrackCue(Pattern.Parse("73:52").Value, Pattern.Parse("75:54").Value, "outro – don’t forget to like and subscribe 💗"),
            ])
        {
            TestName = "With chapters at the end, longer than an hour"
        };

        yield return new TestCaseData<string?, Period, TextTrackCue[]?>(
            """
            0:00 intro

            ⸻

            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam eleifend justo augue, venenatis scelerisque lectus vehicula sit amet. Nam at tellus ut ipsum mollis mattis. Nullam in volutpat purus. Donec vulputate sem nulla. Nullam vel nisi vitae dolor euismod ultrices. Fusce scelerisque enim a sapien sodales ullamcorper. Nunc posuere blandit pretium. Aliquam erat volutpat. Quisque nec ipsum lacus. Integer arcu orci, dictum sit amet orci eu, lobortis varius massa. Vivamus porttitor nunc et mauris hendrerit dictum sed ac massa. Duis non tristique erat. Vivamus sed rutrum enim. Quisque id auctor sapien. Suspendisse non est et felis consequat malesuada vitae eu mauris. Sed volutpat facilisis est, vel consectetur dolor commodo in.

            Sed ornare, ex eu venenatis cursus, dui magna dictum libero, varius mattis sem augue in dolor. Nullam elementum varius dui, in commodo diam varius at. Phasellus mi risus, fermentum et vulputate in, pulvinar a nisl. Nulla malesuada cursus sapien non efficitur. Cras nisi urna, viverra id mauris sit amet, varius posuere est. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Donec ornare urna tristique nibh vulputate, ac bibendum nisl pretium. In molestie nisi risus, sit amet accumsan dui posuere id.

            Quisque id feugiat eros. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Vestibulum tempor nec metus at tincidunt. Suspendisse condimentum ex at dui suscipit, sit amet vulputate tortor laoreet. Nullam sit amet pretium ante, in euismod sapien. Mauris ultricies libero mauris, at fermentum ligula viverra in. Nulla semper augue a sapien semper, ut porta purus congue. Etiam sit amet viverra velit.

            Ut feugiat imperdiet ipsum, id aliquet nisi dictum imperdiet. Aliquam vehicula eget tortor semper eleifend. Nulla ultricies gravida pellentesque. Vivamus elit tortor, sagittis in interdum at, rhoncus quis sapien. Nunc lacinia cursus lacus, eget mollis lacus varius at. In eget felis nisi. Donec et cursus diam, in auctor nulla. Nunc eget ligula neque. Morbi et tempor elit. Mauris ut felis a felis auctor congue. Vestibulum orci sem, dictum nec varius ut, ornare maximus odio. Nunc ut libero euismod, varius magna vel, aliquam velit. Etiam laoreet erat dolor, ullamcorper porta leo vulputate bibendum. Curabitur in blandit metus.

            Integer efficitur odio quis ex facilisis, cursus elementum turpis vestibulum. Duis luctus, erat quis finibus malesuada, lacus nisl pellentesque eros, placerat cursus ligula ex lobortis sem. Mauris non tincidunt turpis. Aliquam erat volutpat. Nulla leo nisi, malesuada cursus vulputate eu, auctor eu arcu. Nunc tincidunt nulla at mauris fermentum volutpat. Aliquam erat volutpat. Mauris sit amet risus a nulla commodo lacinia. Quisque vehicula viverra mauris at malesuada. Aenean eu pellentesque metus. Donec auctor erat ac lorem facilisis, vitae placerat justo ultrices. Duis lobortis, libero eget dictum suscipit, ex lacus consectetur massa, vel suscipit ex purus sit amet turpis. Fusce ut tortor sit amet eros volutpat imperdiet. Nam vestibulum ultricies blandit. 
            """,
            new PeriodBuilder { Minutes = 22, Seconds = 36 }.Build(),
            null)
        {
            TestName = "Must have multiple chapters"
        };
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}