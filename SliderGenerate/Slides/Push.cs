using FFmpegArgs;
using FFmpegArgs.Cores.Maps;
using FFmpegArgs.Filters;
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
    public class Push : Slide
    {
        public Push(List<FileInfo> images) : base(images)
        {
        }
        public SlideDirection Direction { get; set; }
        public VerticalDirection VerticalDirection { get; set; }
        public HorizontalDirection HorizontalDirection { get; set; }

        public override TimeSpan TotalDuration
            => TimeSpan.FromTicks((ImageDuration.Ticks + TransitionDuration.Ticks) * Images.Count() - TransitionDuration.Ticks);

        internal override ImageMap MadeSlide(FFmpegArg ffmpegArg, IEnumerable<ImageMap> images)
        {
            ImageFilterGraphInput background_fi = new ImageFilterGraphInput();
            background_fi.FilterGraph.ColorFilter().Color(BackgroundColor).Size(Size).MapOut.FpsFilter().Fps(Fps);
            ImageMap background = ffmpegArg.AddImagesInput(background_fi).First();

            ImageFilterGraphInput transparent_fi = new ImageFilterGraphInput();
            transparent_fi.FilterGraph.NullsrcFilter().Size(Size).MapOut.FpsFilter().Fps(Fps);
            ImageMap transparent = ffmpegArg.AddImagesInput(transparent_fi).First();

            List<IEnumerable<ImageMap>> prepareInputs = this.InputScreenModes(images);

            List<ImageMap> overlaids = new List<ImageMap>();
            List<ImageMap> startings = new List<ImageMap>();
            List<ImageMap> endings = new List<ImageMap>();
            for (int i = 0; i < images.Count(); i++)
            {
                overlaids.Add(prepareInputs[i].First()
                    .OverlayFilterOn(background)
                        .X("(main_w-overlay_w)/2")
                        .Y("(main_h-overlay_h)/2")
                        .Format(OverlayPixFmt.rgb).MapOut
                    .TrimFilter().Duration(ImageDuration).MapOut
                    .SelectFilter($"lte(n,{ImageFrameCount})").MapOut);


                var temp = prepareInputs[i].Last()
                    .OverlayFilterOn(background)
                        .X($"(main_w-overlay_w)/2")
                        .Y($"(main_h-overlay_h)/2")
                        .Format(OverlayPixFmt.rgb).MapOut
                    .TrimFilter().Duration(TransitionDuration).MapOut
                    .SelectFilter($"lte(n,{TransitionFrameCount})").MapOut;
                if (i == 0)
                {
                    endings.Add(temp);
                }
                else if (i == images.Count() - 1)
                {
                    startings.Add(temp);
                }
                else
                {
                    var split = temp.SplitFilter(2).MapsOut;
                    startings.Add(split.First());
                    endings.Add(split.Last());
                }
            }

            List<ImageMap> blendeds = new List<ImageMap>();
            for (int i = 0; i < endings.Count; i++)
            {
                switch (Direction)
                {
                    case SlideDirection.Vertical:
                        switch (VerticalDirection)
                        {
                            case VerticalDirection.TopToBottom:
                                {
                                    var moving = endings[i]
                                        .OverlayFilterOn(transparent)
                                            .X("0")
                                            .Y($"t/{TransitionDuration.TotalSeconds}*{Size.Height}").MapOut
                                        .TrimFilter()
                                            .Duration(TransitionDuration).MapOut
                                        .SelectFilter($"lte(n,{TransitionFrameCount})").MapOut;

                                    blendeds.Add(startings[i]
                                        .OverlayFilterOn(moving)
                                            .X("0")
                                            .Y($"-h+t/{TransitionDuration.TotalSeconds}*{Size.Height}")
                                            .Shortest(true).MapOut
                                        .TrimFilter()
                                            .Duration(TransitionDuration).MapOut
                                        .SelectFilter($"lte(n,{TransitionFrameCount})").MapOut);
                                }
                                break;

                            case VerticalDirection.BottomToTop:
                                {
                                    var moving = endings[i]
                                        .OverlayFilterOn(transparent)
                                            .X("0")
                                            .Y($"-t/{TransitionDuration.TotalSeconds}*{Size.Height}").MapOut
                                        .TrimFilter()
                                            .Duration(TransitionDuration).MapOut
                                        .SelectFilter($"lte(n,{TransitionFrameCount})").MapOut;

                                    blendeds.Add(startings[i]
                                        .OverlayFilterOn(moving)
                                            .X("0")
                                            .Y($"h-t/{TransitionDuration.TotalSeconds}*{Size.Height}")
                                            .Shortest(true).MapOut
                                        .TrimFilter()
                                            .Duration(TransitionDuration).MapOut
                                        .SelectFilter($"lte(n,{TransitionFrameCount})").MapOut);
                                }
                                break;
                        }
                        break;

                    case SlideDirection.Horizontal:
                        switch (HorizontalDirection)
                        {
                            case HorizontalDirection.LeftToRight:
                                {
                                    var moving = endings[i]
                                        .OverlayFilterOn(transparent)
                                            .X($"t/{TransitionDuration.TotalSeconds}*{Size.Width}")
                                            .Y($"0").MapOut
                                        .TrimFilter()
                                            .Duration(TransitionDuration).MapOut
                                        .SelectFilter($"lte(n,{TransitionFrameCount})").MapOut;

                                    blendeds.Add(startings[i]
                                        .OverlayFilterOn(moving)
                                            .X($"-w+t/{TransitionDuration.TotalSeconds}*{Size.Width}")
                                            .Y($"0")
                                            .Shortest(true).MapOut
                                        .TrimFilter()
                                            .Duration(TransitionDuration).MapOut
                                        .SelectFilter($"lte(n,{TransitionFrameCount})").MapOut);
                                }
                                break;

                            case HorizontalDirection.RightToLeft:
                                {
                                    var moving = endings[i]
                                        .OverlayFilterOn(transparent)
                                            .X($"-t/{TransitionDuration.TotalSeconds}*{Size.Width}")
                                            .Y("0").MapOut
                                        .TrimFilter()
                                            .Duration(TransitionDuration).MapOut
                                        .SelectFilter($"lte(n,{TransitionFrameCount})").MapOut;

                                    blendeds.Add(startings[i]
                                        .OverlayFilterOn(moving)
                                            .Y("0")
                                            .X($"w-t/{TransitionDuration.TotalSeconds}*{Size.Width}")
                                            .Shortest(true).MapOut
                                        .TrimFilter()
                                            .Duration(TransitionDuration).MapOut
                                        .SelectFilter($"lte(n,{TransitionFrameCount})").MapOut);
                                }
                                break;
                        }
                        break;
                }
            }

            return overlaids.ConcatOverlaidsAndBlendeds(blendeds);
        }
    }
}
