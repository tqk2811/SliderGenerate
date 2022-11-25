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
    public class FadeInOne : Slide
    {
        public FadeInOne(List<FileInfo> images) : base(images)
        {
        }

        public override TimeSpan TotalDuration 
            => TimeSpan.FromTicks((ImageDuration.Ticks + TransitionDuration.Ticks) * Images.Count() - TransitionDuration.Ticks);

        internal override ImageMap MadeSlide(FFmpegArg ffmpegArg, IEnumerable<ImageMap> images)
        {
            List<IEnumerable<ImageMap>> prepareInputs = this.InputScreenModes(images);

            var overlaids = this.Overlaids(prepareInputs.Select(x => x.First()));

            var startEnd = this.StartEnd(prepareInputs.Select(x => x.Last()).ToList());

            var blendeds = startEnd.Blendeds(TransitionFrameCount, blend => blend
                .Shortest(true)
                .All_Expr(
                    $"A*(if( gte(T,{TransitionDuration.TotalSeconds}),{TransitionDuration.TotalSeconds},T/{TransitionDuration.TotalSeconds})) + " +
                    $"B*(1-(if(gte(T,{TransitionDuration.TotalSeconds}),{TransitionDuration.TotalSeconds},T/{TransitionDuration.TotalSeconds})))"));

            return overlaids.ConcatOverlaidsAndBlendeds(blendeds);
        }
    }
}
