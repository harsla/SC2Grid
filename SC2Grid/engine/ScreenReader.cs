using System;
using System.Windows;
using System.Drawing;

namespace SC2Grid
{
    public class ScreenReader
    {
        string ocr_path = @"../../engine/tessdata/";

        //constructor
        private OCR.TesseractWrapper.TesseractProcessor Ocr;
        public ScreenReader() 
        {
            Ocr = new OCR.TesseractWrapper.TesseractProcessor();
            //Ocr.Init(@"tessdata\", "sc2", 3);
            Ocr.Init(ocr_path, "sc2", 3);
            Ocr.SetPageSegMode(OCR.TesseractWrapper.ePageSegMode.PSM_SINGLE_WORD);

        }
        
        [Serializable, System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct w32POINT
        {
            public int x;
            public int y;

            public w32POINT(int nx, int ny)
            {
                x = nx;
                y = ny;
            }
        }

        [Serializable, System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct w32RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public w32RECT(int left_, int top_, int right_, int bottom_)
            {
                Left = left_;
                Top = top_;
                Right = right_;
                Bottom = bottom_;
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out w32RECT lpRect);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hWnd, out w32POINT lpPoint);

        //FOR-DEBUG: Count images
        //private int imageCount = 0;
        
        // set the Segmode if provided
        public void SetPSM(OCR.TesseractWrapper.ePageSegMode PageSegMode)
        {
            /*
           OCR.TesseractWrapper.ePageSegMode auto = OCR.TesseractWrapper.ePageSegMode.PSM_AUTO;
           OCR.TesseractWrapper.ePageSegMode auto_only = OCR.TesseractWrapper.ePageSegMode.PSM_AUTO_ONLY;
           OCR.TesseractWrapper.ePageSegMode auto_osd = OCR.TesseractWrapper.ePageSegMode.PSM_AUTO_OSD;
           OCR.TesseractWrapper.ePageSegMode circle_word =  OCR.TesseractWrapper.ePageSegMode.PSM_CIRCLE_WORD;
           OCR.TesseractWrapper.ePageSegMode psm_count = OCR.TesseractWrapper.ePageSegMode.PSM_COUNT;
           OCR.TesseractWrapper.ePageSegMode osd_only = OCR.TesseractWrapper.ePageSegMode.PSM_OSD_ONLY;
           OCR.TesseractWrapper.ePageSegMode single_block = OCR.TesseractWrapper.ePageSegMode.PSM_SINGLE_BLOCK;
           OCR.TesseractWrapper.ePageSegMode vert_text = OCR.TesseractWrapper.ePageSegMode.PSM_SINGLE_BLOCK_VERT_TEXT;
           OCR.TesseractWrapper.ePageSegMode single_char = OCR.TesseractWrapper.ePageSegMode.PSM_SINGLE_CHAR;
           OCR.TesseractWrapper.ePageSegMode single_column = OCR.TesseractWrapper.ePageSegMode.PSM_SINGLE_COLUMN;
           OCR.TesseractWrapper.ePageSegMode single_line = OCR.TesseractWrapper.ePageSegMode.PSM_SINGLE_LINE;
           OCR.TesseractWrapper.ePageSegMode single_word = OCR.TesseractWrapper.ePageSegMode.PSM_SINGLE_WORD;
             * */
            if (Ocr != null)
                Ocr.SetPageSegMode(PageSegMode);
        }

        // set the language if provided
        public void SetLanguage(string language)
        { 
        if (Ocr != null)
            Ocr.Init(ocr_path, language, 3);
        }

        //set the whitelist if provided
        public void SetWhitelist(string whitelist)
        {
            if (Ocr != null)
                Ocr.SetVariable("tessedit_char_whitelist", whitelist);
        }

        public string ReadScreen(double _x, double _y, double _width, double _height)
        {
            //set screen area
            Rect area = new Rect(_x, _y, _width, _height);

            //create a new bitmap image
            using (Bitmap image = new Bitmap((int)area.Width, (int)area.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
            {
                //do the screen grab
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.CopyFromScreen((int)area.X, (int)area.Y, 0, 0, new System.Drawing.Size((int)area.Width, (int)area.Height));
                }
                
                //FOR-DEBUG: SAVE THE IMAGE
                //image.Save(string.Format("c:\\debug\\{0}.png", imageCount++));

                //loop through the horizontal pixels
                for (int x = 0; x < image.Width; x++)
                {
                    //loop thought the vertical pixels
                    for (int y = 0; y < image.Height; y++)
                    {
                        //get the color of the pixel at (x,y)
                        System.Drawing.Color color = image.GetPixel(x, y);

                        //set variables to 0 for maths
                        float r = 0.0f;
                        float deltaG = 0.0f;
                        float deltaB = 0.0f;

                        //find non-red
                        if (color.R != 0)
                        {
                            //save color value for non red
                            deltaG = Math.Abs(((float)color.R - (float)color.G)) / (float)color.R;
                            deltaB = Math.Abs(((float)color.R - (float)color.B)) / (float)color.R;
                        }

                        //set all non red too 0, all red to 1
                        if (((deltaG <= 0.23) && (deltaB <= 0.23)) && (color.R > 200))
                            r = 0.0f;
                        else r = 1.0f;

                        // write the modified pixels
                        image.SetPixel(x, y, System.Drawing.Color.FromArgb((byte)(r * 255.0f), (byte)(r * 255.0f), (byte)(r * 255.0f)));
                    }
                }
            
                //resize image
                int scale = 3;
                Bitmap scaledImage = new Bitmap(image.Width * scale, image.Height * scale);
                using (Graphics graphics = Graphics.FromImage(scaledImage))
                {
                    //enhance
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                    //write the scaled image
                    graphics.DrawImage(image, new System.Drawing.Rectangle(0, 0, scaledImage.Width, scaledImage.Height));
                }

                //FOR-DEBUG: SAVE THE IMAGE
                //image.Save(string.Format("c:\\debug\\{0}.png", imageCount++));
                
                //return the ocr text
                return Ocr.Recognize(scaledImage);
            }
        }
        
        public string ReadScreen(Bitmap image)
        {
                return Ocr.Recognize(image);
        }
    }
}