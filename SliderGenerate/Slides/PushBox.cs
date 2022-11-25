using FFmpegArgs;
using FFmpegArgs.Cores.Maps;
using FFmpegArgs.Cores.Enums;
using FFmpegArgs.Filters.MultimediaFilters;
using FFmpegArgs.Filters.VideoFilters;
using FFmpegArgs.Filters.VideoSources;
using FFmpegArgs.Inputs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SliderGenerate.Slides
{
    public class PushBox : Slide
    {
        public PushBox(List<FileInfo> images) : base(images)
        {
        }

        public SlideDirection Direction { get; set; }
        public VerticalDirection VerticalDirection { get; set; }
        public HorizontalDirection HorizontalDirection { get; set; }

        public override TimeSpan TotalDuration
            => TimeSpan.FromTicks((ImageDuration.Ticks + 2 * TransitionDuration.Ticks) * Images.Count() + TransitionDuration.Ticks * 2 * Images.Count() / 5);

        internal override ImageMap MadeSlide(FFmpegArg ffmpegArg, IEnumerable<ImageMap> images)
        {
            if (TransitionDuration > TimeSpan.FromSeconds(1)) TransitionDuration = TimeSpan.FromSeconds(1);

            var TRANSITION_PHASE_DURATION = TimeSpan.FromMilliseconds(TransitionDuration.TotalMilliseconds / 2);
            var CHECKPOINT_DURATION = TimeSpan.FromMilliseconds(TransitionDuration.Milliseconds / 5);

            ImageFilterGraphInput background_fi = new ImageFilterGraphInput();
            background_fi.FilterGraph.ColorFilter().Color(BackgroundColor).Size(Size).MapOut.FpsFilter().Fps(Fps);
            ImageMap background = ffmpegArg.AddImagesInput(background_fi).First();

            List<IEnumerable<ImageMap>> prepareInputs = this.InputScreenModes(images);

            List<ImageMap> overlaids = prepareInputs.Select(x => x.First()
                .TrimFilter().Duration(ImageDuration).MapOut
                .SelectFilter($"lte(n,{ImageFrameCount})").MapOut).ToList();

            List<List<ImageMap>> pres = prepareInputs.Select(x => x.Last()
                .ScaleFilter()
                    .W($"{Size.Width}/2")
                    .H("-1").MapOut
                .PadFilter()
                    .W($"{Size.Width}")
                    .H($"{Size.Height}")
                    .X("(ow-iw)/2")
                    .Y("(oh-ih)/2")
                    .Color(BackgroundColor).MapOut
                .TrimFilter().Duration(TransitionDuration).MapOut
                .SelectFilter($"lte(n,{TransitionFrameCount})").MapOut
                .SplitFilter(5).MapsOut.ToList()).ToList();//prephasein, checkpoint, prezoomin, prezoomout, prephaseout

            var phaseouts = Direction switch
            {
                SlideDirection.Vertical => VerticalDirection switch
                {
                    VerticalDirection.TopToBottom => pres.Select(x => x[4]//prephaseout
                                    .OverlayFilterOn(background)
                                        .X("0")
                                        .Y($"t/({TransitionDuration.TotalSeconds}/2)*{Size.Height}").MapOut
                                    .TrimFilter().Duration(TRANSITION_PHASE_DURATION).MapOut
                                    .SelectFilter($"lte(n,{TransitionFrameCount}/2)").MapOut).ToList(),

                    VerticalDirection.BottomToTop => pres.Select(x => x[4]//prephaseout
                                    .OverlayFilterOn(background)
                                        .X("0")
                                        .Y($"-t/({TransitionDuration.TotalSeconds}/2)*{Size.Height}").MapOut
                                    .TrimFilter().Duration(TRANSITION_PHASE_DURATION).MapOut
                                    .SelectFilter($"lte(n,{TransitionFrameCount}/2)").MapOut).ToList(),
                    _ => throw new NotImplementedException()
                },
                SlideDirection.Horizontal => HorizontalDirection switch
                {
                    HorizontalDirection.LeftToRight => pres.Select(x => x[4]//prephaseout
                                .OverlayFilterOn(background)
                                    .Y("0")
                                    .X($"t/({TransitionDuration.TotalSeconds}/2)*{Size.Width}").MapOut
                                .TrimFilter().Duration(TRANSITION_PHASE_DURATION).MapOut
                                .SelectFilter($"lte(n,{TransitionFrameCount}/2)").MapOut).ToList(),

                    HorizontalDirection.RightToLeft => pres.Select(x => x[4]//prephaseout
                                    .OverlayFilterOn(background)
                                        .Y("0")
                                        .X($"-t/({TransitionDuration.TotalSeconds}/2)*{Size.Width}").MapOut
                                    .TrimFilter().Duration(TRANSITION_PHASE_DURATION).MapOut
                                    .SelectFilter($"lte(n,{TransitionFrameCount}/2)").MapOut).ToList(),
                    _ => throw new NotImplementedException()
                },
                _ => throw new NotImplementedException()
            };




            var phaseins = Direction switch
            {
                SlideDirection.Vertical => VerticalDirection switch
                {
                    VerticalDirection.TopToBottom => pres.Select(x => x[0]//prephasein
                                    .OverlayFilterOn(pres.IndexOf(x) == 0 ? background : phaseouts[pres.IndexOf(x) - 1])//phaseouts.Last()
                                        .X("0")
                                        .Y($"-h+{Size.Height}*t/({TransitionDuration.TotalSeconds}/2)").MapOut
                                    .TrimFilter().Duration(TRANSITION_PHASE_DURATION).MapOut
                                    .SelectFilter($"lte(n,{TransitionFrameCount}/2)").MapOut).ToList(),

                    VerticalDirection.BottomToTop => pres.Select(x => x[0]//prephasein
                                    .OverlayFilterOn(pres.IndexOf(x) == 0 ? background : phaseouts[pres.IndexOf(x) - 1])//phaseouts.Last()
                                        .X("0")
                                        .Y($"h-{Size.Height}*t/({TransitionDuration.TotalSeconds}/2)").MapOut
                                    .TrimFilter().Duration(TRANSITION_PHASE_DURATION).MapOut
                                    .SelectFilter($"lte(n,{TransitionFrameCount}/2)").MapOut).ToList(),
                    _ => throw new NotImplementedException()
                },

                SlideDirection.Horizontal => HorizontalDirection switch
                {
                    HorizontalDirection.LeftToRight => pres.Select(x => x[0]//prephasein
                                .OverlayFilterOn(pres.IndexOf(x) == 0 ? background : phaseouts[pres.IndexOf(x) - 1])//phaseouts.Last()
                                    .Y("0")
                                    .X($"-{Size.Width}+{Size.Width}*t/({TransitionDuration.TotalSeconds}/2)").MapOut
                                .TrimFilter().Duration(TRANSITION_PHASE_DURATION).MapOut
                                .SelectFilter($"lte(n,{TransitionFrameCount}/2)").MapOut).ToList(),

                    HorizontalDirection.RightToLeft => pres.Select(x => x[0]//prephasein
                                    .OverlayFilterOn(pres.IndexOf(x) == 0 ? background : phaseouts[pres.IndexOf(x) - 1])//phaseouts.Last()
                                        .Y("0")
                                        .X($"{Size.Width}-{Size.Width}*t/({TransitionDuration.TotalSeconds}/2)").MapOut
                                    .TrimFilter().Duration(TRANSITION_PHASE_DURATION).MapOut
                                    .SelectFilter($"lte(n,{TransitionFrameCount}/2)").MapOut).ToList(),
                    _ => throw new NotImplementedException()
                },
                _ => throw new NotImplementedException()
            };

            List<List<ImageMap>> checkin_checkout = pres.Select(x => x[1]//checkpoint
                .TrimFilter().Duration(CHECKPOINT_DURATION).MapOut
                .SplitFilter(2).MapsOut.ToList()).ToList();

            var checkins = checkin_checkout.Select(x => x.First()).ToList();
            var checkouts = checkin_checkout.Select(x => x.Last()).ToList();

            var zoomins = pres.Select(x => x[2]
                .ScaleFilter()
                    .W("iw*5")
                    .H("ih*5").MapOut
                .ZoompanFilter()
                    .Zoom("min(pzoom+0.04,2)")
                    .D(TransitionDuration)
                    .Fps(Fps)
                    .X("iw/2-(iw/zoom/2)")
                    .Y("ih/2-(ih/zoom/2)")
                    .S(Size).MapOut
                .SetPtsFilter("0.5*PTS").MapOut).ToList();

            var zoomouts = pres.Select(x => x[3]
                .ScaleFilter()
                    .W("iw*5")
                    .H("ih*5").MapOut
                .ZoompanFilter()
                    .Zoom("2-in*0.04")
                    .D(TransitionDuration)
                    .Fps(Fps)
                    .X("iw/2-(iw/zoom/2)")
                    .Y("ih/2-(ih/zoom/2)")
                    .S(Size).MapOut
                .SetPtsFilter("0.5*PTS").MapOut).ToList();

            List<ConcatGroup> concatGroups = new List<ConcatGroup>();
            for (int i = 0; i < prepareInputs.Count; i++)
            {
                concatGroups.Add(new ConcatGroup(phaseins[i]));//TRANSITION_PHASE_DURATION = TransitionDuration/2
                concatGroups.Add(new ConcatGroup(checkins[i]));//CHECKPOINT_DURATION = TransitionDuration/5
                concatGroups.Add(new ConcatGroup(zoomins[i]));//TransitionDuration
                concatGroups.Add(new ConcatGroup(overlaids[i]));//ImageDuration
                concatGroups.Add(new ConcatGroup(zoomouts[i]));//TransitionDuration
                concatGroups.Add(new ConcatGroup(checkouts[i]));//CHECKPOINT_DURATION
            }
            concatGroups.Add(new ConcatGroup(phaseouts.Last()));//TRANSITION_PHASE_DURATION
            //total = (TRANSITION_PHASE_DURATION + 2*CHECKPOINT_DURATION + 2*TransitionDuration + ImageDuration) * ImageCount + TRANSITION_PHASE_DURATION
            //=(TransitionDuration/2 + 2 * TransitionDuration/5 + 2*TransitionDuration + ImageDuration) * ImageCount + TransitionDuration/2
            //=(5*TransitionDuration + 4*TransitionDuration + 2*10*TransitionDuration + 10*ImageDuration)*ImageCount / 10 + TransitionDuration/2
            //=(29*TransitionDuration + 10*ImageDuration)*ImageCount / 10 + TransitionDuration/2
            //=ImageDuration * ImageCount + (29*TransitionDuration*ImageCount + 5*TransitionDuration)/10
            ConcatFilter concatFilter = new ConcatFilter(concatGroups);
            return concatFilter.ImageMapsOut.First().FormatFilter(PixFmt.yuv420p).MapOut;
        }
    }
}
