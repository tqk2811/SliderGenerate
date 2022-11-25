using FFmpegArgs;
using FFmpegArgs.Cores;
using FFmpegArgs.Cores.Maps;
using FFmpegArgs.Filters;
using FFmpegArgs.Filters.VideoFilters;
using FFmpegArgs.Inputs;
using SliderGenerate.Slides;
using System.Drawing;

namespace SliderGenerate
{
    public abstract class Slide
    {
        internal static FileInfo FilmStripV;
        internal static FileInfo FilmStripH;

        public static void LoadResource(string RootDir)
        {
            FilmStripV = new FileInfo(Path.Combine(RootDir, "SlideResources\\film_strip_vertical.png"));
            FilmStripH = new FileInfo(Path.Combine(RootDir, "SlideResources\\film_strip.png"));
        }

        protected readonly List<FileInfo> _FilesUsed = new List<FileInfo>();
        public IEnumerable<FileInfo> FilesUsed { get { return _FilesUsed; } }

        internal Slide(List<FileInfo> images)
        {
            if (images == null || images.Count == 0) throw new InvalidDataException(nameof(images));
            if (images.Count == 1) images.Add(images.First());
            this._FilesUsed.AddRange(images);
        }
        public IEnumerable<FileInfo> Images { get { return _FilesUsed; } }

        public string WorkingDir { get; set; } = Directory.GetCurrentDirectory();
        public ScreenMode ScreenMode { get; set; } = ScreenMode.Blur;
        public TimeSpan ImageDuration { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan TransitionDuration { get; set; } = TimeSpan.FromSeconds(1);
        public Color BackgroundColor { get; set; } = Color.FromArgb(0, 0, 0, 0);
        public Size Size { get; set; } = new Size(1280, 720);
        public int Fps { get; set; } = 24;


        internal int TransitionFrameCount { get => (int)(TransitionDuration.TotalSeconds * Fps); }
        internal int ImageFrameCount { get => (int)(ImageDuration.TotalSeconds * Fps); }

        public ImageMap GetLayerResult(FFmpegArg ffmpegArg)
        {

            var imagesMap = Images.Select(x =>
            {
                if (string.IsNullOrWhiteSpace(WorkingDir))
                    return ffmpegArg.AddImagesInput(new ImageFileInput(x.Name).SetOption("-loop", 1)).First();
                else
                {
                    if (x.FullName.StartsWith(WorkingDir))
                        return ffmpegArg.AddImagesInput(new ImageFileInput(x.FullName.Substring(WorkingDir.Trim('\\').Length + 1)).SetOption("-loop", 1)).First();
                    else
                        return ffmpegArg.AddImagesInput(new ImageFileInput(x.FullName).SetOption("-loop", 1)).First();
                }
            }).ToList();

            var slideMap = MadeSlide(ffmpegArg, imagesMap)
                .TrimFilter().Duration(TotalDuration).MapOut;

            return slideMap;
        }

        public abstract TimeSpan TotalDuration { get; }
        internal abstract ImageMap MadeSlide(FFmpegArg ffmpegArg, IEnumerable<ImageMap> images);

        internal ImageMap LoadResource(FFmpegArg ffmpegArg, FileInfo fileInfo)
        {
            _FilesUsed.Add(fileInfo);
            if (string.IsNullOrWhiteSpace(WorkingDir))
                return ffmpegArg.AddImagesInput(new ImageFileInput(fileInfo.Name).SetOption("-loop", 1)).First();
            else
            {
                if (fileInfo.FullName.StartsWith(WorkingDir))
                    return ffmpegArg.AddImagesInput(new ImageFileInput(fileInfo.FullName.Substring(WorkingDir.Trim('\\').Length + 1)).SetOption("-loop", 1)).First();
                else
                    return ffmpegArg.AddImagesInput(new ImageFileInput(fileInfo.FullName).SetOption("-loop", 1)).First();
            }
        }


        public static Slide GetSlide(SlideSetting slideSetting, SlideType slideType, List<FileInfo> fileInfos)
        {
            return slideType switch
            {
                SlideType.BarsOneVertical => new BarsOne(fileInfos) { Direction = SlideDirection.Vertical },
                SlideType.BarsOneHorizontal => new BarsOne(fileInfos) { Direction = SlideDirection.Horizontal },
                SlideType.CheckerBoard => new CheckerBoard(fileInfos) { CellSize = slideSetting.CellSize },
                SlideType.Clock => new Clock(fileInfos) { },
                SlideType.Collapse => new CollapseExpand(fileInfos) { CollapseExpandType = CollapseExpandType.Collapse, CollapseExpandMode = slideSetting.CollapseExpandMode },
                SlideType.Expand => new CollapseExpand(fileInfos) { CollapseExpandType = CollapseExpandType.Expand, CollapseExpandMode = slideSetting.CollapseExpandMode },
                SlideType.CoverVertical => new Cover(fileInfos) { Direction = SlideDirection.Vertical, VerticalDirection = slideSetting.VerticalDirection },
                SlideType.CoverHorizontal => new Cover(fileInfos) { Direction = SlideDirection.Horizontal, HorizontalDirection = slideSetting.HorizontalDirection },
                SlideType.FadeInOne => new FadeInOne(fileInfos) { },
                SlideType.FadeInTwo => new FadeInTwo(fileInfos) { },
                //SlideType.PhotoCollection => new PhotoCollection(fileInfos) { MaxImageAngle = slideSetting.MaxImageAngle },
                SlideType.PushVertical => new Push(fileInfos) { Direction = SlideDirection.Vertical, VerticalDirection = slideSetting.VerticalDirection },
                SlideType.PushHorizontal => new Push(fileInfos) { Direction = SlideDirection.Horizontal, HorizontalDirection = slideSetting.HorizontalDirection },
                SlideType.PushBoxVertical => new PushBox(fileInfos) { Direction = SlideDirection.Vertical, VerticalDirection = slideSetting.VerticalDirection },
                SlideType.PushBoxHorizontal => new PushBox(fileInfos) { Direction = SlideDirection.Horizontal, HorizontalDirection = slideSetting.HorizontalDirection },
                //SlideType.PushFilmVertical => new PushFilm(fileInfos) { Direction = SlideDirection.Vertical, VerticalDirection = slideSetting.VerticalDirection },
                //SlideType.PushFilmHorizontal => new PushFilm(fileInfos) { Direction = SlideDirection.Horizontal, HorizontalDirection = slideSetting.HorizontalDirection },
                _ => throw new NotImplementedException()
            };
        }
    }
}
