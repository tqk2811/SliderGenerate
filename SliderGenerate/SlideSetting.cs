using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SliderGenerate
{
    public class SlideSetting
    {
        public int MaxImageAngle { get; set; } = 25;
        public int TransionDurationMiliSecond { get; set; } = 2000;
        public int ImageDurationMiliSecond { get; set; } = 2000;
        public int CellSize { get; set; } = 64;

        public HorizontalDirection HorizontalDirection { get; set; }
        public VerticalDirection VerticalDirection { get; set; }
        public ScreenMode ScreenMode { get; set; } = ScreenMode.Blur;
        public CollapseExpandMode CollapseExpandMode { get; set; }


        public SlideSetting Random()
        {
            var r = new Random(DateTime.Now.Millisecond);
            HorizontalDirection = (HorizontalDirection)r.Next(0, 2);
            VerticalDirection = (VerticalDirection)r.Next(0, 2);
            CollapseExpandMode = (CollapseExpandMode)r.Next(0, 4);
            return this;
        }

        public static SlideSetting NewRandom()
        {
            return new SlideSetting().Random();
        }
    }
}
