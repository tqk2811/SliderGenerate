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
    public class PhotoCollection : Slide
    {
        public PhotoCollection(List<FileInfo> images) : base(images)
        {
        }

        public int MaxImageAngle { get; set; } = 25;

        public override TimeSpan TotalDuration 
            => TimeSpan.FromTicks((ImageDuration.Ticks + TransitionDuration.Ticks) * Images.Count() - TransitionDuration.Ticks);

        internal override ImageMap MadeSlide(FFmpegArg ffmpegArg, IEnumerable<ImageMap> images)
        {
            ImageFilterGraphInput backgroundInput = new ImageFilterGraphInput();
            backgroundInput.FilterGraph.ColorFilter().Color(BackgroundColor).Size(Size).MapOut.FpsFilter().Fps(Fps);
            ImageMap background = ffmpegArg.AddImagesInput(backgroundInput).First();

            double TRANSITION_DURATION = TransitionDuration.TotalSeconds;

            Random random = new Random(DateTime.Now.Millisecond);

            var lastOverLay = background;
            var _images = this.InputScreenMode(images);
            for (int c = 0; c < _images.Count; c++)
            {
                var ANGLE_RANDOMNESS = random.Next() % MaxImageAngle + 1;

                var start = TimeSpan.FromTicks((TransitionDuration.Ticks + ImageDuration.Ticks) * c);
                var end = start + TransitionDuration;

                lastOverLay = _images[c]
                    .PadFilter()
                        .W($"{Size.Width * 4}")
                        .H($"{Size.Height}")
                        .X($"({Size.Width * 4}-iw)/2")
                        .Y($"({Size.Height}-ih)/2")
                        .Color(BackgroundColor).MapOut
                    .TrimFilter().Duration(TimeSpan.FromTicks((c + 1) * (TransitionDuration.Ticks + ImageDuration.Ticks))).MapOut
                    .SetPtsFilter("PTS-STARTPTS").MapOut


                    .RotateFilter().Angle($"if(between(t,{start.TotalSeconds},{end.TotalSeconds})," +
                                        $"2*PI*t+if(eq(mod({c},2),0),1,-1)*{ANGLE_RANDOMNESS}*PI/180," +
                                        $"if(eq(mod({c},2),0),1,-1)*{ANGLE_RANDOMNESS}*PI/180)")
                        .OW($"{Size.Width * 4}").FillColor(BackgroundColor).MapOut
                    .OverlayFilterOn(lastOverLay)
                        .X($"if(gt(t,{start.TotalSeconds})," +
                            $"if(lt(t,{end.TotalSeconds})," +
                                $"{Size.Width}*3/2 -w+(t-{start.TotalSeconds})/{TRANSITION_DURATION}*{Size.Width}," +
                                "(main_w-overlay_w)/2)," +
                            "-w)")
                        .Y("(main_h-overlay_h)/2").MapOut;
            }

            return lastOverLay.FormatFilter(PixFmt.yuv420p).MapOut;
        }
    }
}
