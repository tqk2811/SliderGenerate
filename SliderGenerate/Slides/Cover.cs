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
    public class Cover : Slide
    {
        public Cover(List<FileInfo> images) : base(images)
        {
        }

        public SlideDirection Direction { get; set; }
        public VerticalDirection VerticalDirection { get; set; }
        public HorizontalDirection HorizontalDirection { get; set; }

        public override TimeSpan TotalDuration 
            => TimeSpan.FromTicks((ImageDuration.Ticks + TransitionDuration.Ticks) * Images.Count() - TransitionDuration.Ticks);

        internal override ImageMap MadeSlide(FFmpegArg ffmpegArg, IEnumerable<ImageMap> images)
        {
            List<IEnumerable<ImageMap>> prepareInputs = this.InputScreenModes(images);

            var overlaids = this.Overlaids(prepareInputs.Select(x => x.First()));

            var startEnd = this.StartEnd(prepareInputs.Select(x => x.Last()).ToList());

            string expr = string.Empty;

            double TRANSITION_DURATION = TransitionDuration.TotalSeconds;

            switch (Direction)
            {
                case SlideDirection.Vertical:
                    switch (VerticalDirection)
                    {
                        case VerticalDirection.TopToBottom:
                            expr = $"if(gte(Y,H*T/{TRANSITION_DURATION}),B,A)";
                            break;

                        case VerticalDirection.BottomToTop:
                            expr = $"if(gte(Y,H - H*T/{TRANSITION_DURATION}),A,B)";
                            break;
                    }
                    break;

                case SlideDirection.Horizontal:
                    switch (HorizontalDirection)
                    {
                        case HorizontalDirection.LeftToRight:
                            expr = $"if(gte(X,W*T/{TRANSITION_DURATION}),B,A)";
                            break;

                        case HorizontalDirection.RightToLeft:
                            expr = $"if(gte(X,W-W*T/{TRANSITION_DURATION}),A,B)";
                            break;
                    }
                    break;
            }

            var blendeds = startEnd.Blendeds(TransitionFrameCount, blend =>
            {
                blend.Shortest(true);
                blend.All_Expr(expr);
            });

            return overlaids.ConcatOverlaidsAndBlendeds(blendeds);
        }
    }
}
