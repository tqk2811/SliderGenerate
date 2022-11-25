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
    public class CheckerBoard : Slide
    {
        public CheckerBoard(List<FileInfo> images) : base(images)
        {
        }

        public int CellSize { get; set; } = 32;

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
                    $"if(" +
                        $"(" +
                            $"lte(mod(X,{CellSize}),{CellSize}/2-({CellSize}/2)*T/{TransitionDuration.TotalSeconds})" +
                            $"+lte(mod(Y,{CellSize}),{CellSize}/2-({CellSize}/2)*T/{TransitionDuration.TotalSeconds})" +
                        $")+" +
                        $"(" +
                            $"gte(mod(X,{CellSize}),({CellSize}/2)+({CellSize}/2)*T/{TransitionDuration.TotalSeconds})" +
                            $"+gte(mod(Y,{CellSize}),({CellSize}/2)+({CellSize}/2)*T/{TransitionDuration.TotalSeconds})" +
                        $")" +
                        $",B" +
                        $",A)"));

            return overlaids.ConcatOverlaidsAndBlendeds(blendeds);
        }
    }
}
