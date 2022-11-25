using FFmpegArgs;
using FFmpegArgs.Cores.Maps;
using FFmpegArgs.Filters.MultimediaFilters;
using FFmpegArgs.Filters.VideoFilters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SliderGenerate.Slides
{
    public class FadeInTwo : Slide
    {
        public FadeInTwo(List<FileInfo> images) : base(images)
        {
        }

        public override TimeSpan TotalDuration 
            => TimeSpan.FromTicks((ImageDuration.Ticks + TransitionDuration.Ticks) * Images.Count() - TransitionDuration.Ticks);

        internal override ImageMap MadeSlide(FFmpegArg ffmpegArg, IEnumerable<ImageMap> images)
        {
            var inputs = this.InputScreenModes(images);

            var overlaids = this.Overlaids(inputs.Select(x => x.First()));

            List<ImageMap> fadeIns = new List<ImageMap>();
            List<ImageMap> fadeOuts = new List<ImageMap>();
            var fades = inputs.Select(x => x.Last()).ToList();
            foreach (var input in fades)
            {
                var temp = input
                    .PadFilter()
                        .W($"{Size.Width}")
                        .H($"{Size.Height}")
                        .X($"({Size.Width}-iw)/2")
                        .Y($"({Size.Height}-ih)/2")
                        .Color(BackgroundColor).MapOut
                    .TrimFilter().Duration(TransitionDuration).MapOut
                    .SelectFilter($"lte(n,{TransitionFrameCount})").MapOut;

                if (input.Equals(fades.First()))
                {
                    fadeOuts.Add(temp.FadeFilter().Type(FadeType.Out).StartFrame(0).NbFrames(TransitionFrameCount).MapOut);
                }
                else if (input.Equals(fades.Last()))
                {
                    fadeIns.Add(temp.FadeFilter().Type(FadeType.In).StartFrame(0).NbFrames(TransitionFrameCount).MapOut);
                }
                else
                {
                    var split = temp.SplitFilter(2).MapsOut;
                    fadeOuts.Add(split.First().FadeFilter().Type(FadeType.Out).StartFrame(0).NbFrames(TransitionFrameCount).MapOut);
                    fadeIns.Add(split.Last().FadeFilter().Type(FadeType.In).StartFrame(0).NbFrames(TransitionFrameCount).MapOut);
                }
            }

            List<ImageMap> blendeds = new List<ImageMap>();
            for (int i = 0; i < fadeIns.Count; i++)
            {
                blendeds.Add(fadeOuts[i].OverlayFilterOn(fadeIns[i])
                        .X($"(main_w-overlay_w)/2")
                        .Y($"(main_h-overlay_h)/2").MapOut
                    .TrimFilter().Duration(TransitionDuration).MapOut
                    .SelectFilter($"lte(n,{TransitionFrameCount})").MapOut);
            }

            return overlaids.ConcatOverlaidsAndBlendeds(blendeds);
        }
    }
}
