using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
namespace HappyHourParser
{
    class HappyHour
    {
        public delegate void HappyHourHandler(object sender, List<Product> newProducts);
        public event HappyHourHandler Notify;
        private bool working;
        private Thread threadParser;
        private List<Product> oldProducts;
        public HappyHour()
        {
            oldProducts = new List<Product>();
            working = false;
        }
        private List<Product> Parser()
        {
            var config = Configuration.Default.WithDefaultLoader();
            var c = Configuration.Default;
            var data = BrowsingContext.New(config).OpenAsync("https://zaka-zaka.com/happyhour/").GetAwaiter().GetResult();
            var Tables = data.QuerySelectorAll("div").Where(item => item.ClassName != null && item.ClassName.Contains("happy-hour-table-row")).ToList();
            List<Product> products = new List<Product>();
            for (int i = 0; i < Tables.Count; i++)
            {
                var product = new Product();
                product.Name = GetValue(Tables, i, "game-block-name");
                product.Discount = GetValue(Tables, i, "game-block-discount");
                product.DiscountSum = GetValue(Tables, i, "game-block-discount-sum").Replace(" ", "").Replace("c", "").Replace("\n", "");
                product.Price = GetValue(Tables, i, "game-block-price").Replace("c", "").Replace("\n", "").Replace("  ", " ").Split(" ")[1];
                products.Add(product);
            }
            return products;
        }
        private void Notifier()
        {
            while (working)
            {
                try
                {
                    var receivedProducts = Parser();
                    if (Checker(receivedProducts))
                        Notify?.Invoke(new object(), receivedProducts);
                    Thread.Sleep(1200000);
                }
                catch
                {
                    Thread.Sleep(10000);
                }
            }
        }
        private bool Checker(List<Product> products)
        {
            if (products.Count != oldProducts.Count)
            {
                oldProducts = products;
                return true;
            }
            for (int i = 0; i < products.Count; i++)
            {
                if (products[i].Name != oldProducts[i].Name)
                {
                    oldProducts = products;
                    return true;
                } 
            }
            return false;
        }
        private string GetValue(List<IElement> table, int position, string classname)
        {
            return new HtmlParser().
                Parse(table.ToList()[position].OuterHtml).
                QuerySelectorAll("div").
                Where(item => item.ClassName != null && 
                item.ClassName.Contains(classname)).
                ToList()[0].TextContent;
        }
        public void StartMonitoring()
        {
            working = true;
            threadParser = new Thread(Notifier);
            threadParser.Start();
        }
        public void StopMonitoring()
        {
            working = false;
            threadParser.Abort();
        }
    }
}
