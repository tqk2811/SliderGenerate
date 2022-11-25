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
    public class Clock : Slide
    {
        public Clock(List<FileInfo> images) : base(images)
        {
        }

        public override TimeSpan TotalDuration 
            => TimeSpan.FromTicks((ImageDuration.Ticks + TransitionDuration.Ticks) * Images.Count() - TransitionDuration.Ticks);

        internal override ImageMap MadeSlide(FFmpegArg ffmpegArg, IEnumerable<ImageMap> images)
        {
            List<IEnumerable<ImageMap>> prepareInputs = this.InputScreenModes(images);

            var overlaids = this.Overlaids(prepareInputs.Select(x => x.First()));

            var startEnd = this.StartEnd(prepareInputs.Select(x => x.Last()).ToList());

            // (0.5W, 0.5H) -> (0.5W, 0) => vecto v1 = (0.5W-0.5W,0-0.5H)   = (0        ,    -0.5*H)
            // (0.5W, 0.5H) -> (X, Y) => vecto v2                           = (X - 0.5*W ,   Y - 0.5*H);
            // cos(v1,v2) = (a1*a2 + b1*b2)/[sqrt(a1*a1 + b1*b1) * sqrt(a2*a2 + b2*b2)]
            // = (-0.5*H * (Y - 0.5*H))/(sqrt(0.5*H*0.5*H) * sqrt((X - 0.5*W)*(X - 0.5*W) + (Y - 0.5*H)*(Y - 0.5*H)))

            //0 degrees => 1, 90 degrees => 0, 180 degrees => -1:   cos range 1 -> -1, acos 0 -> PI
            //                                                      cos range -1 -> 1, acos PI -> 0
            var cos_result = "((-0.5*H * (Y - 0.5*H))/(0.5*H * sqrt((X - 0.5*W)*(X - 0.5*W) + (Y - 0.5*H)*(Y - 0.5*H))))";
            var expr = $"if(" +
                            $"lt(T,{this.TransitionDuration.TotalSeconds})," +
                            $"if(" +
                                $"lte(" +
                                    $"if(" +
                                        $"gte(X,W/2)," +
                                        $"acos({cos_result})," +// 0 -> PI
                                        $"2*PI-acos({cos_result}))," +// PI -> 0 => 2PI -  (PI -> 0) = PI -> 2PI
                                        $"T*2*PI/{this.TransitionDuration.TotalSeconds})," +//0 -> 2 PI
                                $"A," +
                                $"B)," +
                            $"B)";

            var blendeds = startEnd.Blendeds(TransitionFrameCount, blend => blend
                .Shortest(true)
                .All_Expr(expr));

            return overlaids.ConcatOverlaidsAndBlendeds(blendeds);
        }
    }
}
