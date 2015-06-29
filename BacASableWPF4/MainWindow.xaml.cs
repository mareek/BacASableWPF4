using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Microsoft.Web.XmlTransform;
using Microsoft.Win32;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

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
            ComputeTimeByDatabase(new FileInfo(@"C:\Users\mourisson\Downloads\Log_Kimado_Debug_du_20150629_0000_au_20150629_1119.txt"), "AddCotisationIndividuelleEtatInEtatCotisationIndividuelleProjection");
        }

        private async void TestHttpClientTimeout()
        {
            var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
            client.DefaultRequestHeaders.Accept.Clear();
            client.Timeout = TimeSpan.FromMilliseconds(1);
            client.BaseAddress = new Uri("http://www.w3c.org");

            try
            {
                var response = await client.GetAsync("");
                MessageBox.Show(response.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString());
            }
        }

        private void ComputeTimeByDatabase(FileInfo logFile, string forTransformation = null)
        {
            var timeByDatabase = ComputeDatabaseProcessingDuration(logFile, forTransformation);
            ShowResult(Tuple.Create((long)timeByDatabase.Count, timeByDatabase.Values.Aggregate((a, b) => a + b)), "Temps par Db");
        }

        private void ComputeInsertDuration(FileInfo logFile)
        {
            ShowResult(ComputeInstructionDuration(logFile, l => l.Contains("Execute commande sql 'INSERT INTO Events (DatabaseName, AggregateName, AggregateId,")), "Insertion events");
        }

        private void ComputeUpdateProjectionDuration(FileInfo logFile)
        {
            ShowResult(ComputeInstructionDuration(logFile, l => l.Contains("Execute commande sql 'DELETE FROM")), "Update projection");
        }

        private void ShowResult(Tuple<long, TimeSpan> result, string title)
        {
            var occurences = result.Item1;
            var total = result.Item2;
            var average = new TimeSpan(total.Ticks / occurences);

            MessageBox.Show(this, string.Format("Occurences:\t {0}\nTemps moyen:\t {1}\nTemps total:\t {2}\n", occurences, average, total), title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private Dictionary<string, TimeSpan> ComputeDatabaseProcessingDuration(FileInfo logFile, string forTransformation = null)
        {
            var useDatabaseRegex = new Regex(@"Cegid.Link.Updater.Core.Loggers.Log4NetEventTransformationLogger\t-\tUse database (?<DatabaseName>\S*)", RegexOptions.Compiled);
            var endDatabaseRegex = new Regex(@"Cegid.Link.Updater.Core.Loggers.Log4NetEventTransformationLogger\t-\tEnd database (?<DatabaseName>\S*)", RegexOptions.Compiled);
            var startTransformationRegex = new Regex(@"Start transformation : (?<Transformation>\S*)", RegexOptions.Compiled);
            var endTransformationRegex = new Regex(@"End transformation : (?<Transformation>\S*)", RegexOptions.Compiled);

            var useDatabaseLineByDatabaseName = new Dictionary<string, LogLine>();
            var endDatabaseLineByDatabaseName = new Dictionary<string, LogLine>();

            using (var textStream = logFile.OpenText())
            {
                Action<string, Regex, Dictionary<string, LogLine>> addLineToDicIfMatch = (line, regex, dico) =>
                    {
                        var regexResult = regex.Match(line);
                        if (regexResult.Success)
                        {
                            dico.Add(regexResult.Groups["DatabaseName"].Value, new LogLine(line));
                        }
                    };

                bool recording = forTransformation == null;
                while (!textStream.EndOfStream)
                {
                    var line = textStream.ReadLine();
                    if (!recording)
                    {
                        recording = startTransformationRegex.Match(line).Groups["Transformation"].Value == forTransformation;
                    }
                    else
                    {
                        addLineToDicIfMatch(line, useDatabaseRegex, useDatabaseLineByDatabaseName);
                        addLineToDicIfMatch(line, endDatabaseRegex, endDatabaseLineByDatabaseName);

                        recording = endTransformationRegex.Match(line).Groups["Transformation"].Value != forTransformation;
                    }
                }
            }

            var result = new Dictionary<string, TimeSpan>();

            foreach (var databaseName in useDatabaseLineByDatabaseName.Keys)
            {
                var useLine = useDatabaseLineByDatabaseName[databaseName];
                LogLine endLine;
                if (endDatabaseLineByDatabaseName.TryGetValue(databaseName, out endLine))
                {
                    result.Add(databaseName, endLine.Date - useLine.Date);
                }
                else
                {
                    Console.WriteLine("Traitement en cours sur " + databaseName);
                }
            }

            return result;
        }

        private Tuple<long, TimeSpan> ComputeInstructionDuration(FileInfo logFile, Predicate<string> predicateInstruction, Predicate<string> predicateNextInstruction = null)
        {
            long occurence = 0;
            var duration = new TimeSpan();

            using (var textStream = logFile.OpenText())
            {
                Action<string> processLine = null;

                processLine = line =>
                {
                    if (predicateInstruction(line) && !textStream.EndOfStream)
                    {
                        var nextLine = textStream.ReadLine();

                        if (predicateNextInstruction == null || predicateNextInstruction(nextLine))
                        {
                            occurence++;
                            var currentLogLine = new LogLine(line);
                            var nextLogLine = new LogLine(nextLine);
                            duration += (nextLogLine.Date - currentLogLine.Date);
                        }

                        processLine(nextLine);
                    }
                };

                while (!textStream.EndOfStream)
                {
                    processLine(textStream.ReadLine());
                }
            }

            return Tuple.Create(occurence, duration);
        }

        private void CleanKimadoLogs()
        {
            var lineToRemoveRegex = new Regex(".*Cegid.DsnLink.DataAccess.Databases.Sql.RunSqlCommandService	-	Utilise connexion 'Server=.*;Database=Link;User .*", RegexOptions.Compiled);
            var logFile = new FileInfo(@"C:\Users\mourisson\Downloads\Log_Kimado_Debug_du_20150622_1500_au_20150622_1552.txt");
            var outputFile = new FileInfo(@"C:\Users\mourisson\Downloads\Log Debug.txt");

            CleanLogFile(logFile, outputFile, lineToRemoveRegex);

            MessageBox.Show(this, "Success !");
        }

        private void CleanLogFile(FileInfo logFile, FileInfo outputFile, Regex lineToRemoveRegex)
        {
            if (outputFile.Exists)
            {
                throw new FileLoadException("Output file already exists;");
            }

            using (var outputStream = outputFile.CreateText())
            using (var textStream = logFile.OpenText())
            {
                while (!textStream.EndOfStream)
                {
                    var line = textStream.ReadLine();
                    if (!lineToRemoveRegex.IsMatch(line))
                    {
                        outputStream.WriteLine(line);
                    }
                }
            }
        }

        private void CompareCleanlogRegexp()
        {
            const string strRegex = ".*Cegid.DsnLink.DataAccess.Databases.Sql.RunSqlCommandService	-	Utilise connexion 'Server=.*;Database=Link;User .*";
            var compiled = new Regex(strRegex, RegexOptions.Compiled);
            var interpreted = new Regex(strRegex);
            var logFile = new FileInfo(@"C:\Users\mourisson\Downloads\Log_Kimado_Debug_du_20150622_1110_au_20150622_1202.txt");

            CompareRegexpPerfs(logFile, "Compiled", compiled, "Simple", interpreted);
        }

        private void CompareRegexpPerfs(FileInfo testFile, string name1, Regex regex1, string name2, Regex regex2)
        {
            var controlRun = TestRegexPerfsTemoin(testFile);
            var runRegex1 = TestRegexPerfs(testFile, regex1);
            var runRegex2 = TestRegexPerfs(testFile, regex2);

            if (runRegex1.Item1 != runRegex2.Item1 || runRegex1.Item1 > controlRun.Item1)
            {
                MessageBox.Show(this, "Resultats incohérents", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                Func<string, Tuple<int, TimeSpan>, string> formatResult = (name, result) => string.Format("{0} \t : {1}\n", name, result.Item2);

                var message = formatResult("Témoin", controlRun)
                              + formatResult(name1, runRegex1)
                              + formatResult(name2, runRegex2);

                MessageBox.Show(this, message, "Bench", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private Tuple<int, TimeSpan> TestRegexPerfs(FileInfo testFile, Regex regex)
        {
            using (var textStream = testFile.OpenText())
            {
                var chrono = Stopwatch.StartNew();
                int nbResult = 0;

                while (!textStream.EndOfStream)
                {
                    var line = textStream.ReadLine();
                    if (!regex.IsMatch(line))
                    {
                        nbResult++;
                    }
                }

                chrono.Stop();
                return Tuple.Create(nbResult, chrono.Elapsed);
            }
        }

        private Tuple<int, TimeSpan> TestRegexPerfsTemoin(FileInfo testFile)
        {
            using (var textStream = testFile.OpenText())
            {
                var chrono = Stopwatch.StartNew();
                int nbResult = 0;

                while (!textStream.EndOfStream)
                {
                    var line = textStream.ReadLine();
                    nbResult++;
                }

                chrono.Stop();
                return Tuple.Create(nbResult, chrono.Elapsed);
            }
        }

        private void GetAllFuckedUpJeDeclareRetourFromLogFile(FileInfo logFile)
        {
            using (var textStream = logFile.OpenText())
            {
                var allFuckedUpRetours = FindAllFuckedUpJeDeclareRetour(textStream.ReadToEnd()).Distinct();
                MessageBox.Show(this, string.Join(", ", allFuckedUpRetours));
            }
        }

        private void TestMyCode()
        {
            const string testInput =
@"45993829	2015-06-12 08:43:01,753	[210]	WARN	S111000IIS001	Cegid.Link.Web.Api.WebToken.Controllers.Services.Declaration.Retours.UpdateStatutDeclarationsJeDeclareBatch	-	Le document transmis par Net-Entreprises ou JeDeclare ne peut pas être traité (Déclaration: 2015-04-26584397188250, idFlux: 150882542, idRetour: 118567162)
45993831	2015-06-12 08:43:02,067	[210]	WARN	S111000IIS001	Cegid.Link.Web.Api.WebToken.Controllers.Services.Bilans.BilanAnomalie.BilanDeclarationProcessor	-	Type de retour incorrect : 20 (BAN ou CCO attendu)
45993833	2015-06-12 08:43:02,067	[210]	WARN	S111000IIS001	Cegid.Link.Web.Api.WebToken.Controllers.Services.Declaration.Retours.UpdateStatutDeclarationsJeDeclareBatch	-	Le document transmis par Net-Entreprises ou JeDeclare ne peut pas être traité (Déclaration: 2015-04-26584366188246, idFlux: 150883166, idRetour: 118567164)
45993835	2015-06-12 08:43:02,330	[210]	WARN	S111000IIS001	Cegid.Link.Web.Api.WebToken.Controllers.Services.Bilans.BilanAnomalie.BilanDeclarationProcessor	-	Type de retour incorrect : 20 (BAN ou CCO attendu)
45993837	2015-06-12 08:43:02,330	[210]	WARN	S111000IIS001	Cegid.Link.Web.Api.WebToken.Controllers.Services.Declaration.Retours.UpdateStatutDeclarationsJeDeclareBatch	-	Le document transmis par Net-Entreprises ou JeDeclare ne peut pas être traité (Déclaration: 2015-04-26585653188588, idFlux: 150896148, idRetour: 118569220)";

            var expectedResults = new[] { "150882542", "150883166", "150896148", };
            var actualResults = FindAllFuckedUpJeDeclareRetour(testInput).Distinct().ToArray();

            var success = !(expectedResults.Except(actualResults).Any() || actualResults.Except(expectedResults).Any());
            MessageBox.Show(this, "Success : " + success + "\n" + string.Join("\n", actualResults));
        }

        private IEnumerable<string> FindAllFuckedUpJeDeclareRetour(string input)
        {
            var idRetourRegexp = new Regex("Le document transmis par Net-Entreprises ou JeDeclare ne peut pas être traité.*idFlux: (?<idFlux>[0-9]{1,10}).*idRetour: (?<idRetour>[0-9]{1,10})");

            return from match in idRetourRegexp.Matches(input).Cast<System.Text.RegularExpressions.Match>()
                   let idRetour = match.Groups["idRetour"].Value
                   let idFlux = match.Groups["idFlux"].Value
                   orderby idFlux
                   select idFlux;
        }

        private IEnumerable<string> problemeDeMathALaCon()
        {
            return from a in Enumerable.Range(1, 9).Reverse()
                   from b in Enumerable.Range(1, 9).Where(k => !k.In(a))
                   from c in Enumerable.Range(1, 9).Where(k => !k.In(a, b))
                   from d in Enumerable.Range(1, 9).Where(k => !k.In(a, b, c))
                   from e in Enumerable.Range(1, 9).Where(k => !k.In(a, b, c, d))
                   from f in Enumerable.Range(1, 9).Where(k => !k.In(a, b, c, d, e))
                   from g in Enumerable.Range(1, 9).Where(k => !k.In(a, b, c, d, e, f))
                   from h in Enumerable.Range(1, 9).Where(k => !k.In(a, b, c, d, e, f, g))
                   from i in Enumerable.Range(1, 9).Where(k => !k.In(a, b, c, d, e, f, g, h))
                   where OperationDeMathALaConBis(a, b, c, d, e, f, g, h, i) == 66
                   select string.Join(", ", a, b, c, d, e, f, g, h, i);
        }

        private int OperationDeMathALaCon(int a, int b, int c, int d, int e, int f, int g, int h, int i)
        {
            return a + 13 * b / c + d + 12 * e - f - 11 + g * h / i - 10;
        }

        private int OperationDeMathALaConBis(int a, int b, int c, int d, int e, int f, int g, int h, int i)
        {
            return ((((((((((((a + 13) * b) / c) + d) + 12) * e) - f) - 11) + g) * h) / i) - 10);
        }

        private void SumOnNullableDecimals()
        {
            decimal?[] values = { 1, 2, null, 3 };

            MessageBox.Show(this, values.Sum().ToString());
        }

        private void FilterLog()
        {
            var logFile = new FileInfo(@"C:\Users\mourisson\Downloads\log (1).txt");
            var codes = GetHttpResponses(logFile).GroupBy(c => c).Select(g => new { Code = g.Key, Count = g.Count() }.ToString());

            MessageBox.Show(this, string.Join("\n", codes));
        }

        private IEnumerable<string> GetHttpResponses(FileInfo logFile)
        {
            using (var fileStream = logFile.OpenText())
            {
                while (!fileStream.EndOfStream)
                {
                    var line = fileStream.ReadLine();
                    var splitLine = line.Split(new[] { "HttpResponseStatus : " }, StringSplitOptions.RemoveEmptyEntries);
                    if (splitLine.Length > 1)
                    {
                        yield return splitLine[1].Trim();
                    }
                }
            }

        }

        private void ComparePerfsSha1()
        {
            var sha1 = SHA1.Create();
            var data = new byte[10];
            new Random().NextBytes(data);

            ComparePerfs("SHA1 Create + Compute", "SHA1 Compute", () => SHA1.Create().ComputeHash(data), () => sha1.ComputeHash(data));
        }

        private void ComparePerfs(string action1Name, string action2Name, Action action1, Action action2, int timeOutInMilliSeconds = 500)
        {
            var reportBuilder = new StringBuilder();

            var action1Time = TimeSpan.Zero;
            var action2Time = TimeSpan.Zero;
            var sampleSize = 10;

            while (action1Time.TotalMilliseconds < timeOutInMilliSeconds && action2Time.TotalMilliseconds < timeOutInMilliSeconds)
            {
                action1Time = MeasureActionTime(action1, sampleSize);
                action2Time = MeasureActionTime(action2, sampleSize);

                reportBuilder.AppendFormat("Sample size: {0:n0}\n    chrono {1} : {2}\n    chrono {3} : {4}\n\n", sampleSize, action1Name, action1Time, action2Name, action2Time);

                sampleSize *= 10;
            }

            MessageBox.Show(this, reportBuilder.ToString());
        }

        private TimeSpan MeasureActionTime(Action action, int nbIteration)
        {
            var chrono = Stopwatch.StartNew();

            for (int i = 0; i < nbIteration; i++)
            {
                action();
            }

            chrono.Stop();
            return chrono.Elapsed;
        }

        private bool IsDefault<T>(T value)
        {
            return EqualityComparer<T>.Default.Equals(value, default(T));
        }

        private void MyPrivatePex()
        {
            var possibleValues = new[]
            {
                default(string),
                "",
                "1",
                "Une string au hasard"
            };

            foreach (var val1 in possibleValues)
            {
                foreach (var val2 in possibleValues)
                {
                    if (FonctionRefactoree(val1, val2) != FonctionOriginale(val1, val2))
                    {
                        MessageBox.Show(this, "You fucked up", "FAIL", MessageBoxButton.OK, MessageBoxImage.Stop);
                        return;
                    }
                }
            }

            MessageBox.Show(this, "Succes !");
        }

        private T FonctionRefactoree<T>(T valuePivot, T oldValue)
        {
            if (oldValue != null && valuePivot != null && !oldValue.Equals(valuePivot))
            {
                return oldValue;
            }

            return default(T);

        }

        private T FonctionOriginale<T>(T valuePivot, T oldValue)
        {
            if ((valuePivot != null && oldValue != null) && !valuePivot.Equals(oldValue))
            {
                return oldValue != null ? oldValue : default(T);
            }

            return default(T);
        }

        private void TinkerWithILookup()
        {
            var baseEnumerable = Enumerable.Range(0, 11);
            var tableMultiplication = baseEnumerable.Select(i => new { number = i, multiples = baseEnumerable.Select(j => i * j) })
                                                    .SelectMany(a => a.multiples.Select(m => new { a.number, multiple = m }));
            var lookup = tableMultiplication.ToLookup(a => a.number);
            MessageBox.Show(this, string.Join("\n", lookup.Skip(1).Select(e => string.Join(", ", e.Skip(1).Select(a => a.multiple.ToString("00"))))));
        }

        private string GenerateGuidList(int count)
        {
            return string.Join("\n", Enumerable.Range(0, count).Select(_ => Guid.NewGuid().ToString()));
        }

        private void TestReadStreamAsyncOnHttpResponse()
        {
            var client = new HttpClient();
            var response = client.GetAsync("http://www.google.fr").Result;

            var l1 = GetResponseLengthTedious(response).Result;
            var l2 = GetResponseLengthTedious(response).Result; // Fail !

            MessageBox.Show(this, string.Format("l1 : {0}\nl2 : {1}", l1, l2));
        }

        private async Task<long> GetResponseLength(HttpResponseMessage response)
        {
            using (var fileStream = await response.Content.ReadAsStreamAsync())
            {
                return fileStream.Length;
            }
        }

        private async Task<long> GetResponseLengthTedious(HttpResponseMessage response)
        {
            var fileStream = await response.Content.ReadAsStreamAsync();
            long length = -1;
            var lastResult = 1;
            while (lastResult != -1)
            {
                lastResult = fileStream.ReadByte();
                length += 1;
            }

            return length;
        }

        private void CompareNullableArithmetic()
        {
            Func<decimal?, decimal?, decimal?> simpleMethod = (a, b) => (a ?? 0) - (b ?? 0);
            Func<decimal?, decimal?, decimal?> complicatedMethod = (pivot, reference) =>
                {
                    decimal? result = 0;
                    if (pivot.HasValue && reference.HasValue)
                    {
                        result = pivot.Value - reference.Value;
                    }
                    else if (pivot.HasValue && !reference.HasValue)
                    {
                        result = pivot.Value - 0;
                    }
                    else if (!pivot.HasValue & reference.HasValue)
                    {
                        result = 0 - reference.Value;
                    }

                    return result;
                };

            Func<decimal?, decimal?, Tuple<decimal?, decimal?>> tc = (a, b) => Tuple.Create(a, b);

            var jeuDeTest = new[]
            {
                tc(5, 3),
                tc(null, 3),
                tc(5, null),
                tc(null, null)
            };

            foreach (var item in jeuDeTest)
            {
                var simpleResult = simpleMethod(item.Item1, item.Item2);
                var complicatedResult = complicatedMethod(item.Item1, item.Item2);

                Debug.Assert(simpleResult == complicatedResult, string.Format("Simple is wrong on {0}", item));
            }

            MessageBox.Show(this, "Everything is awesome !");
        }

        private void Xdt()
        {
            var transform = new XmlTransformation(@"C:\Users\mourisson\Desktop\Web.Prod.config");
            var config = new XmlDocument { PreserveWhitespace = true };
            config.Load(@"C:\Users\mourisson\Desktop\Web.config");
            transform.Apply(config);
            config.Save(@"C:\Users\mourisson\Desktop\Web.config.transformed");
        }

        private void PivotAnalysis()
        {
            var pivotDirectory = new DirectoryInfo(@"C:\Users\mourisson\Downloads\pivot_rec");
            var pivots = pivotDirectory.EnumerateFiles("*.pivot").Select(LoadPivot).ToList();

            /*
             Declarations>
                    <Declaration siret="44185769500023" nature="01" type="01" fraction="11" du="01012014" champ="01" depot="01" versionCTSortie="P01V01">
                        <IndividusDeclares>
                             <IndividuDeclare refId="11007@0000"/>
            */

            var individus = (from pivot in pivots
                             from declaration in pivot.Elements("Declarations").Elements("Declaration")
                             from individu in declaration.Elements("IndividusDeclares").Elements("IndividuDeclare")
                             let refId = individu.Attribute("refId").Value
                             let splitedId = refId.Split('@')
                             select new
                             {
                                 siret = declaration.Attribute("siret").Value,
                                 individuId = refId,
                                 matricule = splitedId[0],
                                 detrompeur = splitedId[1],
                                 Label = refId + "-" + declaration.Attribute("siret").Value
                             }).Distinct().OrderBy(i => i.Label).ToList();

            var individusByMatricule = individus.ToLookup(i => i.matricule).Where(l => l.Count() > 1).ToList();

            //var messages = individusByMatricule.Select(l => l.Key + " : " + string.Join(", ", l.Select(i => i.Label)));
            var messages = individus.Select(i => i.Label);

            MessageBox.Show(string.Join("\n", messages));
        }

        private XElement LoadPivot(FileInfo pivotFile)
        {
            using (var stream = pivotFile.OpenRead())
            {
                return XElement.Load(stream);
            }
        }

        private void BsonPlayGround()
        {
            var strBson = "F8000000034465636C61726174696F6E4964003700000003506572696F6465001A00000010416E6E656500E2070000104D6F6973000400000000124F726472650081384C7B2609000000034465636C61726174696F6E52656D706C6163616E74654964003700000003506572696F6465001A00000010416E6E656500E2070000104D6F6973000400000000124F7264726500C291BF822609000000025F74003C0000004465636C61726174696F6E4465636C61726174696F6E52656D706C6163616E7465557064617465642C2043656769642E4C696E6B2E446F6D61696E00094576656E7443726561746564417400A1AE5AE64901000000";
            var rawBson = Enumerable.Range(0, strBson.Length)
                                    .Where(x => x % 2 == 0)
                                    .Select(x => Convert.ToByte(strBson.Substring(x, 2), 16))
                                    .ToArray();

            var bsonDocument = BsonSerializer.Deserialize<BsonDocument>(rawBson);

            Console.WriteLine(bsonDocument);

            var reasonElement = bsonDocument["Reason"];
            var reasonValue = reasonElement["_v"];
            var oldReason = reasonValue.AsInt32;
            var newReason = oldReason + 1;
            var newReasonValue = BsonValue.Create(newReason);
            reasonElement["_v"] = newReasonValue;

            bsonDocument.Remove("Reason");
            bsonDocument.Add("ErrorCode", reasonElement);

            using (var memoryStream = new MemoryStream())
            {
                using (var bsonWriter = BsonWriter.Create(memoryStream))
                {
                    BsonSerializer.Serialize(bsonWriter, bsonDocument);
                }

                var newRawBson = memoryStream.ToArray();
                var newStrBson = BitConverter.ToString(newRawBson).Replace("-", "");
            }
        }

        private void TestHashSet()
        {
            var testValues = new[] { "1", "1", "2", "2", };

            var hash = new HashSet<string>(testValues);
            MessageBox.Show(this, string.Format("values count : {0}\nhash count : {1}", testValues.Length, hash.Count));
        }

        private void TestEnumConversion()
        {
            var testValues = new[] { "01", "2", "", "WTF", null, "00", "1,75", "1 000", "1.000", "1,000" };

            var results = testValues.Select(ConvertNullableStrict<MyEnum>);

            MessageBox.Show(this, "[" + string.Join("]\n[", results) + "]");
        }

        public static T? ConvertNullableStrict<T>(string value) where T : struct
        {
            var enumValues = Enum.GetValues(typeof(T));

            int parsedValue;
            if (int.TryParse(value, out parsedValue)
                && enumValues.Cast<int>().Contains(parsedValue))
            {
                return (T)Enum.ToObject(typeof(T), parsedValue);
            }
            else
            {
                return null;
            }
        }

        private enum MyEnum
        {
            FirstValue = 1,
            SecondValue = 2
        }

        private void TestNullableDateComparaison()
        {
            Func<DateTime?, DateTime?, bool> dateInferiorTo =
                (d1, d2) => d1 <= d2;
            Func<DateTime?, DateTime?, string> testDateCouple =
                (d1, d2) => string.Format("{0} <= {1} == {2}", d1, d2, dateInferiorTo(d1, d2));

            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            var testResults = new[]
            {
                testDateCouple(null, null),
                testDateCouple(null, today),
                testDateCouple(today, null),
                testDateCouple(yesterday, today),
                testDateCouple(today, yesterday),
                testDateCouple(today, today)
            };

            MessageBox.Show(this, string.Join("\n", testResults));
        }

        private void TestDateTimeParseExact()
        {
            MessageBox.Show(this, DateTime.ParseExact("201408031830", "yyyyMMddHHmm", CultureInfo.CurrentCulture).ToString());
        }

        private void TestTimeToNextTick(int nbMinutesBetweenTick)
        {
            bool hasError = false;
            var baseDate = DateTime.Now.Date;
            for (int i = 0; i < 3600; i++)
            {
                var testDate = baseDate.AddSeconds(i);
                var timetoNextTick = GetTimeToNextTick(nbMinutesBetweenTick, testDate);
                var calculatedQuarterDateTime = testDate.Add(timetoNextTick);
                hasError = calculatedQuarterDateTime.Minute % nbMinutesBetweenTick != 0
                           || calculatedQuarterDateTime < testDate
                           || (calculatedQuarterDateTime - testDate) > TimeSpan.FromMinutes(nbMinutesBetweenTick);
                if (hasError)
                {
                    MessageBox.Show(this, string.Format("Error on {0} : {1}", testDate.ToShortTimeString(), calculatedQuarterDateTime.ToShortTimeString()));
                    break;
                }
            }
            if (!hasError)
                MessageBox.Show(this, "Success !!");

        }

        private static DateTime GetDateTimeNextTick(int nbMinutesBetweenTicks, DateTime now)
        {
            var nbMinutesFromPreviousTick = now.Minute % nbMinutesBetweenTicks;
            var dateTimePreviousTick = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute - nbMinutesFromPreviousTick, 0);
            return dateTimePreviousTick.AddMinutes(nbMinutesBetweenTicks);
        }

        private static TimeSpan GetTimeToNextTick(int nbMinutesBetweenTicks, DateTime now)
        {
            var dateTimeNextTick = GetDateTimeNextTick(nbMinutesBetweenTicks, now);
            var result = dateTimeNextTick - now;

            if (result > TimeSpan.Zero)
                return result;
            else
                return TimeSpan.Zero;
        }

        private void TestCedricQuarterCode()
        {
            Func<DateTime, int> getNbMinuteToQuarterByCedricP = now =>
                {
                    var nowMinute = now.Minute;

                    var quarter = nowMinute < 0 ? 0 : (nowMinute < 15 ? 15 : (nowMinute < 30 ? 30 : 45));

                    return (now.Minute < 45) ? quarter - now.Minute : 60 - now.Minute;
                };

            bool hasError = false;
            var baseDate = DateTime.Now;
            for (int i = 0; i < 3600; i++)
            {
                var testDate = baseDate.AddSeconds(i);
                var nbMinuteToQuarter = getNbMinuteToQuarterByCedricP(testDate);
                var calculatedQuarterDateTime = testDate.AddMinutes(nbMinuteToQuarter);
                hasError = calculatedQuarterDateTime.Minute % 15 != 0
                           || calculatedQuarterDateTime < testDate
                           || (calculatedQuarterDateTime - testDate) > TimeSpan.FromMinutes(15);
                if (hasError)
                {
                    MessageBox.Show(this, string.Format("Cedric Error on {0} : {1}", testDate.Minute, calculatedQuarterDateTime.Minute));
                    break;
                }
            }
            if (!hasError)
                MessageBox.Show(this, "Cedric Success !!");
        }

        private void TestMatthieuQuarterCode()
        {
            Func<int, DateTime, DateTime> GetDateTimeNextTick = (nbMinutesBetweenTicks, now) =>
                {
                    var nbMinutesFromPreviousTick = now.Minute % nbMinutesBetweenTicks;
                    var dateTimePreviousTick = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute - nbMinutesFromPreviousTick, 0);
                    return dateTimePreviousTick.AddMinutes(nbMinutesBetweenTicks);
                };

            bool hasError = false;
            var baseDate = DateTime.Now;
            for (int i = 0; i < 3600; i++)
            {
                var testDate = baseDate.AddSeconds(i);
                var calculatedQuarterDateTime = GetDateTimeNextTick(15, testDate);
                hasError = calculatedQuarterDateTime.Minute % 15 != 0
                           || calculatedQuarterDateTime < testDate
                           || (calculatedQuarterDateTime - testDate) > TimeSpan.FromMinutes(15);
                if (hasError)
                {
                    MessageBox.Show(this, string.Format("Matthieu Error on {0} : {1}", testDate.Minute, calculatedQuarterDateTime.Minute));
                    break;
                }
            }
            if (!hasError)
                MessageBox.Show(this, "Matthieu Success !!");
        }

        private void TestCommentsInLinqToXml()
        {
            var root = new XElement("Root",
                           new XElement("Father_1",
                               new XElement("Son_1.1"),
                               new XElement("Son_1.2"),
                               new XElement("Son_1.3")),
                           new XElement("Father_2",
                               new XElement("Son_2.1"),
                               new XElement("Son_2.2"),
                               new XElement("Son_2.3")));

            var father1 = root.Element("Father_1");
            father1.AddAfterSelf(new XComment("This is my comment"));

            var firstElementAfter = father1.ElementsAfterSelf().First();
            var firstNodeAfter = father1.NodesAfterSelf().First();
            if (firstNodeAfter is XComment)
                firstNodeAfter.Remove();
            MessageBox.Show(this, firstElementAfter + "\n\n" + firstNodeAfter + "\n\n" + root);
        }

        private void TestFlorianFormating()
        {
            var propertyName = "PercentTimeInRange";
            var numIndex = 0;
            var indexPropertyName = string.Format("{0}_{1:11}", propertyName, numIndex);
            MessageBox.Show(this, indexPropertyName);
        }

        private void TestMathAbs()
        {
            // a quick test to know if Math.Abs can return a negative value as in Java (cf : http://henrikwarne.com/2014/01/27/a-bug-a-trace-a-test-a-twist/ )
            // .net throw an OverflowException when passed int.MinValue
            // as always .net>java
            int min = int.MinValue;
            unchecked
            {
                var result = Math.Abs(min);
                MessageBox.Show(this, string.Format("Math.Abs({0}) == {1}", min, result));
            }
        }

        private void TestGeneratedRoute()
        {
            var xsd = new FileInfo(@"C:\Users\mmouriss\Desktop\génération doc route.xml\routeV2.xsd");
            var xml = new FileInfo(@"C:\Users\mmouriss\Desktop\route.xml");

            if (ValidateXml(xsd, xml))
                MessageBox.Show(this, "Success !!");
            else
                MessageBox.Show(this, "FAILURE !!!!!!!!");
        }

        private void SubstringArena()
        {
            var test = "1234567890";
            //var result = test.Substring(5, 10); CRASH
            //var result = test.Remove(19); CRASH too
            var result = test.PadRight(5); //doesn't crash but doesn't do what I want either
            MessageBox.Show(this, result + "\n" + result.Length);
        }

        private void CastAndLinq()
        {
            var emptyArray = new decimal?[0];
            var intDecimals = new decimal?[] { 1m, 2m, 3m, 4m };
            var easyDecimals = new decimal?[] { 1m, 1.1m, 3500m, 0.7m, 0.1m };
            var easyDecimalsWithNulls = new decimal?[] { 1m, 1.1m, 3500m, 0.7m, 0.1m };
            var hardDecimals = new decimal?[] { 1m, 1.000000000001m, 10000000000m, 0.0000000000007m };

            int?[] result;

            result = TestCastMethods(emptyArray);
            result = TestCastMethods(intDecimals);
            result = TestCastMethods(easyDecimals);
            result = TestCastMethods(easyDecimalsWithNulls);
            result = TestCastMethods(hardDecimals);
        }

        private int?[] TestCastMethods(decimal?[] decimals)
        {
            int?[] result;

            var tempList = new List<int?>();
            foreach (var dec in decimals)
            {
                if (dec.HasValue)
                    tempList.Add((int)dec);
                else
                    tempList.Add(null);
            }

            result = tempList.ToArray();
            result = decimals.Select(d => (int?)d).ToArray();
            result = decimals.Cast<int?>().ToArray();

            return result;
        }


        private IEnumerable<string> AllExistingDatePaterns()
        {
            return CultureInfo.GetCultures(CultureTypes.AllCultures).Select(c => c.DateTimeFormat.ShortDatePattern).Distinct();
        }

        private void TestFloatingPoint()
        {
            double fp1 = 0.1;
            fp1 = fp1 * 0.1;
            fp1 = fp1 / 0.1;
            double fp2 = 0.8;
            fp2 = fp2 * 0.1;
            fp2 = fp2 / 0.1;
            double fp3 = fp1 + fp2;
            fp3 = fp3 - 0.9;
            MessageBox.Show(this, fp3.ToString());
        }

        private void CultureTest()
        {
            var currentCulture = CultureInfo.CurrentCulture;
            var currentUICulture = CultureInfo.CurrentUICulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(1033);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(1033);
            var truc = new ManagementObject("");
        }

        private void TestTimeZones()
        {
            var result = string.Join("\n", TimeZoneInfo.GetSystemTimeZones().Select(tz => tz.StandardName.Replace(" ", "") + " (GMT " + tz.BaseUtcOffset.TotalHours.ToString() + ") " + tz.DisplayName));
            MessageBox.Show(this, result);
        }

        private void TestRegistry()
        {
            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Updates\UpdateExeVolatile
            //HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\PendingFileRenameOperations

            var UpdateExeVolatileKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Updates\UpdateExeVolatile");
        }

        private void TestBoyerMoore()
        {
            string text = "Stupid Spring String";
            string searchTerm = "String";

            var singleResult = text.BoyerMooreSearch(searchTerm).ToList();

            text = "La raison est que l’algorithme utilise le tableau après avoir trouvé un caractère qui ne correspond pas. Le tableau lui indique le nombre de positions vers l’avant que l'algorithme doit sauter avant que ce caractère puisse théoriquement correspondre dans le texte. Par exemple, si en vérifiant la neuvième position du texte, l’algorithme trouve un I plutôt qu’un A, cela indiquerait que la prochaine correspondance potentielle pourrait être trouvée une position plus loin vers l’avant, et que la dixième position doit être vérifiée pour y chercher un A. S’il s'agit d’un A, soit l’algorithme le trouve dans la dernière position, et dans ce cas, la vérification est un succès, soit la dernière position a déjà été vérifiée ; dans ce second cas, il n'existe aucun endroit dans le reste de la sous-chaîne clé où le A peut correspondre. De ce fait, aucune correspondance n’est possible jusqu'à ce que l’algorithme cherche complètement au-delà de la position du A.";
            searchTerm = "algorithme";

            var multipleResults = text.BoyerMooreSearch(searchTerm).ToArray();
            //MessageBox.Show(this, string.Join(", ", multipleResults));

            searchTerm = "anpanman";
            var patternJumpMap = string.Join(", ", BoyerMoore.GeneratePatternJumpMap(searchTerm));
            var expectedJumpMap = "1, 8, 3, 6, 6, 6, 6, 6";
            MessageBox.Show(this, patternJumpMap + "\n" + expectedJumpMap);
        }

        private static string FormatByteArray(byte[] byteArray)
        {
            var baseString = Convert.ToBase64String(byteArray).Replace("=", "").Replace('+', 'A').Replace('/', '0').ToUpper();

            const int chunkSize = 4;
            var builder = new StringBuilder();
            for (var i = 0; i < baseString.Length - chunkSize; i += chunkSize)
            {
                builder.Append(baseString, i, chunkSize);
                builder.Append('-');
            }
            builder.Remove(builder.Length - 1, 1);

            return builder.ToString();
        }

        private void PrepareFrameForUnitTest()
        {
            var x59CombinedHexFrame = "1388855312C01506B6141303000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000142012000033013502FFFFFF2000";
            var x59HeatHexFrame = "1286855312C00D0FB61422010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000144312000034023202FFFFFF2000";
            var intelisHexFrame = "0B160B0002B614C8465F850100560055005600560056005500540056005600550056005800560056005600550056005600560056005500560056005600F4010000";
            Debug.Print("X59 combined : " + HexStringToCSharpDeclaration(x59CombinedHexFrame));
            Debug.Print("X59 heat : " + HexStringToCSharpDeclaration(x59HeatHexFrame));
            Debug.Print("Intelis : " + HexStringToCSharpDeclaration(intelisHexFrame));
        }

        private string HexStringToCSharpDeclaration(string hexString)
        {
            return ParseHexString(hexString).ToCSharpDeclaration();
        }

        private byte[] ParseHexString(string hexData)
        {
            var chunkedString = new string[hexData.Length / 2];
            for (var i = 0; i < chunkedString.Length; i++)
            {
                chunkedString[i] = hexData[i * 2].ToString() + hexData[i * 2 + 1].ToString();
            }

            return chunkedString.Select(s => Convert.ToByte(s, 16)).ToArray();
        }

        private void DoEncryptPulseMachin()
        {
            var machin = EncryptPulseMachin.GetInstance();
            var sourceFile = new FileInfo(@"C:\Users\mmouriss\Desktop\ItronPulseValues.xml");
            var destFile = new FileInfo(@"C:\Users\mmouriss\Desktop\ItronPulseValues.xmlenc");

            using (var stramReader = sourceFile.OpenRead())
            using (var encryptedStram = machin.Encrypt(stramReader))
            using (var stramWriter = destFile.Create())
                encryptedStram.CopyTo(stramWriter);
        }

        private void LinqForPatriq()
        {
            var oneHourData = from tupleData in GenerateRandomData(TimeSpan.FromSeconds(1), 20).Take(3600)
                              select new { Time = tupleData.Item1, Value = tupleData.Item2 };

            var trameLuesParMinute = from data in oneHourData
                                     group data.Value by data.Time.ToString("hh':'mm") into myGroup
                                     orderby myGroup.Key
                                     select new { Heure = myGroup.Key, TrameLues = myGroup.Sum() };

            MessageBox.Show(this, string.Join("\n", trameLuesParMinute));
        }

        private IEnumerable<Tuple<TimeSpan, int>> GenerateRandomData(TimeSpan interval, int max)
        {
            var rnd = new Random();
            var time = TimeSpan.Zero;
            while (true)
            {
                yield return Tuple.Create(time, rnd.Next(max));
                time += interval;
            }
        }

        private void DictionnaryExperiment()
        {
            var mydic = new Dictionary<string, string>();
            mydic["Blue Öyster Cult"] = "Don't fear the Reaper";
            mydic["Metallica"] = "Enter sandman";
            MessageBox.Show(this, string.Join(", ", mydic));
        }

        //MMO : Code found at this url : http://stackoverflow.com/questions/1193955/how-to-query-an-ntp-server-using-c
        public static DateTime? GetNetworkTime(TimeSpan timeout)
        {
            //default Windows time server
            const string ntpServer = "pool.ntp.org";

            try
            {
                // NTP message size - 16 bytes of the digest (RFC 2030)
                var ntpData = new byte[48];

                //Setting the Leap Indicator, Version Number and Mode values
                ntpData[0] = 0x1B; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

                var addresses = Dns.GetHostEntry(ntpServer).AddressList;

                //The UDP port number assigned to NTP is 123
                var ipEndPoint = new IPEndPoint(addresses[0], 123);
                //NTP uses UDP
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                                        {
                                            SendTimeout = (int)timeout.TotalMilliseconds,
                                            ReceiveTimeout = (int)timeout.TotalMilliseconds,
                                        };

                socket.Connect(ipEndPoint);

                socket.Send(ntpData);
                socket.Receive(ntpData);
                socket.Close();

                //Offset to get to the "Transmit Timestamp" field (time at which the reply 
                //departed the server for the client, in 64-bit timestamp format."
                const byte serverReplyTime = 40;

                //Get the seconds part
                ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

                //Get the seconds fraction
                ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

                // stackoverflow.com/a/3294698/162671
                Func<ulong, uint> swapEndianness = x => (uint)(((x & 0x000000ff) << 24) +
                                                               ((x & 0x0000ff00) << 8) +
                                                               ((x & 0x00ff0000) >> 8) +
                                                               ((x & 0xff000000) >> 24));
                //Convert From big-endian to little-endian
                intPart = swapEndianness(intPart);
                fractPart = swapEndianness(fractPart);

                var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

                //**UTC** time
                var networkDateTime = (new DateTime(1900, 1, 1)).AddMilliseconds((long)milliseconds);

                return networkDateTime;
            }
            catch
            {
                return null;
            }
        }

        private void AnalyzeBigBranches(FileInfo architectureFile)
        {
            using (var streamReader = architectureFile.OpenText())
            {
                var architectureDoc = XDocument.Load(streamReader);
                var branches = from ap in architectureDoc.Root.Elements()
                               from headCollector in ap.Elements("Collector")
                               let miuCount = headCollector.Descendants("Miu").Count()
                               where miuCount > 250
                               from childCollector in headCollector.Elements("Collector")
                               select new
                               {
                                   ApSerialNumber = ap.Attribute("SerialNumber").Value,
                                   HeadCollectorSerialNumber = headCollector.Attribute("SerialNumber").Value,
                                   BranchMiuCount = miuCount,
                                   ChildCollector = childCollector.Attribute("SerialNumber").Value,
                                   MiuCount = childCollector.Descendants("Miu").Count(),
                               };


                var csvHeaderBuffer = new[] { "Access point serial number", "Head of branch collector", "Mius on branch", "Child collector", "Mius on sub branch" };
                var csvDataBuffer = branches.Select(m => new[] { " " + m.ApSerialNumber, " " + m.HeadCollectorSerialNumber, m.BranchMiuCount.ToString(), m.ChildCollector, m.MiuCount.ToString() }).ToList();

                var fileDialog = new SaveFileDialog { Filter = "( *.csv) | *.csv" };
                if (fileDialog.ShowDialog(this) ?? false)
                {
                    CSVHelper.WriteToFile(new FileInfo(fileDialog.FileName), csvHeaderBuffer, csvDataBuffer);
                }
            }
        }

        private void AnalyzeMiusByCollector(FileInfo architectureFile)
        {
            using (var streamReader = architectureFile.OpenText())
            {
                var architectureDoc = XDocument.Load(streamReader);
                var branches = from ap in architectureDoc.Root.Elements()
                               from collector in ap.Descendants("Collector")
                               select new
                               {
                                   ApSerialNumber = ap.Attribute("SerialNumber").Value,
                                   CollectorSerialNumber = collector.Attribute("SerialNumber").Value,
                                   MiuCount = collector.Elements("Miu").Count()
                               };


                var csvHeaderBuffer = new[] { "Access point serial number", "Collector", "Mius on collector" };
                var csvDataBuffer = branches.Select(m => new[] { " " + m.ApSerialNumber, " " + m.CollectorSerialNumber, m.MiuCount.ToString() }).ToList();

                var fileDialog = new SaveFileDialog { Filter = "( *.csv) | *.csv" };
                if (fileDialog.ShowDialog(this) ?? false)
                {
                    CSVHelper.WriteToFile(new FileInfo(fileDialog.FileName), csvHeaderBuffer, csvDataBuffer);
                }

            }
        }

        private void AnalyzeCollectorChilds(FileInfo architectureFile)
        {
            using (var streamReader = architectureFile.OpenText())
            {
                var architectureDoc = XDocument.Load(streamReader);
                var branches = from ap in architectureDoc.Root.Elements()
                               from collector in ap.Descendants("Collector")
                               select new
                               {
                                   ApSerialNumber = ap.Attribute("SerialNumber").Value,
                                   CollectorSerialNumber = collector.Attribute("SerialNumber").Value,
                                   CollectorChildsCount = collector.Elements("Collector").Count()
                               };

                branches = branches.Union(architectureDoc.Root.Elements().Select(ap => new
                               {
                                   ApSerialNumber = ap.Attribute("SerialNumber").Value,
                                   CollectorSerialNumber = "",
                                   CollectorChildsCount = ap.Elements("Collector").Count()
                               }));

                var csvHeaderBuffer = new[] { "Access point serial number", "Collector Serial Number", "Child collectors" };
                var csvDataBuffer = branches.Select(m => new[] { " " + m.ApSerialNumber, " " + m.CollectorSerialNumber, m.CollectorChildsCount.ToString() }).ToList();

                var fileDialog = new SaveFileDialog { Filter = "( *.csv) | *.csv" };
                if (fileDialog.ShowDialog(this) ?? false)
                {
                    CSVHelper.WriteToFile(new FileInfo(fileDialog.FileName), csvHeaderBuffer, csvDataBuffer);
                }

            }
        }

        private void AnalyzeMiusByBranch(FileInfo architectureFile)
        {
            using (var streamReader = architectureFile.OpenText())
            {
                var architectureDoc = XDocument.Load(streamReader);
                var branches = from ap in architectureDoc.Root.Elements()
                               from headCollector in ap.Elements("Collector")
                               select new
                               {
                                   ApSerialNumber = ap.Attribute("SerialNumber").Value,
                                   HeadCollectorSerialNumber = headCollector.Attribute("SerialNumber").Value,
                                   MiuCount = headCollector.Descendants("Miu").Count()
                               };


                var csvHeaderBuffer = new[] { "Access point serial number", "Head of branch collector", "Mius on branch" };
                var csvDataBuffer = branches.Select(m => new[] { " " + m.ApSerialNumber, " " + m.HeadCollectorSerialNumber, m.MiuCount.ToString() }).ToList();

                var fileDialog = new SaveFileDialog { Filter = "( *.csv) | *.csv" };
                if (fileDialog.ShowDialog(this) ?? false)
                {
                    CSVHelper.WriteToFile(new FileInfo(fileDialog.FileName), csvHeaderBuffer, csvDataBuffer);
                }

            }
        }

        private void ScanFilesFromFtp()
        {
            var dir = new DirectoryInfo(@"C:\Users\mmouriss\Desktop\ftp");
            var files = dir.GetFiles();

            long dummyLong;
            Func<FileInfo, string> getPrefix = f => string.Join("_", f.Name.Remove(f.Name.Length - 4).Split(new[] { "_" }, StringSplitOptions.RemoveEmptyEntries).TakeWhile(sub => !long.TryParse(sub, out dummyLong)));

            var prefixes = files.Select(getPrefix).Distinct();

            var filesByTime = from file in files
                              group file by new { file.LastWriteTime.Hour, file.LastWriteTime.Minute } into grouped
                              orderby grouped.Key.Hour, grouped.Key.Minute
                              select string.Format("{0:00}:{1:00} - {2:000} ({3:0000}Ko) ", grouped.Key.Hour, grouped.Key.Minute, grouped.Count(), grouped.Sum(f => f.Length) / 1000);

            MessageBox.Show(this, string.Join("\n", filesByTime));
        }

        private void BenchPi()
        {
            var chrono = Stopwatch.StartNew();
            var pi = CalcPi(5000000);
            chrono.Stop();
            var calcPi = chrono.Elapsed;

            chrono = Stopwatch.StartNew();
            pi = CalcPiParallel(5000000);
            chrono.Stop();
            var calcPiParallel = chrono.Elapsed;



            MessageBox.Show(this, string.Format("CalcPi : {0}\nCalcPiParrallel : {1}", calcPi, calcPiParallel));
        }

        private decimal CalcPi(decimal itérations)
        {
            decimal acc = 0;
            for (decimal i = 0; i < itérations; i++)
            {
                var signe = (i % 2 == 0) ? 1 : -1;
                acc += signe / (2 * i + 1);
            }
            return 4 * acc;
        }

        private decimal CalcPiParallel(decimal itérations)
        {
            if (itérations < 2)
            {
                return CalcPi(itérations);
            }
            else
            {
                Func<decimal, decimal, decimal> accGenerator = (start, inc) =>
                    {
                        decimal signe = (start % 2 == 0) ? 1 : -1;
                        decimal acc = 0;
                        for (decimal i = start; i < itérations; i += inc)
                        {
                            acc += signe / (2 * i + 1);
                        }
                        return acc;
                    };
                return 4 * (from seed in Enumerable.Range(0, 4).AsParallel()
                            select accGenerator(seed, 4)).Sum();
            }
        }

        private string Base64ToHexadeciaml(string base64String)
        {
            return Convert.FromBase64String(base64String).ToRawhexString();
        }

        private string Base64ToCSharpDeclaration(string base64String)
        {
            return Convert.FromBase64String(base64String).ToCSharpDeclaration();
        }

        private void parseXmlAnne()
        {
            const string xml =
@"<?xml version='1.0'?>
<Response>
    <ProcessServlet>
        <Error>xml validation error 1</Error>
        <Message>ProfileType must be empty or 3 cars long</Message>
        <Error>xml validation error 2</Error>
        <Message>CreationDateTime is mandatory</Message>
        <Error>xml validation error 3</Error>
        <Message>LastModifyDateTime is mandatory</Message>
        <Error>xml validation error 4</Error>
        <Message>Rail SeatDirection should be (Yes or No)</Message>
    </ProcessServlet>
</Response>";
            //J'aimerais pouvoir récupérer une liste de couples {error, message}...mais je n'y arrive pas. Est-ce que c'est faisable en linq ?

            var doc = XDocument.Parse(xml);

            var processServletElement = doc.Root.Element("ProcessServlet");

            var errors = processServletElement.Elements("Error");
            var messages = processServletElement.Elements("Message");

            var couples = errors.Zip(messages, (error, message) => new { Error = error.Value, Message = message.Value });

            MessageBox.Show(string.Join("\n", couples.Select(couple => couple.Error + " : " + couple.Message)));
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
            ValidateXml(new FileInfo(@"C:\Users\mmouriss\Desktop\TestXSD\xsd2.xsd"), new FileInfo(@"C:\Users\mmouriss\Desktop\shiporder.xml"));
        }

        private bool ValidateXml(FileInfo xsdFile, FileInfo xmlFile)
        {
            var validationEventHandler = new ValidationEventHandler((sender, e) =>
                {
                    var reader = sender as XmlReader;

                    Console.WriteLine("NodeType : {0}\nInfo : {1}\nException : {2}", reader.NodeType, reader.SchemaInfo, e.Exception.ToString());
                });

            XmlSchema schema;
            using (var stream = xsdFile.OpenRead())
                schema = XmlSchema.Read(stream, validationEventHandler);

            var readerSettings = new XmlReaderSettings { ValidationType = ValidationType.Schema };
            readerSettings.ValidationEventHandler += validationEventHandler;
            readerSettings.Schemas.Add(schema);

            bool continueRead = true;

            using (var fileStream = xmlFile.OpenRead())
            using (var xmlReader = XmlReader.Create(fileStream, readerSettings))
                while (continueRead)
                    continueRead = xmlReader.Read();

            return true;
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
