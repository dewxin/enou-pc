﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OCRLibrary;

namespace Enou
{
    /// <summary>
    /// ScreenCaptureWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ScreenCaptureWindow : Window
    {
        BitmapImage img;

        Point iniP;
        private ViewModel viewModel;
        Rect selectRect;
        double scale;

        public static System.Drawing.Rectangle OCRArea;

        int capMode;

        public ScreenCaptureWindow(BitmapImage i, int mode = 1)
        {
            img = i;
            scale = Common.GetScale();
            capMode = mode;
            selectRect = Rect.Empty;
            InitializeComponent();

            imgMeasure.Source = img;

            DrawingAttributes drawingAttributes = new DrawingAttributes
            {
                Color = Colors.Red,
                Width = 2,
                Height = 2,
                StylusTip = StylusTip.Rectangle,
                //FitToCurve = true,
                IsHighlighter = false,
                IgnorePressure = true,
            };
            inkCanvasMeasure.DefaultDrawingAttributes = drawingAttributes;

            viewModel = new ViewModel
            {
                MeaInfo = "按住鼠标左键并拖动鼠标绘制出要识别的区域，确认完成后单击右键退出",
                InkStrokes = new StrokeCollection(),
            };

            DataContext = viewModel;

        }

        private void InkCanvasMeasure_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                iniP = e.GetPosition(inkCanvasMeasure);
            }

            if (e.ChangedButton == MouseButton.Right)
            {
                Point cursorPoint = e.GetPosition(inkCanvasMeasure);
                Point realPoint = new Point(cursorPoint.X * scale, cursorPoint.Y * scale);
                if (selectRect.Equals(Rect.Empty))
                {
                    this.Hide();
                    this.Close();
                }

                if(selectRect.Contains(realPoint))
                {
                    //to memory leak problem 
                    System.Drawing.Image img = getImage();
                    this.Close();
                    DoOCR(img);
                }
                else
                {
                    selectRect = Rect.Empty;
                    viewModel.InkStrokes.Clear();
                }
            }
        }

        private void InkCanvasMeasure_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Draw square
                System.Windows.Point endP = e.GetPosition(inkCanvasMeasure);
                List<System.Windows.Point> pointList = new List<System.Windows.Point>
                    {
                        new System.Windows.Point(iniP.X, iniP.Y),
                        new System.Windows.Point(iniP.X, endP.Y),
                        new System.Windows.Point(endP.X, endP.Y),
                        new System.Windows.Point(endP.X, iniP.Y),
                        new System.Windows.Point(iniP.X, iniP.Y),
                    };
                StylusPointCollection point = new StylusPointCollection(pointList);
                Stroke stroke = new Stroke(point)
                {
                    DrawingAttributes = inkCanvasMeasure.DefaultDrawingAttributes.Clone()
                };
                viewModel.InkStrokes.Clear();
                viewModel.InkStrokes.Add(stroke);

                selectRect = new Rect(new Point(iniP.X * scale, iniP.Y * scale), new Point(endP.X * scale, endP.Y * scale));
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {

        }

        private System.Drawing.Image getImage()
        {
            OCRArea = new System.Drawing.Rectangle((int)selectRect.Location.X, (int)selectRect.Location.Y, (int)selectRect.Size.Width, (int)selectRect.Size.Height);
            //全局OCR截图，直接打开结果页面
            System.Drawing.Image img = ScreenCapture.GetWindowRectCapture(System.IntPtr.Zero, OCRArea, true);
            return img;
        }

        private void DoOCR(System.Drawing.Image image)
        {
            var reswin = new GlobalOCRWindow();
            reswin.DoSearchWithoutInit(image);
            reswin.Close();
        }

    }

    class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string meaInfo;
        public string MeaInfo
        {
            get => meaInfo;
            set
            {
                meaInfo = value;
                OnPropertyChanged("MeaInfo");
            }
        }

        private StrokeCollection inkStrokes;
        public StrokeCollection InkStrokes
        {
            get
            {
                return inkStrokes;
            }
            set
            {
                inkStrokes = value;
                OnPropertyChanged("InkStrokes");
            }
        }
    }

}