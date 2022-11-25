// See https://aka.ms/new-console-template for more information
using FFmpegArgs.Cores.Maps;
using FFmpegArgs.Inputs;
using FFmpegArgs.Outputs;
using FFmpegArgs;
using FFMpegCore;
using System.Runtime.CompilerServices;
using SliderGenerate;
using System.Drawing;
using FFmpegArgs.Executes;

Directory.CreateDirectory("Outputs");
IEnumerable<string> ImageExtensionSupport = new string[] { "png", "jpg", "jpeg" };
List<FileInfo> fileInfos = Directory
                    .GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Images"))
                    .Select(x => new FileInfo(x))
                    .Where(x => ImageExtensionSupport.Contains(x.Extension.ToLower().TrimStart('.')))
                    .OrderBy(x => Guid.NewGuid())
                    .ToList();
int takeCount = 5;

SlideSetting slideSetting = SlideSetting.NewRandom();
SlideType slideType = SlideType.BarsOneHorizontal;
Slide slide = Slide.GetSlide(slideSetting, slideType, fileInfos.Take(takeCount).ToList());

FFmpegArg fFmpegArg = new FFmpegArg();
ImageMap imageMap = slide.GetLayerResult(fFmpegArg);

string file_out = Path.Combine(Directory.GetCurrentDirectory(), "Outputs", $"{slideType}.mp4");

var output = new ImageFileOutput(file_out, imageMap);
output.ImageOutputAVStream.Fps(30);
fFmpegArg.AddOutput(output);

FFmpegRender render = fFmpegArg.Render();
FFmpegRenderResult renderResult = render.Execute();