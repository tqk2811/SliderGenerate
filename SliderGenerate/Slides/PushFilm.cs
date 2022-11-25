using FFmpegArgs;
using FFmpegArgs.Cores.Maps;
using FFmpegArgs.Filters;
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
    public class PushFilm : Slide
    {
        public PushFilm(List<FileInfo> images) : base(images)
        {
        }
        public SlideDirection Direction { get; set; }
        public VerticalDirection VerticalDirection { get; set; }
        public HorizontalDirection HorizontalDirection { get; set; }

        public override TimeSpan TotalDuration => TimeSpan.FromTicks(TransitionDuration.Ticks * Images.Count());

        internal override ImageMap MadeSlide(FFmpegArg ffmpegArg, IEnumerable<ImageMap> images)
        {
            var background = ffmpegArg.FilterGraph
                .ColorFilter()
                    .Color(BackgroundColor)
                    .Size(Size)
                    .Duration(TotalDuration).MapOut
                .FpsFilter().Fps(Fps).MapOut;


            ImageMap film_strip_map = Direction switch
            {
                SlideDirection.Horizontal => LoadResource(ffmpegArg,FilmStripH),
                SlideDirection.Vertical => LoadResource(ffmpegArg, FilmStripV),
                _ => throw new NotImplementedException()
            };


            List<ImageMap> images_Prepare = this.Prepare(images.ToList(), film_strip_map);

            var strip_images = film_strip_map
                .SetPtsFilter("PTS-STARTPTS").MapOut
                .ScaleFilter()
                    .W($"if(gte(iw/ih,{Size.Width}/{Size.Height}),min(iw,{Size.Width}),-1)")
                    .H($"if(gte(iw/ih,{Size.Width}/{Size.Height}),-1,min(ih,{Size.Height}))").MapOut
                .ScaleFilter().W("trunc(iw/2)*2").H("trunc(ih/2)*2").MapOut
                .SetSarFilter().Ratio(1).MapOut
                .SplitFilter(images.Count()).MapsOut.ToList();

            // OVERLAY FILM STRIP ON TOP OF INPUTS

            List<ImageMap> image_overlay_on_strips = new List<ImageMap>();
            for (int i = 0; i < images_Prepare.Count; i++)
            {
                image_overlay_on_strips.Add(strip_images[i]
                    .OverlayFilterOn(images_Prepare[i]).X("(main_w-overlay_w)/2").Y("(main_h-overlay_h)/2").Format(OverlayPixFmt.rgb).MapOut);
            }

            var lastOverLay = background;
            for (int i = 0; i < images_Prepare.Count; i++)
            {
                var start = TimeSpan.FromTicks(i * TransitionDuration.Ticks);
                var end = TimeSpan.FromTicks(start.Ticks + TransitionDuration.Ticks * 2);
                switch(Direction)
                {
                    case SlideDirection.Horizontal:
                        switch (HorizontalDirection)
                        {
                            case HorizontalDirection.LeftToRight:
                                {
                                    lastOverLay = image_overlay_on_strips[i].SetPtsFilter("PTS-STARTPTS").MapOut
                                        .OverlayFilterOn(lastOverLay)
                                            .X($"-{Size.Width}+(t-{start.TotalSeconds})/{TransitionDuration.TotalSeconds}*{Size.Width}")
                                            .Y("0")//from -WIDTH to +WIDTH
                                            .Enable($"between(t,{start.TotalSeconds},{end.TotalSeconds})").MapOut;
                                    break;
                                }

                            case HorizontalDirection.RightToLeft:
                                {
                                    lastOverLay = image_overlay_on_strips[i].SetPtsFilter("PTS-STARTPTS").MapOut
                                        .OverlayFilterOn(lastOverLay)
                                            .X($"{Size.Width}-(t-{start.TotalSeconds})/{TransitionDuration.TotalSeconds}*{Size.Width}")
                                            .Y("0")//from +WIDTH to -WIDTH
                                            .Enable($"between(t,{start.TotalSeconds},{end.TotalSeconds})").MapOut;
                                    break;
                                }
                        }
                        break;


                    case SlideDirection.Vertical:
                        switch (VerticalDirection)
                        {
                            case VerticalDirection.TopToBottom:
                                {
                                    lastOverLay = image_overlay_on_strips[i].SetPtsFilter("PTS-STARTPTS").MapOut
                                        .OverlayFilterOn(lastOverLay)
                                            .X("0")
                                            .Y($"-{Size.Height}+(t-{start.TotalSeconds})/{TransitionDuration.TotalSeconds}*{Size.Height}")//from -HEIGHT to +HEIGHT
                                            .Enable($"between(t,{start.TotalSeconds},{end.TotalSeconds})").MapOut;
                                    break;
                                }

                            case VerticalDirection.BottomToTop:
                                {
                                    lastOverLay = image_overlay_on_strips[i].SetPtsFilter("PTS-STARTPTS").MapOut
                                        .OverlayFilterOn(lastOverLay)
                                            .X("0")
                                            .Y($"{Size.Height}-(t-{start.TotalSeconds})/{TransitionDuration.TotalSeconds}*{Size.Height}")//from +HEIGHT to -HEIGHT
                                            .Enable($"between(t,{start.TotalSeconds},{end.TotalSeconds})").MapOut;
                                    break;
                                }
                        }
                        break;
                }
                
            }

            return lastOverLay
                .FpsFilter().Fps(Fps).MapOut
                .FormatFilter(PixFmt.yuv420p).MapOut;
        }



        List<ImageMap> Prepare(List<ImageMap> imageMaps, ImageMap film_strip_map)
        {
            List<ImageMap> images_Prepare = new List<ImageMap>();
            for (int i = 0; i < imageMaps.Count; i++)
            {
                switch (ScreenMode)
                {
                    case ScreenMode.Center:
                        {
                            images_Prepare.Add(imageMaps[i]
                                .ScaleFilter()
                                    .W($"if(gte(iw/ih,{Size.Width}/{Size.Height}),min(iw,{Size.Width}),-1)")
                                    .H($"if(gte(iw/ih,{Size.Width}/{Size.Height}),-1,min(ih,{Size.Height}))").MapOut
                                .ScaleFilter().W("trunc(iw/2)*2").H("trunc(ih/2)*2").MapOut
                                .PadFilter()
                                    .W($"{Size.Width}")
                                    .H($"{Size.Height}")
                                    .X($"({Size.Width}-iw)/2")
                                    .Y($"({Size.Height}-ih)/2")
                                    .Color(BackgroundColor).MapOut
                                .SetSarFilter().Ratio(1).MapOut);
                            break;
                        }

                    case ScreenMode.Crop:
                        {
                            images_Prepare.Add(imageMaps[i]
                                .ScaleFilter()
                                    .W($"if(gte(iw/ih,{Size.Width}/{Size.Height}),-1,{Size.Width})")
                                    .H($"if(gte(iw/ih,{Size.Width}/{Size.Height}),{Size.Height},-1)").MapOut
                                .OverlayFilterOn(film_strip_map).X("0").Y("0").MapOut
                                .CropFilter().W($"{Size.Width}").H($"{Size.Height}").MapOut
                                .SetSarFilter().Ratio(1).MapOut);
                            break;
                        }

                    case ScreenMode.Scale:
                        {
                            images_Prepare.Add(imageMaps[i]
                                .ScaleFilter().W($"{Size.Width}").H($"{Size.Height}").MapOut
                                .SetSarFilter().Ratio(1).MapOut);
                            break;
                        }

                    case ScreenMode.Blur:
                        {
                            images_Prepare.Add(imageMaps[i]
                                .MakeBlurredBackground(Size.Width, Size.Height, Fps, "100")
                                .FormatFilter(PixFmt.rgba).MapOut
                                .SetSarFilter().Ratio(1).MapOut);
                            break;
                        }
                }
            }
            return images_Prepare;
        }

    }
}
