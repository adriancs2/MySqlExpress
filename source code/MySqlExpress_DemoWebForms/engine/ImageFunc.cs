using System;
//using System.Collections.Generic;
//
//using System.Web;
//using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace System
{
    public class ImageFunc
    {
        public static Image ResizeHeight(Image fullSizeImage, int height)
        {
            int width = height * fullSizeImage.Width / fullSizeImage.Height;
            return ResizeImageExact(fullSizeImage, width, height);
        }

        public static Image ResizeWidth(Image fullSizeImage, int width)
        {
            int height = width * fullSizeImage.Height / fullSizeImage.Width;
            return ResizeImageExact(fullSizeImage, width, height);
        }

        public static Image ResizeCropFitToSize(Image fullSizeImage, int width, int height)
        {
            Image img = ResizeImageMinSize(fullSizeImage, width, height);
            Image img2 = CropImageCenter(img, width, height);
            img.Dispose();
            return img2;
        }

        public static Image ResizeImageMaxSize(Image FullsizeImage, int MaxWidth, int MaxHeight, bool OnlyResizeIfWider)
        {
            // Prevent using images internal thumbnail
            FullsizeImage.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipNone);
            FullsizeImage.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipNone);

            int NewWidth = MaxWidth;

            if (OnlyResizeIfWider)
            {
                if (FullsizeImage.Width <= NewWidth)
                {
                    NewWidth = FullsizeImage.Width;
                }
            }

            int NewHeight = FullsizeImage.Height * NewWidth / FullsizeImage.Width;
            if (NewHeight > MaxHeight)
            {
                // Resize with height instead
                NewWidth = FullsizeImage.Width * MaxHeight / FullsizeImage.Height;
                NewHeight = MaxHeight;
            }

            System.Drawing.Image NewImage = (Image)(new Bitmap(FullsizeImage, NewWidth, NewHeight));

            return NewImage;
        }

        public static Image ResizeImageMinSize(Image FullsizeImage, int MinimumWidth, int MinimumHeight)
        {
            // Prevent using images internal thumbnail
            FullsizeImage.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipNone);
            FullsizeImage.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipNone);

            int NewWidth = 0;
            int NewHeight = 0;

            NewHeight = MinimumWidth * FullsizeImage.Height / FullsizeImage.Width;
            NewWidth = MinimumWidth;
            if (NewHeight < MinimumHeight)
            {
                NewHeight = MinimumHeight;
                NewWidth = MinimumHeight * FullsizeImage.Width / FullsizeImage.Height;
            }

            System.Drawing.Image NewImage = (Image)(new Bitmap(FullsizeImage, NewWidth, NewHeight));

            return NewImage;
        }

        public static Image ResizeImageExact(Image image, int Width, int Height)
        {
            //The flips are in here to prevent any embedded image thumbnails -- usually from cameras
            //from displaying as the thumbnail image later, in other words, we want a clean
            //resize, not a grainy one.
            image.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipX);
            image.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipX);

            //return the resized image
            return image.GetThumbnailImage(Width, Height, null, IntPtr.Zero);
        }

        public static Image ResizeImagePercentage(Image image, int percentage)
        {
            int newWidth = image.Width * percentage / 100;
            int newHeight = image.Height * percentage / 100;
            return ResizeImageExact(image, newWidth, newHeight);
        }

        public static Image ResizeImagePercentage(Image image, int percentWidth, int percentHeight)
        {
            int newWidth = image.Width * percentWidth / 100;
            int newHeight = image.Height * percentHeight / 100;
            return ResizeImageExact(image, newWidth, newHeight);
        }

        public static Image CropImageCenter(Image image, int Width, int Height)
        {
            int StartAtX = (image.Width - Width) / 2;
            int StartAtY = (image.Height - Height) / 2;

            return CropImage(image, StartAtX, StartAtY, Width, Height);
        }

        public static Image CropImage(Image image, int StartAtX, int StartAtY, int Width, int Height)
        {
            Image outimage;
            MemoryStream mm = null;
            try
            {
                //check the image height against our desired image height
                if (image.Height < Height)
                {
                    Height = image.Height;
                }

                if (image.Width < Width)
                {
                    Width = image.Width;
                }

                //create a bitmap window for cropping
                Bitmap bmPhoto = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
                bmPhoto.SetResolution(72, 72);

                //create a new graphics object from our image and set properties
                Graphics grPhoto = Graphics.FromImage(bmPhoto);
                grPhoto.SmoothingMode = SmoothingMode.AntiAlias;
                grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;
                grPhoto.PixelOffsetMode = PixelOffsetMode.HighQuality;

                //now do the crop
                grPhoto.DrawImage(image, new Rectangle(0, 0, Width, Height), StartAtX, StartAtY, Width, Height, GraphicsUnit.Pixel);

                // Save out to memory and get an image from it to send back out the method.
                mm = new MemoryStream();
                bmPhoto.Save(mm, System.Drawing.Imaging.ImageFormat.Jpeg);
                image.Dispose();
                bmPhoto.Dispose();
                grPhoto.Dispose();
                outimage = Image.FromStream(mm);

                return outimage;
            }
            catch (Exception ex)
            {
                throw new Exception("Error cropping image, the error was: " + ex.Message);
            }
        }

        public static byte[] ImageToByteArray(Image image)
        {
            MemoryStream ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
            return ms.ToArray();
        }

        public static Image ByteArrayToImage(byte[] byteArrayIn)
        {
            MemoryStream ms = new MemoryStream(byteArrayIn);
            Image returnImage = Image.FromStream(ms);
            return returnImage;
        }
    }
}