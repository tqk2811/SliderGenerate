using FFmpegArgs.Cores.Maps;
using FFmpegArgs.Filters.MultimediaFilters;
using FFmpegArgs.Filters.VideoFilters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using FFmpegArgs.Cores.Enums;

namespace SliderGenerate
{
    internal static class SlideExtensions
    {
        public static ImageMap MakeBlurredBackground(this ImageMap image,
          int width, int height, int fps = 24, string lumaRadius = "100")
        {
            List<ImageMap> inputs = new List<ImageMap>();
            if (image.IsInput)
            {
                inputs.Add(image);
                inputs.Add(image);
            }
            else
            {
                inputs.AddRange(image.SplitFilter(2).MapsOut);
            }

            var blurred = inputs.First()
                .ScaleFilter().W($"{width}").H($"{height}").MapOut
                .SetSarFilter().Ratio(1).MapOut
                .FpsFilter().Fps(fps).MapOut
                .FormatFilter(PixFmt.rgba).MapOut
                .BoxBlurFilter().LumaRadius($"{lumaRadius}").MapOut
                .SetSarFilter().Ratio(1).MapOut;

            var raw = inputs.Last()
                .ScaleFilter()
                    //.W($"if(gte(iw/ih,{width}/{height}),min(iw,{width}),-1)")
                    //.H($"if(gte(iw/ih,{width}/{height}),-1,min(ih,{height}))").MapOut
                    .W($"iw/max(iw/{width},ih/{height})")
                    .H($"ih/max(iw/{width},ih/{height})").MapOut
                .ScaleFilter()
                    .W("trunc(iw/2)*2")
                    .H("trunc(ih/2)*2").MapOut
                .SetSarFilter().Ratio(1).MapOut
                .FpsFilter().Fps(fps).MapOut
                .FormatFilter(PixFmt.rgba).MapOut;

            return raw
                .OverlayFilterOn(blurred)
                    .X("(main_w-overlay_w)/2")
                    .Y("(main_h-overlay_h)/2").MapOut//center
                .SetPtsFilter("PTS-STARTPTS").MapOut;
        }

        public static List<IEnumerable<ImageMap>> InputScreenModes(this Slide Slide, IEnumerable<ImageMap> inputs, string lumaRadius = "100")
           => Slide.InputScreenMode(inputs,lumaRadius).Select(x => x.SplitFilter(2).MapsOut).ToList();

        public static List<ImageMap> InputScreenMode(this Slide Slide, IEnumerable<ImageMap> inputs, string lumaRadius = "100")
        {
            List<ImageMap> prepareInputs = new List<ImageMap>();
            prepareInputs.AddRange(inputs.Select(x => x.SetPtsFilter("PTS-STARTPTS").MapOut).Select(x =>
            {
                switch (Slide.ScreenMode)
                {
                    case ScreenMode.Center:
                        return x
                            .ScaleFilter()
                                .W($"if(gte(iw/ih,{Slide.Size.Width}/{Slide.Size.Height}),min(iw,{Slide.Size.Width}),-1)")
                                .H($"if(gte(iw/ih,{Slide.Size.Width}/{Slide.Size.Height}),-1,min(ih,{Slide.Size.Height}))").MapOut
                            .ScaleFilter()
                                .W("trunc(iw/2)*2")
                                .H("trunc(ih/2)*2").MapOut
                            .SetSarFilter().Ratio(1).MapOut
                            .FpsFilter().Fps(Slide.Fps).MapOut
                            .FormatFilter(PixFmt.rgba).MapOut;

                    case ScreenMode.Crop:
                        return x
                            .ScaleFilter()
                                .W($"if(gte(iw/ih,{Slide.Size.Width}/{Slide.Size.Height}),-1,{Slide.Size.Width})")
                                .H($"if(gte(iw/ih,{Slide.Size.Width}/{Slide.Size.Height}),{Slide.Size.Height},-1)").MapOut
                             .CropFilter()
                                .W($"{Slide.Size.Width}")
                                .H($"{Slide.Size.Height}").MapOut
                             .SetSarFilter().Ratio("1/1").MapOut
                             .FpsFilter().Fps(Slide.Fps).MapOut
                             .FormatFilter(PixFmt.rgba).MapOut;

                    case ScreenMode.Scale:
                        return x
                            .ScaleFilter()
                                .W($"{Slide.Size.Width}")
                                .H($"{Slide.Size.Height}").MapOut
                            .SetSarFilter().Ratio("1/1").MapOut
                            .FpsFilter().Fps(Slide.Fps).MapOut
                            .FormatFilter(PixFmt.rgba).MapOut;

                    case ScreenMode.Blur:
                        return x.MakeBlurredBackground(Slide.Size.Width, Slide.Size.Height, Slide.Fps, lumaRadius);
                }
                return null;
            }));
            return prepareInputs;
        }

        public static List<ImageMap> Overlaids(this Slide Slide, IEnumerable<ImageMap> inputs)
        {
            return inputs.Select(x => x
                .PadFilter()
                    .W(Slide.Size.Width.ToString())
                    .H(Slide.Size.Height.ToString())
                    .X($"({Slide.Size.Width}-iw)/2")
                    .Y($"({Slide.Size.Height}-ih)/2")
                    .Color(Slide.BackgroundColor).MapOut
                .TrimFilter()
                    .Duration(Slide.ImageDuration).MapOut).ToList();
        }

        public static StartEnd StartEnd(this Slide Slide, List<ImageMap> inputs)
        {
            StartEnd startEnd = new StartEnd();
            for (int i = 0; i < inputs.Count; i++)
            {
                //first create ed only (if only 1 image -> create ed)
                //mid: split to ed and op
                //last create op

                var res = inputs[i]
                  .PadFilter()
                    .W(Slide.Size.Width.ToString())
                    .H(Slide.Size.Height.ToString())
                    .X($"({Slide.Size.Width}-iw)/2")
                    .Y($"({Slide.Size.Height}-ih)/2")
                    .Color(Slide.BackgroundColor).MapOut
                  .TrimFilter()
                    .Duration(Slide.TransitionDuration).MapOut
                  .SelectFilter($"lte(n,{(int)(Slide.TransitionDuration.TotalSeconds * Slide.Fps)})").MapOut;

                if (i == 0)//first
                {
                    if (inputs.Count > 1)
                    {
                        startEnd.Endings.Add(res);
                    }
                }
                else if (i == inputs.Count - 1)//last
                {
                    startEnd.Startings.Add(res);
                }
                else//mid
                {
                    var splits = res.SplitFilter(2).MapsOut;
                    startEnd.Startings.Add(splits.First());
                    startEnd.Endings.Add(splits.Last());
                }
            }
            return startEnd;
        }

        public static List<ImageMap> Blendeds(this StartEnd startEnd, int TransitionFrameCount, Action<BlendFilter> blend)
        {
            List<ImageMap> blendeds = new List<ImageMap>();
            for (int i = 0; i < startEnd.Startings.Count; i++)
            {
                var blendFilter = startEnd.Startings[i].BlendFilterOn(startEnd.Endings[i]);
                blend.Invoke(blendFilter);
                blendeds.Add(blendFilter.MapOut
                    .SelectFilter($"lte(n,{TransitionFrameCount})").MapOut
                    );
            }
            return blendeds;
        }

        public static ImageMap ConcatOverlaidsAndBlendeds(this List<ImageMap> overlaids, List<ImageMap> blendeds)
        {
            List<ConcatGroup> concatGroups = new List<ConcatGroup>();
            for (int i = 0; i < overlaids.Count; i++)
            {
                concatGroups.Add(new ConcatGroup(overlaids[i]));
                if (i < overlaids.Count - 1) concatGroups.Add(new ConcatGroup(blendeds[i]));
            }
            ConcatFilter concatFilter = new ConcatFilter(concatGroups);
            return concatFilter.ImageMapsOut.First().FormatFilter(PixFmt.yuv420p).MapOut;
        }
    }
}
