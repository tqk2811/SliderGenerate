using FFmpegArgs;
using FFmpegArgs.Cores.Maps;
using FFmpegArgs.Filters;
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
    public enum CollapseExpandType
    {
        Collapse,
        Expand
    }
    public class CollapseExpand : Slide
    {
        public CollapseExpand(List<FileInfo> images) : base(images)
        {
        }

        public CollapseExpandMode CollapseExpandMode { get; set; } = CollapseExpandMode.Circular;
        public CollapseExpandType CollapseExpandType { get; set; } = CollapseExpandType.Collapse;

        public override TimeSpan TotalDuration 
            => TimeSpan.FromTicks((ImageDuration.Ticks + TransitionDuration.Ticks) * Images.Count() - TransitionDuration.Ticks);

        internal override ImageMap MadeSlide(FFmpegArg ffmpegArg, IEnumerable<ImageMap> images)
        {
            List<IEnumerable<ImageMap>> prepareInputs = this.InputScreenModes(images);

            var overlaids = this.Overlaids(prepareInputs.Select(x => x.First()));

            var startEnd = this.StartEnd(prepareInputs.Select(x => x.Last()).ToList());

            string expr = string.Empty;

            double TRANSITION_DURATION = TransitionDuration.TotalSeconds;

            switch(CollapseExpandType)
            {
                case CollapseExpandType.Collapse:
                    switch (CollapseExpandMode)
                    {
                        case CollapseExpandMode.Vertical:
                            expr = $"if(gte(Y,(H/2)*T/{TRANSITION_DURATION})*lte(Y,H-(H/2)*T/{TRANSITION_DURATION}),B,A)";
                            break;

                        case CollapseExpandMode.Horizontal:
                            expr = $"if(gte(X,(W/2)*T/{TRANSITION_DURATION})*lte(X,W-(W/2)*T/{TRANSITION_DURATION}),B,A)";
                            break;

                        case CollapseExpandMode.Circular:
                            StartEnd _startEnd = new StartEnd();

                            _startEnd.Startings = startEnd.Startings.Select(x => x
                               .GeqFilter()
                                   .Lum("p(X,Y)")
                                   .A($"if(lte(pow(sqrt(pow(W/2,2)+pow(H/2,2))-sqrt(pow(T/{TRANSITION_DURATION}*W/2,2)+pow(T/{TRANSITION_DURATION}*H/2,2)),2),pow(X-(W/2),2)+pow(Y-(H/2),2)),255,0)").MapOut).ToList();
                            _startEnd.Endings = startEnd.Endings;

                            startEnd = _startEnd;
                            break;

                        case CollapseExpandMode.Both:
                            expr = $"if((gte(X,(W/2)*T/{TRANSITION_DURATION})*gte(Y,(H/2)*T/{TRANSITION_DURATION}))*(lte(X,W-(W/2)*T/{TRANSITION_DURATION})*lte(Y,H-(H/2)*T/{TRANSITION_DURATION})),B,A)";
                            break;
                    }
                    break;

                case CollapseExpandType.Expand:
                    switch (CollapseExpandMode)
                    {
                        case CollapseExpandMode.Vertical:
                            expr = $"if(lte(Y,(H/2)-(H/2)*T/{TRANSITION_DURATION})+gte(Y,(H/2)+(H/2)*T/{TRANSITION_DURATION}),B,A)";
                            break;

                        case CollapseExpandMode.Horizontal:
                            expr = $"if(lte(X,(W/2)-(W/2)*T/{TRANSITION_DURATION})+gte(X,(W/2)+(W/2)*T/{TRANSITION_DURATION}),B,A)";
                            break;

                        case CollapseExpandMode.Circular:
                            StartEnd _startEnd = new StartEnd();

                            _startEnd.Startings = startEnd.Startings.Select(x => x
                               .GeqFilter()
                                   .Lum("p(X,Y)")
                                   .A($"if(lte(pow(sqrt(pow(T/{TRANSITION_DURATION}*W/2,2)+pow(T/{TRANSITION_DURATION}*H/2,2)),2),pow(X-(W/2),2)+pow(Y-(H/2),2)),0,255)").MapOut).ToList();
                            _startEnd.Endings = startEnd.Endings;

                            startEnd = _startEnd;
                            break;

                        case CollapseExpandMode.Both:
                            expr = $"if((lte(X,(W/2)-(W/2)*T/{TRANSITION_DURATION})+lte(Y,(H/2)-(H/2)*T/{TRANSITION_DURATION}))+(gte(X,(W/2)+(W/2)*T/{TRANSITION_DURATION})+gte(Y,(H/2)+(H/2)*T/{TRANSITION_DURATION})),B,A)";
                            break;
                    }
                    break;
            }

            ImageMap out_map = null;
            switch (CollapseExpandMode)
            {
                case CollapseExpandMode.Circular:
                    {
                        var blendeds = new List<ImageMap>();
                        for (int i = 0; i < startEnd.Startings.Count; i++)
                        {
                            blendeds.Add(startEnd.Startings[i].OverlayFilterOn(startEnd.Endings[i])
                                .X("0").Y("0").Shortest(true).MapOut);
                        }
                        out_map = overlaids.ConcatOverlaidsAndBlendeds(blendeds);
                    }
                    break;

                default:
                    {
                        var blendeds = startEnd.Blendeds(TransitionFrameCount, blend =>
                        {
                            blend.Shortest(true);
                            blend.All_Expr(expr);
                        });
                        out_map = overlaids.ConcatOverlaidsAndBlendeds(blendeds);
                    }
                    break;
            }
            return out_map;
        }
    }
}
