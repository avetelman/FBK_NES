using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NewFbk
{
    class Program
    {
        private static readonly string Folder = new Settings1().Folder;
        private static readonly string Output = new Settings1().Output;

        private static readonly string UrlFromFile =
            "http://zakupki.gov.ru/pgz/public/action/orders/info/common_info/show?source=epz&notificationId=";

        private static readonly string Url1 =
                "http://zakupki.gov.ru/pgz/public/action/orders/info/commission_work_result/show?source=epz&notificationId="
            ;

        private static readonly string Url2 =
                "http://zakupki.gov.ru/pgz/public/action/protocol/info/orders/show?source=epz&protocol_id="
            ;

        static void Main(string[] args)
        {
            var files = Directory.GetFiles(Folder);
            string line;

            foreach (var file in files)
            {
                Console.WriteLine(file);
                using (var f = new StreamReader(file))
                {
                    try
                    {
                        var str = File.ReadAllLines(Output);
                        while ((line = f.ReadLine()) != null)
                        {
                            if (str.Last().Contains(line)) break;
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    while ((line = f.ReadLine()) != null)
                    {
                        line = line.Replace('|', ';');
                        var t = line.Split(';');
                        if (t[4].Contains("Запрос котировок"))
                        {
                            //if (regex.IsMatch(line))
                            if (!Write(line, file))
                            {
                                Console.WriteLine("У НАС ОБЕД!!!!");
                                Console.ReadKey();
                                return;
                            }
                        }
                    }
                }
            }
        }

        static bool Write(string line, string file)
        {
            string pattern = @".*[а-яА-Я]+[a-zA-Z]+[а-яА-Я]+.*";
            var regex = new Regex(pattern);

            var number = line.Split(';')[9].Replace(UrlFromFile, "");
            var url = Url1 + number;
            // var client = new HttpClient(new SocksPortHandler("127.0.0.1", socksPort: 9050));
            var client = new HttpClient();
            var headers = client.DefaultRequestHeaders;
            headers.Accept.ParseAdd(
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            headers.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.94 Safari/537.36");
            var protokolUrl = "/pgz/public/action/protocol/info/details/show?protocol_id=";

            /*   var controlPortClient = new Client("127.0.0.1", controlPort: 9051, password: "ILoveBitcoin21");
                   controlPortClient.ChangeCircuitAsync().Wait();
                   */
            var response = client.GetAsync(url).GetAwaiter().GetResult();

            if (response.StatusCode != HttpStatusCode.OK)
                return false;

            var winner = string.Empty;
            var winnerPrice = string.Empty;
            var winnerTime = string.Empty;
            var second = string.Empty;
            var secondPrice = string.Empty;
            var secondTime = string.Empty;
            string result;


            using (HttpContent content = response.Content)
            {
                result = content.ReadAsStringAsync().GetAwaiter().GetResult();
            }

            var startDocument = result.LastIndexOf(protokolUrl, StringComparison.OrdinalIgnoreCase);
            var document = startDocument > 0
                ? result.Substring(startDocument + protokolUrl.Length,
                    result.IndexOf("&source=epz", startDocument, StringComparison.OrdinalIgnoreCase) -
                    startDocument - protokolUrl.Length)
                : null;

            if (document != null)
            {
                var url2 = Url2 + document;

                var response2 = client.GetAsync(url2).GetAwaiter().GetResult();

                if (response2.StatusCode != HttpStatusCode.OK)
                    return false;

                using (HttpContent content = response2.Content)
                {
                    // ... Read the string.


                    result = content.ReadAsStringAsync().GetAwaiter().GetResult();

                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(result);
                    var tables = doc.DocumentNode.SelectNodes("//table")
                        .Where(n => n.Attributes.Contains("class"))
                        .Where(n => n.Attributes["class"].Value == "width100p iceDatTbl");
                    ;
                    foreach (HtmlNode table in tables)
                    {
                        var list = new List<string[]>();
                        Console.WriteLine("Found: " + table.Id);
                        foreach (HtmlNode row in table.SelectNodes("tbody").First().SelectNodes("tr"))
                        {
                            Console.WriteLine("row");
                            foreach (HtmlNode cell in row.SelectNodes("th|td"))
                            {
                                Console.WriteLine("cell: " + cell.InnerText);
                            }

                            list.Add(row.SelectNodes("th|td").Select(s => s.InnerText).ToArray());
                        }

                        winner = list.FirstOrDefault(s => s.Contains("Победитель"))?.ToArray()[1]
                            .Replace(";", ",");
                        if (winner != null && winner.Contains("\n"))
                            winner = winner.Replace("\n", "").Substring(0, winner.Length / 2 - 1);

                        winnerPrice = list.FirstOrDefault(s => s.Contains("Победитель"))?.ToArray()[3]
                            .Replace(";", ",").Replace(" ", "");
                        if (winnerPrice?.Length > 0)
                            winnerPrice = winnerPrice?.Substring(0,
                                winnerPrice.IndexOf(".", StringComparison.Ordinal));

                        winnerTime = list.FirstOrDefault(s => s.Contains("Победитель"))?.ToArray()[2]
                            .Replace(";", ",").Substring(13);


                        second = list.FirstOrDefault(s =>
                                s.Contains("Лучшее предложение о цене контракта после победителя"))
                            ?.ToArray()[1].Replace(";", ",");
                        if (second != null && second.Contains("\n"))
                            second = second.Replace("\n", "").Substring(0, second.Length / 2 - 1);
                        secondPrice = list.FirstOrDefault(s =>
                                s.Contains("Лучшее предложение о цене контракта после победителя"))
                            ?.ToArray()[3].Replace(";", ",");
                        if (secondPrice?.Length > 0)
                            secondPrice = secondPrice?.Substring(0,
                                secondPrice.IndexOf(".", StringComparison.Ordinal));
                        secondTime = list.FirstOrDefault(s =>
                                s.Contains("Лучшее предложение о цене контракта после победителя"))
                            ?.ToArray()[2]
                            .Replace(";", ",").Substring(13);

                    }
                }
            }

            var isMatch = regex.IsMatch(line) ? 1 : 0;

            line = $"{line};{winner};{winnerPrice};{winnerTime};{second};{secondPrice};{secondTime};{isMatch};{file}\n";

            File.AppendAllText(Output, line);
            Console.WriteLine(line);
            //System.Console.WriteLine(line);
            return true;
        }
    }
}