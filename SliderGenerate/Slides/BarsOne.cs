using FFmpegArgs;
using FFmpegArgs.Cores.Maps;
using FFmpegArgs.Filters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SliderGenerate.Slides
{
    public class BarsOne : Slide
    {
        public BarsOne(List<FileInfo> images) : base(images)
        {

        }

        public int BarCount { get; set; } = 16;
        public SlideDirection Direction { get; set; } = SlideDirection.Vertical;

        public override TimeSpan TotalDuration 
            => TimeSpan.FromTicks((ImageDuration.Ticks + TransitionDuration.Ticks) * Images.Count() - TransitionDuration.Ticks);

        internal override ImageMap MadeSlide(FFmpegArg ffmpegArg, IEnumerable<ImageMap> images)
        {
            List<IEnumerable<ImageMap>> prepareInputs = this.InputScreenModes(images);

            var overlaids = this.Overlaids(prepareInputs.Select(x => x.First()));

            var startEnd = this.StartEnd(prepareInputs.Select(x => x.Last()).ToList());

            string expr = Direction switch
            {
                SlideDirection.Vertical => $"if((lte(mod(X,({Size.Width}/{BarCount})),({Size.Width}/{BarCount})*T/{TransitionDuration.TotalSeconds})),A,B)",
                SlideDirection.Horizontal => $"if((lte(mod(Y,({Size.Height}/{BarCount})),({Size.Height}/{BarCount})*T/{TransitionDuration.TotalSeconds})),A,B)",
                _ => throw new NotImplementedException()
            };

            var blendeds = startEnd.Blendeds(TransitionFrameCount, blend => blend
                .Shortest(true)
                .All_Expr(expr));

            return overlaids.ConcatOverlaidsAndBlendeds(blendeds);
        }
    }
}
