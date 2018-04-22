using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace InkPostcard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {        
        public MainWindow()
        {
            InitializeComponent();
            icFront.Color = Colors.Black;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int frontIndex = (int)icFront.GetValue(Canvas.ZIndexProperty);
            int backIndex = (int)icBack.GetValue(Canvas.ZIndexProperty);
            icFront.SetValue(Canvas.ZIndexProperty, backIndex);
            icBack.SetValue(Canvas.ZIndexProperty, frontIndex);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDlg = new OpenFileDialog();
            fileDlg.Filter = "jpg files (*.jpg)|*.jpg|All files (*.*)|*.*";
            if (fileDlg.ShowDialog() == true)
            {
                BitmapImage bitmap = new BitmapImage(new Uri(fileDlg.FileName));
                backgroundImage.SetValue(ImageBrush.ImageSourceProperty, bitmap);
                icFront.Strokes.Clear();
                icBack.Strokes.Clear();
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            string guid = Guid.NewGuid().ToString();
            string appDataPath = Environment.GetEnvironmentVariable("temp");
            string filePath1 = appDataPath + @"\icFront" + guid + ".jpg";
            string filePath2 = appDataPath + @"\icBack" + guid + ".jpg";
            string inkFile1 = appDataPath + @"\inkFront" + guid + ".isf";
            string inkFile2 = appDataPath + @"\inkBack" + guid + ".isf";
            string zipPath = appDataPath + @"\postcard" + guid + ".ipc";
            CreateImageFiles(filePath1, icFront);
            CreateImageFiles(filePath2, icBack);
            CreateZipFile(zipPath, filePath1, filePath2);

            Microsoft.Office.Interop.Outlook.Application app = new Microsoft.Office.Interop.Outlook.Application();
            Microsoft.Office.Interop.Outlook.MailItem mail = app.CreateItem(Microsoft.Office.Interop.Outlook.OlItemType.olMailItem);
            mail.HTMLBody = @"<p>Open this postcard in the app to bring it to life, and create your own!</p><img src='cid:" + "icFront" + guid + ".jpg" + "' width=480 height=270/><p>  </p><img src='cid:" + "icBack" + guid + ".jpg" + "' width=480 height=270/><p/><a href='https://www.microsoft.com/store/apps/9NBNHTJPCS5F'>Sent from InkPostcard App</a>";
            mail.Attachments.Add(filePath1);
            mail.Attachments.Add(filePath2);
            mail.Attachments.Add(zipPath);
            mail.Display();
        }

        private void CreateZipFile(string zipPath, string file1, string file2)
        {
            using (FileStream zipToOpen = new FileStream(zipPath, FileMode.OpenOrCreate))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    ZipArchiveEntry entry1 = archive.CreateEntryFromFile(file1, "front.jpg");
                    ZipArchiveEntry entry2 = archive.CreateEntryFromFile(file2, "back.jpg");
                }
            }
        }

        private void CreateImageFiles(string path, FrameworkElement element)
        {
            RenderTargetBitmap targetBitmap =
                new RenderTargetBitmap((int)element.ActualWidth,
                                       (int)element.ActualHeight,
                                       96d, 96d,
                                       PixelFormats.Default);
            targetBitmap.Render(element);

            // add the RenderTargetBitmap to a Bitmapencoder
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = 100;
            encoder.Frames.Add(BitmapFrame.Create(targetBitmap));

            // save file to disk            
            using (FileStream fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                encoder.Save(fs);
            }
        }
    }
}
