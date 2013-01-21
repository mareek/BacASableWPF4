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
using System.Xml.Linq;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Globalization;
using System.Threading.Tasks;

namespace BacASableWPF4
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private const string PRIVATE_KEY = @"
<RSAKeyValue>
  <Modulus>sJKuRsD1Uk6c4rtzOGfuhel1sBGY1J0HxEAWROa21c7yy8zPJxvn6mySsCUYamhBEailK4zyz9He/A48F1GV8R2jR7SlG6ppW/O9ZTUeGL74DQTI8EggY+PfTa9xFSH2Bk5UsgqdsNRk1cOGv67WlJoPL9Vn4JkBFJ6gcHAsfds=</Modulus>
  <Exponent>AQAB</Exponent>
  <P>7ZNDkTJzOo2jMTiM11vqHhX9F85S82lOz10Rs3xxzNBR1GSbdcOXOK8tTZWlgsmVn4ErSOXlqDwBI3EBKo+HUw==</P>
  <Q>vkRMhONQrfyB96ftIYL4+Riw7FWl3vO2KmVEEpyJEur5EGgyyofy7dRReqKqAb+K+iP9TsaU22opiAechauGWQ==</Q>
  <DP>f0aTvifO/6F9uhLXsVB2nmOdUbGhUvIp3IG5x/R1awp3rFexyWddjmqa1KPFJcolNGyY6dbwMC7lVT1nKIv4LQ==</DP>
  <DQ>OqXA1GFhGBAyW402idLePaH/vwlzdHK43v6R6g64Lc2h8g28QjN/jRGZ/+wt7RYGl64KQYLylWN248g81fMWGQ==</DQ>
  <InverseQ>pdUQPLG8kEd1GsYdWeQYkQxoNrolZp/RjRSNAGL8vFurTOFd61GbNT8CVOES0uLA7PM8ZxmxcF98bsiXCRTuyQ==</InverseQ>
  <D>hki3M2Xx7AuPMrueL8qS0tKuxx1K3n8h9fVLOlE/wTDm42k6LaMCZ/z0PfOoMtxgh/56xrklvDj+3TAyMQXCAlzHsjhrCTGPAUucYWXrKHIXKiICU5QC5f8j0sJqV2YA5qCmUO2ANpxMYGs0xwbU6qCaAjHHgCZDguKCabvB8TE=</D>
</RSAKeyValue>";

        private const string PUBLIC_KEY = @"
<RSAKeyValue>
  <Modulus>sJKuRsD1Uk6c4rtzOGfuhel1sBGY1J0HxEAWROa21c7yy8zPJxvn6mySsCUYamhBEailK4zyz9He/A48F1GV8R2jR7SlG6ppW/O9ZTUeGL74DQTI8EggY+PfTa9xFSH2Bk5UsgqdsNRk1cOGv67WlJoPL9Vn4JkBFJ6gcHAsfds=</Modulus>
  <Exponent>AQAB</Exponent>
</RSAKeyValue>";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            ExtractDataFromXml();
        }

        private void ExtractDataFromXml()
        {
            using (var reader = File.OpenText(@"C:\Temp\rawXML.xml"))
            {
                var doc = XDocument.Load(reader);
                var mlogDatas = from mius in doc.Root.Elements()
                                from datas in mius.Elements()
                                select new
                                {
                                    SerialNumber = mius.Attribute("RFAddress").Value,
                                    Date = XmlConvert.ToDateTime(datas.Attribute("DataDate").Value, XmlDateTimeSerializationMode.Unspecified),
                                    RawData = Convert.FromBase64String(datas.Attribute("RawData").Value),
                                } into miuData
                                orderby miuData.SerialNumber, miuData.Date
                                select miuData;

                MessageBox.Show(this, string.Join("\n", mlogDatas.Select(d => string.Format("{0} - {1} : {2}", d.SerialNumber, d.Date.ToShortDateString(), d.RawData.ToRawhexString()))));
            }
        }

        private void PlayUnitConversionTestRun()
        {
            TestUnitConversion(EnergyUnit.Wh, 10000, EnergyUnit.kWh, 10);
            TestUnitConversion(EnergyUnit.Wh, 100, EnergyUnit.Wh, 100);
            TestUnitConversion(EnergyUnit.MWh, 0.001m, EnergyUnit.kWh, 1);
            TestUnitConversion(EnergyUnit.MWh, 0.0001m, EnergyUnit.Wh, 100);
            TestUnitConversion(EnergyUnit.kWh, 0.0001m, EnergyUnit.Wh, 0.1m);
            TestUnitConversion(EnergyUnit.kWh, 0.1m, EnergyUnit.Wh, 100);
            TestUnitConversion(EnergyUnit.kWh, 1000000000000m, EnergyUnit.MWh, 1000000000m);
            TestUnitConversion(EnergyUnit.Wh, 0.001m, EnergyUnit.Wh, 0.001m);
            TestUnitConversion(EnergyUnit.Wh, 3742, EnergyUnit.Wh, 3742);

            TestUnitConversion(EnergyUnit.J, 10000, EnergyUnit.kJ, 10);
            TestUnitConversion(EnergyUnit.J, 100, EnergyUnit.J, 100);
            TestUnitConversion(EnergyUnit.MJ, 0.001m, EnergyUnit.kJ, 1);
            TestUnitConversion(EnergyUnit.MJ, 0.0001m, EnergyUnit.J, 100);
            TestUnitConversion(EnergyUnit.kJ, 0.0001m, EnergyUnit.J, 0.1m);
            TestUnitConversion(EnergyUnit.kJ, 0.1m, EnergyUnit.J, 100);
            TestUnitConversion(EnergyUnit.kJ, 1000000000000m, EnergyUnit.GJ, 1000000m);
            TestUnitConversion(EnergyUnit.GJ, 0.001m, EnergyUnit.MJ, 1);
            TestUnitConversion(EnergyUnit.GJ, 0.0001m, EnergyUnit.kJ, 100);
            TestUnitConversion(EnergyUnit.J, 3742, EnergyUnit.J, 3742);

            MessageBox.Show(this, "All tests clear !");
        }

        private static void TestUnitConversion(EnergyUnit originalUnit, decimal originalFactor, EnergyUnit desiredUnit, decimal desiredFactor)
        {
            var convertedFactor = originalFactor;
            var convertedUnit = originalUnit.GetAdjustedUnit(ref convertedFactor);

            /* quand je serai dans un projet de test
            Assert.AreEqual(convertedUnit, desiredUnit);
            Assert.AreEqual(convertedFactor, desiredFactor);
             */

            if (convertedUnit != desiredUnit)
                throw new ApplicationException(string.Format("Excepted : {0} - Actual : {1}", desiredUnit, convertedUnit));
            if (convertedFactor != desiredFactor)
                throw new ApplicationException(string.Format("Excepted : {0} - Actual : {1}", desiredFactor, convertedFactor));
        }

        private void SafeCallTest()
        {
            Button myButton = TestButton;

            var result = myButton.SC(e => e.Background).SC(e => e.ToString());
            //            var result = myButton.Background.ToString();

            MessageBox.Show(this, result ?? "null");
        }

        private void AwesomeExtensionMethod()
        {
            var searchedItem = 3;

            var paramsResult = searchedItem.In(1, 2, 3, 4, 5);
            var enumerableResult = searchedItem.In(Enumerable.Range(0, 5));

            var truc = new[] { 1, 2, 3, 4, 5 }.Contains(searchedItem);

            MessageBox.Show(this, string.Format("Search by params : {0}\nSearch by enumerable : {1}", paramsResult, enumerableResult));
        }

        private IEnumerable<int> LinqMadness()
        {
            return from truc in Enumerable.Range(0, 100)
                   where truc.In(from i in Enumerable.Range(1, 100)
                                 select i * 2)
                   select truc;
        }

        private void ExtensionsMethodsOnEnumsAndGenerics()
        {
            var result = string.Join("\n",
                                     from myEnum in Enum.GetValues(typeof(ParallelMergeOptions)).Cast<ParallelMergeOptions>()
                                     select string.Format("{0} : {1}", myEnum, myEnum.IsMyKindOfEnum()));
            result.truc();
            MessageBox.Show(this, result);
        }

        private void TestProductByteParsing()
        {
            const byte zero = 0;
            byte productByte = 0x15;
            byte meterTypeByteSlice = 0;
            meterTypeByteSlice = zero.SetBitSlice(0, productByte.GetBitSlice(0, 3));

            byte mediumByteSlice = 0;
            mediumByteSlice = zero.SetBitSlice(0, productByte.GetBitSlice(3, 2));

            MessageBox.Show(this, "meter type : " + meterTypeByteSlice.ToString() + "\nmedium type : " + mediumByteSlice.ToString());
        }

        private void BCDConversions()
        {
            var shortBCDByteArray = new byte[] { 0x10, 0x32, };
            var longBCDByteArray = new byte[] { 0x10, 0x32, 0x54, 0x76, };

            MessageBox.Show(this, string.Format("long BCD : {0}\nshort BCD : {1}\n", longBCDByteArray.BCDToInteger(), shortBCDByteArray.BCDToInteger()));

        }

        private void MoreBitParsing()
        {
            var témoin = BitConverter.ToUInt32(new byte[] { 1, 0, 0, 0 }, 0);
            var truc = new byte[] { 1, 0, 0, 0 };
            truc = truc.Concat(new byte[] { 0, 0, 0, 0 }).ToArray();
            var result = BitConverter.ToUInt32(truc, 0);
            MessageBox.Show(this, string.Format("témoin : {0}\ntruc : {1}", témoin, result));
        }

        private void TestLinqSortingByXmlAttribute()
        {
            var doc = XDocument.Parse("<Root>    <Element Attribute=\"1\"/>    <Element Attribute=\"2\"/>    <Element/></Root>");
            var orderedElements = from element in doc.Root.Elements()
                                  orderby element.Attribute("Attribute") == null ? null : element.Attribute("Attribute").ToString()
                                  select element.ToString();
            MessageBox.Show(this, string.Join("\n", orderedElements));
        }

        private void TestBitParsing()
        {

            var dataBytes = new byte[] { 0x90, 0x1A };

            const byte zero = 0;

            var Day = zero.SetBitSlice(0, dataBytes[0].GetBitSlice(0, 5));

            var Month = zero.SetBitSlice(0, dataBytes[1].GetBitSlice(0, 4));

            var yearBitArray = dataBytes[0].GetBitSlice(5, 3).Concat(dataBytes[1].GetBitSlice(4, 4)).ToArray();
            var Year = zero.SetBitSlice(0, yearBitArray);


            //var Day = Enumerable.Range(0, 5).Aggregate((byte)0, (res, i) => res.SetBit(i, dataBytes[0].GetBit(i)));
            //var Month = Enumerable.Range(0, 4).Aggregate((byte)0, (res, i) => res.SetBit(i, dataBytes[1].GetBit(i)));
            //var Year = Enumerable.Range(5, 3).Select(i => dataBytes[0].GetBit(i)).Concat(Enumerable.Range(4, 4).Select(i => dataBytes[1].GetBit(i))).Zip(Enumerable.Range(0, 7), (bit, i) => new { Val = bit, Index = i }).Aggregate((byte)0, (res, i) => res.SetBit(i.Index, i.Val));

            //var YearBitArray = Enumerable.Range(5, 3).Select(i => dataBytes[0].GetBit(i)).Concat(Enumerable.Range(4, 4).Select(i => dataBytes[1].GetBit(i)));
            //var Year = Enumerable.Range(0, 7).Aggregate((byte)0, (res, i) => res.SetBit(i, YearBitArray[i]));

            MessageBox.Show(this, string.Format("20{0}-{1}-{2}", Year, Month, Day));
        }


        private void TestRsaSignature()
        {
            var testFile = XDocument.Parse(PRIVATE_KEY);
            var garbage = XDocument.Parse("<Toto><AlaPlage tata=\"true\"/></Toto>");

        }

        private void TestCompression()
        {
            var report = from i in Enumerable.Range(7, 12)
                         select (int)Math.Pow(2, i) into dataLength
                         select string.Format("{0:000000} : {1}", dataLength, TestCompressionUsefullness(dataLength));

            MessageBox.Show(this, string.Join("\n", report));
        }

        private string TestCompressionUsefullness(int dataLength)
        {
            var rnd = new Random();
            var randomData = new byte[dataLength];
            rnd.NextBytes(randomData);

            var higlyCompressibleData = new byte[dataLength];

            var lowlyCompressibleData = new byte[dataLength];
            var increment = 1;
            var val = 0;
            var pass = 0;
            for (var i = 0; i < dataLength; i++)
            {
                lowlyCompressibleData[i] = (byte)val;

                val = (val + increment) % 256;
                pass++;
                if (pass > 255)
                {
                    pass = 0;
                    increment += 2;
                }
            }

            var fileData = new byte[dataLength];
            using (var fileReader = File.OpenRead(@"D:\temp\allocine.xml"))
            {
                fileReader.Read(fileData, 0, dataLength);
            }

            var rndZipped = ZipData(randomData);
            var highZipped = ZipData(higlyCompressibleData);
            var lowZipped = ZipData(lowlyCompressibleData);
            var fileZipped = ZipData(fileData);

            Func<byte[], decimal> calcPercent = zippedArray => 100m * (1m - ((decimal)zippedArray.Length) / (decimal)dataLength);

            return string.Format("Random : {0:00.0}  ; Low : {2:00.0}  ; High : {1:00.0}  ; File : {3:00.0}",
                                 calcPercent(rndZipped),
                                 calcPercent(highZipped),
                                 calcPercent(lowZipped),
                                 calcPercent(fileZipped));
        }

        private static byte[] ZipData(byte[] dataToZip)
        {
            using (var zipStream = new MemoryStream())
            {
                using (var dataStream = new MemoryStream(dataToZip))
                using (var compressionStream = new DeflateStream(zipStream, CompressionMode.Compress))
                {
                    dataStream.CopyTo(compressionStream);
                }
                return zipStream.ToArray();
            }
        }

        private static byte[] UnzipData(byte[] zippedData)
        {
            using (var decompressedStream = new MemoryStream())
            {
                using (var compressedStream = new MemoryStream(zippedData))
                using (var decompressionStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
                {
                    decompressionStream.CopyTo(decompressedStream);
                }
                return decompressedStream.ToArray();
            }
        }


        private Byte[] SignLicenseFile(XDocument xdocFile)
        {
            using (var rsa = new RSACryptoServiceProvider())
            using (SHA512 sha = SHA512.Create())
            using (var xmlStream = new MemoryStream())
            {
                rsa.FromXmlString(PRIVATE_KEY);
                xdocFile.Save(xmlStream);
                xmlStream.Position = 0;
                return rsa.SignHash(sha.ComputeHash(xmlStream), "SHA512");
            }
        }

        private bool VerifySignature(XDocument xdocFile, byte[] signature)
        {
            using (var rsa = new RSACryptoServiceProvider())
            using (SHA512 sha = SHA512.Create())
            using (var xmlStream = new MemoryStream())
            {
                rsa.FromXmlString(PUBLIC_KEY);
                xdocFile.Save(xmlStream);
                xmlStream.Position = 0;
                return rsa.VerifyHash(sha.ComputeHash(xmlStream), "SHA512", signature);
            }
        }

        private string GetMotherBoardSerialNumber()
        {
            var searcher = new ManagementObjectSearcher("select SerialNumber from Win32_BaseBoard ");

            return searcher.Get().Cast<ManagementBaseObject>().First().Properties.Cast<PropertyData>().First().Value.ToString();
        }

        private string GetHardwareProperties(string className)
        {
            var searcher = new ManagementObjectSearcher("select * from " + className);

            var resultBuilder = new StringBuilder();
            foreach (ManagementBaseObject mo in searcher.Get())
            {
                foreach (PropertyData property in mo.Properties)
                {
                    var truc = mo[property.Name];
                    resultBuilder.AppendFormat("{0} : {1}\n", property.Name, property.Value);
                }
                resultBuilder.Append("=====================================\n");
                resultBuilder.AppendLine();
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
