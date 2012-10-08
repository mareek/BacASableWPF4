using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Xml;
using System.Security.Principal;
using System.Xml.Schema;
using System.Diagnostics;
using System.Management;

namespace BacASableWPF4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, "MB SN : " + GetMotherBoardSerialNumber());
        }

        private string GetMotherBoardSerialNumber()
        {
            var searcher = new ManagementObjectSearcher("select * from Win32_BaseBoard ");

            var resultBuilder = new StringBuilder();
            return searcher.Get().Cast<ManagementBaseObject>().First()["SerialNumber"].ToString();
        }

        private string GetHardwareProperties()
        {
            var searcher = new ManagementObjectSearcher("select * from Win32_BaseBoard ");

            var resultBuilder = new StringBuilder();
            var mo = searcher.Get().Cast<ManagementBaseObject>().First();
            foreach (PropertyData property in mo.Properties)
            {
                var truc = mo[property.Name];
                resultBuilder.AppendFormat("{0} : {1}\n", property.Name, mo[property.Name]);
            }
            return resultBuilder.ToString();
        }

        private void TestYieldVsList()
        {
            var reportBuilder = new StringBuilder();

            foreach (var sampleSize in new[] { 10, 1000, 1000000, 100000000 })
            {
                reportBuilder.AppendFormat("Sample size: {0}\n    chrono Yield : {1}\n    chrono List : {2}\n\n", sampleSize, TestPerfsYield(sampleSize), TestPerfsList(sampleSize));
            }

            MessageBox.Show(this, reportBuilder.ToString());
        }

        private TimeSpan TestPerfsYield(int sampleSize)
        {
            var chronoYield = Stopwatch.StartNew();
            int total = 0;
            foreach (var num in GenerateEnumerable(sampleSize))
            {
                total += num;
            }
            chronoYield.Stop();
            return chronoYield.Elapsed;
        }

        private IEnumerable<int> GenerateEnumerable(int sampleSize)
        {
            var rnd = new Random();
            for (var i = 0; i < sampleSize; i++)
            {
                yield return 1;
            }
            Console.WriteLine("Yield working set max size : {0} Mo", Environment.WorkingSet / 1000000);
        }

        private TimeSpan TestPerfsList(int sampleSize)
        {
            var chronoList = Stopwatch.StartNew();
            int total = 0;
            foreach (var num in GenerateList(sampleSize))
            {
                total += num;
            }
            chronoList.Stop();
            return chronoList.Elapsed;
        }

        private List<int> GenerateList(int sampleSize)
        {
            var rnd = new Random();
            var result = new List<int>();
            for (var i = 0; i < sampleSize; i++)
            {
                result.Add(1);
            }
            Console.WriteLine("list working set max size : {0} Mo", Environment.WorkingSet / 1000000);
            return result;
        }

        private void TestExplicitInterface()
        {
            var flag = true;
            var myConcreteObject = new ExplicitConcreteType(flag);
            ExplicitInterfaceTest myInterfacedObject = myConcreteObject;
            MessageBox.Show(this, string.Format("Flag : {0}\nConcrete : {1} \n Interface : {2}", flag, myConcreteObject.InvertFlag, myInterfacedObject.MyExplicitFlag));
        }

        public interface ExplicitInterfaceTest
        {
            bool MyExplicitFlag { get; }
        }

        public class ExplicitConcreteType : ExplicitInterfaceTest
        {
            private readonly bool myFlag;
            bool ExplicitInterfaceTest.MyExplicitFlag
            {
                get { return myFlag; }
            }

            public bool InvertFlag
            {
                get { return !myFlag; }
            }

            public ExplicitConcreteType(bool myFlag)
            {
                this.myFlag = myFlag;
            }
        }

        private void TestXmlValidation()
        {
            var validationEventHandler = new ValidationEventHandler((sender, e) => Console.WriteLine(e.Exception.Message));

            XmlSchema schema;
            using (var stream = File.OpenRead(@"D:\Src\IINT-MCN-EverBluSoftware\branches\2.3\MCN_ERM_WINCE_NET\Actaris\Actaris.ERM.IO\Tools\XSD\routeV22.xsd"))
            {
                schema = XmlSchema.Read(stream, validationEventHandler);
            }

            var readerSettings = new XmlReaderSettings { ValidationType = ValidationType.Schema };
            readerSettings.ValidationEventHandler += validationEventHandler;
            readerSettings.Schemas.Add(schema);

            using (XmlReader reader = XmlReader.Create(@"C:\Users\mmouriss\Desktop\FdcData_20120921_1344118566.xml", readerSettings))
            {
                while (reader.Read()) ;
            }
        }

        private Rect GetXkcdBounds(DirectoryInfo xkcdFileDir)
        {
            var points = GetXkcdPoints(xkcdFileDir);

            var xMin = points.Min(p => p.X);
            var xMax = points.Max(p => p.X);
            var yMin = points.Min(p => p.Y);
            var yMax = points.Max(p => p.Y);

            return new Rect(new Point(xMin, yMin), new Point(xMax, yMax));
        }

        private bool TestXkcdNeighbourhood(DirectoryInfo xkcdFileDir)
        {
            var points = GetXkcdPoints(xkcdFileDir);
            var lonelyPoints = new List<Point>();
            foreach (var point in points)
            {
                var neighbours = GetNeighbours(point).ToArray();
                if (!points.Intersect(neighbours).Any())
                {
                    lonelyPoints.Add(point);
                }
            }

            return !lonelyPoints.Any();
        }

        private Point[] GetXkcdPoints(DirectoryInfo xkcdFileDir)
        {
            return xkcdFileDir.EnumerateFiles().Select(f => CoordinatesOfXkcdFile(f)).ToArray();
        }

        private Point CoordinatesOfXkcdFile(FileInfo xkcdFile)
        {
            var cleanName = xkcdFile.Name.Replace(xkcdFile.Extension, "");

            var coords = cleanName.Split("nsew".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            var x = double.Parse(coords[1]) * (cleanName.Contains('w') ? -1.0 : 1.0);
            var y = double.Parse(coords[0]) * (cleanName.Contains('n') ? -1.0 : 1.0);

            return new Point(x, y);
        }

        private IEnumerable<Point> GetNeighbours(Point point)
        {
            yield return new Point(point.X + 1, point.Y);
            yield return new Point(point.X - 1, point.Y);
            yield return new Point(point.X, point.Y + 1);
            yield return new Point(point.X, point.Y - 1);

            yield return new Point(point.X + 1, point.Y + 1);
            yield return new Point(point.X - 1, point.Y - 1);
            yield return new Point(point.X - 1, point.Y + 1);
            yield return new Point(point.X + 1, point.Y - 1);
        }

        private void TestBinaryFormater()
        {
            var testSet = Enumerable.Range(0, 32).Select(i => (uint)Math.Pow(2, i) - 1);
            MessageBox.Show(string.Join("\r\n", testSet.Select(n => n.ToString("0 000 000 000") + " : " + ToBinaryString(n))));
        }

        private string ToBinaryString(uint number)
        {
            var resultBuilder = new StringBuilder(32);
            long consumedNumber = number;

            for (int i = 0; i < 32; i++)
            {
                if (i % 4 == 0)
                {
                    resultBuilder.Insert(0, " ");
                }
                if (i % 8 == 0)
                {
                    resultBuilder.Insert(0, " ");
                }
                long modulo = consumedNumber % (long)Math.Pow(2, i + 1);
                var bit = modulo != 0;
                consumedNumber -= modulo;
                resultBuilder.Insert(0, bit ? "1" : "0");
            }

            return resultBuilder.ToString().Trim();
        }

        public static uint ByteArrayToUIntEx(byte[] bytes)
        {
            uint result = 0;

            var lowerBound = 0;
            var upperBound = 4;
            while (lowerBound < bytes.Length)
            {
                uint chunkResult = 0;
                for (int i = lowerBound; i < bytes.Length || i < upperBound; i++)
                {
                    chunkResult += (uint)bytes[i] * (uint)Math.Pow(2, i - lowerBound);
                }

                result ^= chunkResult;

                lowerBound += 4;
                upperBound += 4;
            }
            return result;
        }

        private void LinqTest()
        {
            var rnd = new Random();
            var listInt = Enumerable.Range(0, 50).Select(i => rnd.Next(100)).ToList();
            var coupledList = Enumerable.Range(0, listInt.Count).Zip(listInt, (i, j) => Tuple.Create(i, j)).ToList();

            var formatedList = coupledList.Select(c => c.Item1.ToString("00") + " - " + c.Item2.ToString("00"));

            MessageBox.Show(string.Join("\r\n", formatedList));
        }

        private void TestTruncateIndex()
        {
            decimal nbInt = 4;
            decimal nbDec = 2;
            decimal nbDigit = nbInt + nbDec;
            decimal index = 12345.12345m;

            decimal factorDec = Convert.ToDecimal(Math.Pow(10, (double)nbDec));
            decimal factorInt = Convert.ToDecimal(Math.Pow(10, (double)nbInt));

            decimal result = index % factorInt;

            result = decimal.Floor(result * factorDec) / factorDec;
            MessageBox.Show(this, result.ToString());
        }

        private void TestTupleEquality()
        {
            string[] arraytest = { "a", "b", "c" };
            var transformed = from truc in arraytest
                              select Tuple.Create("HAHAHA", "HIHIHI", "HOHOHO");
            MessageBox.Show(transformed.Count().ToString() + " - " + transformed.Distinct().Count().ToString());
        }

        private static double MeanFileSize(DirectoryInfo directory)
        {
            return directory.GetFiles().Select(f => f.Length).Average();
        }

        private static long MedianFileSize(DirectoryInfo directory)
        {
            var fileSizes = (from file in directory.GetFiles()
                             orderby file.Length
                             select file.Length).ToList();
            return fileSizes[fileSizes.Count / 2];
        }

        private static void CreateShorcut()
        {
            var shell = new IWshRuntimeLibrary.WshShell();
            string shortcutAddress = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\Notepad.lnk";
            var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = "New shortcut for a Notepad";
            shortcut.TargetPath = Environment.GetFolderPath(Environment.SpecialFolder.System) + @"\notepad.exe";
            shortcut.Save();
        }
    }
}
