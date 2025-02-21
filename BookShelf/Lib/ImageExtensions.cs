using BookShelf.Lib;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace BookShelf.Lib;

static internal class ImageExtensions
{
    public static Image Resize(this Image image, int width, int height, ResizeMode mode = ResizeMode.Max)
    {
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = mode
        }));
        return image;
    }
}